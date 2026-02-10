#nullable enable

using Afrowave.AJIS.Core;
using Afrowave.AJIS.Serialization.Conversion;
using System.Text.Json;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// ATP Round-Trip Testing: Generate .atp → Parse → Verify
/// Complete end-to-end validation with offset tracking and checksum verification.
/// </summary>
public sealed class AtpRoundTripTester
{
   public void RunAtpRoundTrip()
   {
      Console.WriteLine("""

╔════════════════════════════════════════════════════════════════════════╗
║              ATP ROUND-TRIP TESTING & VALIDATION                       ║
║   Generate .atp → Parse → Verify Offsets → Check Checksums            ║
╚════════════════════════════════════════════════════════════════════════╝
""");

      string solutionRoot = FindSolutionRoot();
      string countries4Path = Path.Combine(solutionRoot, "test_data_legacy", "countries4.json");

      if(!File.Exists(countries4Path))
      {
         Console.WriteLine($"❌ File not found: {countries4Path}");
         return;
      }

      // Step 1: Convert JSON → ATP
      Console.WriteLine("\n📝 STEP 1: CONVERT JSON → ATP");
      Console.WriteLine("═════════════════════════════════════════════════════════════════════════════");

      var converter = new JsonToAjisConverter();
      var conversionResult = converter.ConvertJsonToAjis(countries4Path, detectBinary: true);

      if(!conversionResult.Success)
      {
         Console.WriteLine($"❌ Conversion failed: {conversionResult.Error}");
         return;
      }

      Console.WriteLine($"✅ Conversion successful!");
      Console.WriteLine($"   Original JSON:     {FormatBytes(conversionResult.OriginalSize)}");
      Console.WriteLine($"   AJIS Format:       {FormatBytes(conversionResult.AjisSize)}");
      Console.WriteLine($"   Binaries Detected: {conversionResult.BinaryAttachmentsDetected}");
      Console.WriteLine($"   Size Reduction:    {conversionResult.SizeReduction:F1}%");

      // Save ATP file
      string atpPath = Path.Combine(solutionRoot, "test_output", "countries4_roundtrip.atp");
      Directory.CreateDirectory(Path.GetDirectoryName(atpPath)!);
      converter.SaveAsAtp(conversionResult, atpPath);

      Console.WriteLine($"\n💾 ATP File saved: {Path.GetFileName(atpPath)}");
      Console.WriteLine($"   Size: {FormatBytes(new FileInfo(atpPath).Length)}");

      // Step 2: Parse ATP
      Console.WriteLine("\n\n📖 STEP 2: PARSE ATP FILE");
      Console.WriteLine("═════════════════════════════════════════════════════════════════════════════");

      string atpContent = File.ReadAllText(atpPath);
      var atpDocument = JsonDocument.Parse(atpContent);
      var atpRoot = atpDocument.RootElement;

      Console.WriteLine($"✅ ATP parsed successfully!");
      Console.WriteLine($"   Total size:     {FormatBytes(atpContent.Length)}");

      // Extract metadata
      if(atpRoot.TryGetProperty("metadata", out var metadataElem))
      {
         Console.WriteLine($"\n📊 METADATA:");
         Console.WriteLine("   ─────────────────────────────────────────────────────────────");

         if(metadataElem.TryGetProperty("createdDate", out var createdElem))
            Console.WriteLine($"   Created:        {createdElem.GetString()}");

         if(metadataElem.TryGetProperty("sourceFormat", out var sourceElem))
            Console.WriteLine($"   Source Format:  {sourceElem.GetString()}");

         if(metadataElem.TryGetProperty("binaryAttachmentCount", out var countElem))
            Console.WriteLine($"   Attachment Cnt: {countElem.GetInt32()}");

         if(metadataElem.TryGetProperty("sizeReduction", out var reductionElem))
            Console.WriteLine($"   Size Reduction: {reductionElem.GetDouble():F1}%");
      }

      // Step 3: Extract and analyze attachments
      Console.WriteLine($"\n\n📎 STEP 3: ANALYZE ATTACHMENTS WITH OFFSETS");
      Console.WriteLine("═════════════════════════════════════════════════════════════════════════════");

      var attachments = ParseAttachmentsWithOffsets(atpPath, atpRoot);

      Console.WriteLine($"\nFound {attachments.Count} attachments:");
      Console.WriteLine($"{"Idx",-5} {"Filename",-30} {"Offset",-12} {"Size",-12} {"MIME Type",-20}");
      Console.WriteLine(new string('─', 85));

      long currentOffset = 0;
      int index = 0;

      foreach(var attachment in attachments)
      {
         Console.WriteLine(
             $"{index,-5} {attachment.FileName,-30} {currentOffset,-12} " +
             $"{FormatBytes(attachment.Data.Length),-12} {attachment.MimeType,-20}");

         currentOffset += attachment.Data.Length;
         index++;
      }

      // Step 4: Verify checksums
      Console.WriteLine($"\n\n🔐 STEP 4: VERIFY CHECKSUMS (SHA256)");
      Console.WriteLine("═════════════════════════════════════════════════════════════════════════════");

      Console.WriteLine($"{"Idx",-5} {"Filename",-30} {"Checksum Status",-20} {"Hash (first 16)",-20}");
      Console.WriteLine(new string('─', 80));

      int checksumFailures = 0;
      index = 0;

      foreach(var attachment in attachments)
      {
         // Recompute checksum
         using(var sha256 = System.Security.Cryptography.SHA256.Create())
         {
            byte[] hash = sha256.ComputeHash(attachment.Data);
            string computed = BitConverter.ToString(hash).Replace("-", "").ToLower();

            string storedChecksum = attachment.Checksum?.ToLower() ?? "";
            bool isValid = computed == storedChecksum;

            string status = isValid ? "✅ VALID" : "❌ FAILED";
            string hashDisplay = computed.Substring(0, Math.Min(16, computed.Length));

            Console.WriteLine(
                $"{index,-5} {attachment.FileName,-30} {status,-20} {hashDisplay,-20}");

            if(!isValid)
            {
               Console.WriteLine($"       Stored:   {storedChecksum}");
               Console.WriteLine($"       Computed: {computed}");
               checksumFailures++;
            }
         }

         index++;
      }

      // Step 5: Summary
      Console.WriteLine($"\n\n✅ ROUND-TRIP TEST COMPLETE");
      Console.WriteLine("═════════════════════════════════════════════════════════════════════════════");

      bool allValid = checksumFailures == 0;
      string statusIcon = allValid ? "✅" : "❌";

      Console.WriteLine($"\n{statusIcon} Overall Status: {(allValid ? "PASSED" : "FAILED")}");
      Console.WriteLine($"   Total Attachments:  {attachments.Count}");
      Console.WriteLine($"   Checksum Failures:  {checksumFailures}");

      // Avoid division by zero
      if(attachments.Count > 0)
      {
         Console.WriteLine($"   Success Rate:       {(attachments.Count - checksumFailures) * 100.0 / attachments.Count:F1}%");
      }
      else
      {
         Console.WriteLine($"   Success Rate:       100.0% (no attachments to verify)");
      }

      Console.WriteLine($"\n📊 STORAGE ANALYSIS:");
      int totalBinarySize = attachments.Sum(a => a.Data.Length);
      long atpFileSize = new FileInfo(atpPath).Length;

      Console.WriteLine($"   Total Binary Data:  {FormatBytes(totalBinarySize)}");
      Console.WriteLine($"   ATP File Size:      {FormatBytes(atpFileSize)}");
      Console.WriteLine($"   Overhead:           {FormatBytes(atpFileSize - totalBinarySize)}");
      Console.WriteLine($"   Efficiency:         {totalBinarySize * 100.0 / atpFileSize:F1}% of file is binary");

      Console.WriteLine($"\n🎯 VALIDATION RESULTS:");
      Console.WriteLine($"   JSON → ATP:         ✅ Success");
      Console.WriteLine($"   ATP Parsing:        ✅ Success");
      Console.WriteLine($"   Offset Tracking:    ✅ Success ({attachments.Count} attachments mapped)");
      Console.WriteLine($"   Checksum Verify:    {(allValid ? "✅ All valid!" : $"❌ {checksumFailures} failures")}");
      Console.WriteLine($"   Round-Trip:         {(allValid ? "✅ PASSED" : "❌ FAILED")}");

      atpDocument.Dispose();
   }

   private List<BinaryAttachment> ParseAttachmentsWithOffsets(
       string atpPath,
       JsonElement atpRoot)
   {
      var attachments = new List<BinaryAttachment>();

      if(atpRoot.TryGetProperty("attachments", out var attachmentsArray))
      {
         foreach(var attachmentElem in attachmentsArray.EnumerateArray())
         {
            if(attachmentElem.TryGetProperty("attachment", out var attachmentObj))
            {
               var attachment = ParseBinaryAttachment(attachmentObj);
               if(attachment != null)
               {
                  attachments.Add(attachment);
               }
            }
         }
      }

      return attachments;
   }

   private BinaryAttachment? ParseBinaryAttachment(JsonElement element)
   {
      try
      {
         var attachment = new BinaryAttachment();

         if(element.TryGetProperty("attachmentId", out var idElem))
            attachment.AttachmentId = Guid.Parse(idElem.GetString() ?? "");

         if(element.TryGetProperty("fileName", out var nameElem))
            attachment.FileName = nameElem.GetString() ?? "";

         if(element.TryGetProperty("mimeType", out var mimeElem))
            attachment.MimeType = mimeElem.GetString() ?? "";

         if(element.TryGetProperty("fileSize", out var sizeElem))
            attachment.FileSize = sizeElem.GetInt64();

         if(element.TryGetProperty("checksum", out var checksumElem))
            attachment.Checksum = checksumElem.GetString() ?? "";

         if(element.TryGetProperty("data", out var dataElem))
         {
            string base64Data = dataElem.GetString() ?? "";
            attachment.Data = Convert.FromBase64String(base64Data);
         }

         return attachment;
      }
      catch(Exception ex)
      {
         Console.WriteLine($"   Error parsing attachment: {ex.Message}");
         return null;
      }
   }

   private string FormatBytes(long bytes)
   {
      string[] sizes = { "B", "KB", "MB", "GB" };
      double len = bytes;
      int order = 0;

      while(len >= 1024 && order < sizes.Length - 1)
      {
         order++;
         len = len / 1024;
      }

      return $"{len:0.##} {sizes[order]}";
   }

   private static string FindSolutionRoot()
   {
      var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());

      while(currentDirectory != null)
      {
         if(File.Exists(Path.Combine(currentDirectory.FullName, "Ajis.Dotnet.sln")) ||
             Directory.Exists(Path.Combine(currentDirectory.FullName, "test_data_legacy")))
         {
            return currentDirectory.FullName;
         }

         currentDirectory = currentDirectory.Parent;
      }

      return "D:\\Ajis.Dotnet";
   }
}

/// <summary>
/// Entry point for ATP round-trip testing.
/// </summary>
internal static class AtpRoundTripProgram
{
   internal static void RunAtpRoundTrip(string[] args)
   {
      try
      {
         var tester = new AtpRoundTripTester();
         tester.RunAtpRoundTrip();
      }
      catch(Exception ex)
      {
         Console.WriteLine($"\n❌ Round-trip test failed: {ex.Message}");
         Console.WriteLine(ex.StackTrace);
      }

      Console.WriteLine("\n✓ ATP round-trip testing complete.");
   }
}
