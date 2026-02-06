#nullable enable

using Afrowave.AJIS.Net;
using Xunit;

namespace Afrowave.AJIS.Net.Tests;

public sealed class Class1Tests
{
   [Fact]
   public void Constructor_CreatesInstance()
   {
      var instance = new Class1();
      Assert.NotNull(instance);
   }
}
