#nullable enable

using Afrowave.AJIS.Serialization.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace Afrowave.AJIS.EntityFramework;

/// <summary>
/// EF Core value converter for AJIS serialized objects.
/// </summary>
public class AjisValueConverter<T> : ValueConverter<T, string> where T : notnull
{
    private readonly AjisConverter<T> _converter;

    public AjisValueConverter(AjisConverter<T> converter) : base(
        v => converter.Serialize(v),
        v => DeserializeOrThrow(converter, v))
    {
        _converter = converter;
    }

    public AjisValueConverter() : this(new AjisConverter<T>())
    {
    }

    private static T DeserializeOrThrow(AjisConverter<T> converter, string value)
    {
        return converter.Deserialize(value) ?? throw new InvalidOperationException("Failed to deserialize AJIS");
    }
}

/// <summary>
/// EF Core value converter for collections serialized as AJIS arrays.
/// </summary>
public class AjisCollectionConverter<T> : ValueConverter<ICollection<T>, string> where T : notnull
{
    private readonly AjisConverter<List<T>> _converter;

    public AjisCollectionConverter(AjisConverter<List<T>> converter) : base(
        v => converter.Serialize(v.ToList()),
        v => converter.Deserialize(v) ?? new List<T>())
    {
        _converter = converter;
    }

    public AjisCollectionConverter() : this(new AjisConverter<List<T>>())
    {
    }
}

/// <summary>
/// Extension methods for configuring AJIS in EF Core.
/// </summary>
public static class AjisEntityFrameworkExtensions
{
    /// <summary>
    /// Configures a property to use AJIS serialization.
    /// </summary>
    public static Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<T> UseAjisSerialization<T>(
        this Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<T> propertyBuilder) where T : notnull
    {
        return propertyBuilder.HasConversion(new AjisValueConverter<T>());
    }

    /// <summary>
    /// Configures a collection property to use AJIS array serialization.
    /// </summary>
    public static Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<ICollection<T>> UseAjisCollectionSerialization<T>(
        this Microsoft.EntityFrameworkCore.Metadata.Builders.PropertyBuilder<ICollection<T>> propertyBuilder) where T : notnull
    {
        return propertyBuilder.HasConversion(new AjisCollectionConverter<T>());
    }

    /// <summary>
    /// Configures an entity to use AJIS for complex properties.
    /// </summary>
    public static Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> UseAjisForComplexProperties<T>(
        this Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<T> entityBuilder) where T : class
    {
        // This would be called in OnModelCreating
        // Example: entityBuilder.Property(e => e.ComplexObject).UseAjisSerialization();
        return entityBuilder;
    }
}

/// <summary>
/// Base class for DbContext with AJIS support.
/// </summary>
public abstract class AjisDbContext : DbContext
{
    protected AjisDbContext(DbContextOptions options) : base(options)
    {
    }

    /// <summary>
    /// Gets an AJIS converter for the specified type.
    /// </summary>
    protected AjisConverter<T> GetAjisConverter<T>() where T : notnull
    {
        return new AjisConverter<T>();
    }

    /// <summary>
    /// Serializes an object to AJIS for storage.
    /// </summary>
    protected string SerializeToAjis<T>(T obj) where T : notnull
    {
        return GetAjisConverter<T>().Serialize(obj);
    }

    /// <summary>
    /// Deserializes an object from AJIS.
    /// </summary>
    protected T? DeserializeFromAjis<T>(string ajisText) where T : notnull
    {
        return GetAjisConverter<T>().Deserialize(ajisText);
    }

    /// <summary>
    /// Applies AJIS configuration to the model.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply AJIS conversions to marked properties
        foreach(var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach(var property in entityType.GetProperties())
            {
                var clrType = property.ClrType;

                // Check for AjisSerializableAttribute
                if(Attribute.IsDefined(clrType, typeof(AjisSerializableAttribute)))
                {
                    // Configure property to use AJIS serialization
                    var converterType = typeof(AjisValueConverter<>).MakeGenericType(clrType);
                    var converter = Activator.CreateInstance(converterType);
                    property.SetValueConverter((ValueConverter)converter!);
                }
            }
        }
    }
}

/// <summary>
/// Attribute to mark properties for AJIS serialization in EF Core.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AjisSerializableAttribute : Attribute
{
}

/// <summary>
/// Repository pattern implementation with AJIS file storage.
/// </summary>
public class AjisFileRepository<T> where T : class, new()
{
    private readonly string _filePath;
    private readonly AjisConverter<List<T>> _converter;

    public AjisFileRepository(string filePath)
    {
        _filePath = filePath;
        _converter = new AjisConverter<List<T>>();
    }

    public async Task<List<T>> GetAllAsync()
    {
        if(!File.Exists(_filePath))
            return new List<T>();

        var json = await File.ReadAllTextAsync(_filePath);
        return _converter.Deserialize(json) ?? new List<T>();
    }

    public async Task<T?> GetByIdAsync(object id)
    {
        var all = await GetAllAsync();
        // Simple implementation - assumes T has Id property
        var idProperty = typeof(T).GetProperty("Id");
        if(idProperty == null) return null;

        return all.FirstOrDefault(item => idProperty.GetValue(item)?.Equals(id) == true);
    }

    public async Task AddAsync(T item)
    {
        var all = await GetAllAsync();
        all.Add(item);
        await SaveAllAsync(all);
    }

    public async Task UpdateAsync(T item)
    {
        var all = await GetAllAsync();
        // Simple implementation - assumes T has Id property
        var idProperty = typeof(T).GetProperty("Id");
        if(idProperty == null) return;

        var id = idProperty.GetValue(item);
        var existing = all.FirstOrDefault(x => idProperty.GetValue(x)?.Equals(id) == true);
        if(existing != null)
        {
            var index = all.IndexOf(existing);
            all[index] = item;
            await SaveAllAsync(all);
        }
    }

    public async Task DeleteAsync(object id)
    {
        var all = await GetAllAsync();
        var idProperty = typeof(T).GetProperty("Id");
        if(idProperty == null) return;

        all.RemoveAll(item => idProperty.GetValue(item)?.Equals(id) == true);
        await SaveAllAsync(all);
    }

    private async Task SaveAllAsync(List<T> items)
    {
        var json = _converter.Serialize(items);
        await File.WriteAllTextAsync(_filePath, json);
    }
}