using Afrowave.AJIS.Testing.TestData;

if(args.Length == 0)
{
   PrintUsage();
   return;
}

string command = args[0].ToLowerInvariant();

switch(command)
{
   case "generate":
      RunGenerate(args);
      break;
   case "benchmark":
      await RunBenchmarkAsync(args);
      break;
   default:
      PrintUsage();
      break;
}

static void RunGenerate(string[] args)
{
   if(args.Length < 2)
   {
      PrintUsage();
      return;
   }

   string outputPath = args[1];
   int userCount = args.Length > 2 && int.TryParse(args[2], out int u) ? u : 150000;
   int addressesPerUser = args.Length > 3 && int.TryParse(args[3], out int a) ? a : 3;

   AjisLargePayloadGenerator.WriteUsersJsonFile(outputPath, userCount, addressesPerUser);

   Console.WriteLine($"Generated {userCount} users with {addressesPerUser} addresses to {outputPath}");
}

static async Task RunBenchmarkAsync(string[] args)
{
   if(args.Length < 2)
   {
      PrintUsage();
      return;
   }

   string inputPath = args[1];
   var results = await AjisBenchmarkRunner.RunAsync(inputPath);

   Console.WriteLine("Name\tElapsed(ms)\tMemoryDelta(bytes)\tUnits");
   foreach(var result in results)
   {
      Console.WriteLine($"{result.Name}\t{result.Elapsed.TotalMilliseconds:F2}\t{result.MemoryDeltaBytes}\t{result.SegmentCount}");
   }
}

static void PrintUsage()
{
   Console.WriteLine("Usage:");
   Console.WriteLine("  Ajis.Benchmarks generate <outputPath> [userCount] [addressesPerUser]");
   Console.WriteLine("  Ajis.Benchmarks benchmark <inputPath>");
   Console.WriteLine("Example:");
   Console.WriteLine("  Ajis.Benchmarks generate test_data_ajis/big/users_150k.json 150000 3");
   Console.WriteLine("  Ajis.Benchmarks benchmark test_data_ajis/big/users_150k.json");
}
