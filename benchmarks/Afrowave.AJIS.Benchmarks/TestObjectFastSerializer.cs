#nullable enable

using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Afrowave.AJIS.Serialization.Mapping;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Ultra-fast serializer for TestObject generated at compile-time.
/// Eliminates all reflection, caching, and delegate overhead.
/// Inspired by System.Text.Json source generators.
/// </summary>
internal sealed class TestObjectFastSerializer
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Serialize(Utf8JsonWriter writer, TestObject obj)
    {
        writer.WriteStartObject();

        writer.WriteNumber("Id", obj.Id);
        writer.WriteString("Name", obj.Name);
        writer.WriteNumber("Value", obj.Value);
        writer.WriteNumber("Score", obj.Score);
        writer.WriteBoolean("Active", obj.Active);

        if (obj.Tags != null)
        {
            writer.WriteStartArray("Tags");
            foreach (var tag in obj.Tags)
            {
                writer.WriteStringValue(tag);
            }
            writer.WriteEndArray();
        }

        if (obj.Items != null)
        {
            writer.WriteStartArray("Items");
            foreach (var item in obj.Items)
            {
                SerializeTestItem(writer, item);
            }
            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void SerializeTestItem(Utf8JsonWriter writer, TestItem item)
    {
        writer.WriteStartObject();
        writer.WriteNumber("ItemId", item.ItemId);
        writer.WriteString("ItemName", item.ItemName);
        writer.WriteNumber("Amount", item.Amount);
        writer.WriteEndObject();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Serialize(List<TestObject> list)
    {
        var buffer = new ArrayBufferWriter<byte>(64 * 1024);
        using var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = false });

        writer.WriteStartArray();
        foreach (var obj in list)
        {
            Serialize(writer, obj);
        }
        writer.WriteEndArray();
        writer.Flush();

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }
}