#nullable enable

using System.Text;

namespace Afrowave.AJIS.Streaming.Segments.Transforms;

/// <summary>
/// Describes how a directive binds within a segment stream.
/// </summary>
public enum AjisDirectiveBindingScope
{
   /// <summary>
   /// Directive bound at the document level (before root value).
   /// </summary>
   Document = 0,

   /// <summary>
   /// Directive bound to the next value/member/element.
   /// </summary>
   Target = 1,

   /// <summary>
   /// Directive bound after the root value (trailer).
   /// </summary>
   Trailer = 2
}

/// <summary>
/// Represents a bound directive and its target metadata.
/// </summary>
public readonly record struct AjisDirectiveBinding(
   AjisSliceUtf8 Directive,
   AjisDirectiveBindingScope Scope,
   string TargetPath,
   AjisSegmentKind? TargetKind);

/// <summary>
/// Represents a parsed directive binding.
/// </summary>
public readonly record struct AjisParsedDirectiveBinding(
   global::Afrowave.AJIS.Core.Directives.AjisDirective Directive,
   AjisDirectiveBindingScope Scope,
   string TargetPath,
   AjisSegmentKind? TargetKind);

/// <summary>
/// Binds directive segments to their target location.
/// </summary>
public static class AjisDirectiveBinder
{
   /// <summary>
   /// Binds directives according to AJIS directive rules.
   /// </summary>
   public static IEnumerable<AjisDirectiveBinding> BindDirectives(IEnumerable<AjisSegment> segments)
   {
      ArgumentNullException.ThrowIfNull(segments);

      var tracker = new AjisSegmentPathTracker();
      var pending = new List<AjisSliceUtf8>();
      bool rootStarted = false;
      bool rootCompleted = false;
      int rootDepth = 0;

      foreach(AjisSegment segment in segments)
      {
         if(segment.Kind == AjisSegmentKind.Directive)
         {
            if(segment.Slice is null)
               continue;

            if(!rootStarted)
            {
               yield return new AjisDirectiveBinding(segment.Slice.Value, AjisDirectiveBindingScope.Document, "$", null);
            }
            else if(rootCompleted)
            {
               yield return new AjisDirectiveBinding(segment.Slice.Value, AjisDirectiveBindingScope.Trailer, "$", null);
            }
            else
            {
               pending.Add(segment.Slice.Value);
            }

            continue;
         }

         if(segment.Kind == AjisSegmentKind.Comment)
            continue;

         if(!rootStarted && segment.Kind is AjisSegmentKind.Value or AjisSegmentKind.EnterContainer)
         {
            rootStarted = true;
            rootDepth = segment.Depth;
         }

         string path = tracker.Update(segment);

         if(pending.Count > 0 && IsBindingTarget(segment))
         {
            foreach(AjisSliceUtf8 directive in pending)
               yield return new AjisDirectiveBinding(directive, AjisDirectiveBindingScope.Target, path, segment.Kind);
            pending.Clear();
         }

         if(rootStarted && !rootCompleted)
         {
            if(segment.Kind == AjisSegmentKind.Value && segment.Depth == rootDepth)
               rootCompleted = true;
            else if(segment.Kind == AjisSegmentKind.ExitContainer && segment.Depth == rootDepth)
               rootCompleted = true;
         }
      }
      if(pending.Count > 0)
      {
         AjisDirectiveBindingScope scope = rootStarted ? AjisDirectiveBindingScope.Trailer : AjisDirectiveBindingScope.Document;
         foreach(AjisSliceUtf8 directive in pending)
            yield return new AjisDirectiveBinding(directive, scope, "$", null);
      }
   }

   /// <summary>
   /// Binds and parses directives into structured metadata.
   /// </summary>
   public static IEnumerable<AjisParsedDirectiveBinding> BindAndParseDirectives(IEnumerable<AjisSegment> segments)
   {
      ArgumentNullException.ThrowIfNull(segments);

      foreach(AjisDirectiveBinding binding in BindDirectives(segments))
      {
         string text = Encoding.UTF8.GetString(binding.Directive.Bytes.Span);
         var directive = global::Afrowave.AJIS.Core.Directives.AjisDirectiveParser.Parse(text);
         yield return new AjisParsedDirectiveBinding(directive, binding.Scope, binding.TargetPath, binding.TargetKind);
      }
   }

   private static bool IsBindingTarget(AjisSegment segment)
      => segment.Kind is AjisSegmentKind.PropertyName or AjisSegmentKind.Value or AjisSegmentKind.EnterContainer;
}
