# Dependency resolution

Source: `results/compiler.md` (reference resolution) and `results/dependencies.md`.

| Relations (incl. related_documents) | Resolved | Unresolved | …id-like | …paths | …other |
|---|---|---|---|---|---|
| 1,888 | 1,325 | 563 | 1 | 532 | 30 |

* The one unresolved id (`RQ-SDE-2026-0002`, cited by dokimos) does not exist anywhere
  in the corpus — confirmed by independent replication.
* 532 unresolved targets are document paths in `related_documents`; EKS does not
  treat documents without ids as records. 30 are Praxis input ids (`INPUT-…`), commit
  SHAs and prose.
* Resolution order: same repository → unique id elsewhere → path suffix. Ambiguous ids
  (29 vendored governance ids) are reported, never silently picked.
* Cross-repository typed edges: 0 (see `dependency-tracking-results.md`).
* Case errors: a lower-cased reference to an existing id is an error (EKS0002); the
  first rule version flagged 33 non-id references as errors and was recalibrated.

Mutations confirm resolution failures are caught: id typo, lower-cased id,
non-existent replacement and a broken list indentation each produce EKS0002 (blocking).
