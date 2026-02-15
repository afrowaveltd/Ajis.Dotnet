using System;
using System.Collections.Generic;
using System.IO;
using AjisFile = Afrowave.AJIS.IO.AjisFile;

namespace AjisCountriesTest.DataGenerator;

class GenerateData
{
    static void Main(string[] args)
    {
        string testDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test_data_ajis", "json", "valid");
        string legacyDir = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "test_data_legacy");
        
        List<AjisCountriesTest.Country> allCountries = new();
        
        if (Directory.Exists(testDir))
        {
            foreach (var jsonFile in Directory.GetFiles(testDir, "*.json"))
            {
                var json = File.ReadAllText(jsonFile);
                var countries = System.Text.Json.JsonSerializer.Deserialize<List<AjisCountriesTest.Country>>(json);
                if (countries != null)
                    allCountries.AddRange(countries);
            }
        }
        
        if (Directory.Exists(legacyDir))
        {
            foreach (var jsonFile in Directory.GetFiles(legacyDir, "*.json"))
            {
                var json = File.ReadAllText(jsonFile);
                var countries = System.Text.Json.JsonSerializer.Deserialize<List<AjisCountriesTest.Country>>(json);
                if (countries != null)
                    allCountries.AddRange(countries);
            }
        }
        
        string outputDir = Path.Combine(AppContext.BaseDirectory);
        string outputFile = Path.Combine(outputDir, "countries.ajis");
        
        Directory.CreateDirectory(outputDir);
        AjisFile.Create(outputFile, allCountries);
        
        Console.WriteLine($"Created {outputFile} with {allCountries.Count} countries");
    }
}
