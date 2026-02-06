#nullable enable

using Afrowave.AJIS.Streaming.Walk;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class StreamWalkRunnerOptionsTests
{
   [Fact]
   public void Defaults_AreExpected()
   {
      var options = new AjisStreamWalkRunnerOptions();

      Assert.False(options.AllowVisitorAbort);
      Assert.Equal(AjisStreamWalkEnginePreference.Balanced, options.EnginePreference);
      Assert.Equal(256 * 1024, options.LargePayloadThresholdBytes);
      Assert.False(options.EmitDebugDiagnostics);
      Assert.Null(options.OnDiagnostic);
   }

   [Fact]
   public void Default_MatchesConstructorDefaults()
   {
      var options = new AjisStreamWalkRunnerOptions();
      Assert.Equal(options, AjisStreamWalkRunnerOptions.Default);
   }

   [Fact]
   public void WithProcessingProfile_MapsToEnginePreference()
   {
      var options = AjisStreamWalkRunnerOptions.Default
         .WithProcessingProfile(global::Afrowave.AJIS.Core.AjisProcessingProfile.LowMemory);

      Assert.Equal(AjisStreamWalkEnginePreference.LowMemory, options.EnginePreference);

      options = options.WithProcessingProfile(global::Afrowave.AJIS.Core.AjisProcessingProfile.HighThroughput);
      Assert.Equal(AjisStreamWalkEnginePreference.Speed, options.EnginePreference);

      options = options.WithProcessingProfile(global::Afrowave.AJIS.Core.AjisProcessingProfile.Universal);
      Assert.Equal(AjisStreamWalkEnginePreference.Balanced, options.EnginePreference);
   }

   [Fact]
   public void WithProcessingProfileIfDefault_DoesNotOverrideExplicitPreference()
   {
      var options = new AjisStreamWalkRunnerOptions { EnginePreference = AjisStreamWalkEnginePreference.Speed }
         .WithProcessingProfileIfDefault(global::Afrowave.AJIS.Core.AjisProcessingProfile.LowMemory);

      Assert.Equal(AjisStreamWalkEnginePreference.Speed, options.EnginePreference);
   }

   [Fact]
   public void FromSettings_AppliesParserProfile()
   {
      var settings = new global::Afrowave.AJIS.Core.Configuration.AjisSettings
      {
         ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.LowMemory
      };

      var options = AjisStreamWalkRunnerOptions.FromSettings(settings);

      Assert.Equal(AjisStreamWalkEnginePreference.LowMemory, options.EnginePreference);
   }
}
