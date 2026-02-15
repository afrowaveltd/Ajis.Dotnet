using System;
using System.Collections.Generic;
using System.IO;
using AjisFile = Afrowave.AJIS.IO.AjisFile;

class GenerateData
{
    static void Main(string[] args)
    {
        string testDir = @"E:\C#\Ajis.Dotnet\tests\AjisCountriesTest\test_data_ajis\json\valid";
        string legacyDir = @"E:\C#\Ajis.Dotnet\tests\AjisCountriesTest\test_data_legacy";
        
        List<AjisCountriesTest.Country> allCountries = new();
        
        if (Directory.Exists(testDir))
        {
            foreach (var jsonFile in Directory.GetFiles(testDir, "*.json"))
            {
                var json = File.ReadAllText(jsonFile);
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var countries = System.Text.Json.JsonSerializer.Deserialize<List<AjisCountriesTest.Country>>(json, options);
                if (countries != null)
                    allCountries.AddRange(countries);
            }
        }
        
        if (Directory.Exists(legacyDir))
        {
            foreach (var jsonFile in Directory.GetFiles(legacyDir, "*.json"))
            {
                var json = File.ReadAllText(jsonFile);
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var countries = System.Text.Json.JsonSerializer.Deserialize<List<AjisCountriesTest.Country>>(json, options);
                if (countries != null)
                    allCountries.AddRange(countries);
            }
        }
        
        string outputDir = @"E:\C#\Ajis.Dotnet\tests\AjisCountriesTest\bin\Debug\net10.0";
        string outputFile = Path.Combine(outputDir, "countries.ajis");
        
        Directory.CreateDirectory(outputDir);
        AjisFile.Create(outputFile, allCountries);
        
        Console.WriteLine($"Created {outputFile} with {allCountries.Count} countries");
    }
}
