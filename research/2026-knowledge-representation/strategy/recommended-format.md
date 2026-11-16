# Recommended format

**Canonical source: Markdown with YAML front matter, one record per file — ADOPT
(it is already Echelon's format; Praxis decision DF-ROS-2026-A001).**

* Fields: keep the Praxis field names (`id`, `status`, `confidence`, `supports`,
  `contradicts`, `depends_on`, `derived_from`, `supersedes`, `superseded_by`,
  `supporting_evidence`, `contradicting_evidence`, `related_documents`).
* Author evidence links on the evidence side; the compiler derives inverses.
* Confidence: `very-low | low | medium | high | very-high` (Praxis artifact-metadata
  enum); other labels are preserved but flagged.
* Cross-repository references: `repo:ID` (optionally `@version`) — EXPERIMENTAL.
* Add an optional `schema:` field when a record type's schema changes (SchemaVer).

Generated (never hand-edited) formats: `index.md`, `records.jsonl`, JSON registries,
task packages. Rejected as sources: JSON/JSONL, YAML streams, F# records, custom DSL.
