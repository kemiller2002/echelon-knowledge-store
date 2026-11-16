# Intermediate representation

The IR is an inert tree (`Yaml = YNull | YScalar | YSeq | YMap`) plus the typed
record (`KnowledgeRecord`). `Codec.toYaml`/`ofYaml` map between them; every syntax
is a printer/reader of the tree.

Design choices (and why):
* **All scalars are strings** in the IR. Typing (dates, enums, ids) happens in the
  F# model, not in the parser, so YAML's implicit typing ("no" → false, dates) cannot
  change meaning. JSON numbers and booleans are kept as their raw text.
* **Unknown fields are preserved** (`Extra`), which makes the Markdown round trip
  lossless and keeps forward compatibility with fields EKS does not know.
* **Field order is canonical** on output (id, kind, title, status, confidence,
  provenance, relations, extras, body), so regenerated files diff minimally.
* **Body text is opaque.** The compiler never interprets prose as instructions or
  relations (mentions are not edges by default).
* Normalisation: leading/trailing blank lines of bodies are trimmed. That is the
  only lossy step, and it is documented.
