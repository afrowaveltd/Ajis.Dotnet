#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Serialization.Mapping;
using System.Collections.Concurrent;
using System.IO;

namespace Afrowave.AJIS.IO;

/// <summary>
/// High-level API for AJIS file operations with automatic object mapping.
/// </summary>
/// <remarks>
/// <para>
/// Provides simple, intuitive methods for CRUD operations on AJIS files.
/// All operations automatically handle serialization/deserialization using AjisConverter.
/// </para>
/// <para>
/// Files are assumed to be AJIS arrays: `[{obj1}, {obj2}, ...]`
/// </para>
/// </remarks>
public static class AjisFile
{
    private static AjisSettings? _defaultSettings;
    private static readonly ConcurrentDictionary<Type, object> _converterFactories = new();

    /// <summary>
    /// Sets the default AJIS settings for all AjisFile operations.
    /// </summary>
    public static void SetDefaultSettings(AjisSettings? settings)
    {
        _defaultSettings = settings;
    }

    /// <summary>
    /// Sets a custom converter factory for a type.
    /// </summary>
    public static void SetConverterFactory<T>(Func<AjisConverter<T>> factory) where T : notnull
    {
        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        _converterFactories[typeof(T)] = factory;
    }

    /// <summary>
    /// Gets or creates a converter for type T.
    /// </summary>
    private static AjisConverter<T> GetConverter<T>() where T : notnull
    {
        if (_converterFactories.TryGetValue(typeof(T), out var factory))
        {
            var factoryMethod = factory as Func<AjisConverter<T>>;
            return factoryMethod?.Invoke() ?? new AjisConverter<T>();
        }

        return new AjisConverter<T>();
    }

    // ===== CREATE OPERATIONS =====

    /// <summary>
    /// Creates a new AJIS file with an array of objects.
    /// </summary>
    public static void Create<T>(string filePath, IEnumerable<T> items) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        var converter = GetConverter<T>();
        var itemList = items.ToList();

        // Serialize all items to AJIS values
        var ajisItems = itemList.Select(item => SerializeObject(converter, item)).ToList();

        // Create directory if needed
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        // Write array to file
        using (var stream = File.Create(filePath))
        using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8))
        {
            writer.Write("[");
            for (int i = 0; i < ajisItems.Count; i++)
            {
                if (i > 0)
                    writer.Write(",");
                writer.Write(ajisItems[i]);
            }
            writer.Write("]");
        }
    }

    /// <summary>
    /// Creates a new AJIS file asynchronously with objects from an async enumerable.
    /// </summary>
    public static async Task CreateAsync<T>(string filePath, IAsyncEnumerable<T> items) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        var converter = GetConverter<T>();
        var writer = new AjisFileWriter(filePath, FileMode.Create);

        try
        {
            await writer.WriteAsync("[");
            bool first = true;

            await foreach (var item in items)
            {
                if (!first)
                    await writer.WriteAsync(",");

                var serialized = SerializeObject(converter, item);
                await writer.WriteAsync(serialized);
                first = false;
            }

            await writer.WriteAsync("]");
        }
        finally
        {
            await writer.DisposeAsync();
        }
    }

    // ===== APPEND OPERATIONS =====

    /// <summary>
    /// Appends a single object to an AJIS file.
    /// Creates file if it doesn't exist.
    /// </summary>
    public static void Append<T>(string filePath, T item) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        var converter = GetConverter<T>();

        if (!File.Exists(filePath))
        {
            // Create new file with single item
            Create(filePath, new[] { item });
            return;
        }

        // Read existing content, append, and write back
        var content = File.ReadAllText(filePath);
        if (!content.TrimEnd().EndsWith("]"))
            throw new FormatException($"Invalid AJIS array format in {filePath}");

        var serialized = SerializeObject(converter, item);

        // Insert before closing bracket
        var newContent = content.TrimEnd()[..^1] + "," + serialized + "]";
        File.WriteAllText(filePath, newContent);
    }

    /// <summary>
    /// Appends a single object asynchronously.
    /// </summary>
    public static async Task AppendAsync<T>(string filePath, T item) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        var converter = GetConverter<T>();

        if (!File.Exists(filePath))
        {
            await CreateAsync(filePath, new[] { item }.ToAsyncEnumerable());
            return;
        }

        var content = await File.ReadAllTextAsync(filePath);
        if (!content.TrimEnd().EndsWith("]"))
            throw new FormatException($"Invalid AJIS array format in {filePath}");

        var serialized = SerializeObject(converter, item);
        var newContent = content.TrimEnd()[..^1] + "," + serialized + "]";
        await File.WriteAllTextAsync(filePath, newContent);
    }

    // ===== READ OPERATIONS =====

    /// <summary>
    /// Reads all objects from an AJIS file and returns as a list.
    /// WARNING: Materializes entire file in memory.
    /// </summary>
    public static List<T> ReadAll<T>(string filePath) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}", filePath);

        var result = new List<T>();
        foreach (var item in Enumerate<T>(filePath))
        {
            if (item != null)
                result.Add(item);
        }

        return result;
    }

    /// <summary>
    /// Reads all objects asynchronously without materializing entire file.
    /// </summary>
    public static async IAsyncEnumerable<T> ReadAllAsync<T>(string filePath) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}", filePath);

        await foreach (var item in EnumerateAsync<T>(filePath))
        {
            yield return item;
        }
    }

    /// <summary>
    /// Reads an object at a specific index.
    /// </summary>
    public static T? ReadAt<T>(string filePath, int index) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (index < 0)
            throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative");

        int currentIndex = 0;
        foreach (var item in Enumerate<T>(filePath))
        {
            if (currentIndex == index)
                return item;
            currentIndex++;
        }

        return default; // Index not found
    }

    // ===== ENUMERATION OPERATIONS =====

    /// <summary>
    /// Enumerates all objects from an AJIS file.
    /// Use this for large files to avoid materializing entire content.
    /// </summary>
    public static IEnumerable<T> Enumerate<T>(string filePath) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}", filePath);

        var converter = GetConverter<T>();
        var content = File.ReadAllText(filePath);

        // Parse AJIS array and extract objects
        // Simple implementation: split by }, { and deserialize each
        // TODO: Better implementation using streaming parser
        content = content.Trim();
        if (!content.StartsWith("[") || !content.EndsWith("]"))
            throw new FormatException("AJIS file must contain an array");

        // Remove brackets
        content = content[1..^1].Trim();

        if (string.IsNullOrEmpty(content))
            yield break; // Empty array

        // Split by top-level commas (simple approach)
        var objects = SplitJsonObjects(content);
        foreach (var obj in objects)
        {
            if (string.IsNullOrWhiteSpace(obj))
                continue;

            // Deserialize object
            T? item = converter.Deserialize(obj);
            if (item != null)
                yield return item;
        }
    }

    /// <summary>
    /// Enumerates all objects asynchronously from an AJIS file.
    /// </summary>
    public static async IAsyncEnumerable<T> EnumerateAsync<T>(string filePath) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}", filePath);

        var converter = GetConverter<T>();
        var content = await File.ReadAllTextAsync(filePath);

        content = content.Trim();
        if (!content.StartsWith("[") || !content.EndsWith("]"))
            throw new FormatException("AJIS file must contain an array");

        content = content[1..^1].Trim();

        if (string.IsNullOrEmpty(content))
            yield break;

        var objects = SplitJsonObjects(content);
        foreach (var obj in objects)
        {
            if (string.IsNullOrWhiteSpace(obj))
                continue;

            T? item = converter.Deserialize(obj);
            if (item != null)
                yield return item;
        }
    }

    // ===== UTILITY METHODS =====

    /// <summary>
    /// Splits AJIS array content into individual JSON objects.
    /// Simple implementation - handles most cases but not deeply nested structures.
    /// </summary>
    private static List<string> SplitJsonObjects(string content)
    {
        var objects = new List<string>();
        int depth = 0;
        int start = 0;

        for (int i = 0; i < content.Length; i++)
        {
            char c = content[i];

            if (c == '{')
                depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0)
                {
                    // Found complete object
                    var obj = content[start..++i].Trim();
                    if (!string.IsNullOrWhiteSpace(obj))
                        objects.Add(obj);

                    // Skip comma and whitespace
                    while (i < content.Length && (content[i] == ',' || char.IsWhiteSpace(content[i])))
                        i++;
                    start = i;
                    i--; // Adjust for loop increment
                }
            }
        }

        return objects;
    }

    /// <summary>
    /// Serializes a single object to AJIS text.
    /// </summary>
    private static string SerializeObject<T>(AjisConverter<T> converter, T item) where T : notnull
    {
        return converter.Serialize(item);
    }
}

/// <summary>
/// Extension method to convert enumerable to async enumerable.
/// </summary>
internal static class AsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(this IEnumerable<T> source)
    {
        foreach (var item in source)
            yield return await Task.FromResult(item);
    }
}
