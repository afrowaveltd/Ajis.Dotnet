#nullable enable

using Afrowave.AJIS.Streaming.Walk;
using Afrowave.AJIS.Streaming.Walk.Input;
using Afrowave.AJIS.Streaming;
using NSubstitute;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisStreamWalkRunnerTests
{
   [Fact]
   public void Run_LaxMode_Completes()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Lax };

      AjisStreamWalkRunner.Run("{}"u8, options, visitor, default);

      visitor.Received(1).OnCompleted();
      visitor.DidNotReceive().OnError(Arg.Any<AjisStreamWalkError>());
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
   public void Run_WithComments_EmitsCommentEvent()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Comments = true };

      AjisStreamWalkRunner.Run("// note\ntrue"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "COMMENT"));
   }

   [Fact]
   public void Run_WithDirectives_EmitsDirectiveEvent()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Directives = true };

      AjisStreamWalkRunner.Run("#tool hint=fast\ntrue"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "DIRECTIVE"));
   }

   [Fact]
   public void Run_LaxMode_AllowsUnterminatedString()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Lax };

      AjisStreamWalkRunner.Run("\"abc"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "STRING"));
   }

   [Fact]
   public void Run_LaxMode_AllowsUnterminatedBlockComment()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Lax, Comments = true };

      AjisStreamWalkRunner.Run("/* comment"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "COMMENT"));
   }

   [Fact]
   public void Run_LaxMode_AllowsTrailingCommaInObject()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Lax };

      AjisStreamWalkRunner.Run("{\"a\":1,}"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "END_OBJECT"));
   }

   [Fact]
   public void Run_LaxMode_AllowsMissingEndBracket()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Lax };

      AjisStreamWalkRunner.Run("[1"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "END_ARRAY"));
   }

   [Fact]
   public void Run_LaxMode_AllowsLeadingPlusNumber()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Lax };

      AjisStreamWalkRunner.Run("+1"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "NUMBER"));
   }

   [Fact]
   public void Run_LaxMode_AllowsNaNLiteral()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Lax };

      AjisStreamWalkRunner.Run("NaN"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "NUMBER"));
   }

   [Fact]
   public void Run_LaxMode_EmitsIdentifier()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Lax };

      AjisStreamWalkRunner.Run("identifier"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "IDENTIFIER"));
   }

   [Fact]
   public void Run_LaxMode_AllowsUnquotedPropertyName()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Lax, Identifiers = true };

      AjisStreamWalkRunner.Run("{name:1}"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "NAME"));
   }

   [Fact]
   public void Run_AjisMode_EmitsTypedLiteralAsNumber()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Ajis };

      AjisStreamWalkRunner.Run("{\"RegisteredAt\":T1707489221}"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "NUMBER"));
   }

   [Fact]
   public void Run_AjisMode_EmitsTypedLiteralFlag()
   {
      AjisStreamWalkEvent? captured = null;
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Do<AjisStreamWalkEvent>(e =>
      {
         if(e.Kind == "NUMBER")
            captured = e;
      })).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Ajis };

      AjisStreamWalkRunner.Run("T170"u8, options, visitor, default);

      Assert.Equal(AjisSliceFlags.IsNumberTyped, captured?.Slice.Flags);
   }

   [Fact]
   public void Run_AjisMode_InvalidTypedLiteral_WithIdentifiers_EmitsIdentifier()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Ajis, Identifiers = true };

      AjisStreamWalkRunner.Run("T170A"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "IDENTIFIER"));
   }

   [Fact]
   public void Run_AjisMode_InvalidTypedLiteral_WhenIdentifiersDisabled_EmitsError()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Ajis, Identifiers = false };

      AjisStreamWalkRunner.Run("T170A"u8, options, visitor, default);

      visitor.Received(1).OnError(Arg.Is<AjisStreamWalkError>(e => e.Code == "typed_literal_invalid"));
   }

   [Fact]
   public void Run_LaxMode_EmitsTypedLiteralAsNumber()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Lax };

      AjisStreamWalkRunner.Run("T1707489221"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "NUMBER"));
   }

   [Fact]
   public void Run_AjisMode_EmitsIdentifier()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Ajis, Identifiers = true };

      AjisStreamWalkRunner.Run("identifier"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "IDENTIFIER"));
   }

   [Fact]
   public void Run_AjisMode_AllowsUnquotedPropertyName()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var options = AjisStreamWalkOptions.DefaultForM1 with { Mode = AjisStreamWalkMode.Ajis, Identifiers = true };

      AjisStreamWalkRunner.Run("{name:1}"u8, options, visitor, default);

      visitor.Received(1).OnEvent(Arg.Is<AjisStreamWalkEvent>(e => e.Kind == "NAME"));
   }

   [Fact]
   public void RunWithDirectives_AppliesModeDirective()
   {
      var visitor = Substitute.For<IAjisStreamWalkVisitor>();
      visitor.OnEvent(Arg.Any<AjisStreamWalkEvent>()).Returns(true);

      var settings = new global::Afrowave.AJIS.Core.Configuration.AjisSettings
      {
         AllowDirectives = true
      };

      AjisStreamWalkRunner.RunWithDirectives("#ajis mode value=lex\ntrue"u8, visitor, settings, default);

      visitor.Received(1).OnCompleted();
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

      visitor.Received(1).OnCompleted();
      visitor.DidNotReceive().OnError(Arg.Any<AjisStreamWalkError>());
   }

   private sealed class NoSpanInput : IAjisInput
   {
      public long LengthBytes => -1;

      public bool TryGetUtf8Span(out ReadOnlySpan<byte> utf8)
      {
         utf8 = [];
         return false;
      }

      public Stream? OpenStream() => null;
   }
}
