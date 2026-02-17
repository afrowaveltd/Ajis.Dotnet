#nullable enable

using Afrowave.AJIS.Streaming.Reader;

namespace Afrowave.AJIS.Core.Tests.Reader;

public sealed class AjisLexerTests
{
   [Fact]
   public void Lexer_TokenizesBasicJson()
   {
      AjisSpanReader reader = new AjisSpanReader("{\"a\":1}"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader);

      var left = lexer.NextToken();
      Assert.Equal(AjisTokenKind.LeftBrace, left.Kind);
      Assert.Equal(1, left.Line);
      Assert.Equal(1, left.Column);

      var name = lexer.NextToken();
      Assert.Equal(AjisTokenKind.String, name.Kind);

      var colon = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Colon, colon.Kind);

      var number = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Number, number.Kind);

      var right = lexer.NextToken();
      Assert.Equal(AjisTokenKind.RightBrace, right.Kind);

      var end = lexer.NextToken();
      Assert.Equal(AjisTokenKind.End, end.Kind);
   }

   [Fact]
   public void Lexer_TokenizesStringEscapes()
   {
      AjisSpanReader reader = new AjisSpanReader("\"a\\n\""u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.String, token.Kind);
      Assert.Equal("a\n", token.Text);
   }

   [Fact]
   public void Lexer_JsonMode_RejectsNewlines()
   {
      AjisSpanReader reader = new AjisSpanReader("\"a\n\""u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Json);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }

   [Fact]
   public void Lexer_AjisMode_SkipsLineComments()
   {
      AjisCommentOptions commentOptions = new global::Afrowave.AJIS.Core.AjisCommentOptions
      {
         AllowLineComments = true
      };

      AjisSpanReader reader = new AjisSpanReader("// c\ntrue"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, commentOptions: commentOptions, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Ajis);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.True, token.Kind);
   }

   [Fact]
   public void Lexer_AjisMode_ReadsTypedLiteralAsNumber()
   {
      AjisSpanReader reader = new AjisSpanReader("T1707489221"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Ajis);

      var token = lexer.NextToken();

      Assert.Equal(AjisTokenKind.Number, token.Kind);
   }

   [Fact]
   public void Lexer_AjisMode_SkipsBlockComments()
   {
      AjisCommentOptions commentOptions = new global::Afrowave.AJIS.Core.AjisCommentOptions
      {
         AllowBlockComments = true
      };

      AjisSpanReader reader = new AjisSpanReader("/* c */false"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, commentOptions: commentOptions, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Ajis);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.False, token.Kind);
   }

   [Fact]
   public void Lexer_JsonMode_RejectsComments()
   {
      AjisSpanReader reader = new AjisSpanReader("// c\ntrue"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Json);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }

   [Fact]
   public void Lexer_LexMode_AllowsUnterminatedBlockComment()
   {
      AjisSpanReader reader = new AjisSpanReader("/* c"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Lex);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.End, token.Kind);
   }

   [Fact]
   public void Lexer_AjisMode_ReadsDirectives()
   {
      AjisSpanReader reader = new AjisSpanReader("#ajis mode value=tryparse\ntrue"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Ajis, allowDirectives: true);

      var directive = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Directive, directive.Kind);
      Assert.Equal("ajis mode value=tryparse", directive.Text);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.True, token.Kind);
   }

   [Fact]
   public void Lexer_JsonMode_RejectsDirectives()
   {
      AjisSpanReader reader = new AjisSpanReader("#ajis mode value=tryparse\ntrue"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Json, allowDirectives: true);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }

   [Fact]
   public void Lexer_LexMode_AllowsUnterminatedString()
   {
      AjisSpanReader reader = new AjisSpanReader("\"abc"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Lex);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.String, token.Kind);
      Assert.Equal("abc", token.Text);
   }

   [Fact]
   public void Lexer_AjisMode_RejectsUnterminatedString()
   {
      AjisSpanReader reader = new AjisSpanReader("\"abc"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Ajis);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }

   [Fact]
   public void Lexer_LexMode_AllowsUnterminatedEscape()
   {
      AjisSpanReader reader = new AjisSpanReader("\"a\\"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Lex);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.String, token.Kind);
      Assert.Equal("a\\", token.Text);
   }

   [Fact]
   public void Lexer_AjisMode_AllowsUnquotedPropertyNames()
   {
      AjisStringOptions stringOptions = new global::Afrowave.AJIS.Core.AjisStringOptions
      {
         AllowUnquotedPropertyNames = true
      };

      AjisSpanReader reader = new AjisSpanReader("name"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, stringOptions: stringOptions, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Ajis);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Identifier, token.Kind);
      Assert.Equal("name", token.Text);
   }

   [Fact]
   public void Lexer_JsonMode_RejectsUnquotedPropertyNames()
   {
      AjisStringOptions stringOptions = new global::Afrowave.AJIS.Core.AjisStringOptions
      {
         AllowUnquotedPropertyNames = true
      };

      AjisSpanReader reader = new AjisSpanReader("name"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, stringOptions: stringOptions, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Json);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }

   [Fact]
   public void Lexer_AjisMode_AllowsMultiline_WhenEnabled()
   {
      AjisStringOptions stringOptions = new global::Afrowave.AJIS.Core.AjisStringOptions
      {
         AllowMultiline = true,
         EnableEscapes = true
      };

      AjisSpanReader reader = new AjisSpanReader("\"a\n\""u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, stringOptions: stringOptions, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Ajis);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.String, token.Kind);
      Assert.Equal("a\n", token.Text);
   }

   [Fact]
   public void Lexer_LexMode_AllowsInvalidEscapes()
   {
      AjisSpanReader reader = new AjisSpanReader("\"a\\q\""u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Lex);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.String, token.Kind);
      Assert.Equal("aq", token.Text);
   }

   [Fact]
   public void Lexer_AjisMode_DisallowsMultiline_WhenDisabled()
   {
      AjisStringOptions stringOptions = new global::Afrowave.AJIS.Core.AjisStringOptions
      {
         AllowMultiline = false,
         EnableEscapes = true
      };

      AjisSpanReader reader = new AjisSpanReader("\"a\n\""u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, stringOptions: stringOptions, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Ajis);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }

   [Fact]
   public void Lexer_AjisMode_DisabledEscapes_KeepBackslash()
   {
      AjisStringOptions stringOptions = new global::Afrowave.AJIS.Core.AjisStringOptions
      {
         AllowMultiline = true,
         EnableEscapes = false
      };

      AjisSpanReader reader = new AjisSpanReader("\"a\\n\""u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, stringOptions: stringOptions, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Ajis);

      var token = lexer.NextToken();
      Assert.Equal("a\\n", token.Text);
   }

   [Fact]
   public void Lexer_AjisMode_AllowsSingleQuotes()
   {
      AjisStringOptions stringOptions = new global::Afrowave.AJIS.Core.AjisStringOptions
      {
         AllowSingleQuotes = true,
         EnableEscapes = true
      };

      AjisSpanReader reader = new AjisSpanReader("'hi'"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, stringOptions: stringOptions, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Ajis);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.String, token.Kind);
      Assert.Equal("hi", token.Text);
   }

   [Fact]
   public void Lexer_JsonMode_RejectsSingleQuotes()
   {
      AjisStringOptions stringOptions = new global::Afrowave.AJIS.Core.AjisStringOptions
      {
         AllowSingleQuotes = true,
         EnableEscapes = true
      };

      AjisSpanReader reader = new AjisSpanReader("'hi'"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, stringOptions: stringOptions, textMode: global::Afrowave.AJIS.Core.AjisTextMode.Json);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }

   [Fact]
   public void Lexer_ParsesUnicodeEscape()
   {
      AjisSpanReader reader = new AjisSpanReader("\"\\u0041\""u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.String, token.Kind);
      Assert.Equal("A", token.Text);
   }

   [Fact]
   public void Lexer_TokenizesNumbersWithExponent()
   {
      AjisSpanReader reader = new AjisSpanReader("-1.5e+2"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Number, token.Kind);
      Assert.Equal("-1.5e+2", token.Text);
   }

   [Fact]
   public void Lexer_AllowsLeadingPlus_WhenEnabled()
   {
      AjisNumberOptions options = new global::Afrowave.AJIS.Core.AjisNumberOptions
      {
         AllowLeadingPlusOnNumbers = true
      };

      AjisSpanReader reader = new AjisSpanReader("+12"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, options);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Number, token.Kind);
      Assert.Equal("+12", token.Text);
   }

   [Fact]
   public void Lexer_AllowsNaN_WhenEnabled()
   {
      AjisNumberOptions options = new global::Afrowave.AJIS.Core.AjisNumberOptions
      {
         AllowNaNAndInfinity = true
      };

      AjisSpanReader reader = new AjisSpanReader("NaN"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, options);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Number, token.Kind);
      Assert.Equal("NaN", token.Text);
   }

   [Fact]
   public void Lexer_AllowsInfinity_WhenEnabled()
   {
      AjisNumberOptions options = new global::Afrowave.AJIS.Core.AjisNumberOptions
      {
         AllowNaNAndInfinity = true
      };

      AjisSpanReader reader = new AjisSpanReader("-Infinity"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, options);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Number, token.Kind);
      Assert.Equal("-Infinity", token.Text);
   }

   [Fact]
   public void Lexer_RejectsNumberOverMaxTokenBytes()
   {
      AjisNumberOptions options = new global::Afrowave.AJIS.Core.AjisNumberOptions
      {
         MaxTokenBytes = 2
      };

      AjisSpanReader reader = new AjisSpanReader("123"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, options);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }

   [Fact]
   public void Lexer_RejectsStringOverMaxStringBytes()
   {
      AjisStringOptions stringOptions = new global::Afrowave.AJIS.Core.AjisStringOptions
      {
         MaxStringBytes = 2
      };

      AjisSpanReader reader = new AjisSpanReader("\"abc\""u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, stringOptions: stringOptions);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }

   [Theory]
   [InlineData("01")]
   [InlineData("1.")]
   [InlineData("1e")]
   [InlineData("+")]
   [InlineData("+1")]
   public void Lexer_InvalidNumbers_Throw(string input)
   {
      AjisSpanReader reader = new AjisSpanReader(System.Text.Encoding.UTF8.GetBytes(input));
      AjisLexer lexer = new AjisLexer(reader);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }

   [Fact]
   public void Lexer_WorksWithStreamReader()
   {
      using MemoryStream stream = new MemoryStream("[true,false,null]"u8.ToArray());
      using AjisStreamReader reader = new AjisStreamReader(stream, bufferSize: 4);
      AjisLexer lexer = new AjisLexer(reader);

      Assert.Equal(AjisTokenKind.LeftBracket, lexer.NextToken().Kind);
      Assert.Equal(AjisTokenKind.True, lexer.NextToken().Kind);
      Assert.Equal(AjisTokenKind.Comma, lexer.NextToken().Kind);
      Assert.Equal(AjisTokenKind.False, lexer.NextToken().Kind);
      Assert.Equal(AjisTokenKind.Comma, lexer.NextToken().Kind);
      Assert.Equal(AjisTokenKind.Null, lexer.NextToken().Kind);
      Assert.Equal(AjisTokenKind.RightBracket, lexer.NextToken().Kind);
      Assert.Equal(AjisTokenKind.End, lexer.NextToken().Kind);
   }

   [Fact]
   public void Lexer_TokenizesPrefixedNumbers_WhenEnabled()
   {
      AjisNumberOptions options = new global::Afrowave.AJIS.Core.AjisNumberOptions
      {
         EnableBasePrefixes = true,
         EnableDigitSeparators = false
      };

      AjisSpanReader reader = new AjisSpanReader("0xFF"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, options);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Number, token.Kind);
      Assert.Equal("0xFF", token.Text);
   }

   [Fact]
   public void Lexer_TokenizesPrefixedNumbers_WithSeparators()
   {
      AjisNumberOptions options = new global::Afrowave.AJIS.Core.AjisNumberOptions
      {
         EnableBasePrefixes = true,
         EnableDigitSeparators = true
      };

      AjisSpanReader reader = new AjisSpanReader("0b1010_0101"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, options);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Number, token.Kind);
      Assert.Equal("0b1010_0101", token.Text);
   }

   [Theory]
   [InlineData("1_000")]
   [InlineData("12_345_678")]
   public void Lexer_TokenizesDecimalSeparators(string input)
   {
      AjisNumberOptions options = new global::Afrowave.AJIS.Core.AjisNumberOptions
      {
         EnableDigitSeparators = true,
         EnforceSeparatorGroupingRules = true
      };

      AjisSpanReader reader = new AjisSpanReader(System.Text.Encoding.UTF8.GetBytes(input));
      AjisLexer lexer = new AjisLexer(reader, options);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Number, token.Kind);
      Assert.Equal(input, token.Text);
   }

   [Theory]
   [InlineData("0xFF_FF")]
   [InlineData("0xFFFF_FFFF")]
   public void Lexer_TokenizesHexSeparators(string input)
   {
      AjisNumberOptions options = new global::Afrowave.AJIS.Core.AjisNumberOptions
      {
         EnableBasePrefixes = true,
         EnableDigitSeparators = true,
         EnforceSeparatorGroupingRules = true
      };

      AjisSpanReader reader = new AjisSpanReader(System.Text.Encoding.UTF8.GetBytes(input));
      AjisLexer lexer = new AjisLexer(reader, options);

      var token = lexer.NextToken();
      Assert.Equal(AjisTokenKind.Number, token.Kind);
      Assert.Equal(input, token.Text);
   }

   [Fact]
   public void Lexer_InvalidPrefixedNumber_Throws()
   {
      AjisNumberOptions options = new global::Afrowave.AJIS.Core.AjisNumberOptions
      {
         EnableBasePrefixes = true,
         EnableDigitSeparators = false
      };

      AjisSpanReader reader = new AjisSpanReader("0x"u8.ToArray());
      AjisLexer lexer = new AjisLexer(reader, options);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }

   [Theory]
   [InlineData("1__000")]
   [InlineData("_100")]
   [InlineData("100_")]
   [InlineData("1_00")]
   [InlineData("0b10_001")]
   [InlineData("0xF_FF")]
   public void Lexer_InvalidSeparatorGrouping_Throws(string input)
   {
      AjisNumberOptions options = new global::Afrowave.AJIS.Core.AjisNumberOptions
      {
         EnableBasePrefixes = true,
         EnableDigitSeparators = true,
         EnforceSeparatorGroupingRules = true
      };

      AjisSpanReader reader = new AjisSpanReader(System.Text.Encoding.UTF8.GetBytes(input));
      AjisLexer lexer = new AjisLexer(reader, options);

      Assert.Throws<FormatException>(() => lexer.NextToken());
   }
}