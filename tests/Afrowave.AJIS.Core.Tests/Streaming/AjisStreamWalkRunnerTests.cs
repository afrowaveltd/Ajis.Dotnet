#nullable enable

using Afrowave.AJIS.Streaming.Walk;
using Afrowave.AJIS.Streaming.Walk.Input;
using NSubstitute;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisStreamWalkRunnerTests
{
   [Fact]
   public void Run_LaxMode_EmitsNotSupported()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var diagnostics = new List<global::Afrowave.AJIS.Core.Diagnostics.AjisDiagnostic>();
      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Lax };
      var runnerOptions = new AjisStreamWalkRunnerOptions { OnDiagnostic = d => diagnostics.Add(d) };

      AjisStreamWalkRunner.Run("{}"u8, options, visitor, runnerOptions);

      Assert.Equal(2, diagnostics.Count);
      Assert.Contains(diagnostics, d => d.Code == global::Afrowave.AJIS.Core.Diagnostics.AjisDiagnosticCode.ModeNotSupported);
      Assert.Contains(diagnostics, d => d.Code == global::Afrowave.AJIS.Core.Diagnostics.AjisDiagnosticCode.Unknown);

      visitor.Received(1).OnError(Arg.Is<AjisStreamWalkError>(e => e.Code == global::Afrowave.AJIS.Core.Diagnostics.AjisDiagnosticKeys.ModeNotSupported));
   }

   [Fact]
   public void Run_InputWithoutSpan_EmitsInputNotSupported()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var diagnostics = new List<global::Afrowave.AJIS.Core.Diagnostics.AjisDiagnostic>();
      var runnerOptions = new AjisStreamWalkRunnerOptions { OnDiagnostic = d => diagnostics.Add(d) };

      AjisStreamWalkRunner.Run(new NoSpanInput(), AjisStreamWalkOptions.DefaultForM1, visitor, runnerOptions);

      Assert.Single(diagnostics);
      Assert.Equal(global::Afrowave.AJIS.Core.Diagnostics.AjisDiagnosticCode.InputNotSupported, diagnostics[0].Code);

      visitor.Received(1).OnError(Arg.Is<AjisStreamWalkError>(e => e.Code == global::Afrowave.AJIS.Core.Diagnostics.AjisDiagnosticKeys.InputNotSupported));
   }

   [Fact]
   public void Run_ValidInput_Completes()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      AjisStreamWalkRunner.Run("null"u8, AjisStreamWalkOptions.DefaultForM1, visitor, default);

      visitor.Received(1).OnCompleted();
      visitor.DidNotReceive().OnError(Arg.Any<AjisStreamWalkError>());
      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "NULL"));
      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "END_DOCUMENT"));
   }

   [Fact]
   public void Run_VisitorAbort_EmitsVisitorAbortError()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(false);

      var runnerOptions = new AjisStreamWalkRunnerOptions { AllowVisitorAbort = true };

      AjisStreamWalkRunner.Run("null"u8, AjisStreamWalkOptions.DefaultForM1, visitor, runnerOptions);

      visitor.Received(1).OnError(Arg.Is<AjisStreamWalkError>(e => e.Code == global::Afrowave.AJIS.Core.Diagnostics.AjisDiagnosticKeys.VisitorAbort));
      visitor.DidNotReceive().OnCompleted();
   }

   [Fact]
   public void Run_LiteralOverMaxTokenBytes_EmitsError()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { MaxTokenBytes = 3 };

      AjisStreamWalkRunner.Run("true"u8, options, visitor, default);

      visitor.Received(1).OnError(Arg.Is<AjisStreamWalkError>(e => e.Code == "max_token_bytes_exceeded"));
      visitor.DidNotReceive().OnCompleted();
   }

   [Fact]
   public void Run_WithSettings_MapsParserProfileToEnginePreference()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var diagnostics = new List<global::Afrowave.AJIS.Core.Diagnostics.AjisDiagnostic>();

      var runnerOptions = new AjisStreamWalkRunnerOptions
      {
         EmitDebugDiagnostics = true,
         OnDiagnostic = diagnostics.Add
      };

      var settings = new global::Afrowave.AJIS.Core.Configuration.AjisSettings
      {
         ParserProfile = global::Afrowave.AJIS.Core.AjisProcessingProfile.LowMemory
      };

      AjisStreamWalkRunner.Run("null"u8, AjisStreamWalkOptions.DefaultForM1, visitor, settings, runnerOptions);

      var selected = diagnostics.FirstOrDefault(d => d.Code == global::Afrowave.AJIS.Core.Diagnostics.AjisDiagnosticCode.EngineSelected);
      Assert.NotNull(selected);

      var data = Assert.IsType<global::Afrowave.AJIS.Core.Diagnostics.AjisEngineSelectedData>(selected!.Data);
      Assert.Equal("LowMemory", data.Preference);
   }

   [Fact]
   public void Run_WithSettingsOverload_MapsLexModeToLax()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var settings = new global::Afrowave.AJIS.Core.Configuration.AjisSettings
      {
         TextMode = global::Afrowave.AJIS.Core.AjisTextMode.Lex
      };

      AjisStreamWalkRunner.Run("null"u8, visitor, settings, default);

      visitor.Received(1).OnError(Arg.Is<AjisStreamWalkError>(e => e.Code == global::Afrowave.AJIS.Core.Diagnostics.AjisDiagnosticKeys.ModeNotSupported));
   }

   private sealed class NoSpanInput : IAjisInput
   {
      public long LengthBytes => -1;

      public bool TryGetUtf8Span(out ReadOnlySpan<byte> utf8)
      {
         utf8 = ReadOnlySpan<byte>.Empty;
         return false;
      }

      public Stream? OpenStream() => null;
   }
}
