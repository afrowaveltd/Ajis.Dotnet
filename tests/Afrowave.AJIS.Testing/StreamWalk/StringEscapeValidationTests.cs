#nullable enable

using Afrowave.AJIS.Streaming.Walk;
using Xunit;

namespace Afrowave.AJIS.Testing.StreamWalk;

public sealed class StringEscapeValidationTests
{
   [Fact]
   public void AjisMode_InvalidEscape_IsRejected()
   {
      AjisStreamWalkOptions opt = AjisStreamWalkOptions.DefaultForM1 with
      {
         Mode = AjisStreamWalkMode.Ajis
      };

      CapturingVisitor v = new();

      AjisStreamWalkRunner.Run("{\"s\":\"a\\q\"}"u8, opt, v, new AjisStreamWalkRunnerOptions());

      Assert.NotNull(v.Error);
      Assert.Equal("invalid_escape", v.Error!.Code);
      Assert.Equal(8, v.Error.Offset);
   }

   private sealed class CapturingVisitor : IAjisStreamWalkVisitor
   {
      public AjisStreamWalkError? Error { get; private set; }
      public bool OnEvent(AjisStreamWalkEvent evt) => true;
      public void OnError(AjisStreamWalkError error) => Error = error;
      public void OnCompleted() { }
   }
}
