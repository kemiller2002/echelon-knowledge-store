# Typed schema (F#)

Implementation: `src/Echelon.KnowledgeStore.Core/Model.fs` (permissive model),
`Strict.fs` (strict per-kind model), `Validation.fs` (corpus rules).

## Two layers

1. **Permissive record** (`KnowledgeRecord`): id, kind, title, status, qualitative
   confidence, typed relations, provenance (repository, path, author, source,
   created/updated/retrieved), body, and `Extra` (every unrecognised field, kept
   verbatim). Every real record parses into it; nothing is lost (568/568 lossless).
2. **Strict per-kind model** (`StrictRecord`): discriminated union whose cases make
   single-record invariants unrepresentable:
   * `StrictRequirement` needs `NonEmpty<acceptance criterion>`;
   * `StrictDecision` needs `NonEmpty<grounds>` and a `Lifecycle` where
     `Superseded` carries `NonEmpty<replacement>`;
   * `StrictHypothesis` carries a `Standing` (`Supported` cannot exist without
     evidence ids; `Contested` needs both directions) — and a status claiming
     support without evidence is rejected;
   * `StrictEvidence` needs `External source | Linked (NonEmpty ids)`;
   * `StrictExperiment` needs `NonEmpty<hypothesis>`.

The strict model is computed from the permissive one (`Strict.classify`), never
stored. Records that do not fit are reported, not rejected at parse time.

## Coverage on the real corpus (measured, 568 front-matter records)

| Kind | Representable in strict model |
|---|---|
| decision, governance, evidence, method, document, hypothesis, journal, research package | 100% |
| requirement | 64.3% (20 lack an acceptance list in the recognised form) |
| experiment | 73.3% (4 lack a hypothesis link) |

## Static vs runtime

| Invariant | Enforced by |
|---|---|
| Conclusion/claim cannot be "supported" without evidence ids | type (`Standing`) + classifier |
| Requirement has acceptance criteria | type (`NonEmpty`) + extraction heuristic |
| Superseded decision names a replacement | type (`Lifecycle.Superseded`) |
| Experiment records a hypothesis | type (`NonEmpty`) |
| Reference to a nonexistent record | runtime, whole corpus (`dangling-reference`) |
| Replacement actually exists and links back | runtime (`supersession-asymmetry`) |
| No dependency cycles | runtime (`dependency-cycle`) |
| Ids unique per repository | runtime (`duplicate-id`) |
| Incompatible schema version | runtime at decode (planned field `schema`; see migration/compatibility-plan.md) |
| Statement is true | **nobody** — validity is structural only |

Types catch presence errors in one record. Every cross-record invariant needs
the corpus and therefore runtime validation. That is why F# serves as the
validation/compiler language, not as the storage language.

## Schema publication

No stable schema is published. Field names follow existing Praxis front matter
(`supports`, `supporting_evidence`, `superseded_by`, …) so adoption needs no migration.
