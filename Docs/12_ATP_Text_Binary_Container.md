# AJIS ATP – Text + Binary Transport Container

> Normative conceptual specification of the Afrowave Transport Package (ATP)

---

## 1. Purpose of ATP

ATP (Afrowave Transport Package) is a **container format** that combines:

* AJIS text section (metadata, structure, descriptors)
* one or more binary payload sections

into a **single, stream-friendly file or transport unit**.

ATP is designed to:

* avoid base64 or text inflation for binary data
* support large binary payloads (GB+)
* enable streaming parsing and streaming consumption
* support signing and integrity verification

ATP is **not required** for pure AJIS text usage.

---

## 2. High-level structure

An ATP container consists of:

```
[ ATP Header ]
[ AJIS Text Section ]
[ Binary Section 1 ]
[ Binary Section 2 ]
[ ... ]
[ Optional Footer / Signatures ]
```

All sections are arranged **linearly** and are readable in a single forward pass.

---

## 3. ATP Header

The ATP header:

* identifies the container as ATP
* defines offsets and lengths
* declares feature flags

Typical header fields:

* magic (e.g. `ATP1`)
* version
* flags
* textSectionLength
* binarySectionCount

Header size is fixed or minimally bounded.

---

## 4. AJIS text section

The text section:

* is a valid AJIS document
* describes:

  * binary payloads
  * metadata
  * structure
  * optional signatures

The text section:

* must be fully parseable using the streaming AJIS parser
* ends at a precisely defined byte boundary

The parser **must stop exactly** at the end of the text section.

---

## 5. Binary sections

Each binary section:

* immediately follows the text section or previous binary
* has a known length declared in the text section
* may represent:

  * raw binary blob
  * compressed data
  * encrypted data

Binary sections are:

* opaque to the AJIS parser
* consumable as raw streams

---

## 6. Referencing binary data from AJIS

Within the AJIS text section, binary payloads are referenced using descriptors.

Conceptual example:

```
{
  "files": [
    {
      "name": "image.png",
      "binaryRef": {
        "index": 0,
        "length": 1245789,
        "sha256": "..."
      }
    }
  ]
}
```

The exact schema is implementation-defined but must:

* uniquely identify the binary section
* declare length
* optionally declare checksum or signature

---

## 7. Streaming behavior

ATP is explicitly designed for streaming:

* AJIS parser consumes only the text section
* binary payloads are read as raw streams
* consumers may:

  * skip binary payloads
  * forward them elsewhere
  * process them incrementally

No buffering of full binary payloads is required.

---

## 8. Integrity and signatures

ATP may support:

* per-binary checksums
* whole-package signatures
* detached signatures

Signature metadata is typically stored in the AJIS text section.

Verification:

* may be performed during streaming
* may be deferred

---

## 9. Error handling

Possible error classes:

* malformed ATP header
* text section parse error
* declared binary length mismatch
* checksum/signature failure

Errors in binary sections must:

* not corrupt text parsing state
* be reported with precise offsets

---

## 10. Use cases

ATP enables:

* AJIS + file bundles
* network transport of structured + binary data
* database export/import
* content-addressed storage
* signed configuration packages

---

## 11. Relationship to other documents

ATP builds on:

* AJIS Format Norms
* Streaming Parser Algorithm
* Streaming & Pipelines

ATP extends AJIS **without complicating** the core text format.

---

## 12. Status of this specification

ATP is currently **conceptual but stable**.

Binary layout details may evolve,
but the streaming and separation principles are fixed.
