#nullable enable

namespace Afrowave.AJIS.Serialization.Mapping;

/// <summary>
/// Specifies a custom AJIS key name for a property during serialization/deserialization.
/// </summary>
/// <remarks>
/// <para>
/// This attribute allows you to map a .NET property to a different AJIS key name,
/// overriding the naming policy for that specific property.
/// </para>
/// <para>
/// Example:
/// <code>
/// public class User
/// {
///     [AjisPropertyName("user_id")]
///     public int UserId { get; set; }
/// }
/// </code>
/// When serialized, the `UserId` property will appear as "user_id" in the AJIS output,
/// regardless of the naming policy in use.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AjisPropertyNameAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AjisPropertyNameAttribute"/> class.
    /// </summary>
    /// <param name="name">The AJIS key name to use for this property.</param>
    /// <exception cref="ArgumentNullException">Thrown if name is null.</exception>
    public AjisPropertyNameAttribute(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Gets the AJIS key name for this property.
    /// </summary>
    public string Name { get; }
}

/// <summary>
/// Marks a property as ignored during AJIS serialization and deserialization.
/// </summary>
/// <remarks>
/// <para>
/// Properties decorated with this attribute will not be included in AJIS serialization
/// and will not be set during deserialization.
/// </para>
/// <para>
/// Example:
/// <code>
/// public class User
/// {
///     public string Name { get; set; }
///     
///     [AjisIgnore]
///     public string Password { get; set; }
/// }
/// </code>
/// When serialized, the `Password` property will not appear in the AJIS output.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AjisIgnoreAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AjisIgnoreAttribute"/> class.
    /// </summary>
    public AjisIgnoreAttribute()
    {
    }
}

/// <summary>
/// Marks a property as required and cannot be null during deserialization.
/// </summary>
/// <remarks>
/// <para>
/// Properties decorated with this attribute must have a non-null value in the AJIS input.
/// If a required property is missing or null, deserialization will throw an exception.
/// </para>
/// <para>
/// Example:
/// <code>
/// public class User
/// {
///     [AjisRequired]
///     public string Email { get; set; }
///     
///     public string? Phone { get; set; }
/// }
/// </code>
/// If `Email` is missing or null in the AJIS input, deserialization will fail with a clear error message.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AjisRequiredAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AjisRequiredAttribute"/> class.
    /// </summary>
    public AjisRequiredAttribute()
    {
    }
}

/// <summary>
/// Specifies a custom numeric format for a number property.
/// </summary>
/// <remarks>
/// <para>
/// This attribute controls how numeric values are serialized and deserialized.
/// </para>
/// <para>
/// Example:
/// <code>
/// public class Config
/// {
///     [AjisNumberFormat(AjisNumberStyle.Hex)]
///     public int Color { get; set; }
/// }
/// </code>
/// If `Color` is 255, it will be serialized as "0xFF" instead of "255".
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AjisNumberFormatAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AjisNumberFormatAttribute"/> class.
    /// </summary>
    /// <param name="style">The numeric format style.</param>
    public AjisNumberFormatAttribute(AjisNumberStyle style)
    {
        Style = style;
    }

    /// <summary>
    /// Gets the numeric format style.
    /// </summary>
    public AjisNumberStyle Style { get; }
}

/// <summary>
/// Specifies numeric format styles for AJIS serialization.
/// </summary>
public enum AjisNumberStyle
{
    /// <summary>
    /// Standard decimal format (e.g., 255).
    /// </summary>
    Decimal = 0,

    /// <summary>
    /// Hexadecimal format (e.g., 0xFF).
    /// </summary>
    Hex = 1,

    /// <summary>
    /// Binary format (e.g., 0b11111111).
    /// </summary>
    Binary = 2,

    /// <summary>
    /// Octal format (e.g., 0o377).
    /// </summary>
    Octal = 3
}
