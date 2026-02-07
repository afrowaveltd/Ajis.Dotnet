#nullable enable

using System.Text;

namespace Afrowave.AJIS.Streaming.Segments.Transforms;

/// <summary>
/// Tracks the current JSON-style path while streaming segments.
/// </summary>
public sealed class AjisSegmentPathTracker
{
   private readonly List<PathFrame> _stack = [];
   private string? _pendingPropertyName;

   /// <summary>
   /// Gets the last computed path.
   /// </summary>
   public string CurrentPath { get; private set; } = "$";

   /// <summary>
   /// Updates the path tracker with the provided segment and returns the current path.
   /// </summary>
   public string Update(AjisSegment segment)
   {
      ArgumentNullException.ThrowIfNull(segment);

      switch(segment.Kind)
      {
         case AjisSegmentKind.EnterContainer:
            string? containerSegment = GetValueSegment();
            CurrentPath = BuildPath(containerSegment);
            ConsumeValue();
            _stack.Add(new PathFrame(segment.ContainerKind ?? AjisContainerKind.Object, containerSegment));
            break;
         case AjisSegmentKind.ExitContainer:
            if(_stack.Count > 0)
               _stack.RemoveAt(_stack.Count - 1);
            CurrentPath = BuildPath(null);
            break;
         case AjisSegmentKind.PropertyName:
            _pendingPropertyName = Decode(segment.Slice);
            CurrentPath = BuildPath(_pendingPropertyName is null ? null : $".{_pendingPropertyName}");
            break;
         case AjisSegmentKind.Value:
            CurrentPath = BuildPath(GetValueSegment());
            ConsumeValue();
            break;
         default:
            break;
      }

      return CurrentPath;
   }

   private string? GetValueSegment()
   {
      if(_stack.Count == 0)
         return _pendingPropertyName is null ? null : $".{_pendingPropertyName}";

      PathFrame parent = _stack[^1];
      if(parent.Kind == AjisContainerKind.Array)
         return $"[{parent.NextIndex}]";

      return _pendingPropertyName is null ? null : $".{_pendingPropertyName}";
   }

   private void ConsumeValue()
   {
      if(_stack.Count == 0)
      {
         _pendingPropertyName = null;
         return;
      }

      PathFrame parent = _stack[^1];
      if(parent.Kind == AjisContainerKind.Array)
      {
         parent = parent with { NextIndex = parent.NextIndex + 1 };
         _stack[^1] = parent;
      }
      else
      {
         _pendingPropertyName = null;
      }
   }

   private string BuildPath(string? leafSegment)
   {
      var builder = new StringBuilder("$");
      foreach(PathFrame frame in _stack)
         builder.Append(frame.PathSegment);

      if(!string.IsNullOrEmpty(leafSegment))
         builder.Append(leafSegment);

      return builder.ToString();
   }

   private static string? Decode(AjisSliceUtf8? slice)
      => slice is null ? null : Encoding.UTF8.GetString(slice.Value.Bytes.Span);

   private readonly record struct PathFrame(AjisContainerKind Kind, string? PathSegment)
   {
      public int NextIndex { get; init; }
   }
}
