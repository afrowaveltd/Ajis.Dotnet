#nullable enable

using System.Text;

namespace Afrowave.AJIS.Testing.TestData;

public static class AjisLargePayloadGenerator
{
   public static void WriteUsersJson(Stream output, int userCount, int addressesPerUser)
   {
      ArgumentNullException.ThrowIfNull(output);
      if(userCount < 0) throw new ArgumentOutOfRangeException(nameof(userCount));
      if(addressesPerUser < 0) throw new ArgumentOutOfRangeException(nameof(addressesPerUser));

      using var writer = new StreamWriter(output, new UTF8Encoding(false), leaveOpen: true);
      writer.Write("{\"users\":[");

      for(int i = 0; i < userCount; i++)
      {
         if(i > 0) writer.Write(',');

         writer.Write("{\"id\":");
         writer.Write(i + 1);
         writer.Write(",\"name\":\"User");
         writer.Write(i + 1);
         writer.Write("\",\"addresses\":[");

         for(int a = 0; a < addressesPerUser; a++)
         {
            if(a > 0) writer.Write(',');

            writer.Write("{\"street\":\"Street ");
            writer.Write(a + 1);
            writer.Write("\",\"city\":\"City ");
            writer.Write(i + 1);
            writer.Write("\",\"zip\":\"");
            writer.Write(10000 + a);
            writer.Write("\"}");
         }

         writer.Write("]}");
      }

      writer.Write("]}");
      writer.Flush();
   }

   public static void WriteUsersJsonFile(string path, int userCount, int addressesPerUser)
   {
      ArgumentException.ThrowIfNullOrWhiteSpace(path);

      Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");

      using var stream = File.Create(path);
      WriteUsersJson(stream, userCount, addressesPerUser);
   }
}
