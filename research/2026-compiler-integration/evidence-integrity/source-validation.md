# Source validation (referential validity)

Referential validity: the evidence a record cites exists and can be located. It says nothing
about whether the evidence supports the claim (`semantic-support.md`) or whether a test really
passed (verification validity).

## What EKS checks today

| Check | Rule | Level | Notes |
|---|---|---|---|
| Evidence names *some* source (`source_uri`, `source_title`, `derived_from`, related documents) | EKS0005 | error | Structural: a title satisfies it. |
| `source_uri` exists in the workspace | EKS0027 `evidence-source-exists` | warning | Runs only when the caller supplies existing paths. |
| Typed references resolve (supports, contradicts, depends_on…) | core validation | error | Case-sensitive. |

## Defect found and fixed in this phase

The source-existence rule read a relative `source_uri` two ways: relative to the record file,
and relative to the repository root (the convention for sibling repositories, `../other/…`).
It skipped the check whenever **either** reading left the repository. A path like
`../../sources/report.pdf` written in `research/evidence/` leaves the repository under the
root-relative reading, so it was never checked, while the rule still reported itself as
executed. This is the same class of failure as Phase 1's unwired EKS0026: a rule that exists
and "runs" but does not apply.

Fix (`Pipeline.fs`, `missingSources`): a source is accepted when **any** reading finds it,
including a reading that names a loaded sibling repository. Otherwise it is reported.
When some reading leaves the workspace, the message says the source may be in a repository
that is not loaded and cannot be verified. Nothing passes silently. Effect on the public
corpus: 1 → 4 warnings. The three new ones are real: `praxis` cites
`../Framework-engineering/…`, a repository that is not in the workspace.

Regression tests: `file-relative source that does not exist is reported` and the updated
`evidence citing a missing source file is a warning`.

## Remaining gaps (measured in the evidence trial)

1. **`source_title` is unverifiable but accepted.** Both fabrications in the trial used it.
   Proposal: accepted evidence whose only source is a free-text title gets `requires-review`
   ("source cannot be located"). Low cost; the false-positive rate needs measuring on the
   public corpus first (titles of books and meetings are legitimate).
2. **External URIs are out of scope.** URLs are not fetched (no network during compilation;
   determinism). They are recorded as external, not as verified.
3. **Existence is not identity.** A file that exists may be the wrong file or the wrong
   revision. Content hashes (`source_sha256`) or commit-pinned URIs would make the reference
   checkable. That is a schema change and needs owner review.
