# AJIS Text Format – Deep Dive

> Normative specification of the AJIS text layer (beyond JSON)

---

## 1. Purpose

This document defines the **AJIS text format** in detail.

AJIS is **JSON-compatible by design**, but extends JSON in controlled,
explicitly specified ways to support:

* human-friendly authoring
* large-scale data
* transport scenarios (ATP)
* deterministic parsing

This document focuses **only on the text layer**.

---

## 2. Relationship to JSON

### 2.1 Compatibility goals

* Every valid JSON document is a valid AJIS document
* AJIS parsers may accept additional constructs
* AJIS serializers can emit strict-JSON-compatible output if configured

### 2.2 Non-goals

* AJIS does not attempt to be JavaScript
* No executable expressions
* No implicit type coercions

---

## 3. Whitespace and layout

### 3.1 Whitespace characters

The following characters are treated as whitespace:

* space (U+0020)
* horizontal tab (U+0009)
* line feed (U+000A)
* carriage return (U+000D)

Whitespace may appear:

* between any tokens
* before or after structural characters

Whitespace is **not significant**, except inside strings.

---

## 4. Comments

AJIS supports comments for human readability.

### 4.1 Line comments

```
// this is a comment
```

* valid until end-of-line

### 4.2 Block comments

```
/* multi-line
   comment */
```

* block comments may span lines
* block comments do not nest

### 4.3 Rules

* comments are treated as whitespace
* comments must not appear inside strings
* serializers may omit comments

---

## 5. Strings

### 5.1 Basic syntax

Strings are delimited by double quotes:

```
"example"
```

### 5.2 Escape sequences

Supported escapes:

* `\\` backslash
* `\"` double quote
* `\n` newline
* `\r` carriage return
* `\t` tab
* `\uXXXX` Unicode code point

### 5.3 Multiline strings

AJIS allows multiline strings:

* newline characters may appear inside strings
* line breaks are preserved

Example:

```
"line one
line two"
```

### 5.4 Parsing rule

Once a string starts (`"`):

* all characters are ignored until a **non-escaped closing quote**
* `{`, `}`, `[`, `]` inside strings do **not** affect nesting

This rule is **absolute** and simplifies streaming parsing.

---

## 6. Numbers

AJIS extends JSON numbers with additional bases and separators.

### 6.1 Supported bases

| Base        | Prefix | Example  |
| ----------- | ------ | -------- |
| Binary      | `0b`   | `0b1010` |
| Octal       | `0o`   | `0o755`  |
| Decimal     | none   | `12345`  |
| Hexadecimal | `0x`   | `0xFF`   |

### 6.2 Digit separators

AJIS allows `_` as a digit separator:

* improves readability
* ignored during numeric parsing

Rules:

* decimal: separator every 3 digits (from right)
* octal: separator every 3 digits
* hexadecimal: separator every **2 or 4 digits** (not mixed)
* binary: separator every **4 digits**

Examples:

```
1_000_000
0xFF_EE
0b1010_1100
```

Rules:

* first group may be shorter
* separators are **not allowed** after decimal point

---

## 7. Booleans and null

Same as JSON:

* `true`
* `false`
* `null`

Case-sensitive.

---

## 8. Objects

Objects use JSON syntax:

```
{
  "key": "value"
}
```

Rules:

* keys must be strings
* duplicate keys are allowed or forbidden depending on settings

---

## 9. Arrays

Arrays use JSON syntax:

```
[1, 2, 3]
```

Trailing commas may be allowed depending on settings.

---

## 10. Trailing commas (optional)

AJIS may allow trailing commas:

```
{
  "a": 1,
}
```

This behavior is controlled by `AjisSettings`.

---

## 11. Error handling and diagnostics

AJIS parsers must:

* detect syntax errors precisely
* report:

  * byte offset
  * line and column
  * error code

Errors must be raised **as soon as detected**.

---

## 12. End-of-text boundary

AJIS text parsing stops:

* at end-of-stream, or
* at the first byte following a complete root value

This enables:

* AJIS + binary concatenation
* ATP container usage

Parser must not consume bytes beyond the text boundary.

---

## 13. Canonical text output

Serializers may emit canonical AJIS:

* normalized whitespace
* normalized number representation
* optional JSON-compatibility mode

Canonicalization is optional and settings-controlled.

---

## 14. Summary

AJIS text format:

* is a strict superset of JSON
* supports comments and multiline strings
* supports readable numeric formats
* is designed for streaming parsing
* cleanly hands off to binary data

---

## 15. Status

Stable.

This document defines the normative AJIS text format.
