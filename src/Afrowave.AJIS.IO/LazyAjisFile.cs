#nullable enable

using Afrowave.AJIS.Serialization.Mapping;
using System.Collections.Concurrent;

namespace Afrowave.AJIS.IO;

/// <summary>
/// Lazy-loaded AJIS file with background updates.
/// </summary>
public class LazyAjisFile<T> : IDisposable where T : class
{
    private readonly string _filePath;
    private readonly AjisConverter<List<T>> _converter;
    private readonly ConcurrentQueue<PendingOperation> _pendingOperations = new();
    private readonly SemaphoreSlim _operationSemaphore = new(1, 1);
    private List<T>? _cachedData;
    private bool _isDirty;
    private Task? _backgroundTask;
    private CancellationTokenSource? _cts;

    public LazyAjisFile(string filePath)
    {
        _filePath = filePath;
        _converter = new AjisConverter<List<T>>();
        _cts = new CancellationTokenSource();
        _backgroundTask = Task.Run(() => ProcessOperationsAsync(_cts.Token));
    }

    /// <summary>
    /// Gets all items (lazy loaded).
    /// </summary>
    public async Task<List<T>> GetAllAsync()
    {
        await EnsureLoadedAsync();
        return _cachedData ?? new List<T>();
    }

    /// <summary>
    /// Gets an item by predicate (lazy loaded).
    /// </summary>
    public async Task<T?> GetAsync(Func<T, bool> predicate)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(predicate);
    }

    /// <summary>
    /// Adds an item (lazy update).
    /// </summary>
    public void Add(T item)
    {
        EnsureLoadedAsync().GetAwaiter().GetResult();
        _pendingOperations.Enqueue(new PendingOperation(OperationType.Add, item));
        _isDirty = true;
    }

    /// <summary>
    /// Updates an item (lazy update).
    /// </summary>
    public void Update(T item, Func<T, bool> predicate)
    {
        EnsureLoadedAsync().GetAwaiter().GetResult();
        _pendingOperations.Enqueue(new PendingOperation(OperationType.Update, item, predicate));
        _isDirty = true;
    }

    /// <summary>
    /// Deletes items matching predicate (lazy update).
    /// </summary>
    public void Delete(Func<T, bool> predicate)
    {
        EnsureLoadedAsync().GetAwaiter().GetResult();
        _pendingOperations.Enqueue(new PendingOperation(OperationType.Delete, predicate: predicate));
        _isDirty = true;
    }

    /// <summary>
    /// Forces immediate save of all pending operations.
    /// </summary>
    public async Task FlushAsync()
    {
        await _operationSemaphore.WaitAsync();
        try
        {
            await ProcessPendingOperationsAsync();
            _isDirty = false;
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets the count of items without loading all data.
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        await EnsureLoadedAsync();
        return _cachedData?.Count ?? 0;
    }

    private async Task EnsureLoadedAsync()
    {
        if(_cachedData != null) return;

        await _operationSemaphore.WaitAsync();
        try
        {
            if(!File.Exists(_filePath))
            {
                _cachedData = new List<T>();
                return;
            }

            var json = await File.ReadAllTextAsync(_filePath);
            _cachedData = _converter.Deserialize(json) ?? new List<T>();
        }
        finally
        {
            _operationSemaphore.Release();
        }
    }

    private async Task ProcessOperationsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(1000, cancellationToken); // Process every second

                if (_isDirty)
                {
                    await _operationSemaphore.WaitAsync(cancellationToken);
                    try
                    {
                        await ProcessPendingOperationsAsync();
                        _isDirty = false;
                    }
                    finally
                    {
                        _operationSemaphore.Release();
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Log error but continue
                Console.Error.WriteLine($"Background operation failed: {ex.Message}");
            }
        }
    }

    private async Task ProcessPendingOperationsAsync()
    {
        if(_cachedData == null) return;

        while(_pendingOperations.TryDequeue(out var operation))
        {
            switch(operation.Type)
            {
                case OperationType.Add:
                    _cachedData.Add(operation.Item!);
                    break;

                case OperationType.Update:
                    var itemToUpdate = _cachedData.FirstOrDefault(operation.Predicate!);
                    if(itemToUpdate != null && operation.Item != null)
                    {
                        var index = _cachedData.IndexOf(itemToUpdate);
                        _cachedData[index] = operation.Item;
                    }
                    break;

                case OperationType.Delete:
                    _cachedData.RemoveAll(new Predicate<T>(operation.Predicate!));
                    break;
            }
        }

        // Save to file
        var json = _converter.Serialize(_cachedData);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public void Dispose()
    {
        // Final flush synchronně, pokud je něco k uložení
        if (_isDirty)
        {
            FlushAsync().GetAwaiter().GetResult();
        }

        _cts?.Cancel();
        try
        {
            _backgroundTask?.Wait();
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException || e is OperationCanceledException))
        {
            // Ignoruj zrušení
        }
        _cts?.Dispose();
        _operationSemaphore.Dispose();
    }

    private enum OperationType { Add, Update, Delete }

    private class PendingOperation
    {
        public OperationType Type { get; }
        public T? Item { get; }
        public Func<T, bool>? Predicate { get; }

        public PendingOperation(OperationType type, T? item = null, Func<T, bool>? predicate = null)
        {
            Type = type;
            Item = item;
            Predicate = predicate;
        }
    }
}

/// <summary>
/// Observable AJIS file that notifies about changes.
/// </summary>
public class ObservableAjisFile<T> where T : class
{
    private readonly LazyAjisFile<T> _file;
    private readonly List<Action<T, ChangeType>> _changeHandlers = new();

    public enum ChangeType { Added, Updated, Deleted }

    public ObservableAjisFile(string filePath)
    {
        _file = new LazyAjisFile<T>(filePath);
    }

    public void Subscribe(Action<T, ChangeType> handler)
    {
        _changeHandlers.Add(handler);
    }

    public async Task<List<T>> GetAllAsync()
    {
        return await _file.GetAllAsync();
    }

    public async Task<T?> GetAsync(Func<T, bool> predicate)
    {
        return await _file.GetAsync(predicate);
    }

    public void Add(T item)
    {
        _file.Add(item);
        NotifyChangeHandlers(item, ChangeType.Added);
    }

    public void Update(T item, Func<T, bool> predicate)
    {
        _file.Update(item, predicate);
        NotifyChangeHandlers(item, ChangeType.Updated);
    }

    public void Delete(Func<T, bool> predicate)
    {
        // Get item before deletion for notification
        Task.Run(async () =>
        {
            var item = await _file.GetAsync(predicate);
            if(item != null)
            {
                _file.Delete(predicate);
                NotifyChangeHandlers(item, ChangeType.Deleted);
            }
        });
    }

    public async Task FlushAsync()
    {
        await _file.FlushAsync();
    }

    private void NotifyChangeHandlers(T item, ChangeType changeType)
    {
        foreach(var handler in _changeHandlers)
        {
            try
            {
                handler(item, changeType);
            }
            catch(Exception ex)
            {
                Console.Error.WriteLine($"Change handler failed: {ex.Message}");
            }
        }
    }
}

/// <summary>
/// Extension methods for lazy AJIS file operations.
/// </summary>
public static class LazyAjisExtensions
{
    /// <summary>
    /// Creates a lazy-loaded AJIS file.
    /// </summary>
    public static LazyAjisFile<T> AsLazy<T>(this string filePath) where T : class
    {
        return new LazyAjisFile<T>(filePath);
    }

    /// <summary>
    /// Creates an observable AJIS file.
    /// </summary>
    public static ObservableAjisFile<T> AsObservable<T>(this string filePath) where T : class
    {
        return new ObservableAjisFile<T>(filePath);
    }
}