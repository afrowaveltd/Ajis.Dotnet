namespace Afrowave.AJIS.Identity;

/// <summary>
/// Context for AJIS-based identity operations.
/// </summary>
public class AjisContext
{
    public string BasePath { get; }

    public AjisContext(string basePath)
    {
        BasePath = basePath;
        Directory.CreateDirectory(basePath);
    }

    public List<T> Query<T>(string filename)
    {
        var filePath = Path.Combine(BasePath, filename);
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            try
            {
                var result = System.Text.Json.JsonSerializer.Deserialize<List<T>>(json);
                return result ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }
        return new List<T>();
    }
}
