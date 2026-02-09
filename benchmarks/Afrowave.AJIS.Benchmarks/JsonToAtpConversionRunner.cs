#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Afrowave.AJIS.Serialization.Conversion;

namespace Afrowave.AJIS.Benchmarks.Conversion;

/// <summary>
/// Demonstrates JSON → AJIS → .atp conversion with ATP binary attachment detection.
/// </summary>
public sealed class JsonToAtpConversionRunner
{
    public void RunConversion()
    {
        Console.WriteLine("""
╔════════════════════════════════════════════════════════════════════════╗
║              JSON → AJIS → .ATP CONVERSION DEMO                        ║
║        Automatic Binary Detection & ATP File Generation                ║
╚════════════════════════════════════════════════════════════════════════╝
""");

        var solutionRoot = FindSolutionRoot();
        var legacyDataPath = Path.Combine(solutionRoot, "test_data_legacy");

        if (!Directory.Exists(legacyDataPath))
        {
            Console.WriteLine($"❌ Legacy data directory not found: {legacyDataPath}");
            return;
        }

        var converter = new JsonToAjisConverter();
        var conversionResults = new List<AjisConversionResult>();

        // Convert each JSON file
        var jsonFiles = Directory.GetFiles(legacyDataPath, "*.json").OrderBy(f => f).ToList();

        foreach (var jsonFile in jsonFiles)
        {
            Console.WriteLine($"\n\n📄 Processing: {Path.GetFileName(jsonFile)}");
            Console.WriteLine("═════════════════════════════════════════════════════════════════════════════");

            var result = converter.ConvertJsonToAjis(jsonFile, detectBinary: true);
            conversionResults.Add(result);

            if (result.Success)
            {
                Console.WriteLine($"✅ Conversion successful!");
                Console.WriteLine($"   Original JSON:  {FormatBytes(result.OriginalSize)}");
                Console.WriteLine($"   AJIS Format:    {FormatBytes(result.AjisSize)}");
                Console.WriteLine($"   Size Reduction: {result.SizeReduction:F1}%");

                if (result.BinaryAttachmentsDetected > 0)
                {
                    Console.WriteLine($"   ✨ Binary Attachments Detected: {result.BinaryAttachmentsDetected}");
                    
                    foreach (var (path, attachment) in result.DetectedAttachments ?? new())
                    {
                        Console.WriteLine($"      • {path}: {attachment.FileName} ({FormatBytes(attachment.Data.Length)})");
                    }

                    // Save as .atp
                    var atpOutputPath = Path.Combine(
                        solutionRoot,
                        "converted_atp",
                        Path.GetFileNameWithoutExtension(jsonFile) + ".atp");

                    Directory.CreateDirectory(Path.GetDirectoryName(atpOutputPath)!);
                    converter.SaveAsAtp(result, atpOutputPath);

                    Console.WriteLine($"\n   💾 ATP File: {Path.GetFileName(atpOutputPath)}");
                    Console.WriteLine($"      Size: {FormatBytes(new FileInfo(atpOutputPath).Length)}");
                }
            }
            else
            {
                Console.WriteLine($"❌ Conversion failed: {result.Error}");
            }
        }

        // Summary
        PrintConversionSummary(conversionResults);
    }

    private void PrintConversionSummary(List<AjisConversionResult> results)
    {
        Console.WriteLine("""

╔════════════════════════════════════════════════════════════════════════╗
║                      CONVERSION SUMMARY                                ║
╚════════════════════════════════════════════════════════════════════════╝
""");

        Console.WriteLine("\n📊 CONVERSION STATISTICS:");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");

        var successful = results.Count(r => r.Success);
        var withBinary = results.Count(r => r.BinaryAttachmentsDetected > 0);
        var totalOriginal = results.Sum(r => r.OriginalSize);
        var totalAjis = results.Sum(r => r.AjisSize);
        var totalBinary = results.SelectMany(r => r.DetectedAttachments ?? new())
            .Sum(x => x.Attachment.Data.Length);

        Console.WriteLine($"Files Processed:         {results.Count}");
        Console.WriteLine($"Successful Conversions:  {successful}/{results.Count}");
        Console.WriteLine($"Files with ATP:          {withBinary}/{results.Count}");
        Console.WriteLine($"Total Binary Detected:   {results.Sum(r => r.BinaryAttachmentsDetected)} attachments");

        Console.WriteLine($"\n💾 STORAGE ANALYSIS:");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine($"Original JSON Total:     {FormatBytes(totalOriginal)}");
        Console.WriteLine($"AJIS Format Total:       {FormatBytes(totalAjis)}");
        Console.WriteLine($"Binary Data Total:       {FormatBytes(totalBinary)}");
        Console.WriteLine($"Average Reduction:       {results.Average(r => r.SizeReduction):F1}%");

        Console.WriteLine($"\n🎯 ATP BENEFITS:");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        Console.WriteLine($"✅ Automatic detection:  Binary data identified automatically");
        Console.WriteLine($"✅ Type safety:          Structured BinaryAttachment objects");
        Console.WriteLine($"✅ Size efficiency:      {results.Average(r => r.SizeReduction):F1}% average reduction");
        Console.WriteLine($"✅ Atomic storage:       Single .atp file for complete document");
        Console.WriteLine($"✅ Database ready:       Can be stored in MongoDB/EF Core");
        Console.WriteLine($"✅ Integrity:            SHA256 checksums for all attachments");

        Console.WriteLine($"\n📁 OUTPUT FILES:");
        Console.WriteLine("─────────────────────────────────────────────────────────────────");
        
        var atpDir = Path.Combine(FindSolutionRoot(), "converted_atp");
        if (Directory.Exists(atpDir))
        {
            var atpFiles = Directory.GetFiles(atpDir, "*.atp");
            foreach (var file in atpFiles)
            {
                Console.WriteLine($"  • {Path.GetFileName(file),-30} ({FormatBytes(new FileInfo(file).Length)})");
            }
        }

        Console.WriteLine($"\n✓ JSON → AJIS → ATP conversion complete!");
    }

    private string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    private static string FindSolutionRoot()
    {
        var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
        
        while (currentDirectory != null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "Ajis.Dotnet.sln")) ||
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
/// Entry point for JSON to ATP conversion.
/// </summary>
internal static class JsonToAtpConversionProgram
{
    internal static void RunJsonToAtp(string[] args)
    {
        try
        {
            var runner = new JsonToAtpConversionRunner();
            runner.RunConversion();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Conversion failed: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }

        Console.WriteLine("\n✓ Conversion demo complete.");
    }
}
