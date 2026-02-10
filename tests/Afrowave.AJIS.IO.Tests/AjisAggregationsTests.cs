using Afrowave.AJIS.IO;
using Xunit;

namespace Afrowave.AJIS.IO.Tests;

/// <summary>
/// Tests for AJIS aggregation functions.
/// </summary>
public class AjisAggregationsTests : IDisposable
{
    private const string TestFile = "test_aggregations.ajis";

    public AjisAggregationsTests()
    {
        // Clean up before each test
        if (File.Exists(TestFile))
            File.Delete(TestFile);
    }

    public void Dispose()
    {
        // Clean up after each test
        if (File.Exists(TestFile))
            File.Delete(TestFile);
    }

    // ===== COUNT TESTS =====

    [Fact]
    public void Count_EmptyFile_ReturnsZero()
    {
        // Arrange
        AjisFile.Create(TestFile, new List<TestProduct>());

        // Act
        var count = AjisQuery.FromFile<TestProduct>(TestFile).Count();

        // Assert
        Assert.Equal(0, count);
    }

    [Fact]
    public void Count_WithElements_ReturnsCorrectCount()
    {
        // Arrange
        var products = new List<TestProduct>
        {
            new TestProduct { Id = 1, Name = "Product1", Price = 10.0m },
            new TestProduct { Id = 2, Name = "Product2", Price = 20.0m },
            new TestProduct { Id = 3, Name = "Product3", Price = 30.0m }
        };
        AjisFile.Create(TestFile, products);

        // Act
        var count = AjisQuery.FromFile<TestProduct>(TestFile).Count();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void Count_WithPredicate_ReturnsFilteredCount()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var count = AjisQuery.FromFile<TestProduct>(TestFile)
            .Count(p => p.Price > 15.0m);

        // Assert
        Assert.Equal(2, count); // Product2 and Product3
    }

    // ===== ANY TESTS =====

    [Fact]
    public void Any_EmptyFile_ReturnsFalse()
    {
        // Arrange
        AjisFile.Create(TestFile, new List<TestProduct>());

        // Act
        var result = AjisQuery.FromFile<TestProduct>(TestFile).Any();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Any_WithElements_ReturnsTrue()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var result = AjisQuery.FromFile<TestProduct>(TestFile).Any();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Any_WithPredicate_MatchFound_ReturnsTrue()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var result = AjisQuery.FromFile<TestProduct>(TestFile)
            .Any(p => p.Price > 25.0m);

        // Assert
        Assert.True(result); // Product3 has price 30
    }

    [Fact]
    public void Any_WithPredicate_NoMatch_ReturnsFalse()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var result = AjisQuery.FromFile<TestProduct>(TestFile)
            .Any(p => p.Price > 100.0m);

        // Assert
        Assert.False(result);
    }

    // ===== ALL TESTS =====

    [Fact]
    public void All_AllMatch_ReturnsTrue()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var result = AjisQuery.FromFile<TestProduct>(TestFile)
            .All(p => p.Price > 0);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void All_NotAllMatch_ReturnsFalse()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var result = AjisQuery.FromFile<TestProduct>(TestFile)
            .All(p => p.Price > 15.0m);

        // Assert
        Assert.False(result); // Product1 has price 10
    }

    // ===== SUM TESTS =====

    [Fact]
    public void Sum_Decimal_ReturnsCorrectSum()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var sum = AjisQuery.FromFile<TestProduct>(TestFile)
            .Sum(p => p.Price);

        // Assert
        Assert.Equal(60.0m, sum); // 10 + 20 + 30
    }

    [Fact]
    public void Sum_Int_ReturnsCorrectSum()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var sum = AjisQuery.FromFile<TestProduct>(TestFile)
            .Sum(p => p.Stock);

        // Assert
        Assert.Equal(60, sum); // 10 + 20 + 30
    }

    // ===== AVERAGE TESTS =====

    [Fact]
    public void Average_Decimal_ReturnsCorrectAverage()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var avg = AjisQuery.FromFile<TestProduct>(TestFile)
            .Average(p => (double)p.Price);

        // Assert
        Assert.Equal(20.0, avg); // (10 + 20 + 30) / 3
    }

    [Fact]
    public void Average_Int_ReturnsCorrectAverage()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var avg = AjisQuery.FromFile<TestProduct>(TestFile)
            .Average(p => p.Stock);

        // Assert
        Assert.Equal(20.0, avg); // (10 + 20 + 30) / 3
    }

    // ===== MIN/MAX TESTS =====

    [Fact]
    public void Min_ReturnsMinimumValue()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var min = AjisQuery.FromFile<TestProduct>(TestFile)
            .Min(p => p.Price);

        // Assert
        Assert.Equal(10.0m, min);
    }

    [Fact]
    public void Max_ReturnsMaximumValue()
    {
        // Arrange
        var products = CreateTestProducts();
        AjisFile.Create(TestFile, products);

        // Act
        var max = AjisQuery.FromFile<TestProduct>(TestFile)
            .Max(p => p.Price);

        // Assert
        Assert.Equal(30.0m, max);
    }

    // ===== DISTINCT TESTS =====

    [Fact]
    public void Distinct_RemovesDuplicates()
    {
        // Arrange
        var products = new List<TestProduct>
        {
            new TestProduct { Id = 1, Name = "ProductA", Price = 10.0m },
            new TestProduct { Id = 2, Name = "ProductB", Price = 20.0m },
            new TestProduct { Id = 3, Name = "ProductA", Price = 10.0m }, // Duplicate  
            new TestProduct { Id = 4, Name = "ProductB", Price = 20.0m }  // Duplicate
        };
        AjisFile.Create(TestFile, products);

        // Act - Use DistinctBy instead of Select + Distinct
        var distinct = AjisQuery.FromFile<TestProduct>(TestFile)
            .DistinctBy(p => p.Name)
            .ToList();

        // Assert
        Assert.Equal(2, distinct.Count);
        Assert.Contains(distinct, p => p.Name == "ProductA");
        Assert.Contains(distinct, p => p.Name == "ProductB");
    }

    [Fact]
    public void DistinctBy_RemovesDuplicatesByKey()
    {
        // Arrange
        var products = new List<TestProduct>
        {
            new TestProduct { Id = 1, Name = "ProductA", Price = 10.0m },
            new TestProduct { Id = 2, Name = "ProductB", Price = 20.0m },
            new TestProduct { Id = 3, Name = "ProductA", Price = 15.0m }, // Same name, different price
            new TestProduct { Id = 4, Name = "ProductB", Price = 25.0m }  // Same name, different price
        };
        AjisFile.Create(TestFile, products);

        // Act
        var distinct = AjisQuery.FromFile<TestProduct>(TestFile)
            .DistinctBy(p => p.Name)
            .ToList();

        // Assert
        Assert.Equal(2, distinct.Count);
        Assert.Contains(distinct, p => p.Name == "ProductA");
        Assert.Contains(distinct, p => p.Name == "ProductB");
    }

    // ===== APPENDMANY TESTS =====

    [Fact]
    public void AppendMany_AddsMultipleItems()
    {
        // Arrange
        var initial = new List<TestProduct>
        {
            new TestProduct { Id = 1, Name = "Product1", Price = 10.0m }
        };
        AjisFile.Create(TestFile, initial);

        var toAdd = new List<TestProduct>
        {
            new TestProduct { Id = 2, Name = "Product2", Price = 20.0m },
            new TestProduct { Id = 3, Name = "Product3", Price = 30.0m }
        };

        // Act
        AjisFile.AppendMany(TestFile, toAdd);

        // Assert
        var all = AjisFile.ReadAll<TestProduct>(TestFile);
        Assert.Equal(3, all.Count);
        Assert.Contains(all, p => p.Id == 1);
        Assert.Contains(all, p => p.Id == 2);
        Assert.Contains(all, p => p.Id == 3);
    }

    [Fact]
    public void AppendMany_ToNewFile_CreatesFile()
    {
        // Arrange
        var products = CreateTestProducts();

        // Act
        AjisFile.AppendMany(TestFile, products);

        // Assert
        Assert.True(File.Exists(TestFile));
        var all = AjisFile.ReadAll<TestProduct>(TestFile);
        Assert.Equal(3, all.Count);
    }

    [Fact]
    public async Task AppendManyAsync_AddsMultipleItems()
    {
        // Arrange
        var initial = new List<TestProduct>
        {
            new TestProduct { Id = 1, Name = "Product1", Price = 10.0m }
        };
        AjisFile.Create(TestFile, initial);

        var toAdd = new List<TestProduct>
        {
            new TestProduct { Id = 2, Name = "Product2", Price = 20.0m },
            new TestProduct { Id = 3, Name = "Product3", Price = 30.0m }
        };

        // Act
        await AjisFile.AppendManyAsync(TestFile, toAdd);

        // Assert
        var all = AjisFile.ReadAll<TestProduct>(TestFile);
        Assert.Equal(3, all.Count);
    }

    // ===== COMPLEX QUERIES =====

    [Fact]
    public void ComplexQuery_WithMultipleAggregations()
    {
        // Arrange
        var products = new List<TestProduct>
        {
            new TestProduct { Id = 1, Name = "Product1", Price = 10.0m, Stock = 100 },
            new TestProduct { Id = 2, Name = "Product2", Price = 20.0m, Stock = 50 },
            new TestProduct { Id = 3, Name = "Product3", Price = 30.0m, Stock = 75 },
            new TestProduct { Id = 4, Name = "Product4", Price = 15.0m, Stock = 200 }
        };
        AjisFile.Create(TestFile, products);

        // Act & Assert
        var count = AjisQuery.FromFile<TestProduct>(TestFile).Count();
        Assert.Equal(4, count);

        var avgPrice = AjisQuery.FromFile<TestProduct>(TestFile).Average(p => (double)p.Price);
        Assert.Equal(18.75, avgPrice); // (10+20+30+15)/4

        var totalStock = AjisQuery.FromFile<TestProduct>(TestFile).Sum(p => p.Stock);
        Assert.Equal(425, totalStock); // 100+50+75+200

        var maxPrice = AjisQuery.FromFile<TestProduct>(TestFile).Max(p => p.Price);
        Assert.Equal(30.0m, maxPrice);

        var anyExpensive = AjisQuery.FromFile<TestProduct>(TestFile).Any(p => p.Price > 25.0m);
        Assert.True(anyExpensive);
    }

    // ===== HELPER METHODS =====

    private static List<TestProduct> CreateTestProducts()
    {
        return new List<TestProduct>
        {
            new TestProduct { Id = 1, Name = "Product1", Price = 10.0m, Stock = 10 },
            new TestProduct { Id = 2, Name = "Product2", Price = 20.0m, Stock = 20 },
            new TestProduct { Id = 3, Name = "Product3", Price = 30.0m, Stock = 30 }
        };
    }
}

/// <summary>
/// Test model for aggregation tests.
/// </summary>
public class TestProduct
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
}
