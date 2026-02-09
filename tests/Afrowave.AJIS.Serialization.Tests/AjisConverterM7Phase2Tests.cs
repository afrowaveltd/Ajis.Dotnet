#nullable enable

using Afrowave.AJIS.Serialization.Mapping;
using System.Text;
using Xunit;

namespace Afrowave.AJIS.Serialization.Tests;

public sealed class AjisConverterM7Phase2Tests
{
    // ===== M7 Phase 2: Attribute-Based Mapping Tests =====

    [Fact]
    public void Serialize_WithAjisPropertyNameAttribute()
    {
        var person = new PersonWithAttributes { UserId = 42, FullName = "Alice" };
        var converter = new AjisConverter<PersonWithAttributes>();
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
        var user = new UserWithPassword { Name = "Bob", Password = "secret123" };
        var converter = new AjisConverter<UserWithPassword>();
        string result = converter.Serialize(user);

        Assert.Contains("\"Name\"", result);
        Assert.Contains("\"Bob\"", result);
        Assert.DoesNotContain("\"Password\"", result);
        Assert.DoesNotContain("secret123", result);
    }

    [Fact]
    public void Serialize_WithMixedNamingPolicy()
    {
        var data = new PersonWithMixedAttributes 
        { 
            FirstName = "Charlie", 
            CustomKey = "special_value" 
        };
        var converter = new AjisConverter<PersonWithMixedAttributes>(new CamelCaseNamingPolicy());
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
        var address = new Address { City = "Prague", Country = "Czech Republic" };
        var company = new Company { Name = "ACME Corp", Address = address };
        
        var converter = new AjisConverter<Company>();
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
        var address = new Address { City = "Paris", Country = "France" };
        var company = new Company { Name = "Tech Inc", Address = address };
        var person = new PersonWithCompany { Name = "Diana", Company = company };

        var converter = new AjisConverter<PersonWithCompany>();
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
        var data = new CollectionHolder { Numbers = new[] { 1, 2, 3, 4, 5 } };
        var converter = new AjisConverter<CollectionHolder>();
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
        var addresses = new List<Address>
        {
            new() { City = "Berlin", Country = "Germany" },
            new() { City = "Madrid", Country = "Spain" }
        };
        var holder = new AddressListHolder { Addresses = addresses };

        var converter = new AjisConverter<AddressListHolder>();
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
        var data = new DataWithNullable { Value = 42, OptionalValue = null };
        var converter = new AjisConverter<DataWithNullable>();
        string result = converter.Serialize(data);

        Assert.Contains("\"Value\"", result);
        Assert.Contains("42", result);
        Assert.Contains("\"OptionalValue\"", result);
        Assert.Contains("null", result);
    }

    [Fact]
    public void Serialize_NullablePropertyWithValue()
    {
        var data = new DataWithNullable { Value = 10, OptionalValue = 99 };
        var converter = new AjisConverter<DataWithNullable>();
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
