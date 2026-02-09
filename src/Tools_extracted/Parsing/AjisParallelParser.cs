using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Afrowave.AJIS;

/// <summary>
/// Provides parallel parsing capabilities for large AJIS/JSON arrays.
/// </summary>
public static class AjisParallelParser
{
    private static readonly System.Collections.Concurrent.ConcurrentBag<List<string>> _partsPool = new();

    private sealed class ReadOnlyMemoryByteComparer : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
        {
            var xs = x.Span;
            var ys = y.Span;
            if (xs.Length != ys.Length) return false;
            return xs.SequenceEqual(ys);
        }

        public int GetHashCode(ReadOnlyMemory<byte> obj)
        {
            var span = obj.Span;
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + span.Length;
                if (span.Length > 0)
                {
                    hash = (hash * 31) + span[0];
                    hash = (hash * 31) + span[span.Length - 1];
                    if (span.Length > 2) hash = (hash * 31) + span[span.Length / 2];
                }
                return hash;
            }
        }
    }

    private static List<string> RentParts(int capacity)
    {
        if (_partsPool.TryTake(out var list))
        {
            list.Clear();
            return list;
        }
        return new List<string>(capacity);
    }

    private static void ReturnParts(List<string> parts)
    {
        parts.Clear();
        _partsPool.Add(parts);
    }
    /// <summary>
    /// Parses a large JSON array in parallel by splitting it into chunks.
    /// Useful for arrays with 1000+ elements.
    /// </summary>
    /// <param name="jsonArray">JSON array string to parse</param>
    /// <param name="chunkSize">Number of elements per chunk (default: 1000)</param>
    /// <param name="lexerOptions">Lexer options</param>
    /// <param name="parserOptions">Parser options</param>
    /// <returns>Parsed array value</returns>
    public static AjisValue ParseLargeArray(
        string jsonArray,
        int chunkSize = 1000,
        AjisLexerOptions? lexerOptions = null,
        AjisParserOptions? parserOptions = null)
    {
        // Extract elements at top-level (token-aware) so we never split strings or nested structures
        var elements = ExtractArrayElements(jsonArray);

        // Diagnostic: print a single summary if telemetry/metrics collection is enabled
        if (parserOptions != null && parserOptions.CollectParallelMetrics)
        {
            Console.WriteLine($"[AjisParallelParser] ExtractArrayElements -> elements={elements.Count}, length={jsonArray.Length}");
        }

        if (elements.Count == 0)
        {
            return AjisDocument.Parse(jsonArray, lexerOptions, parserOptions).Root;
        }

        // If array is small, parse single-threaded
        if (elements.Count <= chunkSize)
        {
            return AjisDocument.Parse(jsonArray, lexerOptions, parserOptions).Root;
        }

        var values = new AjisValue[elements.Count];
        var exceptions = new ConcurrentQueue<Exception>();

        // Choose degree of parallelism based on CPU, but not exceeding element count
        // Decide degree of parallelism: heuristic that balances CPU and work size, with override
        int degree;

        if (parserOptions != null && parserOptions.MaxParallelDegree.HasValue)
        {
            degree = Math.Min(Math.Max(1, parserOptions.MaxParallelDegree.Value), elements.Count);
        }
        else
        {
            var procs = Math.Max(1, Environment.ProcessorCount);

            // If parallel parsing disabled explicitly, force single-threaded
            if (parserOptions != null && !parserOptions.EnableParallelParsing)
            {
                degree = 1;
            }
            else
            {
                // Heuristic inputs
                var totalBytes = Math.Max(1, jsonArray.Length);
                var avgElementBytes = Math.Max(1, totalBytes / elements.Count);

                // Desired threads by data size: aim for ~256KB per thread
                var threadsBySize = Math.Max(1, (int)(totalBytes / (256.0 * 1024.0)));

                // Desired threads by element count: avoid too few elements per thread
                const int MinElementsPerThread = 50;
                var threadsByElements = Math.Max(1, elements.Count / MinElementsPerThread);

                // Base cap: allow up to 2x logical processors (for IO-bound or hyperthreading), but cap absolute
                var maxCap = Math.Min(elements.Count, Math.Min(Math.Max(1, procs * 2), 64));

                // Start with proportional to CPU, but adapt by size and element count
                int candidate = procs;
                // Scale up if data is large
                candidate = Math.Max(candidate, threadsBySize);
                // Ensure we have reasonable work per thread
                candidate = Math.Max(candidate, threadsByElements);

                // Avoid too many threads for very small elements
                if (avgElementBytes < 1024)
                {
                    // small elements: do not exceed logical processors
                    candidate = Math.Min(candidate, procs);
                }

                degree = Math.Clamp(candidate, 1, maxCap);
            }
        }

        var po = new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, degree) };

        try
        {
            Parallel.For(0, elements.Count, po, i =>
            {
                try
                {
                    var range = elements[i];
                    var snippetMem = jsonArray.AsMemory(range.Start, range.Length);
                    var doc = AjisDocument.Parse(snippetMem, lexerOptions, parserOptions);
                    values[i] = doc.Root;
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            });
        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.InnerExceptions) exceptions.Enqueue(ex);
        }

        if (!exceptions.IsEmpty)
        {
            // If any parsing failed, fallback to single-threaded parse for correctness
            return AjisDocument.Parse(jsonArray, lexerOptions, parserOptions).Root;
        }

        // Optional telemetry: compare parallel vs single-threaded execution
        if (parserOptions != null && parserOptions.CollectParallelMetrics)
        {
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var single = AjisDocument.Parse(jsonArray, lexerOptions, parserOptions).Root;
                sw.Stop();
                var singleMs = sw.Elapsed.TotalMilliseconds;

                var parallelMs = 0.0; // best-effort: we don't have individual timing, so measure by parsing the combined final string
                        try
                        {
                            var sw2 = System.Diagnostics.Stopwatch.StartNew();
                            // Reconstruct array text from elements and parse to measure parallel-equivalent work (telemetry only)
                            var sb = new System.Text.StringBuilder(Math.Min(jsonArray.Length, 1024 * 1024));
                            sb.Append('[');
                            for (int idx = 0; idx < elements.Count; idx++)
                            {
                                if (idx > 0) sb.Append(',');
                                var r = elements[idx];
                                sb.Append(jsonArray.AsSpan(r.Start, r.Length));
                            }
                            sb.Append(']');
                            AjisDocument.Parse(sb.ToString(), lexerOptions, parserOptions);
                            sw2.Stop();
                            parallelMs = sw2.Elapsed.TotalMilliseconds;
                        }
                        catch { }

                var file = System.IO.Path.Combine(Environment.CurrentDirectory, "ajis_parallel_bench.csv");
                var header = "Timestamp,Elements,Degree,ParallelMs,SingleMs,Success";
                var line = $"{DateTime.UtcNow:o},{elements.Count},{po.MaxDegreeOfParallelism},{parallelMs:F2},{singleMs:F2},true";
                lock (typeof(AjisParallelParser))
                {
                    if (!System.IO.File.Exists(file)) System.IO.File.WriteAllText(file, header + Environment.NewLine);
                    System.IO.File.AppendAllText(file, line + Environment.NewLine);
                }
            }
            catch { }
        }

        // Validate we parsed all elements
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == null)
            {
                return AjisDocument.Parse(jsonArray, lexerOptions, parserOptions).Root;
            }
        }

        var final = new List<AjisValue>(values.Length);
        final.AddRange(values);
        return AjisValue.Array(final);
    }

    /// <summary>
    /// Byte-based overload: parses a large JSON/AJIS array from UTF-8 bytes.
    /// </summary>
    public static AjisValue ParseLargeArray(
        ReadOnlyMemory<byte> utf8Json,
        int chunkSize = 1000,
        AjisParserOptions? parserOptions = null)
    {
        var elements = ExtractArrayElementsBytes(utf8Json);

        if (elements.Count == 0)
        {
            return AjisUtf8Parser.Parse(utf8Json.Span, parserOptions);
        }

        if (elements.Count <= chunkSize)
        {
            return AjisUtf8Parser.Parse(utf8Json.Span, parserOptions);
        }

        var values = new AjisValue[elements.Count];
        var exceptions = new ConcurrentQueue<Exception>();

        var procs = Math.Max(1, Environment.ProcessorCount);
        var degree = Math.Min(Math.Max(1, procs), elements.Count);
        var po = new ParallelOptions { MaxDegreeOfParallelism = degree };

        try
        {
            Parallel.For(0, elements.Count, po, i =>
            {
                try
                {
                    var r = elements[i];
                    var v = AjisUtf8Parser.Parse(utf8Json, r.Start, r.Length, parserOptions);
                    values[i] = v;
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            });
        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.InnerExceptions) exceptions.Enqueue(ex);
        }

        if (!exceptions.IsEmpty)
        {
            return AjisUtf8Parser.Parse(utf8Json.Span, parserOptions);
        }

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == null)
            {
                return AjisUtf8Parser.Parse(utf8Json.Span, parserOptions);
            }
        }

        var final = new List<AjisValue>(values.Length);
        final.AddRange(values);
        return AjisValue.Array(final);
    }

    /// <summary>
    /// Parses a large JSON object in parallel by processing properties concurrently.
    /// </summary>
    public static AjisValue ParseLargeObject(
        string json,
        AjisLexerOptions? lexerOptions = null,
        AjisParserOptions? parserOptions = null)
    {
        var pairs = ExtractObjectPairs(json);

        if (pairs == null || pairs.Count == 0)
        {
            return AjisDocument.Parse(json, lexerOptions, parserOptions).Root;
        }

        // For small objects, parse single-threaded
        if (pairs.Count < 100)
        {
            return AjisDocument.Parse(json, lexerOptions, parserOptions).Root;
        }

        var result = new ConcurrentDictionary<string, AjisValue>(StringComparer.Ordinal);
        var exceptions = new ConcurrentQueue<Exception>();

        var procs = Math.Max(1, Environment.ProcessorCount - 1);
        var degree = Math.Min(procs, pairs.Count);
        var po = new ParallelOptions { MaxDegreeOfParallelism = degree };

        var keyPool = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        try
        {
            Parallel.ForEach(pairs, po, pair =>
            {
                try
                {
                    var key = new string(json.AsSpan(pair.KeyStart, pair.KeyLength));
                    key = keyPool.GetOrAdd(key, key);
                    var mem = json.AsMemory(pair.ValueStart, pair.ValueLength);
                    var doc = AjisDocument.Parse(mem, lexerOptions, parserOptions);
                    result[key] = doc.Root;
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            });
        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.InnerExceptions) exceptions.Enqueue(ex);
        }

        if (!exceptions.IsEmpty || result.Count != pairs.Count)
        {
            return AjisDocument.Parse(json, lexerOptions, parserOptions).Root;
        }

        return AjisValue.Object(new Dictionary<string, AjisValue>(result));
    }

    /// <summary>
    /// Byte-based overload: parses a large JSON/AJIS object from UTF-8 bytes in parallel.
    /// </summary>
    public static AjisValue ParseLargeObject(ReadOnlyMemory<byte> utf8Json, AjisParserOptions? parserOptions = null)
    {
        var pairs = ExtractObjectPairsBytes(utf8Json);

        if (pairs == null || pairs.Count == 0)
        {
            return AjisUtf8Parser.Parse(utf8Json.Span, parserOptions);
        }

        if (pairs.Count < 100)
        {
            return AjisUtf8Parser.Parse(utf8Json.Span, parserOptions);
        }

        var result = new ConcurrentDictionary<string, AjisValue>(StringComparer.Ordinal);
        var exceptions = new ConcurrentQueue<Exception>();
        var procs = Math.Max(1, Environment.ProcessorCount - 1);
        var degree = Math.Min(procs, pairs.Count);
        var po = new ParallelOptions { MaxDegreeOfParallelism = degree };

        var keyInterner = new ConcurrentDictionary<ReadOnlyMemory<byte>, string>(new ReadOnlyMemoryByteComparer());

        try
        {
            Parallel.ForEach(pairs, po, pair =>
            {
                try
                {
                    var keySlice = utf8Json.Slice(pair.KeyStart, pair.KeyLength);
                    var key = keyInterner.GetOrAdd(keySlice, km => System.Text.Encoding.UTF8.GetString(km.Span));
                    var doc = AjisUtf8Parser.Parse(utf8Json, pair.ValueStart, pair.ValueLength, parserOptions);
                    result[key] = doc;
                }
                catch (Exception ex)
                {
                    exceptions.Enqueue(ex);
                }
            });
        }
        catch (AggregateException ae)
        {
            foreach (var ex in ae.InnerExceptions) exceptions.Enqueue(ex);
        }

        if (!exceptions.IsEmpty || result.Count != pairs.Count)
        {
            return AjisUtf8Parser.Parse(utf8Json.Span, parserOptions);
        }

        return AjisValue.Object(new Dictionary<string, AjisValue>(result));
    }

    private static List<(int Start, int Length)> ExtractArrayElements(string jsonArray)
    {
        var elements = new List<(int Start, int Length)>();

        var start = jsonArray.IndexOf('[');
        var end = jsonArray.LastIndexOf(']');
        if (start < 0 || end <= start)
            return elements;

        var innerOffset = start + 1;
        var innerLength = end - start - 1;
        var inner = jsonArray.AsSpan(innerOffset, innerLength);

        var inString = false;
        var escapeNext = false;
        var depth = 0; // nested depth inside element (objects/arrays)
        var elementStart = -1;

        for (int i = 0; i < inner.Length; i++)
        {
            var c = inner[i];

            if (escapeNext)
            {
                escapeNext = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escapeNext = true;
                continue;
            }

            if (c == '"' && !escapeNext)
            {
                if (!inString)
                {
                    if (elementStart == -1) elementStart = i;
                }

                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == '[' || c == '{')
            {
                if (elementStart == -1) elementStart = i;
                depth++;
                continue;
            }

            if (c == ']' || c == '}')
            {
                depth = Math.Max(0, depth - 1);
                continue;
            }

            if (c == ',' && depth == 0)
            {
                if (elementStart != -1)
                {
                    // trim trailing whitespace
                    var length = i - elementStart;
                    while (length > 0 && char.IsWhiteSpace(inner[elementStart + length - 1])) length--;
                    if (length > 0)
                    {
                        elements.Add((innerOffset + elementStart, length));
                    }
                    elementStart = -1;
                }
                continue;
            }

            if (elementStart == -1 && !char.IsWhiteSpace(c))
            {
                elementStart = i;
            }
        }

        if (elementStart != -1)
        {
            var length = inner.Length - elementStart;
            while (length > 0 && char.IsWhiteSpace(inner[elementStart + length - 1])) length--;
            if (length > 0)
            {
                elements.Add((innerOffset + elementStart, length));
            }
        }

        // single summary logged from caller when metrics collection is enabled
        return elements;
    }

    private static List<(int Start, int Length)> ExtractArrayElementsBytes(ReadOnlyMemory<byte> src)
    {
        var elements = new List<(int Start, int Length)>();

        int start = -1;
        var span = src.Span;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == (byte)'[')
            {
                start = i + 1;
                break;
            }
        }

        if (start == -1) return elements;

        int end = -1;
        for (int i = span.Length - 1; i >= 0; i--)
        {
            if (span[i] == (byte)']') { end = i; break; }
        }

        if (end <= start) return elements;

        var inner = span.Slice(start, end - start);

        bool inString = false;
        bool escape = false;
        int depth = 0;
        int elementStart = -1;

        for (int i = 0; i < inner.Length; i++)
        {
            var b = inner[i];
            if (escape) { escape = false; continue; }
            if (b == (byte)'\\' && inString) { escape = true; continue; }
            if (b == (byte)'"') { if (!inString) { if (elementStart == -1) elementStart = i; } inString = !inString; continue; }
            if (inString) continue;
            if (b == (byte)'[' || b == (byte)'{') { if (elementStart == -1) elementStart = i; depth++; continue; }
            if (b == (byte)']' || b == (byte)'}') { depth = Math.Max(0, depth - 1); continue; }
            if (b == (byte)',' && depth == 0)
            {
                if (elementStart != -1)
                {
                    int len = i - elementStart;
                    // trim trailing whitespace
                    while (len > 0 && IsWhiteSpaceByte(inner[elementStart + len - 1])) len--;
                    if (len > 0) elements.Add((start + elementStart, len));
                    elementStart = -1;
                }
                continue;
            }
            if (elementStart == -1 && !IsWhiteSpaceByte(b)) elementStart = i;
        }

        if (elementStart != -1)
        {
            int len = inner.Length - elementStart;
            while (len > 0 && IsWhiteSpaceByte(inner[elementStart + len - 1])) len--;
            if (len > 0) elements.Add((start + elementStart, len));
        }

        return elements;
    }

    private static bool IsWhiteSpaceByte(byte b) => b == (byte)' ' || b == (byte)'\n' || b == (byte)'\r' || b == (byte)'\t' || b == (byte)'\v' || b == (byte)'\f';

    private static List<(int KeyStart, int KeyLength, int ValueStart, int ValueLength)> ExtractObjectPairs(string jsonObject)
    {
        var pairs = new List<(int KeyStart, int KeyLength, int ValueStart, int ValueLength)>();
        var depth = 0;
        var inString = false;
        var escapeNext = false;
        var keyStart = -1;
        var keyLen = 0;
        var valueStart = -1;

        for (int i = 0; i < jsonObject.Length; i++)
        {
            var c = jsonObject[i];

            if (escapeNext)
            {
                escapeNext = false;
                continue;
            }

            if (c == '\\' && inString)
            {
                escapeNext = true;
                continue;
            }

            if (c == '"')
            {
                // Opening quote: mark start of key or start of a string value
                if (!inString)
                {
                    if (depth == 1 && keyStart == -1 && valueStart == -1)
                    {
                        keyStart = i;
                        keyLen = 0;
                    }
                    else if (depth == 1 && keyStart != -1 && valueStart == -1)
                    {
                        valueStart = i;
                    }
                }
                else
                {
                    // Closing quote: if we just closed a key, capture its length; if we closed a string value, capture pair immediately
                    if (depth == 1 && keyStart != -1 && valueStart == -1)
                    {
                        keyLen = i - keyStart - 1; // exclude quotes
                        // keep keyStart/keyLen for next value
                    }
                    else if (depth == 1 && valueStart != -1)
                    {
                        // capture the string value including quotes
                        pairs.Add((keyStart + 1, keyLen, valueStart, i - valueStart + 1));
                        keyStart = -1;
                        keyLen = 0;
                        valueStart = -1;
                    }
                }

                inString = !inString;
                continue;
            }

            if (inString) continue;

            if (c == '{' || c == '[')
            {
                if (depth == 1 && valueStart == -1 && keyStart != -1)
                    valueStart = i;
                depth++;
            }
            else if (c == '}' || c == ']')
            {
                // If we're closing a nested value and returning to depth==1, include the closing char
                if (depth == 2 && valueStart != -1)
                {
                    pairs.Add((keyStart + 1, keyLen, valueStart, i - valueStart + 1));
                    keyStart = -1;
                    keyLen = 0;
                    valueStart = -1;
                }
                // If we're closing the top-level object (depth==1 -> becomes 0), and a primitive value is active,
                // capture up to the closing brace (exclusive).
                else if (depth == 1 && valueStart != -1)
                {
                    // trim trailing whitespace
                    var len = i - valueStart;
                    while (len > 0 && char.IsWhiteSpace(jsonObject[valueStart + len - 1])) len--;
                    if (len > 0)
                    {
                        pairs.Add((keyStart + 1, keyLen, valueStart, len));
                    }
                    keyStart = -1;
                    keyLen = 0;
                    valueStart = -1;
                }

                depth--;
            }
            else if (c == ',' && depth == 1)
            {
                if (valueStart != -1)
                {
                    var len = i - valueStart;
                    while (len > 0 && char.IsWhiteSpace(jsonObject[valueStart + len - 1])) len--;
                    if (len > 0)
                    {
                        pairs.Add((keyStart + 1, keyLen, valueStart, len));
                    }
                    keyStart = -1;
                    keyLen = 0;
                    valueStart = -1;
                }
            }
            else if (c == ':' && depth == 1 && valueStart == -1)
            {
                // Skip to value
            }
            else if (depth == 1 && valueStart == -1 && !char.IsWhiteSpace(c) && keyStart != -1)
            {
                valueStart = i;
            }
        }

        return pairs;
    }

    private static List<(int KeyStart, int KeyLength, int ValueStart, int ValueLength)> ExtractObjectPairsBytes(ReadOnlyMemory<byte> src)
    {
        var pairs = new List<(int KeyStart, int KeyLength, int ValueStart, int ValueLength)>();
        var span = src.Span;

        int start = -1;
        for (int i = 0; i < span.Length; i++) if (span[i] == (byte)'{') { start = i; break; }
        if (start == -1) return pairs;

        // Find end
        int end = -1;
        for (int i = span.Length - 1; i >= 0; i--) if (span[i] == (byte)'}') { end = i; break; }
        if (end <= start) return pairs;

        var inString = false;
        var escapeNext = false;
        var depth = 0;
        var keyStart = -1;
        var keyLen = 0;
        var valueStart = -1;

        for (int i = 0; i < span.Length; i++)
        {
            var c = span[i];
            if (escapeNext) { escapeNext = false; continue; }
            if (c == (byte)'\\' && inString) { escapeNext = true; continue; }
            if (c == (byte)'"')
            {
                if (!inString)
                {
                    if (depth == 1 && keyStart == -1 && valueStart == -1)
                    {
                        keyStart = i;
                        keyLen = 0;
                    }
                    else if (depth == 1 && keyStart != -1 && valueStart == -1)
                    {
                        valueStart = i;
                    }
                }
                else
                {
                    if (depth == 1 && keyStart != -1 && valueStart == -1)
                    {
                        keyLen = i - keyStart - 1;
                    }
                    else if (depth == 1 && valueStart != -1)
                    {
                        pairs.Add((keyStart + 1, keyLen, valueStart, i - valueStart + 1));
                        keyStart = -1; keyLen = 0; valueStart = -1;
                    }
                }
                inString = !inString;
                continue;
            }
            if (inString) continue;
            if (c == (byte)'{' || c == (byte)'[')
            {
                if (depth == 1 && valueStart == -1 && keyStart != -1) valueStart = i;
                depth++; continue;
            }
            else if (c == (byte)'}' || c == (byte)']')
            {
                if (depth == 2 && valueStart != -1)
                {
                    pairs.Add((keyStart + 1, keyLen, valueStart, i - valueStart + 1));
                    keyStart = -1; keyLen = 0; valueStart = -1;
                }
                else if (depth == 1 && valueStart != -1)
                {
                    var len = i - valueStart; while (len > 0 && IsWhiteSpaceByte(span[valueStart + len - 1])) len--; if (len > 0) pairs.Add((keyStart + 1, keyLen, valueStart, len));
                    keyStart = -1; keyLen = 0; valueStart = -1;
                }
                depth--; continue;
            }
            else if (c == (byte)',' && depth == 1)
            {
                if (valueStart != -1) { var len = i - valueStart; while (len > 0 && IsWhiteSpaceByte(span[valueStart + len - 1])) len--; if (len > 0) pairs.Add((keyStart + 1, keyLen, valueStart, len)); keyStart = -1; keyLen = 0; valueStart = -1; }
            }
            else if (c == (byte)':' && depth == 1 && valueStart == -1) { }
            else if (depth == 1 && valueStart == -1 && !IsWhiteSpaceByte(c) && keyStart != -1) { valueStart = i; }
        }

        return pairs;
    }


}
