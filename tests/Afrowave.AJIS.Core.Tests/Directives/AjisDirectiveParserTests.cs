#nullable enable

using Afrowave.AJIS.Core.Directives;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Directives;

public sealed class AjisDirectiveParserTests
{
   [Fact]
   public void Parse_ReturnsNamespace()
   {
      AjisDirective directive = AjisDirectiveParser.Parse("ajis mode key=tryparse");

      Assert.Equal("AJIS", directive.Namespace);
   }

   [Fact]
   public void Parse_ReturnsCommand()
   {
      AjisDirective directive = AjisDirectiveParser.Parse("ajis mode key=tryparse");

      Assert.Equal("mode", directive.Command);
   }

   [Fact]
   public void Parse_ReturnsArguments()
   {
      AjisDirective directive = AjisDirectiveParser.Parse("tool run key=value");

      Assert.Equal("value", directive.Arguments["key"]);
   }

   [Fact]
   public void Parse_ThrowsWhenMissingCommand()
   {
      Assert.Throws<FormatException>(() => AjisDirectiveParser.Parse("ajis"));
   }
}
