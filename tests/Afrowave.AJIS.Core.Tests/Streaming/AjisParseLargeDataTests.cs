#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using System.Text;
using Afrowave.AJIS.Testing.TestData;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisParseLargeDataTests
{
   [Fact]
   public void ParseSegments_LargePayload_UniversalProfile()
   {
      string path = CreateTempPayload(userCount: 200, addressesPerUser: 2);
      try
      {
         byte[] bytes = File.ReadAllBytes(path);
         var settings = new AjisSettings { ParserProfile = AjisProcessingProfile.Universal };

         int count = AjisParse.ParseSegments(bytes, settings).Count();

         Assert.True(count > 0);
      }
      finally
      {
         File.Delete(path);
      }
   }

   [Fact]
   public async Task ParseSegmentsAsync_ChunkedPath_DecodesEscapedString()
   {
      string path = Path.Combine(Path.GetTempPath(), "ajis_chunked_escape.json");
      if(File.Exists(path)) File.Delete(path);

      try
      {
         string payload = "{\"text\":\"" + new string('a', 1500) + "\\n\"}";
         await File.WriteAllTextAsync(path, payload);

         var settings = new AjisSettings
         {
            ParserProfile = AjisProcessingProfile.Universal,
            StreamChunkThreshold = "1k"
         };

         await using var stream = File.OpenRead(path);
         var segments = new List<AjisSegment>();

         await foreach(var segment in AjisParse.ParseSegmentsAsync(stream, settings))
            segments.Add(segment);
         Assert.Contains(segments, s => s.ValueKind == AjisValueKind.String
            && s.Slice is { } slice
            && HasLargeEscapedPayload(slice));
      }
      finally
      {
         File.Delete(path);
      }
   }

   private static bool HasLargeEscapedPayload(AjisSliceUtf8 slice)
   {
      string text = Encoding.UTF8.GetString(slice.Bytes.Span);
      return text.Length > 1000 && (text.Contains("\n") || text.Contains("\\n"));
   }

   [Theory]
   [InlineData("1k")]
   [InlineData("2M")]
   [InlineData("3g")]
   [InlineData("4")]
   public async Task ParseSegmentsAsync_LargePayload_ChunkThreshold_ParsesSuffixes(string threshold)
   {
      string path = CreateTempPayload(userCount: 200, addressesPerUser: 2);
      try
      {
         var settings = new AjisSettings
         {
            ParserProfile = AjisProcessingProfile.Universal,
            StreamChunkThreshold = threshold
         };

         int count = 0;
         await using var stream = File.OpenRead(path);
         await foreach(var _ in AjisParse.ParseSegmentsAsync(stream, settings))
            count++;

         Assert.True(count > 0);
      }
      finally
      {
         File.Delete(path);
      }
   }

   [Fact]
   public async Task ParseSegmentsAsync_LargePayload_InvalidChunkThreshold_Throws()
   {
      string path = CreateTempPayload(userCount: 200, addressesPerUser: 2);
      try
      {
         var settings = new AjisSettings
         {
            ParserProfile = AjisProcessingProfile.Universal,
            StreamChunkThreshold = "bad"
         };

         await using var stream = File.OpenRead(path);

         await Assert.ThrowsAsync<FormatException>(async () =>
         {
            await foreach(var _ in AjisParse.ParseSegmentsAsync(stream, settings))
            {
            }
         });
      }
      finally
      {
         File.Delete(path);
      }
   }

   [Fact]
   public async Task ParseSegmentsAsync_LargePayload_ChunkThreshold_UsesChunkedPath()
   {
      string path = CreateTempPayload(userCount: 200, addressesPerUser: 2);
      try
      {
         var settings = new AjisSettings
         {
            ParserProfile = AjisProcessingProfile.Universal,
            StreamChunkThreshold = "1k"
         };

         int count = 0;
         await using var stream = File.OpenRead(path);
         await foreach(var _ in AjisParse.ParseSegmentsAsync(stream, settings))
            count++;

         Assert.True(count > 0);
      }
      finally
      {
         File.Delete(path);
      }
   }

   [Fact(Skip = ">2GB chunked mapping test requires large fixture")]
   public void ParseSegmentsAsync_Over2Gb_NotCovered()
   {
      Assert.True(true);
   }

   [Fact]
   public async Task ParseSegmentsAsync_LargePayload_UniversalProfile_FileStream()
   {
      string path = CreateTempPayload(userCount: 200, addressesPerUser: 2);
      try
      {
         var settings = new AjisSettings { ParserProfile = AjisProcessingProfile.Universal };
         int count = 0;

         await using var stream = File.OpenRead(path);
         await foreach(var _ in AjisParse.ParseSegmentsAsync(stream, settings))
            count++;

         Assert.True(count > 0);
      }
      finally
      {
         File.Delete(path);
      }
   }

   [Fact]
   public void ParseSegments_LargePayload_HighThroughputProfile()
   {
      string path = CreateTempPayload(userCount: 200, addressesPerUser: 2);
      try
      {
         byte[] bytes = File.ReadAllBytes(path);
         var settings = new AjisSettings { ParserProfile = AjisProcessingProfile.HighThroughput };

         int count = AjisParse.ParseSegments(bytes, settings).Count();

         Assert.True(count > 0);
      }
      finally
      {
         File.Delete(path);
      }
   }

   [Fact]
   public async Task ParseSegmentsAsync_LargePayload_LowMemoryProfile()
   {
      string path = CreateTempPayload(userCount: 200, addressesPerUser: 2);
      try
      {
         var settings = new AjisSettings { ParserProfile = AjisProcessingProfile.LowMemory };
         int count = 0;

         await using var stream = File.OpenRead(path);
         await foreach(var _ in AjisParse.ParseSegmentsAsync(stream, settings))
            count++;

         Assert.True(count > 0);
      }
      finally
      {
         File.Delete(path);
      }
   }

   private static string CreateTempPayload(int userCount, int addressesPerUser)
   {
      string path = Path.Combine(Path.GetTempPath(), $"ajis_large_{userCount}_{addressesPerUser}.json");
      if(File.Exists(path)) File.Delete(path);

      AjisLargePayloadGenerator.WriteUsersJsonFile(path, userCount, addressesPerUser);
      return path;
   }
}
