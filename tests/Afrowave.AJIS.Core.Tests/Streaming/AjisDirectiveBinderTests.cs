#nullable enable

using Afrowave.AJIS.Streaming;
using Afrowave.AJIS.Streaming.Segments;
using Afrowave.AJIS.Streaming.Segments.Transforms;
using System.Text;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisDirectiveBinderTests
{
   [Fact]
   public void BindDirectives_BindsDocumentDirective()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Directive(0, 0, Slice("#ajis mode=tryparse")),
         AjisSegment.Value(1, 0, AjisValueKind.Boolean, Slice("true"))
      };

      List<AjisDirectiveBinding> bindings = [.. AjisDirectiveBinder.BindDirectives(segments)];

      Assert.Single(bindings);
      Assert.Equal(AjisDirectiveBindingScope.Document, bindings[0].Scope);
      Assert.Equal("$", bindings[0].TargetPath);
   }

   [Fact]
   public void BindDirectives_BindsTargetDirective()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Enter(AjisContainerKind.Object, 0, 0),
         AjisSegment.Name(1, 1, Slice("a")),
         AjisSegment.Directive(2, 1, Slice("#tool hint=fast")),
         AjisSegment.Value(3, 1, AjisValueKind.Number, Slice("1")),
         AjisSegment.Exit(AjisContainerKind.Object, 4, 0)
      };

      List<AjisDirectiveBinding> bindings = [.. AjisDirectiveBinder.BindDirectives(segments)];

      Assert.Single(bindings);
      Assert.Equal(AjisDirectiveBindingScope.Target, bindings[0].Scope);
      Assert.Equal("$.a", bindings[0].TargetPath);
      Assert.Equal(AjisSegmentKind.Value, bindings[0].TargetKind);
   }

   [Fact]
   public void BindDirectives_BindsTrailerDirective()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Value(0, 0, AjisValueKind.Boolean, Slice("true")),
         AjisSegment.Directive(1, 0, Slice("#ajis trailer"))
      };

      List<AjisDirectiveBinding> bindings = [.. AjisDirectiveBinder.BindDirectives(segments)];

      Assert.Single(bindings);
      Assert.Equal(AjisDirectiveBindingScope.Trailer, bindings[0].Scope);
      Assert.Equal("$", bindings[0].TargetPath);
   }

   [Fact]
   public void BindAndParseDirectives_ReturnsSingleBinding()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Directive(0, 0, Slice("ajis mode key=tryparse")),
         AjisSegment.Value(1, 0, AjisValueKind.Boolean, Slice("true"))
      };

      List<AjisParsedDirectiveBinding> bindings = [.. AjisDirectiveBinder.BindAndParseDirectives(segments)];

      Assert.Single(bindings);
   }

   [Fact]
   public void BindAndParseDirectives_ParsesNamespace()
   {
      var segments = new List<AjisSegment>
      {
         AjisSegment.Directive(0, 0, Slice("ajis mode key=tryparse")),
         AjisSegment.Value(1, 0, AjisValueKind.Boolean, Slice("true"))
      };

      List<AjisParsedDirectiveBinding> bindings = [.. AjisDirectiveBinder.BindAndParseDirectives(segments)];

      Assert.Equal("AJIS", bindings[0].Directive.Namespace);
   }

   private static AjisSliceUtf8 Slice(string text)
      => new(Encoding.UTF8.GetBytes(text), AjisSliceFlags.None);
}
