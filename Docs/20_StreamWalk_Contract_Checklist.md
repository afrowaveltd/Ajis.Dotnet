# StreamWalk Contract Checklist (M1)

> Working checklist for aligning implementation and docs

---

## 1. Document Lifecycle

- [x] `OnStartDocument` optional in M1 (.NET reference does not emit)
- [x] `OnEndDocument` emitted exactly once on success
- [x] No events after `OnEndDocument`

## 2. Event Kinds (Trace)

- [x] `BEGIN_OBJECT`, `END_OBJECT`
- [x] `BEGIN_ARRAY`, `END_ARRAY`
- [x] `NAME`, `STRING`, `NUMBER`
- [x] `TRUE`, `FALSE`, `NULL`
- [x] `END_DOCUMENT`

## 3. Slice Semantics

- [x] Property names: bytes without surrounding quotes
- [x] String values: bytes without surrounding quotes
- [x] Numbers: raw token bytes
- [x] Literals: empty slice in M1 reference implementation
- [x] Slice lifetime documented as callback-only

## 4. Error Behavior

- [x] Stops immediately on error
- [x] Emits deterministic error code and offset
- [x] No `END_DOCUMENT` on failure

## 5. Options & Limits

- [x] MaxDepth enforced
- [x] MaxTokenBytes enforced
- [x] Comments/identifiers (M2 lexer)

## 6. Span vs Stream Parity

- [ ] Stream-based input parity (future milestone)

---

## Notes

This checklist is aligned with:

* `Docs/api/streamwalk.md`
* `Docs/api/visitor.md`
* `Docs/tests/streamwalk.md`
