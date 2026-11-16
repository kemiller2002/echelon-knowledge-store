# Hybrid typed knowledge model (candidate E)

= canonical structured Markdown (A) + F# validator + derived graph + generated
views (index cards, JSONL, context packages).

Measured contributions over A alone (`experiments/ablation-results.md`):
derived inverse links (+32 points evidence recall when reading single records),
transitive typed queries (+51 points impact recall; ~300× fewer tokens than
manual reference chasing), cards (~10× fewer tokens than bodies at equal recall),
generated indexes (no drift by construction), compiled packages (2–3× faster
agents at equal accuracy), validation (finds 9 dangling references, 28 asymmetric
links, 5 stale indexes, 3 supports from superseded records).

Costs: ~1,500 lines of F# in the prototype core (see
`adversarial/complexity-analysis.md`), a CI job, and the discipline of never
editing generated files.

**Verdict:** ADOPT (as E = A + compiler), staged; see `strategy/implementation-roadmap.md`.
