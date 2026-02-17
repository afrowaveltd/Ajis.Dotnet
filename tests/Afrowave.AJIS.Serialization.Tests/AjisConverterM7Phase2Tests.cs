#nullable enable

using Afrowave.AJIS.Serialization.Mapping;

namespace Afrowave.AJIS.Serialization.Tests;

public sealed class AjisConverterM7Phase2Tests
{
   // ===== M7 Phase 2: Attribute-Based Mapping Tests =====

   [Fact]
   public void Serialize_WithAjisPropertyNameAttribute()
   {
      PersonWithAttributes person = new PersonWithAttributes { UserId = 42, FullName = "Alice" };
      AjisConverter<PersonWithAttributes> converter = new AjisConverter<PersonWithAttributes>();
      string result = converter.Serialize(person);

      Assert.Contains("\"user_id\"", result);
      Assert.Contains("42", result);
      Assert.Contains("\"full_name\"", result);
      Assert.Contains("\"Alice\"", result);
      Assert.DoesNotContain("\"UserId\"", result);
   }

   [Fact]
   public void Serialize_WithAjisIgnoreAttribute()
   {
      UserWithPassword user = new UserWithPassword { Name = "Bob", Password = "secret123" };
      AjisConverter<UserWithPassword> converter = new AjisConverter<UserWithPassword>(PascalCaseNamingPolicy.Instance);
      string result = converter.Serialize(user);

      Assert.Contains("\"Name\"", result);
      Assert.Contains("\"Bob\"", result);
      Assert.DoesNotContain("\"Password\"", result);
      Assert.DoesNotContain("secret123", result);
   }

   [Fact]
   public void Serialize_WithMixedNamingPolicy()
   {
      PersonWithMixedAttributes data = new PersonWithMixedAttributes
      {
         FirstName = "Charlie",
         CustomKey = "special_value"
      };
      AjisConverter<PersonWithMixedAttributes> converter = new AjisConverter<PersonWithMixedAttributes>(new CamelCaseNamingPolicy());
      string result = converter.Serialize(data);

      // FirstName should use camelCase policy → "firstName"
      Assert.Contains("\"firstName\"", result);
      Assert.Contains("\"Charlie\"", result);

      // CustomKey has explicit attribute → "my_custom_key"
      Assert.Contains("\"my_custom_key\"", result);
      Assert.Contains("\"special_value\"", result);
   }

   // ===== M7 Phase 2: Nested Object Tests =====

   [Fact]
   public void Serialize_NestedObjects()
   {
      Address address = new Address { City = "Prague", Country = "Czech Republic" };
      Company company = new Company { Name = "ACME Corp", Address = address };

      AjisConverter<Company> converter = new AjisConverter<Company>(PascalCaseNamingPolicy.Instance);
      string result = converter.Serialize(company);

      Assert.Contains("\"Name\"", result);
      Assert.Contains("\"ACME Corp\"", result);
      Assert.Contains("\"Address\"", result);
      Assert.Contains("\"City\"", result);
      Assert.Contains("\"Prague\"", result);
      Assert.Contains("\"Country\"", result);
      Assert.Contains("\"Czech Republic\"", result);
   }

   [Fact]
   public void Serialize_DeeplyNestedObjects()
   {
      Address address = new Address { City = "Paris", Country = "France" };
      Company company = new Company { Name = "Tech Inc", Address = address };
      PersonWithCompany person = new PersonWithCompany { Name = "Diana", Company = company };

      AjisConverter<PersonWithCompany> converter = new AjisConverter<PersonWithCompany>(PascalCaseNamingPolicy.Instance);
      string result = converter.Serialize(person);

      Assert.Contains("\"Name\"", result);
      Assert.Contains("\"Diana\"", result);
      Assert.Contains("\"Company\"", result);
      Assert.Contains("\"Tech Inc\"", result);
      Assert.Contains("\"Address\"", result);
      Assert.Contains("\"Paris\"", result);
   }

   // ===== M7 Phase 2: Collection Tests =====

   [Fact]
   public void Serialize_ArrayOfPrimitives()
   {
      CollectionHolder data = new() { Numbers = [1, 2, 3, 4, 5] };
      AjisConverter<CollectionHolder> converter = new AjisConverter<CollectionHolder>(PascalCaseNamingPolicy.Instance);
      string result = converter.Serialize(data);

      Assert.Contains("\"Numbers\"", result);
      Assert.Contains("[", result);
      Assert.Contains("]", result);
      Assert.Contains("1", result);
      Assert.Contains("5", result);
   }

   [Fact]
   public void Serialize_ListOfObjects()
   {
      List<Address> addresses = new List<Address>
        {
            new() { City = "Berlin", Country = "Germany" },
            new() { City = "Madrid", Country = "Spain" }
        };
      AddressListHolder holder = new AddressListHolder { Addresses = addresses };

      AjisConverter<AddressListHolder> converter = new AjisConverter<AddressListHolder>(PascalCaseNamingPolicy.Instance);
      string result = converter.Serialize(holder);

      Assert.Contains("\"Addresses\"", result);
      Assert.Contains("\"Berlin\"", result);
      Assert.Contains("\"Madrid\"", result);
      Assert.Contains("\"Germany\"", result);
      Assert.Contains("\"Spain\"", result);
   }

   // ===== M7 Phase 2: Nullable Type Tests =====

   [Fact]
   public void Serialize_NullableProperty()
   {
      DataWithNullable data = new DataWithNullable { Value = 42, OptionalValue = null };
      AjisConverter<DataWithNullable> converter = new AjisConverter<DataWithNullable>(PascalCaseNamingPolicy.Instance);
      string result = converter.Serialize(data);

      Assert.Contains("\"Value\"", result);
      Assert.Contains("42", result);
      Assert.Contains("\"OptionalValue\"", result);
      Assert.Contains("null", result);
   }

   [Fact]
   public void Serialize_NullablePropertyWithValue()
   {
      DataWithNullable data = new DataWithNullable { Value = 10, OptionalValue = 99 };
      AjisConverter<DataWithNullable> converter = new AjisConverter<DataWithNullable>(PascalCaseNamingPolicy.Instance);
      string result = converter.Serialize(data);

      Assert.Contains("\"OptionalValue\"", result);
      Assert.Contains("99", result);
   }

   // ===== Test Helper Classes =====

   private sealed class PersonWithAttributes
   {
      [AjisPropertyName("user_id")]
      public int UserId { get; set; }

      [AjisPropertyName("full_name")]
      public string FullName { get; set; } = "";
   }

   private sealed class UserWithPassword
   {
      public string Name { get; set; } = "";

      [AjisIgnore]
      public string Password { get; set; } = "";
   }

   private sealed class PersonWithMixedAttributes
   {
      public string FirstName { get; set; } = "";

      [AjisPropertyName("my_custom_key")]
      public string CustomKey { get; set; } = "";
   }

   private sealed class Address
   {
      public string City { get; set; } = "";
      public string Country { get; set; } = "";
   }

   private sealed class Company
   {
      public string Name { get; set; } = "";
      public Address? Address { get; set; }
   }

   private sealed class PersonWithCompany
   {
      public string Name { get; set; } = "";
      public Company? Company { get; set; }
   }

   private sealed class CollectionHolder
   {
      public int[]? Numbers { get; set; }
   }

   private sealed class AddressListHolder
   {
      public List<Address>? Addresses { get; set; }
   }

   private sealed class DataWithNullable
   {
      public int Value { get; set; }
      public int? OptionalValue { get; set; }
   }
}