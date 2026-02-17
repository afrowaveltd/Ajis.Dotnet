#nullable enable

using System.Text;

namespace Afrowave.AJIS.Core.Tests;

public sealed class AjisNumberParserTests
{
   // ===== M6 Performance: Span-Based Number Parsing Tests =====

   [Fact]
   public void TryParseDecimal_ParsesSimpleInteger()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("42");
      Assert.True(AjisNumberParser.TryParseDecimal(bytes, out decimal value));
      Assert.Equal(42m, value);
   }

   [Fact]
   public void TryParseDecimal_ParsesNegativeNumber()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("-123");
      Assert.True(AjisNumberParser.TryParseDecimal(bytes, out decimal value));
      Assert.Equal(-123m, value);
   }

   [Fact]
   public void TryParseDecimal_ParsesDecimal()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("3.14");
      Assert.True(AjisNumberParser.TryParseDecimal(bytes, out decimal value));
      Assert.Equal(3.14m, value);
   }

   [Fact]
   public void TryParseDecimal_ParsesSmallDecimal()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("0.001");
      Assert.True(AjisNumberParser.TryParseDecimal(bytes, out decimal value));
      Assert.Equal(0.001m, value);
   }

   [Fact]
   public void TryParseDecimal_ParsesScientificNotation()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("1.23e-4");
      Assert.True(AjisNumberParser.TryParseDecimal(bytes, out decimal value));
      Assert.True(value > 0.0001m && value < 0.00013m); // Approximate comparison
   }

   [Fact]
   public void TryParseDecimal_ParsesPositiveScientific()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("1.5e3");
      Assert.True(AjisNumberParser.TryParseDecimal(bytes, out decimal value));
      Assert.Equal(1500m, value);
   }

   [Fact]
   public void TryParseDecimal_ParsesZero()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("0");
      Assert.True(AjisNumberParser.TryParseDecimal(bytes, out decimal value));
      Assert.Equal(0m, value);
   }

   [Fact]
   public void TryParseDecimal_RejectsEmpty()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("");
      Assert.False(AjisNumberParser.TryParseDecimal(bytes, out _));
   }

   [Fact]
   public void TryParseDecimal_RejectsNonNumeric()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("abc");
      Assert.False(AjisNumberParser.TryParseDecimal(bytes, out _));
   }

   [Fact]
   public void TryParseDecimal_RejectsPartiallyNumeric()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("123abc");
      Assert.False(AjisNumberParser.TryParseDecimal(bytes, out _));
   }

   [Fact]
   public void TryParseInt64_ParsesInteger()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("9876543210");
      Assert.True(AjisNumberParser.TryParseInt64(bytes, out long value));
      Assert.Equal(9876543210L, value);
   }

   [Fact]
   public void TryParseInt64_RejectsDecimal()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("123.45");
      Assert.False(AjisNumberParser.TryParseInt64(bytes, out _));
   }

   [Fact]
   public void TryParseInt64_ParsesNegative()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("-999");
      Assert.True(AjisNumberParser.TryParseInt64(bytes, out long value));
      Assert.Equal(-999L, value);
   }

   [Fact]
   public void TryParseInt64_RejectsOverflow()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("99999999999999999999");
      Assert.False(AjisNumberParser.TryParseInt64(bytes, out _));
   }

   [Fact]
   public void TryParseInt32_ParsesSmallInteger()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("12345");
      Assert.True(AjisNumberParser.TryParseInt32(bytes, out int value));
      Assert.Equal(12345, value);
   }

   [Fact]
   public void TryParseInt32_RejectsOverflow()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("999999999999");
      Assert.False(AjisNumberParser.TryParseInt32(bytes, out _));
   }

   [Fact]
   public void TryParseDouble_ParsesNumber()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("3.14159");
      Assert.True(AjisNumberParser.TryParseDouble(bytes, out double value));
      Assert.True(Math.Abs(value - 3.14159) < 0.00001);
   }

   [Fact]
   public void TryParseDecimal_HandlesPlusSign()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("+42");
      Assert.True(AjisNumberParser.TryParseDecimal(bytes, out decimal value));
      Assert.Equal(42m, value);
   }

   [Fact]
   public void TryParseDecimal_HandlesManyDecimalPlaces()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("1.123456789012345");
      Assert.True(AjisNumberParser.TryParseDecimal(bytes, out decimal value));
      Assert.True(value > 1m && value < 2m);
   }

   [Fact]
   public void TryParseDecimal_HandlesLeadingZero()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("0.5");
      Assert.True(AjisNumberParser.TryParseDecimal(bytes, out decimal value));
      Assert.Equal(0.5m, value);
   }

   [Fact]
   public void TryParseDecimal_RejectsOnlyDecimalPoint()
   {
      byte[] bytes = Encoding.UTF8.GetBytes(".");
      Assert.False(AjisNumberParser.TryParseDecimal(bytes, out _));
   }

   [Fact]
   public void TryParseDecimal_RejectsInvalidScientific()
   {
      byte[] bytes = Encoding.UTF8.GetBytes("1.23e");
      Assert.False(AjisNumberParser.TryParseDecimal(bytes, out _));
   }
}