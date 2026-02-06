# Afrowave.AJIS.Serialization

AJIS serialization APIs and segment-based serializers.

## Status

* `AjisSerialize.ToText` supports compact serialization from segments.
* `AjisSerialize.ToStreamAsync` supports compact serialization from segments.
* `AjisSerializer` supports compact serialization for AjisValue (text/stream/bytes).
* Strings are escaped for JSON-compatible output (\" \\ \n \r \t and control chars).
* Comment/directive segments are ignored during serialization.
* Set `AjisSerializationOptions.Compact = false` to emit spaces after commas/colons.
