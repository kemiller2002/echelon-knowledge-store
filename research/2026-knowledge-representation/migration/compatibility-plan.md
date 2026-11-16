# Compatibility plan

* **Schema version per record type** (`schema: requirement/1-0-0`, SchemaVer
  MODEL-REVISION-ADDITION; see `versioning-research.md`). Absent field = version 1-0-0.
* **Readers accept all historical versions**; writers emit the current one.
  Unknown fields are preserved (implemented: `Extra`).
* **Minimum version per type** enforced by CI to block downgrade attacks
  (security model); validators run from a pinned EKS release, never from the PR
  under test.
* **Ids are opaque, immutable, never reused**; renames happen via supersession.
* **One file per record** (merge-friendly; JSONL and multi-record YAML conflicted
  in the versioning workstream's git test).
* **Generated files** carry a header naming the generator version and are
  regenerated, never merged by hand.
* Cross-repository references may pin `@version`; unpinned references follow `main`.
