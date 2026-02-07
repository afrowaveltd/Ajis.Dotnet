#nullable enable

using Afrowave.AJIS.Testing.TestData;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.TestData;

public sealed class AjisBenchmarkRunnerTests
{
   [Fact]
   public async Task RunAsync_ProducesResults()
   {
      string path = Path.Combine(Path.GetTempPath(), "ajis_benchmark_test.json");
      if(File.Exists(path)) File.Delete(path);

      AjisLargePayloadGenerator.WriteUsersJsonFile(path, userCount: 2, addressesPerUser: 1);

      var results = await AjisBenchmarkRunner.RunAsync(path, TestContext.Current.CancellationToken);

      Assert.True(results.Count >= 3);
      Assert.Contains(results, r => r.Name.StartsWith("AJIS.", StringComparison.Ordinal));
      Assert.Contains(results, r => r.Name == "System.Text.Json");
      Assert.Contains(results, r => r.Name == "Newtonsoft.Json");

      File.Delete(path);
   }
}
