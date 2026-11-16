# Adoption criteria

Critical failures are gates, not score components (mission §21).

## Gates (any failure = not adopted)

| Gate | Threshold | Status of recommended architecture |
|---|---|---|
| G1 No loss of requirements/decisions in conversion | 100% field equality on round trip | met (568/568) |
| G2 No accuracy regression vs current Markdown on the benchmark | ≥ baseline mean score | met (1.00 vs 1.00) |
| G3 No silent truncation in context packages | every dropped item listed | met (unit-tested) |
| G4 No executable knowledge | data-only parsing | met (YAML aliases/anchors rejected; no evaluation) |
| G5 No private content in public outputs | aggregates only | met after redaction (one lapse, fixed; see executive report) |
| G6 Reversible | every stage is a git/CI change | met (migration-strategy.md) |

## Ranked criteria (mission §21) for candidates passing the gates

| Criterion | A Markdown+FM | E Hybrid (A + compiler) | B JSON | D DSL | C F# source |
|---|---|---|---|---|---|
| 1 Task accuracy | 1.00 | 1.00 | 0.98 | 1.00 | (not trialled) |
| 2 Context completeness | single-file reading misses 32% of evidence | complete (derived links) | as A | as A | as A |
| 3 Token efficiency | 1.00 | packages 3.2k/question; cards 10× < bodies | 1.10–1.13 | 0.98 | 1.00 (metadata +75%) |
| 4 Retrieval performance | grep: ~44k tokens/evidence question | ~340 tokens | jq | grep | needs compiler |
| 5 Update reliability | validated in CI | validated + generated views | escaped bodies | parser diagnostics | compile errors |
| 6 Provenance | per file | per card (source path) | per object | per record | per record |
| 7 Validation | F# validator | F# validator | F# validator | F# validator + grammar | compiler + validator |
| 8 Human usability | best | best (generated views read-only) | poor diffs | new syntax | verbose |
| 9 Cross-repository support | needs `repo:ID` | needs `repo:ID`; federated indexes | same | same | same |
| 10 Security | data-only | data-only | data-only | data-only parser | execution risk |
| 11 Implementation complexity | low | medium | low | high | high |
| 12 Operating cost | CI only | CI only | CI only | CI + tooling | build per edit |
| 13 Maintainability | high | medium | medium | low | low |
| 14 Migration risk | none (current format) | none (additive) | rewrite all files | rewrite all files | rewrite all files |

The ~25% token-reduction target is **not** met by any format (best: 2%). It is
met by delivery: cards and packages, not syntax. Per the mission's own rule, the
adoption case rests on correctness, traceability and maintenance, not tokens.
