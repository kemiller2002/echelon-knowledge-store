# Relationship model

## Relation kinds and change propagation

| Relation (front-matter field) | Meaning | A change to … requires review of … | Observed edges |
|---|---|---|---|
| `depends_on` | X needs Y | Y → X | 5 |
| `derived_from` | X was produced from Y | Y → X | 73 |
| `supports` / inverse `supporting_evidence` | E supports C | E → C | 110 |
| `contradicts` / inverse `contradicting_evidence` | E contradicts C | E → C | 4 |
| `supersedes` / `superseded_by` | X replaces Y | none (history) | 13 + 13 |
| `related_documents`, `related_theories` | untyped association | none | 1,099 |
| prose id mention | untyped | none (excluded: precision 0.26) | — |

The first implementation propagated `supports` the wrong way (from claim to
evidence). The error was found while preparing the trials and fixed; the fix is
now pinned by a unit test. Direction semantics must be specified, not inferred.

## Store one direction, derive the other

Praxis stores evidence links on both sides (`supports` on the evidence,
`supporting_evidence` on the claim). 28 pairs disagree. EKS accepts either side
and derives the inverse in the graph. Recommendation: author on the evidence side
(`supports`), allow the claim-side list for compatibility, and flag disagreement
(`evidence-link-asymmetry`) until one side is retired.

## Missing relation kinds (proposed, not yet used)

* `implements` (code/test → requirement), `tests` / `validates` (Dokimos
  obligation → requirement): needed for traceability to tests; no records use them.
* Cross-repository references `repo:ID` (and `repo:ID@version`): see
  cross-repository-model.md.

## Graph storage

The graph is **derived on demand** from the Markdown files (585 public records
build in well under a second; the index compiles in ~65 ms). No graph database,
no SQLite: nothing measured needs it at this scale (`findings/rejected-ideas.md`).
