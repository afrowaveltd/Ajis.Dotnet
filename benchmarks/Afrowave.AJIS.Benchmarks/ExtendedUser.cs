#nullable enable

using System.Text.Json.Serialization;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Extended User object for stress testing with complex nested structures.
/// </summary>
public class ExtendedUser
{
    public int Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Username { get; set; } = "";
    public DateTime DateOfBirth { get; set; }
    public bool IsActive { get; set; }
    public decimal Salary { get; set; }
    public string Department { get; set; } = "";
    public string JobTitle { get; set; } = "";
    public DateTime HireDate { get; set; }
    public string[] Emails { get; set; } = Array.Empty<string>();
    public Address[] Addresses { get; set; } = Array.Empty<Address>();
    public string[] PhoneNumbers { get; set; } = Array.Empty<string>();
    public Dictionary<string, string> Metadata { get; set; } = new();
    public List<Project> Projects { get; set; } = new();
    public Company Company { get; set; } = new();
}

/// <summary>
/// Address information.
/// </summary>
public class Address
{
    public string Type { get; set; } = ""; // "home", "work", "billing", etc.
    public string Street { get; set; } = "";
    public string City { get; set; } = "";
    public string State { get; set; } = "";
    public string ZipCode { get; set; } = "";
    public string Country { get; set; } = "";
}

/// <summary>
/// Project information.
/// </summary>
public class Project
{
    public int ProjectId { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "";
    public decimal Budget { get; set; }
}

/// <summary>
/// Company information.
/// </summary>
public class Company
{
    public int CompanyId { get; set; }
    public string Name { get; set; } = "";
    public string Industry { get; set; } = "";
    public Address Headquarters { get; set; } = new();
    public int EmployeeCount { get; set; }
}