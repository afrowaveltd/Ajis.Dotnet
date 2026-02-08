#nullable enable

using Afrowave.AJIS.Testing.StreamWalk;
using Afrowave.AJIS.Streaming.Walk;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.Streaming;

public sealed class AjisStreamWalkCaseTests
{
   public static IEnumerable<object[]> Cases => LoadCases();

   [Theory]
   [MemberData(nameof(Cases))]
   public void RunCase_ProducesExpectedResult(AjisStreamWalkTestCase testCase)
   {
      AjisStreamWalkTestRunResult result = AjisStreamWalkTestRunner.Run(testCase, new AjisStreamWalkRunnerOptions());

      Assert.True(result.Success, string.Join("\n", result.Mismatches));
   }

   private static IEnumerable<object[]> LoadCases()
   {
      string root = Path.GetFullPath(Path.Combine(
         AppContext.BaseDirectory,
         "..", "..", "..", "..", "..",
         "tests",
         "Afrowave.AJIS.Testing",
         "StreamWalk",
         "cases"));

      if(!Directory.Exists(root))
         yield break;

      foreach(string path in Directory.EnumerateFiles(root, "*.case", SearchOption.AllDirectories))
         yield return new object[] { AjisStreamWalkTestCaseFile.Load(path) };
   }
}
