#nullable enable

using Afrowave.AJIS.Core.Abstraction;
using Afrowave.AJIS.Core.Localization;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Localization;

public sealed class AjisTextProviderBuilderTests
{
   [Fact]
   public void Build_RespectsPriorityOrder()
   {
      var high = new AjisLocDictionary(new Dictionary<string, string> { ["k"] = "high" });
      var low = new AjisLocDictionary(new Dictionary<string, string> { ["k"] = "low" });

      var provider = new AjisTextProviderBuilder()
         .AddLowPriority(low)
         .AddHighPriority(high)
         .Build();

      Assert.Equal("high", provider.GetText("k"));
   }

   [Fact]
   public void GetText_UsesMissingKeyBehavior()
   {
      var provider = new AjisTextProviderBuilder()
         .Build(MissingKeyBehavior.Bracketed);

      Assert.Equal("[missing:missing]", provider.GetText("missing"));
   }

   [Fact]
   public void GetText_FormatsArgs_WhenProvided()
   {
      var dict = new AjisLocDictionary(new Dictionary<string, string>
      {
         ["fmt"] = "Value {0}"
      });

      var provider = new AjisTextProviderBuilder()
         .AddLowPriority(dict)
         .Build();

      var text = provider.GetText("fmt", data: new Dictionary<string, object?>
      {
         ["args"] = new object?[] { 5 }
      });

      Assert.Equal("Value 5", text);
   }

   [Fact]
   public void Format_UsesCultureAndArgs()
   {
      var dict = new AjisLocDictionary(new Dictionary<string, string>
      {
         ["fmt"] = "Value {0}"
      });

      var provider = new AjisTextProviderBuilder()
         .AddLowPriority(dict)
         .Build();

      Assert.Equal("Value 7", provider.Format("fmt", 7));
   }
}
