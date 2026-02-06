#nullable enable

using Afrowave.AJIS.Testing.TestData;
using Xunit;

namespace Afrowave.AJIS.Core.Tests.TestData;

public sealed class AjisLargePayloadGeneratorTests
{
   [Fact]
   public void WriteUsersJson_WritesExpectedShape()
   {
      using var stream = new MemoryStream();

      AjisLargePayloadGenerator.WriteUsersJson(stream, userCount: 2, addressesPerUser: 1);

      string json = System.Text.Encoding.UTF8.GetString(stream.ToArray());

      Assert.Contains("\"users\":[", json);
      Assert.Contains("\"id\":1", json);
      Assert.Contains("\"name\":\"User1\"", json);
      Assert.Contains("\"addresses\":[", json);
      Assert.Contains("\"street\":\"Street 1\"", json);
      Assert.Contains("\"city\":\"City 1\"", json);
   }

   [Fact]
   public void WriteUsersJsonFile_CreatesFile()
   {
      string path = Path.Combine(Path.GetTempPath(), "ajis_users_test.json");
      if(File.Exists(path)) File.Delete(path);

      AjisLargePayloadGenerator.WriteUsersJsonFile(path, userCount: 1, addressesPerUser: 0);

      Assert.True(File.Exists(path));
      File.Delete(path);
   }
}
