#nullable enable

using Afrowave.AJIS.Streaming.Walk;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class StreamWalkOptionsTests
{
   [Fact]
   public void Defaults_AreExpected()
   {
      AjisStreamWalkOptions options = new AjisStreamWalkOptions();

      Assert.Equal(AjisStreamWalkMode.Ajis, options.Mode);
      Assert.True(options.Comments);
      Assert.True(options.Directives);
      Assert.True(options.Identifiers);
      Assert.Equal(256, options.MaxDepth);
      Assert.Equal(1024 * 1024, options.MaxTokenBytes);
   }

   [Fact]
   public void DefaultForM1_MatchesConstructorDefaults()
   {
      AjisStreamWalkOptions options = new AjisStreamWalkOptions();
      Assert.Equal(options, AjisStreamWalkOptions.DefaultForM1);
   }
}