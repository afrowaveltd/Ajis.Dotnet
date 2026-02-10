#nullable enable

using Afrowave.AJIS.Serialization.Mapping;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using System.Linq.Expressions;

namespace Afrowave.AJIS.MongoDB;

/// <summary>
/// MongoDB serializer for AJIS objects.
/// </summary>
public class AjisBsonSerializer<T> : SerializerBase<T> where T : notnull
{
    private readonly AjisConverter<T> _converter;

    public AjisBsonSerializer(AjisConverter<T> converter)
    {
        _converter = converter;
    }

    public AjisBsonSerializer() : this(new AjisConverter<T>())
    {
    }

    public override T Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonReader = context.Reader;
        var bsonType = bsonReader.GetCurrentBsonType();

        if(bsonType == BsonType.String)
        {
            var ajisText = bsonReader.ReadString();
            return _converter.Deserialize(ajisText) ?? throw new BsonSerializationException("Failed to deserialize AJIS");
        }
        else if(bsonType == BsonType.Document)
        {
            // If stored as document, deserialize normally
            bsonReader.ReadStartDocument();
            var result = Activator.CreateInstance<T>();

            while(bsonReader.ReadBsonType() != BsonType.EndOfDocument)
            {
                // Skip field name - not used in simple implementation
                bsonReader.SkipName();
                bsonReader.SkipValue();
                // Simple property mapping - in real implementation use reflection
                // This is a simplified example
            }

            bsonReader.ReadEndDocument();
            return result;
        }

        throw new BsonSerializationException($"Cannot deserialize {bsonType} to {typeof(T)}");
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
    {
        var bsonWriter = context.Writer;
        var ajisText = _converter.Serialize(value);

        bsonWriter.WriteString(ajisText);
    }
}

/// <summary>
/// MongoDB collection wrapper with AJIS support.
/// </summary>
public class AjisMongoCollection<T> where T : class
{
    private readonly IMongoCollection<BsonDocument> _collection;
    private readonly AjisConverter<T> _converter;
    private readonly string _idField;

    public AjisMongoCollection(IMongoDatabase database, string collectionName, string idField = "Id")
    {
        _collection = database.GetCollection<BsonDocument>(collectionName);
        _converter = new AjisConverter<T>();
        _idField = idField;
    }

    /// <summary>
    /// Inserts a document.
    /// </summary>
    public async Task InsertAsync(T document)
    {
        var ajisText = _converter.Serialize(document);
        var bsonDoc = new BsonDocument();
        bsonDoc.Add("data", ajisText);
        bsonDoc.Add("_id", BsonValue.Create(GetDocumentId(document)));

        await _collection.InsertOneAsync(bsonDoc);
    }

    /// <summary>
    /// Finds a document by ID.
    /// </summary>
    public async Task<T?> FindByIdAsync(object id)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", BsonValue.Create(id));
        var document = await _collection.Find(filter).FirstOrDefaultAsync();

        if(document == null) return null;

        var ajisText = document["data"].AsString;
        return _converter.Deserialize(ajisText);
    }

    /// <summary>
    /// Finds documents using AJIS predicate.
    /// </summary>
    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        // This is a simplified implementation
        // In practice, you'd need to translate the predicate to MongoDB query
        // For now, load all and filter in memory
        var allDocuments = await _collection.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();

        var results = new List<T>();
        foreach(var doc in allDocuments)
        {
            var ajisText = doc["data"].AsString;
            var obj = _converter.Deserialize(ajisText);
            if(obj != null && predicate.Compile()(obj))
            {
                results.Add(obj);
            }
        }

        return results;
    }

    /// <summary>
    /// Updates a document.
    /// </summary>
    public async Task UpdateAsync(T document)
    {
        var id = GetDocumentId(document);
        var ajisText = _converter.Serialize(document);

        var filter = Builders<BsonDocument>.Filter.Eq("_id", BsonValue.Create(id));
        var update = Builders<BsonDocument>.Update.Set("data", ajisText);

        await _collection.UpdateOneAsync(filter, update);
    }

    /// <summary>
    /// Deletes a document.
    /// </summary>
    public async Task DeleteAsync(object id)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("_id", BsonValue.Create(id));
        await _collection.DeleteOneAsync(filter);
    }

    private object GetDocumentId(T document)
    {
        var idProperty = typeof(T).GetProperty(_idField);
        if(idProperty == null)
            throw new InvalidOperationException($"Type {typeof(T)} does not have property {_idField}");

        return idProperty.GetValue(document) ?? throw new InvalidOperationException("Document ID cannot be null");
    }
}

/// <summary>
/// Extension methods for MongoDB with AJIS support.
/// </summary>
public static class AjisMongoExtensions
{
    /// <summary>
    /// Registers AJIS serializers for MongoDB.
    /// </summary>
    public static void RegisterAjisSerializers()
    {
        // Register serializers for common types
        // This would be expanded based on your needs
        BsonSerializer.RegisterSerializer(typeof(User), new AjisBsonSerializer<User>());
    }

    /// <summary>
    /// Creates an AJIS-enabled MongoDB collection.
    /// </summary>
    public static AjisMongoCollection<T> GetAjisCollection<T>(
        this IMongoDatabase database,
        string collectionName,
        string idField = "Id") where T : class
    {
        return new AjisMongoCollection<T>(database, collectionName, idField);
    }
}

/// <summary>
/// Repository pattern for MongoDB with AJIS.
/// </summary>
public class AjisMongoRepository<T> where T : class
{
    private readonly AjisMongoCollection<T> _collection;

    public AjisMongoRepository(IMongoDatabase database, string collectionName)
    {
        _collection = new AjisMongoCollection<T>(database, collectionName);
    }

    public async Task<T?> GetByIdAsync(object id)
    {
        return await _collection.FindByIdAsync(id);
    }

    public async Task<List<T>> GetAllAsync()
    {
        // Simplified - in practice you'd implement proper querying
        return await _collection.FindAsync(_ => true);
    }

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _collection.FindAsync(predicate);
    }

    public async Task InsertAsync(T entity)
    {
        await _collection.InsertAsync(entity);
    }

    public async Task UpdateAsync(T entity)
    {
        await _collection.UpdateAsync(entity);
    }

    public async Task DeleteAsync(object id)
    {
        await _collection.DeleteAsync(id);
    }
}

// Example entity for documentation
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}