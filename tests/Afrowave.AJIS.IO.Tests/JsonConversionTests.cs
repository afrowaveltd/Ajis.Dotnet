#nullable enable

using Xunit;

namespace Afrowave.AJIS.IO.Tests;

public class JsonConversionTests : IDisposable
{
   private readonly string _testDirectory = Path.Combine(Path.GetTempPath(), "JsonConversionTests");

   public JsonConversionTests()
   {
      if(Directory.Exists(_testDirectory))
         Directory.Delete(_testDirectory, recursive: true);
      Directory.CreateDirectory(_testDirectory);
   }

   public void Dispose()
   {
      if(Directory.Exists(_testDirectory))
         Directory.Delete(_testDirectory, recursive: true);
   }

   [Fact]
   public async Task AjisContext_ConvertJsonToAjis_ConvertsSuccessfully()
   {
      // Arrange
      string jsonFilePath = Path.Combine(_testDirectory, "input.json");
      string ajisFilePath = Path.Combine(_testDirectory, "output.ajis");

      // Create sample JSON file
      string jsonContent = """
        [
            { "Id": 1, "Name": "Alice", "Age": 25 },
            { "Id": 2, "Name": "Bob", "Age": 30 },
            { "Id": 3, "Name": "Charlie", "Age": 35 }
        ]
        """;
      await File.WriteAllTextAsync(jsonFilePath, jsonContent);

      // Act
      using var context = new AjisContext();
      await context.ConvertJsonToAjis(jsonFilePath, ajisFilePath, new JsonConversionOptions
      {
         AutoDetectBinary = false
      });

      // Assert
      Assert.True(File.Exists(ajisFilePath));
      string content = await File.ReadAllTextAsync(ajisFilePath);
      Assert.Contains("Alice", content);
      Assert.Contains("Bob", content);
   }

   [Fact]
   public async Task AjisContext_ConvertJsonToAtp_ConvertsBinary()
   {
      // Arrange
      string jsonFilePath = Path.Combine(_testDirectory, "input_with_binary.json");
      string atpFilePath = Path.Combine(_testDirectory, "output.atp");

      // Create sample JSON with base64 binary data
      byte[] binaryData = System.Text.Encoding.UTF8.GetBytes("Hello, World!");
      string base64Data = Convert.ToBase64String(binaryData);

      string jsonContent = "[{\"Id\":1,\"Name\":\"Test\",\"FileData\":\"" + base64Data + "\"}]";
      await File.WriteAllTextAsync(jsonFilePath, jsonContent);

      // Act
      using var context = new AjisContext();
      await context.ConvertJsonToAtp(jsonFilePath, atpFilePath);

      // Assert
      Assert.True(File.Exists(atpFilePath));
      // ATP file should exist and be smaller than JSON (no base64 overhead)
      long jsonSize = new FileInfo(jsonFilePath).Length;
      long atpSize = new FileInfo(atpFilePath).Length;
      Assert.True(atpSize <= jsonSize);
   }

   [Fact]
   public async Task AjisContext_ConvertJsonToAjis_WithConfiguration()
   {
      // Arrange
      string jsonFilePath = Path.Combine(_testDirectory, "input.json");
      string ajisFilePath = Path.Combine(_testDirectory, "output.ajis");

      string jsonContent = """[{ "Id": 1, "Name": "Test" }]""";
      await File.WriteAllTextAsync(jsonFilePath, jsonContent);

      // Act
      using var context = new AjisContext();
      var options = new JsonConversionOptions
      {
         AutoDetectBinary = false,
         Compression = false
      };
      await context.ConvertJsonToAjis(jsonFilePath, ajisFilePath, options);

      // Assert
      Assert.True(File.Exists(ajisFilePath));
   }

   [Fact]
   public async Task AjisContext_ConvertJsonToAjis_CreatesDirectory()
   {
      // Arrange
      string jsonFilePath = Path.Combine(_testDirectory, "input.json");
      string outputDir = Path.Combine(_testDirectory, "subdir");
      string ajisFilePath = Path.Combine(outputDir, "output.ajis");

      string jsonContent = """[{ "Id": 1, "Name": "Test" }]""";
      await File.WriteAllTextAsync(jsonFilePath, jsonContent);

      // Act
      using var context = new AjisContext();
      await context.ConvertJsonToAjis(jsonFilePath, ajisFilePath);

      // Assert
      Assert.True(File.Exists(ajisFilePath));
   }

   [Fact]
   public async Task JsonToAjis_LargeFile_HandlesGracefully()
   {
      // Arrange
      string jsonFilePath = Path.Combine(_testDirectory, "large.json");
      string ajisFilePath = Path.Combine(_testDirectory, "large.ajis");

      // Create JSON with 10000 items
      var items = Enumerable.Range(1, 10000)
          .Select(i => $"{{\"Id\":{i},\"Name\":\"Item{i}\"}}");
      string jsonContent = $"[{string.Join(",", items)}]";
      await File.WriteAllTextAsync(jsonFilePath, jsonContent);

      // Act
      using var context = new AjisContext();
      await context.ConvertJsonToAjis(jsonFilePath, ajisFilePath);

      // Assert
      Assert.True(File.Exists(ajisFilePath));
      var result = AjisFile.ReadAll<LargeItem>(ajisFilePath);
      Assert.Equal(10000, result.Count);
   }

   [Fact]
   public async Task JsonToAjis_PreservesDataTypes()
   {
      // Arrange
      string jsonFilePath = Path.Combine(_testDirectory, "types.json");
      string ajisFilePath = Path.Combine(_testDirectory, "types.ajis");

      string jsonContent = """
        [
            {
                "Id": 1,
                "Name": "Test",
                "Age": 25,
                "IsActive": true,
                "Score": 95.5,
                "CreatedDate": "2024-01-15T10:30:00Z"
            }
        ]
        """;
      await File.WriteAllTextAsync(jsonFilePath, jsonContent);

      // Act
      using var context = new AjisContext();
      await context.ConvertJsonToAjis(jsonFilePath, ajisFilePath);

      // Assert
      var items = AjisFile.ReadAll<DataTypeTest>(ajisFilePath);
      Assert.Single(items);
      Assert.Equal(1, items[0].Id);
      Assert.Equal("Test", items[0].Name);
      Assert.Equal(25, items[0].Age);
      Assert.True(items[0].IsActive);
      Assert.Equal(95.5, items[0].Score);
   }

   [Fact]
   public async Task JsonToAjis_NestedObjects_Preserved()
   {
      // Arrange
      string jsonFilePath = Path.Combine(_testDirectory, "nested.json");
      string ajisFilePath = Path.Combine(_testDirectory, "nested.ajis");

      string jsonContent = """
        [
            {
                "Id": 1,
                "Name": "Test",
                "Address": {
                    "Street": "Main St",
                    "City": "Prague",
                    "Zip": "11000"
                }
            }
        ]
        """;
      await File.WriteAllTextAsync(jsonFilePath, jsonContent);

      // Act
      using var context = new AjisContext();
      await context.ConvertJsonToAjis(jsonFilePath, ajisFilePath);

      // Assert
      var items = AjisFile.ReadAll<NestedTest>(ajisFilePath);
      Assert.Single(items);
      Assert.NotNull(items[0].Address);
      Assert.Equal("Prague", items[0].Address?.City);
   }

   private class LargeItem
   {
      public int Id { get; set; }
      public string Name { get; set; } = "";
   }

   private class DataTypeTest
   {
      public int Id { get; set; }
      public string Name { get; set; } = "";
      public int Age { get; set; }
      public bool IsActive { get; set; }
      public double Score { get; set; }
      public DateTime CreatedDate { get; set; }
   }

   private class NestedTest
   {
      public int Id { get; set; }
      public string Name { get; set; } = "";
      public Address? Address { get; set; }
   }

   private class Address
   {
      public string Street { get; set; } = "";
      public string City { get; set; } = "";
      public string Zip { get; set; } = "";
   }
}