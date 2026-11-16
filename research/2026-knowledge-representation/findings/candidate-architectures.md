# Candidate architectures

| Id | Architecture | Outcome | Key evidence |
|---|---|---|---|
| A | Markdown + front matter, F# validation | ADOPT (as canonical source) | 1.00 trial accuracy; lossless round trip; already used for 568 records |
| B | JSON/JSONL + F# schemas | REJECT as source / ADOPT as export | +10–13% tokens; poor diffs; merge conflicts |
| C | F# source records | REJECT | +75% metadata tokens; unsafe or compiler-dependent reading |
| D | Compact DSL → F# types | REJECT | −0.7% corpus tokens; new language to maintain; no accuracy gain |
| E | Hybrid / compiler-first: A + derived graph + diagnostics + compiled outputs | ADOPT (staged, as an extension of the Praxis pipeline) | ablation gains; 2× faster agents in two trials; 22/26 edit mistakes caught |
| F | Simplest: Praxis registries + inverse links + traversal (+ rules) | first stage of E | registries alone = 0.99 in trial 2; reviewer estimates <15% of E's cost |
| G | Central knowledge repository | REJECT | duplicates owned knowledge; crosses privacy boundaries |
| H | Graph database / SQLite store | REJECT for now | graph builds in < 1 s from files; no measured need |
| I | Existing requirements tool (sphinx-needs, Doorstop, OpenFastTrace) | INCONCLUSIVE | covers ids/links/CI; lacks claim–evidence typing and agent delivery; not trialled |
