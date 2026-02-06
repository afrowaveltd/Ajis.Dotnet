#nullable enable

using Afrowave.AJIS.IO;
using Xunit;

namespace Afrowave.AJIS.IO.Tests;

public sealed class Class1Tests
{
   [Fact]
   public void Constructor_CreatesInstance()
   {
      var instance = new Class1();
      Assert.NotNull(instance);
   }
}
