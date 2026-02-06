#nullable enable

using CoreSettings = global::Afrowave.AJIS.Core.Configuration.AjisSettings;
using CoreNaming = global::Afrowave.AJIS.Core.Configuration.AjisPropertyNaming;
using CoreEvents = global::Afrowave.AJIS.Core.Events;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Configuration;

public sealed class AjisSettingsTests
{
   [Fact]
   public void Defaults_AreExpected()
   {
      var settings = new CoreSettings();

      Assert.Null(settings.Culture);
      Assert.Null(settings.TextProvider);
      Assert.Same(CoreEvents.NullAjisEventSink.Instance, settings.EventSink);
      Assert.Equal(256, settings.MaxDepth);
      Assert.Equal(CoreNaming.PascalCase, settings.Naming);
      Assert.Equal(global::Afrowave.AJIS.Core.AjisProcessingProfile.Universal, settings.ParserProfile);
      Assert.Equal(global::Afrowave.AJIS.Core.AjisProcessingProfile.Universal, settings.SerializerProfile);
      Assert.Equal(global::Afrowave.AJIS.Core.AjisTextMode.Ajis, settings.TextMode);
      Assert.True(settings.AllowDirectives);
      Assert.False(settings.AllowTrailingCommas);
      Assert.False(settings.Strings.AllowSingleQuotes);
      Assert.True(settings.Strings.AllowUnquotedPropertyNames);
      Assert.Equal("2G", settings.StreamChunkThreshold);
   }
}
