#nullable enable

using Afrowave.AJIS.Core;
using System.Linq.Expressions;

namespace Afrowave.AJIS.IO;

/// <summary>
/// Base class for configuring AJIS entities with metadata and mapping options.
/// </summary>
/// <typeparam name="T">The entity type to configure.</typeparam>
public class AjisEntityConfiguration<T> where T : class
{
   /// <summary>
   /// The primary key property expression.
   /// </summary>
   protected Expression<Func<T, object>>? _keyExpression;

   /// <summary>
   /// The primary key property name.
   /// </summary>
   protected string? _keyPropertyName;

   /// <summary>
   /// Properties that are required.
   /// </summary>
   protected readonly HashSet<string> _requiredProperties = [];

   /// <summary>
   /// Binary attachment properties.
   /// </summary>
   protected readonly Dictionary<string, BinaryAttachmentConfiguration> _binaryAttachments = [];

   /// <summary>
   /// Gets the primary key property name.
   /// </summary>
   public string? KeyPropertyName => _keyPropertyName;

   /// <summary>
   /// Gets the entity type being configured.
   /// </summary>
   public Type EntityType { get; } = typeof(T);

   /// <summary>
   /// Configures the primary key property.
   /// </summary>
   /// <param name="expression">Expression pointing to the primary key property.</param>
   public virtual void Key(Expression<Func<T, object>> expression)
   {
      _keyExpression = expression;

      if(expression.Body is MemberExpression memberExpression)
      {
         _keyPropertyName = memberExpression.Member.Name;
      }
      else if(expression.Body is UnaryExpression unaryExpression &&
               unaryExpression.Operand is MemberExpression unaryMember)
      {
         _keyPropertyName = unaryMember.Member.Name;
      }
   }

   /// <summary>
   /// Configures a property as required.
   /// </summary>
   /// <param name="expression">Expression pointing to the property.</param>
   public virtual void Property(Expression<Func<T, object>> expression)
   {
      if(expression.Body is MemberExpression memberExpression)
      {
         _requiredProperties.Add(memberExpression.Member.Name);
      }
      else if(expression.Body is UnaryExpression unaryExpression &&
               unaryExpression.Operand is MemberExpression unaryMember)
      {
         _requiredProperties.Add(unaryMember.Member.Name);
      }
   }

   /// <summary>
   /// Configures a binary attachment property.
   /// </summary>
   /// <param name="expression">Expression pointing to the binary attachment property.</param>
   /// <param name="configurator">Configuration action for the attachment.</param>
   public virtual void BinaryAttachment(
       Expression<Func<T, BinaryAttachment>> expression,
       Action<BinaryAttachmentConfiguration> configurator)
   {
      if(expression.Body is MemberExpression memberExpression)
      {
         string propertyName = memberExpression.Member.Name;
         var config = new BinaryAttachmentConfiguration();
         configurator(config);
         _binaryAttachments[propertyName] = config;
      }
   }

   /// <summary>
   /// Configures a binary attachment property with default settings.
   /// </summary>
   /// <param name="expression">Expression pointing to the binary attachment property.</param>
   public virtual void BinaryAttachment(Expression<Func<T, BinaryAttachment>> expression)
   {
      BinaryAttachment(expression, _ => { });
   }

   /// <summary>
   /// Checks if a property is required.
   /// </summary>
   /// <param name="propertyName">The property name to check.</param>
   /// <returns>True if the property is required, otherwise false.</returns>
   public bool IsRequired(string propertyName)
   {
      return _requiredProperties.Contains(propertyName);
   }

   /// <summary>
   /// Gets the binary attachment configuration for a property.
   /// </summary>
   /// <param name="propertyName">The property name.</param>
   /// <returns>The binary attachment configuration, or null if not configured.</returns>
   public BinaryAttachmentConfiguration? GetBinaryAttachmentConfig(string propertyName)
   {
      return _binaryAttachments.TryGetValue(propertyName, out var config) ? config : null;
   }

   /// <summary>
   /// Checks if the entity has any binary attachments.
   /// </summary>
   /// <returns>True if the entity has binary attachments, otherwise false.</returns>
   public bool HasBinaryAttachments()
   {
      return _binaryAttachments.Count > 0;
   }

   /// <summary>
   /// Gets all configured binary attachment property names.
   /// </summary>
   /// <returns>List of binary attachment property names.</returns>
   public IEnumerable<string> GetBinaryAttachmentProperties()
   {
      return _binaryAttachments.Keys;
   }
}

/// <summary>
/// Configuration for a binary attachment property.
/// </summary>
public class BinaryAttachmentConfiguration
{
   /// <summary>
   /// Enable automatic compression for the attachment. Default: true.
   /// </summary>
   public bool AutoCompress { get; set; } = true;

   /// <summary>
   /// Maximum file size in bytes. 0 = unlimited. Default: 0.
   /// </summary>
   public long MaxFileSize { get; set; } = 0;

   /// <summary>
   /// Storage location for the attachment.
   /// - Internal: Store inside the ATP file (default)
   /// - External: Store as separate files
   /// </summary>
   public AttachmentStorage Location { get; set; } = AttachmentStorage.Internal;

   /// <summary>
   /// Enable checksum verification. Default: true.
   /// </summary>
   public bool VerifyChecksum { get; set; } = true;
}

/// <summary>
/// Specifies where binary attachments are stored.
/// </summary>
public enum AttachmentStorage
{
   /// <summary>
   /// Store attachments inside the ATP file (native ATP format).
   /// </summary>
   Internal = 0,

   /// <summary>
   /// Store attachments as separate external files.
   /// </summary>
   External = 1
}