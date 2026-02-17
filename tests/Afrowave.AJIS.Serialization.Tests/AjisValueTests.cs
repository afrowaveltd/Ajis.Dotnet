#nullable enable

namespace Afrowave.AJIS.Serialization.Tests;

public sealed class AjisValueTests
{
   [Fact]
   public void FactoryMethods_CreateExpectedTypes()
   {
      Assert.IsType<AjisValue.NullValue>(AjisValue.Null());
      Assert.IsType<AjisValue.BoolValue>(AjisValue.Bool(true));
      Assert.IsType<AjisValue.NumberValue>(AjisValue.Number("1"));
      Assert.IsType<AjisValue.StringValue>(AjisValue.String("text"));
      Assert.IsType<AjisValue.ArrayValue>(AjisValue.Array(AjisValue.Null()));
      Assert.IsType<AjisValue.ObjectValue>(AjisValue.Object(new KeyValuePair<string, AjisValue>("k", AjisValue.Null())));
   }

   [Fact]
   public void ValueRecords_ExposeProvidedData()
   {
      AjisValue.BoolValue boolValue = (AjisValue.BoolValue)AjisValue.Bool(true);
      AjisValue.NumberValue numberValue = (AjisValue.NumberValue)AjisValue.Number("42");
      AjisValue.StringValue stringValue = (AjisValue.StringValue)AjisValue.String("v");
      AjisValue.ArrayValue arrayValue = (AjisValue.ArrayValue)AjisValue.Array(AjisValue.Null());
      AjisValue.ObjectValue objectValue = (AjisValue.ObjectValue)AjisValue.Object(new KeyValuePair<string, AjisValue>("k", AjisValue.Bool(false)));

      Assert.True(boolValue.Value);
      Assert.Equal("42", numberValue.Text);
      Assert.Equal("v", stringValue.Value);
      Assert.Single(arrayValue.Items);
      Assert.Single(objectValue.Properties);
   }
}