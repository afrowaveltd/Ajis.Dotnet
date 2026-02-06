#nullable enable

using Afrowave.AJIS.Records;
using Xunit;

namespace Afrowave.AJIS.Records.Tests;

public sealed class Class1Tests
{
   [Fact]
   public void Constructor_CreatesInstance()
   {
      var instance = new Class1();
      Assert.NotNull(instance);
   }
}
