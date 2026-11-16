# Ablation results

Measured on the public corpus (585 records); `results/ablation.md`. Gold = the full
typed system's answer, so this measures what each *feature* contributes to
recovering authored knowledge.

| Task | Variant | Recall | Precision | Mean tokens |
|---|---|---|---|---|
| find-evidence (56) | full typed system | 1.000 | 1.000 | 343 |
| | − inverse links (record's own links only) | **0.679** | 0.732 | 242 |
| | − cards (full bodies) | 1.000 | 1.000 | **4,408** |
| change-impact (66) | full typed system | 1.000 | 1.000 | 1,677 |
| | − transitive traversal (1 hop) | **0.489** | 1.000 | 411 |
| | − inverse links | 0.909 | 1.000 | 1,591 |
| | − relation types (untyped 6-hop neighbourhood) | 1.000 | **0.118** | 19,860 |
| | + prose id mentions as edges | 1.000 | **0.257** | 8,741 |
| | − cards (full bodies) | 1.000 | 1.000 | **14,779** |

Earlier retrieval results give the remaining ablations:

| Feature removed | Effect (measured) |
|---|---|
| Stable ids (lookup by description via BM25→graph) | complete answers drop from 59/59 to 43/59 |
| Typed metadata entirely (agent trials, plain Markdown) | accuracy 1.00 → 0.72–0.76 |
| Generated index (md vs md-index trials) | no accuracy change; −1 tool call, −14 s per run |
| Compact syntax (DSL vs front matter) | 9% fewer metadata tokens (≈0.7% of corpus); no accuracy change |
| Compile-time types (F# records as storage) | not needed for any measured gain; +73% metadata tokens vs front matter |
| Runtime validation | finds 9 dangling references, 28 asymmetric links, 3 supports from superseded records, 5 stale indexes, which no other feature detects |

## Conclusions

* **High value:** stable ids; typed relation kinds (precision 0.12 → 1.0 for
  impact); derived inverse links (+32 points evidence recall); transitive
  traversal (+51 points impact recall); compact cards instead of bodies (≈10×
  fewer tokens at equal recall); runtime validation.
* **No measured value:** custom compact syntax; F# source as storage;
  compile-time typing of the corpus; prose mentions as edges (they hurt precision).
* **Simplest sufficient system:** keep Markdown + front matter, add a derived
  index with inverse links and transitive queries, emit cards, validate in CI.
