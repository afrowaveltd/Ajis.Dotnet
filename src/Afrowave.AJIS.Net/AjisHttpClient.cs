#nullable enable

using Afrowave.AJIS.Serialization.Mapping;
using System.Net.Http.Headers;

namespace Afrowave.AJIS.Net;

/// <summary>
/// HTTP client optimized for AJIS data interchange.
/// </summary>
public class AjisHttpClient : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AjisConverterFactory _converterFactory;

    public AjisHttpClient()
    {
        _httpClient = new HttpClient();
        _converterFactory = new AjisConverterFactory();
        ConfigureDefaults();
    }

    public AjisHttpClient(HttpMessageHandler handler)
    {
        _httpClient = new HttpClient(handler);
        _converterFactory = new AjisConverterFactory();
        ConfigureDefaults();
    }

    private void ConfigureDefaults()
    {
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/ajis+json"));
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    /// <summary>
    /// Gets a typed AJIS response from a URL.
    /// </summary>
    public async Task<T?> GetAsync<T>(string url) where T : notnull
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var converter = _converterFactory.GetConverter<T>();
        return converter.Deserialize(content);
    }

    /// <summary>
    /// Gets a list of typed objects from a URL.
    /// </summary>
    public async Task<List<T>?> GetListAsync<T>(string url) where T : notnull
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var converter = _converterFactory.GetConverter<List<T>>();
        return converter.Deserialize(content);
    }

    /// <summary>
    /// Posts a typed object as AJIS.
    /// </summary>
    public async Task<HttpResponseMessage> PostAsync<T>(string url, T data) where T : notnull
    {
        var converter = _converterFactory.GetConverter<T>();
        var ajisContent = converter.Serialize(data);

        var content = new StringContent(ajisContent, System.Text.Encoding.UTF8, "application/ajis+json");
        return await _httpClient.PostAsync(url, content);
    }

    /// <summary>
    /// Posts a list of typed objects as AJIS.
    /// </summary>
    public async Task<HttpResponseMessage> PostListAsync<T>(string url, IEnumerable<T> data) where T : notnull
    {
        var converter = _converterFactory.GetConverter<List<T>>();
        var ajisContent = converter.Serialize(data.ToList());

        var content = new StringContent(ajisContent, System.Text.Encoding.UTF8, "application/ajis+json");
        return await _httpClient.PostAsync(url, content);
    }

    /// <summary>
    /// Puts a typed object as AJIS.
    /// </summary>
    public async Task<HttpResponseMessage> PutAsync<T>(string url, T data) where T : notnull
    {
        var converter = _converterFactory.GetConverter<T>();
        var ajisContent = converter.Serialize(data);

        var content = new StringContent(ajisContent, System.Text.Encoding.UTF8, "application/ajis+json");
        return await _httpClient.PutAsync(url, content);
    }

    /// <summary>
    /// Patches a typed object as AJIS.
    /// </summary>
    public async Task<HttpResponseMessage> PatchAsync<T>(string url, T data) where T : notnull
    {
        var converter = _converterFactory.GetConverter<T>();
        var ajisContent = converter.Serialize(data);

        var content = new StringContent(ajisContent, System.Text.Encoding.UTF8, "application/ajis+json");
        var request = new HttpRequestMessage(new HttpMethod("PATCH"), url) { Content = content };
        return await _httpClient.SendAsync(request);
    }

    /// <summary>
    /// Streams large AJIS data from a URL.
    /// </summary>
    public async IAsyncEnumerable<T> StreamAsync<T>(string url) where T : notnull
    {
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new System.Text.Json.Utf8JsonReader(stream);

        var converter = _converterFactory.GetConverter<T>();

        // Assume response is an array
        if(!reader.Read() || reader.TokenType != System.Text.Json.JsonTokenType.StartArray)
            yield break;

        while(reader.Read() && reader.TokenType != System.Text.Json.JsonTokenType.EndArray)
        {
            if(reader.TokenType == System.Text.Json.JsonTokenType.StartObject)
            {
                // Deserialize object from current position
                var obj = converter.DeserializeFromUtf8(reader.ValueSpan);
                if(obj != null)
                    yield return obj;
            }
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

/// <summary>
/// Factory for creating AJIS converters with caching.
/// </summary>
internal class AjisConverterFactory
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<Type, object> _converters = new();

    public AjisConverter<T> GetConverter<T>() where T : notnull
    {
        return (AjisConverter<T>)_converters.GetOrAdd(typeof(T), _ => new AjisConverter<T>());
    }
}