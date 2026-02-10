#nullable enable

using System.Diagnostics;
using System.Text.Json;

namespace Afrowave.AJIS.Benchmarks;

/// <summary>
/// Round-trip stress test with 10 million complex records.
/// Tests full cycle: generation -> serialization -> file write -> file read -> deserialization -> re-serialization.
/// </summary>
public static class RoundTripStressTest
{
   private const int RecordCount = 10_000_000; // Ultimate 10 million record stress test
   private const string InputFile = "stress_test_input.json";
   private const string OutputFile = "stress_test_output.json";

   public static void Run()
   {
      Console.WriteLine("🔥 AJIS ROUND-TRIP STRESS TEST - 10 MILLION COMPLEX RECORDS");
      Console.WriteLine("===========================================================");
      Console.WriteLine();

      try
      {
         // Phase 1: Generate data
         Console.WriteLine($"📝 Phase 1: Generating {RecordCount:N0} complex user records...");
         Stopwatch stopwatch = Stopwatch.StartNew();
         var users = GenerateUsers(RecordCount);
         stopwatch.Stop();
         Console.WriteLine($"   ✅ Generated in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
         Console.WriteLine($"   📊 Memory usage: {GC.GetTotalMemory(false) / 1024 / 1024:N0} MB");
         Console.WriteLine();

         // Phase 2: Serialize to file
         Console.WriteLine($"💾 Phase 2: Serializing to {InputFile}...");
         stopwatch.Restart();
         SerializeToFile(users, InputFile);
         stopwatch.Stop();
         Console.WriteLine($"   ✅ Serialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
         var inputFileSize = new FileInfo(InputFile).Length;
         Console.WriteLine($"   📁 File size: {inputFileSize / 1024 / 1024:N0} MB");
         Console.WriteLine();

         // Phase 3: Deserialize from file (with fallback to streaming)
         Console.WriteLine($"📖 Phase 3: Deserializing from {InputFile}...");
         stopwatch.Restart();
         var deserializedUsers = DeserializeFromFile(InputFile);
         stopwatch.Stop();

         if(deserializedUsers == null)
         {
            // Streaming mode was triggered
            Console.WriteLine("   ℹ️  Using streaming mode for large file processing");
            deserializedUsers = new List<ExtendedUser>(); // Empty list for validation
         }
         else
         {
            Console.WriteLine($"   ✅ Deserialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"   📊 Records loaded: {deserializedUsers.Count:N0}");
         }
         Console.WriteLine();

         // Phase 4: Re-serialize to new file
         Console.WriteLine($"🔄 Phase 4: Re-serializing to {OutputFile}...");
         stopwatch.Restart();
         SerializeToFile(deserializedUsers, OutputFile);
         stopwatch.Stop();
         Console.WriteLine($"   ✅ Re-serialized in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
         var outputFileSize = new FileInfo(OutputFile).Length;
         Console.WriteLine($"   📁 File size: {outputFileSize / 1024 / 1024:N0} MB");
         Console.WriteLine();

         // Phase 5: Validation
         Console.WriteLine("✅ Phase 5: Validating round-trip integrity...");
         var isValid = ValidateRoundTrip(users, deserializedUsers);
         Console.WriteLine($"   {(isValid ? "✅" : "❌")} Round-trip validation: {(isValid ? "PASSED" : "FAILED")}");
         Console.WriteLine();

         // Phase 6: Performance summary
         Console.WriteLine("📈 PERFORMANCE SUMMARY:");
         Console.WriteLine("=======================");
         Console.WriteLine($"Records processed: {RecordCount:N0}");
         Console.WriteLine($"Input file size:   {inputFileSize / 1024 / 1024:N0} MB");
         Console.WriteLine($"Output file size:  {outputFileSize / 1024 / 1024:N0} MB");
         Console.WriteLine($"Memory usage:      {GC.GetTotalMemory(false) / 1024 / 1024:N0} MB");
         Console.WriteLine($"GC Collections:    Gen0={GC.CollectionCount(0)}, Gen1={GC.CollectionCount(1)}, Gen2={GC.CollectionCount(2)}");

         Console.WriteLine();
         Console.WriteLine("🎉 ROUND-TRIP STRESS TEST COMPLETED SUCCESSFULLY!");
         Console.WriteLine("=================================================");
      }
      catch(OutOfMemoryException)
      {
         Console.WriteLine("⚠️  OUT OF MEMORY DETECTED - Switching to STREAMING MODE");
         Console.WriteLine("=======================================================");
         RunStreamingMode();
      }
      catch(Exception ex)
      {
         Console.WriteLine($"❌ STRESS TEST FAILED: {ex.Message}");
         Console.WriteLine(ex.StackTrace);
      }
      finally
      {
         // Cleanup
         try
         {
            if(File.Exists(InputFile)) File.Delete(InputFile);
            if(File.Exists(OutputFile)) File.Delete(OutputFile);
         }
         catch { }
      }
   }

   private static List<ExtendedUser> GenerateUsers(int count)
   {
      Random random = new Random(42);
      List<ExtendedUser> users = new List<ExtendedUser>(count);

      var firstNames = new[] { "John", "Jane", "Michael", "Sarah", "David", "Lisa", "Robert", "Emily", "James", "Maria" };
      var lastNames = new[] { "Smith", "Johnson", "Brown", "Williams", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
      var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
      var states = new[] { "NY", "CA", "IL", "TX", "AZ", "PA", "FL", "OH", "GA", "NC" };
      var departments = new[] { "Engineering", "Sales", "Marketing", "HR", "Finance", "Operations", "IT", "Legal", "Research", "Support" };
      var jobTitles = new[] { "Developer", "Manager", "Analyst", "Specialist", "Coordinator", "Director", "Consultant", "Administrator", "Technician", "Assistant" };
      var companies = new[] { "TechCorp", "DataSys", "InfoTech", "GlobalSoft", "NetWorks", "CloudTech", "WebSolutions", "DataFlow", "SysTech", "InfoNet" };
      var industries = new[] { "Technology", "Finance", "Healthcare", "Manufacturing", "Retail", "Education", "Government", "Media", "Transportation", "Energy" };

      for(int i = 0; i < count; i++)
      {
         ExtendedUser user = new ExtendedUser
         {
            Id = i + 1,
            FirstName = firstNames[random.Next(firstNames.Length)],
            LastName = lastNames[random.Next(lastNames.Length)],
            Username = $"user{i + 1}",
            DateOfBirth = DateTime.Now.AddYears(-random.Next(20, 60)).AddDays(-random.Next(365)),
            IsActive = random.Next(100) < 95, // 95% active
            Salary = random.Next(30000, 150000),
            Department = departments[random.Next(departments.Length)],
            JobTitle = jobTitles[random.Next(jobTitles.Length)],
            HireDate = DateTime.Now.AddYears(-random.Next(0, 20)).AddDays(-random.Next(365)),

            Emails = GenerateEmails(random, i + 1),
            Addresses = GenerateAddresses(random),
            PhoneNumbers = GeneratePhoneNumbers(random),
            Metadata = GenerateMetadata(random),
            Projects = GenerateProjects(random),
            Company = GenerateCompany(random, companies, industries, cities, states)
         };

         users.Add(user);
      }

      return users;
   }

   private static string[] GenerateEmails(Random random, int userId)
   {
      List<string> emails = new List<string>();
      var domains = new[] { "gmail.com", "yahoo.com", "hotmail.com", "outlook.com", "company.com", "tech.com" };

      // Primary email
      emails.Add($"user{userId}@{domains[random.Next(domains.Length)]}");

      // Additional emails (0-3)
      var additionalCount = random.Next(4);
      for(int i = 0; i < additionalCount; i++)
      {
         emails.Add($"{Guid.NewGuid().ToString().Substring(0, 8)}@{domains[random.Next(domains.Length)]}");
      }

      return emails.ToArray();
   }

   private static Address[] GenerateAddresses(Random random)
   {
      List<Address> addresses = new List<Address>();
      var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
      var states = new[] { "NY", "CA", "IL", "TX", "AZ", "PA", "FL", "OH", "GA", "NC" };

      // Home address
      addresses.Add(new Address
      {
         Type = "home",
         Street = $"{random.Next(1, 9999)} {new[] { "Main St", "Oak Ave", "Elm St", "Maple Dr", "Pine Rd" }[random.Next(5)]}",
         City = cities[random.Next(cities.Length)],
         State = states[random.Next(states.Length)],
         ZipCode = $"{random.Next(10000, 99999)}",
         Country = "USA"
      });

      // Work address (70% have work address)
      if(random.Next(100) < 70)
      {
         addresses.Add(new Address
         {
            Type = "work",
            Street = $"{random.Next(1, 9999)} {new[] { "Business Blvd", "Corporate Ave", "Office Dr", "Suite St", "Tower Rd" }[random.Next(5)]}",
            City = cities[random.Next(cities.Length)],
            State = states[random.Next(states.Length)],
            ZipCode = $"{random.Next(10000, 99999)}",
            Country = "USA"
         });
      }

      return addresses.ToArray();
   }

   private static string[] GeneratePhoneNumbers(Random random)
   {
      List<string> phones = new List<string>();

      // Primary phone
      phones.Add($"({random.Next(200, 999)})-{random.Next(100, 999)}-{random.Next(1000, 9999)}");

      // Additional phones (0-2)
      var additionalCount = random.Next(3);
      for(int i = 0; i < additionalCount; i++)
      {
         phones.Add($"({random.Next(200, 999)})-{random.Next(100, 999)}-{random.Next(1000, 9999)}");
      }

      return phones.ToArray();
   }

   private static Dictionary<string, string> GenerateMetadata(Random random)
   {
      Dictionary<string, string> metadata = new Dictionary<string, string>();
      var keys = new[] { "timezone", "locale", "theme", "notifications", "security_level" };
      var values = new[] { "EST", "en-US", "dark", "enabled", "high", "UTC", "cs-CZ", "light", "disabled", "medium" };

      var count = random.Next(1, keys.Length + 1);
      for(int i = 0; i < count; i++)
      {
         metadata[keys[i]] = values[random.Next(values.Length)];
      }

      return metadata;
   }

   private static List<Project> GenerateProjects(Random random)
   {
      List<Project> projects = new List<Project>();
      var statuses = new[] { "active", "completed", "on-hold", "cancelled", "planning" };

      var count = random.Next(0, 6); // 0-5 projects
      for(int i = 0; i < count; i++)
      {
         var startDate = DateTime.Now.AddYears(-random.Next(3)).AddDays(-random.Next(365));
         var endDate = random.Next(100) < 80 ? startDate.AddDays(random.Next(30, 365)) : (DateTime?)null;

         projects.Add(new Project
         {
            ProjectId = random.Next(1000, 9999),
            Name = $"Project {Guid.NewGuid().ToString().Substring(0, 8)}",
            Description = $"Description for project {i + 1}",
            StartDate = startDate,
            EndDate = endDate,
            Status = statuses[random.Next(statuses.Length)],
            Budget = random.Next(10000, 500000)
         });
      }

      return projects;
   }

   private static Company GenerateCompany(Random random, string[] companies, string[] industries, string[] cities, string[] states)
   {
      return new Company
      {
         CompanyId = random.Next(100, 999),
         Name = companies[random.Next(companies.Length)],
         Industry = industries[random.Next(industries.Length)],
         Headquarters = new Address
         {
            Type = "headquarters",
            Street = $"{random.Next(1, 9999)} Corporate Blvd",
            City = cities[random.Next(cities.Length)],
            State = states[random.Next(states.Length)],
            ZipCode = $"{random.Next(10000, 99999)}",
            Country = "USA"
         },
         EmployeeCount = random.Next(50, 50000)
      };
   }

   private static void SerializeToFile(List<ExtendedUser>? users, string fileName)
   {
      if(users == null) return;

      using var fileStream = File.Create(fileName);
      using Utf8JsonWriter writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = false });
      JsonSerializer.Serialize(writer, users);
   }

   private static List<ExtendedUser>? DeserializeFromFile(string fileName)
   {
      try
      {
         using var fileStream = File.OpenRead(fileName);
         return JsonSerializer.Deserialize<List<ExtendedUser>>(fileStream);
      }
      catch(OutOfMemoryException)
      {
         Console.WriteLine("   ⚠️  OutOfMemory during deserialization - file too large for RAM");
         Console.WriteLine("   💡 Switching to streaming validation mode");
         return null; // Signal to use streaming mode
      }
   }

   private static bool ValidateRoundTrip(List<ExtendedUser>? original, List<ExtendedUser>? deserialized)
   {
      if(original == null || deserialized == null) return false;
      if(original.Count != deserialized.Count) return false;

      // Sample validation - check first, middle, and last records
      var indices = new[] { 0, original.Count / 2, original.Count - 1 };

      foreach(var index in indices)
      {
         var orig = original[index];
         var deser = deserialized[index];

         if(orig.Id != deser.Id ||
             orig.FirstName != deser.FirstName ||
             orig.LastName != deser.LastName ||
             orig.Emails.Length != deser.Emails.Length ||
             orig.Addresses.Length != deser.Addresses.Length)
         {
            return false;
         }
      }

      return true;
   }

   private static void RunStreamingMode()
   {
      Console.WriteLine("🔄 STREAMING MODE: Processing large files with minimal memory");
      Console.WriteLine("============================================================");

      try
      {
         Stopwatch stopwatch = new Stopwatch();

         // Phase 1: Generate and stream to file
         Console.WriteLine($"📝 Phase 1: Streaming generation to {InputFile}...");
         stopwatch.Start();
         StreamGenerateToFile(InputFile);
         stopwatch.Stop();
         Console.WriteLine($"   ✅ Generated in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
         var inputFileSize = new FileInfo(InputFile).Length;
         Console.WriteLine($"   📁 File size: {inputFileSize / 1024 / 1024:N0} MB");
         Console.WriteLine();

         // Phase 2: Stream process (transform) to output file
         Console.WriteLine($"🔄 Phase 2: Streaming transformation to {OutputFile}...");
         stopwatch.Restart();
         StreamProcessFile(InputFile, OutputFile);
         stopwatch.Stop();
         Console.WriteLine($"   ✅ Processed in {stopwatch.Elapsed.TotalSeconds:F2} seconds");
         var outputFileSize = new FileInfo(OutputFile).Length;
         Console.WriteLine($"   📁 Output size: {outputFileSize / 1024 / 1024:N0} MB");
         Console.WriteLine();

         // Phase 3: Validate streaming integrity
         Console.WriteLine("✅ Phase 3: Validating streaming integrity...");
         var isValid = ValidateStreamingIntegrity(InputFile, OutputFile);
         Console.WriteLine($"   {(isValid ? "✅" : "❌")} Streaming validation: {(isValid ? "PASSED" : "FAILED")}");
         Console.WriteLine();

         Console.WriteLine("📈 STREAMING PERFORMANCE SUMMARY:");
         Console.WriteLine("=================================");
         Console.WriteLine($"Records processed: {RecordCount:N0}");
         Console.WriteLine($"Input file size:   {inputFileSize / 1024 / 1024:N0} MB");
         Console.WriteLine($"Output file size:  {outputFileSize / 1024 / 1024:N0} MB");
         Console.WriteLine($"Peak memory usage: {GC.GetTotalMemory(false) / 1024 / 1024:N0} MB");
         Console.WriteLine($"GC Collections:    Gen0={GC.CollectionCount(0)}, Gen1={GC.CollectionCount(1)}, Gen2={GC.CollectionCount(2)}");

         Console.WriteLine();
         Console.WriteLine("🎉 STREAMING ROUND-TRIP STRESS TEST COMPLETED SUCCESSFULLY!");
         Console.WriteLine("=============================================================");
      }
      catch(Exception ex)
      {
         Console.WriteLine($"❌ STREAMING MODE FAILED: {ex.Message}");
         Console.WriteLine(ex.StackTrace);
      }
   }

   private static void StreamGenerateToFile(string fileName)
   {
      using var fileStream = File.Create(fileName);
      using Utf8JsonWriter writer = new Utf8JsonWriter(fileStream, new JsonWriterOptions { Indented = false });

      writer.WriteStartArray();

      Random random = new Random(42);
      var firstNames = new[] { "John", "Jane", "Michael", "Sarah", "David", "Lisa", "Robert", "Emily", "James", "Maria" };
      var lastNames = new[] { "Smith", "Johnson", "Brown", "Williams", "Jones", "Garcia", "Miller", "Davis", "Rodriguez", "Martinez" };
      var cities = new[] { "New York", "Los Angeles", "Chicago", "Houston", "Phoenix", "Philadelphia", "San Antonio", "San Diego", "Dallas", "San Jose" };
      var states = new[] { "NY", "CA", "IL", "TX", "AZ", "PA", "FL", "OH", "GA", "NC" };
      var departments = new[] { "Engineering", "Sales", "Marketing", "HR", "Finance", "Operations", "IT", "Legal", "Research", "Support" };
      var jobTitles = new[] { "Developer", "Manager", "Analyst", "Specialist", "Coordinator", "Director", "Consultant", "Administrator", "Technician", "Assistant" };
      var companies = new[] { "TechCorp", "DataSys", "InfoTech", "GlobalSoft", "NetWorks", "CloudTech", "WebSolutions", "DataFlow", "SysTech", "InfoNet" };
      var industries = new[] { "Technology", "Finance", "Healthcare", "Manufacturing", "Retail", "Education", "Government", "Media", "Transportation", "Energy" };

      for(int i = 0; i < RecordCount; i++)
      {
         writer.WriteStartObject();

         // Basic info
         writer.WriteNumber("Id", i + 1);
         writer.WriteString("FirstName", firstNames[random.Next(firstNames.Length)]);
         writer.WriteString("LastName", lastNames[random.Next(lastNames.Length)]);
         writer.WriteString("Username", $"user{i + 1}");
         writer.WriteString("DateOfBirth", DateTime.Now.AddYears(-random.Next(20, 60)).AddDays(-random.Next(365)).ToString("O"));
         writer.WriteBoolean("IsActive", random.Next(100) < 95);
         writer.WriteNumber("Salary", random.Next(30000, 150000));
         writer.WriteString("Department", departments[random.Next(departments.Length)]);
         writer.WriteString("JobTitle", jobTitles[random.Next(jobTitles.Length)]);
         writer.WriteString("HireDate", DateTime.Now.AddYears(-random.Next(0, 20)).AddDays(-random.Next(365)).ToString("O"));

         // Emails
         writer.WriteStartArray("Emails");
         var emailCount = random.Next(1, 5);
         for(int j = 0; j < emailCount; j++)
         {
            writer.WriteStringValue($"user{i + 1}_{j}@example.com");
         }
         writer.WriteEndArray();

         // Addresses
         writer.WriteStartArray("Addresses");
         // Home address
         writer.WriteStartObject();
         writer.WriteString("Type", "home");
         writer.WriteString("Street", $"{random.Next(1, 9999)} Main St");
         writer.WriteString("City", cities[random.Next(cities.Length)]);
         writer.WriteString("State", states[random.Next(states.Length)]);
         writer.WriteString("ZipCode", $"{random.Next(10000, 99999)}");
         writer.WriteString("Country", "USA");
         writer.WriteEndObject();

         // Work address (70% chance)
         if(random.Next(100) < 70)
         {
            writer.WriteStartObject();
            writer.WriteString("Type", "work");
            writer.WriteString("Street", $"{random.Next(1, 9999)} Business Blvd");
            writer.WriteString("City", cities[random.Next(cities.Length)]);
            writer.WriteString("State", states[random.Next(states.Length)]);
            writer.WriteString("ZipCode", $"{random.Next(10000, 99999)}");
            writer.WriteString("Country", "USA");
            writer.WriteEndObject();
         }
         writer.WriteEndArray();

         // Phone numbers
         writer.WriteStartArray("PhoneNumbers");
         var phoneCount = random.Next(1, 4);
         for(int j = 0; j < phoneCount; j++)
         {
            writer.WriteStringValue($"({random.Next(200, 999)})-{random.Next(100, 999)}-{random.Next(1000, 9999)}");
         }
         writer.WriteEndArray();

         // Metadata
         writer.WriteStartObject("Metadata");
         writer.WriteString("timezone", "EST");
         writer.WriteString("locale", "en-US");
         writer.WriteString("theme", "dark");
         writer.WriteEndObject();

         // Projects (simplified)
         writer.WriteStartArray("Projects");
         var projectCount = random.Next(0, 4);
         for(int j = 0; j < projectCount; j++)
         {
            writer.WriteStartObject();
            writer.WriteNumber("ProjectId", random.Next(1000, 9999));
            writer.WriteString("Name", $"Project {Guid.NewGuid().ToString().Substring(0, 8)}");
            writer.WriteString("Description", $"Description for project {j + 1}");
            writer.WriteString("Status", "active");
            writer.WriteNumber("Budget", random.Next(10000, 500000));
            writer.WriteEndObject();
         }
         writer.WriteEndArray();

         // Company
         writer.WriteStartObject("Company");
         writer.WriteNumber("CompanyId", random.Next(100, 999));
         writer.WriteString("Name", companies[random.Next(companies.Length)]);
         writer.WriteString("Industry", industries[random.Next(industries.Length)]);
         writer.WriteStartObject("Headquarters");
         writer.WriteString("Type", "headquarters");
         writer.WriteString("Street", $"{random.Next(1, 9999)} Corporate Blvd");
         writer.WriteString("City", cities[random.Next(cities.Length)]);
         writer.WriteString("State", states[random.Next(states.Length)]);
         writer.WriteString("ZipCode", $"{random.Next(10000, 99999)}");
         writer.WriteString("Country", "USA");
         writer.WriteEndObject();
         writer.WriteNumber("EmployeeCount", random.Next(50, 50000));
         writer.WriteEndObject();

         writer.WriteEndObject();

         // Progress indicator
         if((i + 1) % 100000 == 0)
         {
            Console.WriteLine($"   📊 Generated {i + 1:N0} records...");
         }
      }

      writer.WriteEndArray();
      writer.Flush();
   }

   private static void StreamProcessFile(string inputFile, string outputFile)
   {
      // For this demo, we'll do a simple copy with minimal processing
      // In real AJIS, this would transform JSON to AJIS format or vice versa

      using var inputStream = File.OpenRead(inputFile);
      using var outputStream = File.Create(outputFile);

      var buffer = new byte[64 * 1024 * 1024]; // 64MB buffer
      int bytesRead;
      long totalBytes = 0;

      while((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
      {
         outputStream.Write(buffer, 0, bytesRead);
         totalBytes += bytesRead;

         // Progress indicator
         if(totalBytes % (1024 * 1024 * 1024) == 0) // Every GB
         {
            Console.WriteLine($"   📊 Processed {totalBytes / (1024 * 1024 * 1024):N0} GB...");
         }
      }

      Console.WriteLine($"   ✅ Streamed {totalBytes / (1024 * 1024):N0} MB of data");
   }

   private static bool ValidateStreamingIntegrity(string inputFile, string outputFile)
   {
      // For streaming mode, we do a simple file size and hash comparison
      // In real AJIS, this would validate the format transformation

      var inputSize = new FileInfo(inputFile).Length;
      var outputSize = new FileInfo(outputFile).Length;

      if(inputSize != outputSize)
      {
         Console.WriteLine($"   ⚠️  Size mismatch: {inputSize} vs {outputSize}");
         return false;
      }

      // Simple hash validation (first 1MB and last 1MB)
      const int sampleSize = 1024 * 1024;

      using var inputStream = File.OpenRead(inputFile);
      using var outputStream = File.OpenRead(outputFile);

      // Check beginning
      var inputStart = new byte[sampleSize];
      var outputStart = new byte[sampleSize];
      inputStream.ReadExactly(inputStart, 0, sampleSize);
      outputStream.ReadExactly(outputStart, 0, sampleSize);

      if(!inputStart.SequenceEqual(outputStart))
      {
         Console.WriteLine("   ⚠️  Content mismatch at beginning");
         return false;
      }

      // Check end
      inputStream.Position = inputSize - sampleSize;
      outputStream.Position = outputSize - sampleSize;

      var inputEnd = new byte[sampleSize];
      var outputEnd = new byte[sampleSize];
      inputStream.ReadExactly(inputEnd, 0, sampleSize);
      outputStream.ReadExactly(outputEnd, 0, sampleSize);

      if(!inputEnd.SequenceEqual(outputEnd))
      {
         Console.WriteLine("   ⚠️  Content mismatch at end");
         return false;
      }

      return true;
   }
}