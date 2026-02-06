# AJIS Transform Recipes (Streaming)

> Practical, normative recipes for segment-based transforms: filter, map, select, patch

---

## 0. Purpose

This document provides **ready-to-implement recipes** built on:

* `ParseSegments → Transform → SerializeSegments`

All recipes are designed to:

* work in a single forward pass
* remain bounded-memory
* be safe for huge files

---

## 1. Common transform building blocks

### 1.1 Stack tracking

Maintain a runtime state:

* container stack (Object/Array + frameId)
* current property name (for Object frames)
* current array index (for Array frames)

This state is required for path evaluation and for skipping subtrees.

### 1.2 Subtree skipping

To remove a subtree:

* upon encountering its `ContainerStart`, enter skip mode
* increment a local depth counter
* consume segments until the matching `ContainerEnd`

Skip mode must ignore nested starts/ends correctly.

### 1.3 Meta segments

* `Progress` and `Diagnostic` segments are forwarded as-is
* meta segments must not affect structural logic

---

## 2. Recipe: Drop a property by exact path

Goal: remove `$.debug` from the root object.

### 2.1 Matching rule

* when current frame is root object
* and `PropertyName == "debug"`

### 2.2 Action

* skip the following value (primitive or subtree)

Implementation notes:

* after `PropertyName("debug")`, read next segment:

  * if primitive: drop it
  * if `ContainerStart`: skip subtree

Output remains valid because serializer controls commas.

---

## 3. Recipe: Rename keys (PascalCase ↔ camelCase)

Goal: rewrite property names while streaming.

### 3.1 Matching rule

* on every `PropertyName(frameId, name)`

### 3.2 Action

* emit a rewritten `PropertyName(frameId, Convert(name))`

Notes:

* must not rename keys inside strings
* conversion must be deterministic
* can be driven by `AjisSettings.Naming`

Reference helper:

* `AjisSegmentMap.RenameProperties`

---

## 4. Recipe: Select a subtree into a new AJIS document

Goal: extract `$.config` into a new file.

### 4.1 Matching rule

* detect when the currently produced value corresponds to the path `$.config`

### 4.2 Action

* write a new root document containing only that subtree

Strategies:

* **Wrap mode**: output `{ "config": <subtree> }`
* **Bare mode**: output `<subtree>` as the new root

Notes:

* selection may require ignoring all other segments
* if the subtree is a container, copy segments from `ContainerStart` to matching `ContainerEnd`

Reference helpers:

* `AjisSegmentSelect.SelectRootPropertyValue`
* `AjisSegmentSelect.SelectRootPropertyWrapped`

---

## 5. Recipe: Filter array items by predicate

Goal: in `$.users[*]`, keep only items where `isActive == true`.

### 5.1 Constraints

* works best when items are objects
* predicate evaluation must be bounded-memory

### 5.2 Strategy (bounded buffer per item)

* detect `ContainerStart(Array)` for `users`
* for each array item:

  * buffer only that item’s segments until item ends
  * evaluate predicate while buffering
  * if predicate true: flush buffered segments
  * else: discard buffered segments

This keeps memory bounded by **max single item size**, not the full array.

### 5.3 Predicate evaluation

Inside the buffered object:

* track property/value pairs
* when `PropertyName("isActive")` appears:

  * read next value
  * set predicate result

---

## 6. Recipe: Patch – Set a value (create if missing)

Goal: set `$.config.theme = "dark"`.

### 6.1 Strategy

* locate `$.config` object
* within that object:

  * if property `theme` exists: replace its value
  * else: insert a new property at a deterministic position

### 6.2 Replacement behavior

After `PropertyName("theme")`:

* drop existing value (primitive or subtree)
* emit new value segment(s)

### 6.3 Insertion behavior

Insertion requires a rule:

* insert before `ContainerEnd(Object)` of the `config` object

Implementation detail:

* detect `ContainerEnd(Object)` of the config frame
* if `theme` was not seen:

  * emit `PropertyName("theme")`
  * emit `String("dark")`
  * then emit the original `ContainerEnd`

Reference helper:

* `AjisSegmentPatch.ReplacePropertyValue`

---

## 7. Recipe: Patch – Remove a path

Goal: remove `$.users[0].email`.

### 7.1 Strategy

* navigate to array `users`
* count index until 0
* within that object, apply Recipe 2 (drop property)

Notes:

* array index tracking must be deterministic
* if the target item is not an object, operation is a no-op or warning (settings-controlled)

---

## 8. Reference implementation notes

The .NET reference implementation provides `AjisSegmentFilter` with:

* predicate-based filtering with subtree skipping
* `DropPropertyByName` helper for exact-name removal

---

## 8. Recipe: Normalize / Canonicalize (format only)

Goal: rewrite file to canonical formatting (spacing/indent) without semantic changes.

### 8.1 Strategy

* parse segments
* pass through unchanged
* serializer applies canonical formatting

Optional:

* preserve number tokens exactly (raw) OR normalize numeric style

---

## 9. Recipe: Validate + stats in one pass

Goal: validate syntax and compute:

* counts of objects/arrays
* key frequency
* max depth

### 9.1 Strategy

* consume segments
* update counters on `ContainerStart/End` and `PropertyName`

Notes:

* does not require serializer
* can stop early on fatal error

---

## 10. Safety notes

* all transforms should write to a new output file
* patches must be deterministic
* in tolerant mode, warn and continue; in strict mode, throw

---

## 11. Status

Stable.

Recipes may expand over time,
but buffering rules and bounded-memory guarantees must remain intact.
