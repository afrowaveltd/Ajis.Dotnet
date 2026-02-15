#nullable enable

namespace Afrowave.AJIS.Core;

/// <summary>
/// Extension methods for BinaryAttachment.
/// </summary>
public static class BinaryAttachmentExtensions
{
   /// <summary>
   /// Creates a binary attachment from a Stream.
   /// </summary>
   /// <param name="stream">The stream to read from.</param>
   /// <param name="fileName">Optional file name.</param>
   /// <param name="contentType">Optional MIME type.</param>
   /// <returns>The populated attachment.</returns>
   public static BinaryAttachment FromStream(Stream stream, string? fileName = null, string? contentType = null)
   {
      if(stream == null)
         throw new ArgumentNullException(nameof(stream));

      if(!stream.CanRead)
         throw new ArgumentException("Stream is not readable", nameof(stream));

      var data = new byte[stream.Length];
      stream.ReadExactly(data);

      var attachment = new BinaryAttachment
      {
         FileName = fileName ?? "unknown",
         MimeType = contentType ?? AjisAttachmentHelper.GetMimeType(Path.GetExtension(fileName ?? "")),
         Data = data,
         FileSize = data.Length,
         OriginalSize = data.Length
      };

      attachment.ComputeChecksum();
      return attachment;
   }

   /// <summary>
   /// Creates a binary attachment from a byte array.
   /// </summary>
   /// <param name="data">The byte array containing file data.</param>
   /// <param name="fileName">Optional file name.</param>
   /// <param name="contentType">Optional MIME type.</param>
   /// <returns>The populated attachment.</returns>
   public static BinaryAttachment FromBytes(byte[] data, string? fileName = null, string? contentType = null)
   {
      if(data == null)
         throw new ArgumentNullException(nameof(data));

      var attachment = new BinaryAttachment
      {
         FileName = fileName ?? "unknown",
         MimeType = contentType ?? AjisAttachmentHelper.GetMimeType(Path.GetExtension(fileName ?? "")),
         Data = data,
         FileSize = data.Length,
         OriginalSize = data.Length
      };

      attachment.ComputeChecksum();
      return attachment;
   }

   /// <summary>
   /// Saves the attachment to a file stream.
   /// </summary>
   /// <param name="attachment">The attachment to save.</param>
   /// <param name="stream">The stream to write to.</param>
   public static void SaveToStream(BinaryAttachment attachment, Stream stream)
   {
      if(attachment == null)
         throw new ArgumentNullException(nameof(attachment));

      if(stream == null)
         throw new ArgumentNullException(nameof(stream));

      if(!stream.CanWrite)
         throw new ArgumentException("Stream is not writable", nameof(stream));

      stream.Write(attachment.Data);
   }

   /// <summary>
   /// Saves the attachment to a file path.
   /// </summary>
   /// <param name="attachment">The attachment to save.</param>
   /// <param name="filePath">The path where the file should be saved.</param>
   /// <param name="cancellationToken">Cancellation token.</param>
   /// <returns>Task representing the asynchronous operation.</returns>
   public static async Task SaveToFileAsync(BinaryAttachment attachment, string filePath, CancellationToken cancellationToken = default)
   {
      if(attachment == null)
         throw new ArgumentNullException(nameof(attachment));

      string? directory = Path.GetDirectoryName(filePath);
      if(!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
         Directory.CreateDirectory(directory);

      await File.WriteAllBytesAsync(filePath, attachment.Data, cancellationToken);
   }

   /// <summary>
   /// Opens the attachment data as a readable stream.
   /// </summary>
   /// <param name="attachment">The attachment.</param>
   /// <returns>A MemoryStream containing the attachment data.</returns>
   public static Stream OpenReadStream(BinaryAttachment attachment)
   {
      if(attachment == null)
         throw new ArgumentNullException(nameof(attachment));

      return new MemoryStream(attachment.Data);
   }

   /// <summary>
   /// Gets the attachment data as a Base64-encoded string.
   /// </summary>
   /// <param name="attachment">The attachment.</param>
   /// <returns>Base64-encoded string of the attachment data.</returns>
   public static string ToBase64String(BinaryAttachment attachment)
   {
      if(attachment == null)
         throw new ArgumentNullException(nameof(attachment));

      return Convert.ToBase64String(attachment.Data);
   }

   /// <summary>
   /// Creates a binary attachment from a Base64-encoded string.
   /// </summary>
   /// <param name="base64String">The Base64 string containing the data.</param>
   /// <param name="fileName">Optional file name.</param>
   /// <param name="contentType">Optional MIME type.</param>
   /// <returns>The populated attachment.</returns>
   public static BinaryAttachment FromBase64String(string base64String, string? fileName = null, string? contentType = null)
   {
      if(string.IsNullOrEmpty(base64String))
         throw new ArgumentException("Base64 string cannot be null or empty", nameof(base64String));

      var data = Convert.FromBase64String(base64String);

      var attachment = new BinaryAttachment
      {
         FileName = fileName ?? "unknown",
         MimeType = contentType ?? AjisAttachmentHelper.GetMimeType(Path.GetExtension(fileName ?? "")),
         Data = data,
         FileSize = data.Length,
         OriginalSize = data.Length
      };

      attachment.ComputeChecksum();
      return attachment;
   }

   /// <summary>
   /// Clears the attachment data and resets checksums.
   /// </summary>
   /// <param name="attachment">The attachment to clear.</param>
   public static void Clear(BinaryAttachment attachment)
   {
      if(attachment == null)
         throw new ArgumentNullException(nameof(attachment));

      attachment.Data = [];
      attachment.Checksum = "";
      attachment.FileSize = 0;
      attachment.OriginalSize = 0;
   }
}
