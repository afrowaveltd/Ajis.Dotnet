#nullable enable

using Afrowave.AJIS.Streaming.Walk;
using Xunit;

namespace Afrowave.AJIS.Testing.StreamWalk;

public sealed class AjisStreamWalkOptionsSettingsTests
{
   [Fact]
   public void FromSettings_UsesMaxTokenBytes()
   {
      var settings = new global::Afrowave.AJIS.Core.Configuration.AjisSettings
      {
         MaxTokenBytes = 123
      };

      AjisStreamWalkOptions options = AjisStreamWalkOptions.FromSettings(settings);

      Assert.Equal(123, options.MaxTokenBytes);
   }

   [Fact]
   public void FromSettings_UsesIdentifiersSetting()
   {
      var settings = new global::Afrowave.AJIS.Core.Configuration.AjisSettings
      {
         Strings = new global::Afrowave.AJIS.Core.AjisStringOptions
         {
            AllowUnquotedPropertyNames = false
         }
      };

      AjisStreamWalkOptions options = AjisStreamWalkOptions.FromSettings(settings);

      Assert.False(options.Identifiers);
   }

   [Fact]
   public void FromSettings_UsesCommentOptions()
   {
      var settings = new global::Afrowave.AJIS.Core.Configuration.AjisSettings
      {
         Comments = new global::Afrowave.AJIS.Core.AjisCommentOptions
         {
            AllowLineComments = false,
            AllowBlockComments = false
         }
      };

      AjisStreamWalkOptions options = AjisStreamWalkOptions.FromSettings(settings);

      Assert.False(options.Comments);
   }

   [Fact]
   public void FromSettings_MapsJsonMode()
   {
      var settings = new global::Afrowave.AJIS.Core.Configuration.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Json
      };

      AjisStreamWalkOptions options = AjisStreamWalkOptions.FromSettings(settings);

      Assert.Equal(AjisStreamWalkMode.Json, options.Mode);
   }
}
