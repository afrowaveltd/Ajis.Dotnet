# M5 – LAX Parser (JavaScript-Tolerant) Implementation Status

> **Status:** IN PROGRESS → COMPLETION
>
> This document tracks the M5 (LAX Parser) milestone for JavaScript-tolerant AJIS parsing.

---

## 1. M5 Scope

M5 implements a LAX (relaxed) mode parser that tolerates JavaScript-like syntax while maintaining AJIS structure. This enables parsing of JavaScript objects and comments that are valid AJIS-compatible but violate strict JSON/AJIS rules.

**Core Features:**

* Unquoted object keys (identifiers)
* Single-quoted strings (`'...'`)
* JavaScript-style comments (`//`, `/* */`)
* Trailing commas in objects and arrays
* Clear diagnostics marking relaxed constructs
* Full backward compatibility with strict modes

---

## 2. LAX Mode Features

### 2.1 Unquoted Keys

**Syntax:**
```
{ name: "Alice", age: 30 }  // instead of { "name": "Alice", "age": 30 }
```

**Constraints:**
- Keys must be valid identifiers (start with letter, underscore, or $)
- Can contain letters, digits, underscores, $
- No special characters allowed (no spaces, hyphens, dots, etc. in unquoted form)

**Example Valid:**
```
{ _id: 1, $type: "User", name123: "test" }
```

**Example Invalid (must quote):**
```
{ "my-key": 1 }     // hyphens require quotes
{ "my.key": 1 }     // dots require quotes
{ "my key": 1 }     // spaces require quotes
```

### 2.2 Single-Quoted Strings

**Syntax:**
```
{ key: 'value' }    // instead of { "key": "value" }
['item1', 'item2']  // instead of ["item1", "item2"]
```

**Escaping:**
- `\'` for literal single quote
- `\\` for literal backslash
- Standard JSON escape sequences still apply

### 2.3 JavaScript Comments

**Line Comments:**
```
{
  name: "Alice",  // This is a person
  age: 30         // Their age
}
```

**Block Comments:**
```
{
  /* User object */
  name: "Alice",
  /* Contact details */
  email: "alice@example.com"
}
```

### 2.4 Trailing Commas

**Syntax:**
```
{
  name: "Alice",
  email: "alice@example.com",    // trailing comma allowed
}

[
  1,
  2,
  3,    // trailing comma allowed
]
```

---

## 3. Current Implementation Status

### 3.1 Existing Features

**AjisTextMode Enum:**
- [x] AjisTextMode.Json (strict JSON)
- [x] AjisTextMode.Ajis (AJIS strict mode)
- [x] AjisTextMode.Lex (permissive mode - needs investigation)
- [ ] AjisTextMode.Lax (NEW for M5)

**Lexer Capabilities:**
- [x] String parsing (double-quoted)
- [x] Comment tokenization (when enabled)
- [x] Number parsing (all bases)
- [x] Identifier recognition
- [x] Token offset/position tracking

**Gaps:**
- [ ] Single-quoted string support
- [ ] Unquoted key support in parser
- [ ] JavaScript comment styles (need to verify current support)
- [ ] Trailing comma tolerance in parser (partially exists)

---

## 4. M5 Implementation Strategy

### 4.1 Phase 1: Lexer Extensions

**Add to AjisLexer:**
1. Single-quoted string token recognition
2. Preserve quote style in token metadata
3. JavaScript comment style support (`//` and `/* */`)
4. Validate escape sequences in single-quoted strings

### 4.2 Phase 2: Parser Integration

**Modify AjisLexerParser:**
1. Accept unquoted identifiers as property names
2. Handle single-quoted string values
3. Support trailing commas in array/object contexts
4. Emit diagnostic indicators for LAX constructs

### 4.3 Phase 3: Streaming Integration

**Update AjisLexerParserStreamingAsync:**
1. LAX mode support in async parser
2. Memory bounds still enforced
3. Proper segment emission with diagnostics

---

## 5. M5 Completeness Checklist

### 5.1 Functional Requirements

- [ ] Unquoted identifier keys recognized and parsed
- [ ] Single-quoted strings parsed and unescaped correctly
- [ ] JavaScript comments (`//` and `/* */`) handled
- [ ] Trailing commas allowed in objects and arrays
- [ ] Strict modes (JSON, AJIS) remain unchanged
- [ ] Diagnostics clearly mark LAX constructs
- [ ] Error messages helpful for non-LAX mode
- [ ] Memory bounds maintained in LAX mode

### 5.2 Testing Requirements

- [ ] Unquoted key tests (valid and invalid identifiers)
- [ ] Single-quoted string tests (with escapes)
- [ ] JavaScript comment tests (line and block)
- [ ] Trailing comma tests (objects and arrays)
- [ ] Mixed LAX syntax tests (all features together)
- [ ] Backward compatibility tests (JSON/AJIS unchanged)
- [ ] Error handling tests (what fails in LAX mode)
- [ ] Round-trip validation (LAX → serialize → re-parse)

### 5.3 Documentation Requirements

- [ ] M5 specification complete
- [ ] Full XML documentation on all public members
- [ ] Feature descriptions with examples
- [ ] Diagnostic messages documented
- [ ] Test coverage recorded

---

## 6. Test Matrix

### 6.1 Unquoted Key Tests

| Test Case | Input | Expected Output | Status |
|-----------|-------|-----------------|--------|
| Valid identifier | `{x:1}` | Parse as object with key "x" | [ ] |
| Underscore start | `{_id:1}` | Valid key | [ ] |
| Dollar start | `{$type:1}` | Valid key | [ ] |
| Mixed alphanumeric | `{key123:1}` | Valid key | [ ] |
| Digit start | `{1invalid:1}` | Error (invalid identifier) | [ ] |
| Hyphen in key | `{my-key:1}` | Error (invalid identifier) | [ ] |
| Space in key | `{my key:1}` | Error (invalid identifier) | [ ] |

### 6.2 Single-Quoted String Tests

| Test Case | Input | Expected | Status |
|-----------|-------|----------|--------|
| Simple value | `'hello'` | String "hello" | [ ] |
| With escape | `'it\'s'` | String "it's" | [ ] |
| Backslash escape | `'c:\\path'` | String "c:\path" | [ ] |
| Empty string | `''` | Empty string | [ ] |
| Mixed quotes | `['single', "double"]` | Both parsed | [ ] |

### 6.3 Comment Tests

| Test Case | Input | Expected | Status |
|-----------|-------|----------|--------|
| Line comment | `1 // comment` | Parse 1, skip comment | [ ] |
| Block comment | `/* x */ 1` | Parse 1, skip comment | [ ] |
| Nested block | `/* /* nested */ */` | Parse correctly | [ ] |
| In array | `[1, // item\n 2]` | Parse array [1, 2] | [ ] |

### 6.4 Trailing Comma Tests

| Test Case | Input | Expected | Status |
|-----------|-------|----------|--------|
| Object trailing | `{a:1,}` | Parse as {a:1} | [ ] |
| Array trailing | `[1,]` | Parse as [1] | [ ] |
| Nested trailing | `{x:[1,],}` | Parse correctly | [ ] |
| Empty object | `{}` | Valid empty object | [ ] |
| Empty array | `[]` | Valid empty array | [ ] |

### 6.5 Backward Compatibility Tests

| Test Case | Input | JSON | AJIS | Lex |
|-----------|-------|------|------|-----|
| Strict JSON | `{"a":1}` | ✓ | ✓ | ✓ |
| Double quotes | `"string"` | ✓ | ✓ | ✓ |
| Standard comments | `// comment` | ✗ | ✓ | ✓ |
| Trailing commas | `[1,]` | ✗ | ✗* | ✓ |

---

## 7. Integration Points

### 7.1 AjisTextMode Addition

```csharp
public enum AjisTextMode
{
    Json = 0,       // Strict JSON
    Ajis = 1,       // AJIS strict
    Lex = 2,        // Permissive (existing)
    Lax = 3         // JavaScript-tolerant (NEW)
}
```

### 7.2 Lexer Configuration

LAX mode should:
- Enable identifier recognition
- Enable single-quote string parsing
- Enable JavaScript comment styles
- Preserve quote style information

### 7.3 Parser Configuration

LAX mode should:
- Accept identifiers as property names
- Accept single-quoted strings
- Allow trailing commas
- Emit diagnostics for LAX constructs

---

## 8. Diagnostic Strategy

**For each LAX construct, emit:**

1. **Unquoted Key Diagnostic**
   - Message: "Unquoted object key in LAX mode"
   - Severity: Info (diagnostic only)
   - Location: key position

2. **Single-Quoted String Diagnostic**
   - Message: "Single-quoted string in LAX mode"
   - Severity: Info (diagnostic only)
   - Location: string position

3. **JavaScript Comment Diagnostic**
   - Message: "JavaScript-style comment in LAX mode"
   - Severity: Info (diagnostic only)
   - Location: comment position

4. **Trailing Comma Diagnostic**
   - Message: "Trailing comma in LAX mode"
   - Severity: Info (diagnostic only)
   - Location: comma position

---

## 9. References

* [16_Pipelines_and_Segments.md](./16_Pipelines_and_Segments.md) – Segment specification
* [18_Implementation_Roadmap.md](./18_Implementation_Roadmap.md) – M5 definition
* `src/Afrowave.AJIS.Streaming/Reader/` – Lexer/Parser implementation
* `tests/Afrowave.AJIS.Core.Tests/` – Test suite

---

## 10. Status

**M5 Status:** IN PROGRESS

- [x] Specification drafted
- [ ] Current implementation analyzed
- [ ] LAX lexer extensions designed
- [ ] LAX parser integration implemented
- [ ] Streaming integration added
- [ ] Comprehensive tests added
- [ ] All tests passing
- [ ] Documentation complete

---

**Next Step:** Move to Step 2 - Analyze current implementation.
