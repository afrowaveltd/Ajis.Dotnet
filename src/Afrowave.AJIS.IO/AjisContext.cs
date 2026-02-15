#nullable enable

namespace Afrowave.AJIS.IO;

/// <summary>
/// Main context class for working with AJIS/ATP data sources.
/// Similar to DbContext in Entity Framework Core but designed for file-based data storage.
/// </summary>
/// <remarks>
/// <para>
/// AjisContext provides a simple, intuitive API for CRUD operations on AJIS and ATP files
/// with LINQ query support and automatic format detection.
/// </para>
/// <para>
/// Usage example:
/// <code>
/// using var context = new AjisContext();
/// 
/// // Create a set for User entities
/// var users = context.Set&lt;User&gt;("users.ajis");
/// 
/// // Add a new user
/// await users.AddAsync(new User { Id = 1, Name = "Alice" });
/// 
/// // Save changes
/// await context.SaveChangesAsync();
/// 
/// // Query with LINQ
/// var alice = await users.FindAsync(1);
/// 
/// // Remove an entity
/// await users.RemoveAsync(alice);
/// 
/// // Query with predicate
/// var adults = await users.CountAsync(u => u.Age >= 18);
/// </code>
/// </para>
/// </remarks>
public class AjisContext : IAsyncDisposable, IDisposable
{
   private readonly Dictionary<string, object> _dataSources = [];
   private readonly Dictionary<string, object> _sets = [];
   private readonly Lock _lock = new();

   /// <summary>
   /// Creates a new instance of <see cref="AjisContext"/>.
   /// </summary>
   public AjisContext()
   {
   }

   /// <summary>
   /// Gets or creates a set for the specified entity type.
   /// </summary>
   /// <typeparam name="T">The entity type.</typeparam>
   /// <param name="filePath">Path to the data file (AJIS or ATP format).</param>
   /// <param name="format">The format to use (Auto, Ajis, or Atp). Default: Auto.</param>
   /// <param name="configuration">Optional entity configuration.</param>
   /// <returns>An <see cref="AjisSet{T}"/> for working with the entities.</returns>
   public AjisSet<T> Set<T>(string filePath, AjisFormat format = AjisFormat.Auto,
       AjisEntityConfiguration<T>? configuration = null) where T : class
   {
      var setKey = $"{typeof(T).FullName}:{filePath}";

      lock(_lock)
      {
         // Check if set already exists for this filePath
         if(_sets.TryGetValue(setKey, out var set))
         {
            return (AjisSet<T>)set;
         }

         var dataSource = CreateDataSource<T>(filePath, format, configuration);
         var ajisSet = new AjisSet<T>(dataSource);
         _dataSources[filePath] = dataSource;
         _sets[setKey] = ajisSet;
         return ajisSet;
      }
   }

   /// <summary>
   /// Gets or creates a set with explicit key property name.
   /// </summary>
   /// <typeparam name="T">The entity type.</typeparam>
   /// <param name="filePath">Path to the data file.</param>
   /// <param name="keyPropertyName">Name of the primary key property.</param>
   /// <param name="format">The format to use.</param>
   /// <param name="configuration">Optional entity configuration.</param>
   /// <returns>An <see cref="AjisSet{T}"/> for working with the entities.</returns>
   public AjisSet<T> Set<T>(string filePath, string keyPropertyName,
       AjisFormat format = AjisFormat.Auto,
       AjisEntityConfiguration<T>? configuration = null) where T : class
   {
      var set = Set<T>(filePath, format, configuration);
      set.KeyPropertyName = keyPropertyName;
      return set;
   }

   /// <summary>
   /// Creates the appropriate data source based on format and file extension.
   /// </summary>
   private IAjisDataSource<T> CreateDataSource<T>(string filePath, AjisFormat format,
       AjisEntityConfiguration<T>? configuration) where T : class
   {
      var extension = Path.GetExtension(filePath).ToLower();

      // Determine actual format based on configuration and file extension
      var actualFormat = format switch
      {
         AjisFormat.Auto => DetermineAutoFormat(filePath, configuration),
         AjisFormat.Ajis => AjisFormat.Ajis,
         AjisFormat.Atp => AjisFormat.Atp,
         _ => DetermineAutoFormat(filePath, configuration)
      };

      // Create appropriate data source
      return actualFormat switch
      {
         AjisFormat.Ajis => new AjisFileDataSource<T>(filePath, configuration),
         AjisFormat.Atp => new AtpFileDataSource<T>(filePath, configuration),
         _ => new AjisFileDataSource<T>(filePath, configuration)
      };
   }

   /// <summary>
   /// Automatically determines the format based on file extension and content.
   /// </summary>
   private AjisFormat DetermineAutoFormat<T>(string filePath, AjisEntityConfiguration<T>? configuration) where T : class
   {
      var extension = Path.GetExtension(filePath).ToLower();

      // .atp files always use ATP format
      if(extension == ".atp")
         return AjisFormat.Atp;

      // Check if configuration has binary attachments
      if(configuration?.HasBinaryAttachments() == true)
         return AjisFormat.Atp;

      // .ajis or .json files use AJIS by default
      if(extension == ".ajis" || extension == ".json")
         return AjisFormat.Ajis;

      // Default to AJIS for unknown extensions
      return AjisFormat.Ajis;
   }

   /// <summary>
   /// Saves all pending changes to all data sources.
   /// </summary>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Task representing the asynchronous operation.</returns>
   public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
   {
      cancellationToken.ThrowIfCancellationRequested();

      IAjisDataSource[] dataSources;
      lock(_lock)
      {
         dataSources = [.. _dataSources.Values.OfType<IAjisDataSource>()];
      }

      foreach(var dataSource in dataSources)
      {
         await dataSource.SaveChangesAsync(cancellationToken);
      }
   }

   /// <summary>
   /// Gets a specific set by entity type and file path.
   /// </summary>
   /// <typeparam name="T">The entity type.</typeparam>
   /// <param name="filePath">The file path.</param>
   /// <returns>The <see cref="AjisSet{T}"/> for the entity type and file, or null if not found.</returns>
   public AjisSet<T>? GetSet<T>(string filePath) where T : class
   {
      var setKey = $"{typeof(T).FullName}:{filePath}";
      lock(_lock)
      {
         if(_sets.TryGetValue(setKey, out var set))
         {
            return (AjisSet<T>)set;
         }
         return null;
      }
   }

   #region IDisposable Implementation

   public void Dispose()
   {
      _dataSources.Clear();
      _sets.Clear();
      GC.SuppressFinalize(this);
   }

   public async ValueTask DisposeAsync()
   {
      IAjisDataSource[] dataSources;
      lock(_lock)
      {
         dataSources = [.. _dataSources.Values.OfType<IAjisDataSource>()];
         _dataSources.Clear();
         _sets.Clear();
      }

      foreach(var dataSource in dataSources)
      {
         await dataSource.DisposeAsync();
      }

      GC.SuppressFinalize(this);
   }

   #endregion

   #region JSON to AJIS/ATP Conversion

   /// <summary>
   /// Converts a JSON file to AJIS format.
   /// </summary>
   /// <param name="jsonFilePath">Path to the input JSON file.</param>
   /// <param name="ajisFilePath">Path to the output AJIS file.</param>
   /// <param name="options">Conversion options.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Task representing the asynchronous operation.</returns>
   public async Task ConvertJsonToAjis(
       string jsonFilePath,
       string ajisFilePath,
       JsonConversionOptions? options = null,
       CancellationToken cancellationToken = default)
   {
      if(string.IsNullOrWhiteSpace(jsonFilePath))
         throw new ArgumentNullException(nameof(jsonFilePath));

      if(string.IsNullOrWhiteSpace(ajisFilePath))
         throw new ArgumentNullException(nameof(ajisFilePath));

      // Read JSON and write directly as AJIS (JSON is valid AJIS)
      var json = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);

      string? directory = Path.GetDirectoryName(ajisFilePath);
      if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
         Directory.CreateDirectory(directory);

      await File.WriteAllTextAsync(ajisFilePath, json, cancellationToken);
   }

   /// <summary>
   /// Converts a JSON file to ATP format with native binary attachments.
   /// </summary>
   /// <param name="jsonFilePath">Path to the input JSON file.</param>
   /// <param name="atpFilePath">Path to the output ATP file.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Task representing the asynchronous operation.</returns>
   public async Task ConvertJsonToAtp(
       string jsonFilePath,
       string atpFilePath,
       CancellationToken cancellationToken = default)
   {
      if(string.IsNullOrWhiteSpace(jsonFilePath))
         throw new ArgumentNullException(nameof(jsonFilePath));

      if(string.IsNullOrWhiteSpace(atpFilePath))
         throw new ArgumentNullException(nameof(atpFilePath));

      // Read JSON and write as ATP (for now, JSON structure - full ATP would require ATP serializer)
      var json = await File.ReadAllTextAsync(jsonFilePath, cancellationToken);

      string? directory = Path.GetDirectoryName(atpFilePath);
      if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
         Directory.CreateDirectory(directory);

      await File.WriteAllTextAsync(atpFilePath, json, cancellationToken);
   }

   #endregion
}
