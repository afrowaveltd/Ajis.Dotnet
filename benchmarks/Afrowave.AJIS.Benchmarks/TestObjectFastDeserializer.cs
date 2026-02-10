#nullable enable

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Afrowave.AJIS.Serialization.Mapping;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Ultra-fast deserializer for TestObject generated at compile-time.
/// Eliminates all reflection, caching, and delegate overhead.
/// Inspired by System.Text.Json source generators.
/// </summary>
internal sealed class TestObjectFastDeserializer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TestObject? Deserialize(ref Utf8JsonReader reader)
    {
        // Assume we're already at StartObject
        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        var obj = new TestObject();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read(); // Move to value

                switch (propertyName)
                {
                    case "Id":
                        obj.Id = reader.GetInt32();
                        break;
                    case "Name":
                        obj.Name = reader.GetString() ?? "";
                        break;
                    case "Value":
                        obj.Value = reader.GetInt32();
                        break;
                    case "Score":
                        obj.Score = reader.GetDouble();
                        break;
                    case "Active":
                        obj.Active = reader.GetBoolean();
                        break;
                    case "Tags":
                        obj.Tags = ReadStringArray(ref reader) ?? Array.Empty<string>();
                        break;
                    case "Items":
                        obj.Items = ReadTestItemArray(ref reader) ?? new List<TestItem>();
                        break;
                }
            }
        }

        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string[]? ReadStringArray(ref Utf8JsonReader reader)
    {
        // Assume we're already at StartArray
        if (reader.TokenType != JsonTokenType.StartArray)
            return null;

        var list = new System.Collections.Generic.List<string>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                list.Add(reader.GetString() ?? "");
            }
        }
        return list.ToArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static System.Collections.Generic.List<TestItem>? ReadTestItemArray(ref Utf8JsonReader reader)
    {
        // Assume we're already at StartArray
        if (reader.TokenType != JsonTokenType.StartArray)
            return null;

        var list = new System.Collections.Generic.List<TestItem>();
        while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
        {
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                var item = ReadTestItem(ref reader);
                if (item != null)
                    list.Add(item);
            }
        }
        return list;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static TestItem? ReadTestItem(ref Utf8JsonReader reader)
    {
        // Assume we're already at StartObject
        if (reader.TokenType != JsonTokenType.StartObject)
            return null;

        var item = new TestItem();

        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                var propertyName = reader.GetString();
                reader.Read(); // Move to value

                switch (propertyName)
                {
                    case "ItemId":
                        item.ItemId = reader.GetInt32();
                        break;
                    case "ItemName":
                        item.ItemName = reader.GetString() ?? "";
                        break;
                    case "Amount":
                        item.Amount = reader.GetInt32();
                        break;
                }
            }
        }

        return item;
    }
}