# Rejected ideas (with evidence)

| Idea | Evidence for rejection |
|---|---|
| Replacing Markdown | no accuracy gain from any other syntax; Markdown already carries the needed structure |
| Custom compact DSL | −0.7% corpus tokens; new language and tooling |
| F# source as the knowledge store | +75% metadata tokens; ids become schema; checks only shape/existence; execution risk |
| JSON/JSONL as canonical source | +10–13% tokens; unreadable diffs of bodies; merge conflicts |
| Graph database / SQLite | graph builds in < 0.4 s from files |
| Persistent EKS service | sub-second compile, sub-millisecond packages |
| Central knowledge repository | duplicates owned knowledge (already 25% duplicated tokens); crosses privacy boundaries |
| Prose id mentions as dependency edges | impact precision 0.26 |
| Numeric confidence scores | nothing in the corpus calibrates them |
| Optimised text retrieval alone (BM25 excerpts) for relational questions | 0.47 accuracy in trial 2 |
| A separate EKS pipeline parallel to Praxis | Praxis already owns canonical artefacts, registries, validation and CI (adversarial O1) |
| Claiming token savings for compiled delivery | not observed end-to-end (fixed agent overhead dominates) |
