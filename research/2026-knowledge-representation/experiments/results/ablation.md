## Feature ablation (public corpus; gold = full typed system)

| Task | Variant | n | recall | precision | mean tokens |
| --- | --- | --- | --- | --- | --- |
| find-evidence | full typed system | 56 | 1.000 | 1.000 | 342.6 |
| find-evidence | − inverse links (record's own links only) | 56 | 0.679 | 0.732 | 242.3 |
| find-evidence | − cards (full bodies delivered) | 56 | 1.000 | 1.000 | 4408.2 |
| change-impact | full typed system | 66 | 1.000 | 1.000 | 1677.3 |
| change-impact | − transitive traversal (1 hop) | 66 | 0.489 | 1.000 | 410.6 |
| change-impact | − inverse links | 66 | 0.909 | 1.000 | 1591.4 |
| change-impact | − relation types (untyped neighbourhood, 6 hops) | 66 | 1.000 | 0.118 | 19860.3 |
| change-impact | + prose mentions as edges | 66 | 1.000 | 0.257 | 8740.9 |
| change-impact | − cards (full bodies delivered) | 66 | 1.000 | 1.000 | 14779.4 |

## Strict per-kind types: share of real records representable

| Kind | Records | Representable | Reasons (count) |
| --- | --- | --- | --- |
| decision | 134 | 100.0% |  |
| governance | 125 | 100.0% |  |
| evidence | 71 | 100.0% |  |
| method | 70 | 100.0% |  |
| requirement | 56 | 64.3% | requirement without acceptance criteria (20) |
| document | 38 | 100.0% |  |
| hypothesis | 24 | 100.0% |  |
| experiment | 15 | 73.3% | experiment without a hypothesis (4) |
| journal | 12 | 100.0% |  |
| researchpackage | 12 | 100.0% |  |

## Hand-maintained index drift (documents enumerating ≥3 ids of a family)

| Repository | Document | Family | listed | actual | missing from doc | unknown ids in doc |
| --- | --- | --- | --- | --- | --- | --- |
| praxis | context/DECISIONS.md | DF-ROS-2026 | 3 | 52 | 49 | 0 |
| praxis | docs/migrations/fsharp/TRACEABILITY.md | EV-ROS-2026 | 5 | 50 | 45 | 0 |
| praxis | docs/migrations/fsharp/TRACEABILITY.md | DF-ROS-2026 | 14 | 52 | 38 | 0 |
| fides | context/CURRENT-STATE.md | DF-FIDES-2026 | 7 | 16 | 9 | 0 |
| mercatus | context/DECISIONS.md | DF-MERCATUS-FND-2026 | 3 | 4 | 1 | 0 |
| fides | context/DECISIONS.md | DF-FIDES-2026 | 16 | 16 | 0 | 0 |

Documents compared: 6; with ≥1 missing record: 5; with ≥1 unknown id: 0.

## Generated index (knowledge compilation)

| Records | Index tokens | Compile ms | Deterministic (input order permuted) | Lines changed by one status edit |
| --- | --- | --- | --- | --- |
| 585 | 63021 | 63.3 | True | 1 |
