# Migration strategy

**Nothing is migrated in this phase.** No Echelon repository was modified.

Because the recommended canonical source *is* the existing Markdown + front
matter, adoption is additive rather than a migration:

| Stage | Change in a source repository | Reversible by |
|---|---|---|
| 0. Observe | run `eks validate` in CI as a non-blocking report | removing the job |
| 1. Index | commit nothing; CI publishes `INDEX.md`/`index.jsonl` as artefacts | removing the job |
| 2. Fix | owners fix blocking findings (dangling refs) in normal PRs | git revert |
| 3. Enforce | blocking rules become required checks | turning the check off |
| 4. Extend | add `repo:ID` cross-repository references, `implements`/`validates` | fields are optional; ignored by older tools |

Expand–migrate–contract for any field rename: add the new field, have the
compiler read both, rewrite in a reviewed PR whose diff is checked to change only
that field (zero-diff identity test for every other field, as in
`round-trip-validation.md`), then drop the old field.

Rollback: every stage is a CI or front-matter change in git; no database or
service holds state. Original Markdown is never discarded: it *is* the source.
