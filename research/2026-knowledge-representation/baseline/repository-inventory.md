# Repository inventory

Audited 2026-11-16 (commits pinned in `benchmarks/corpus.lock.json`). Names and
availability were verified with the GitHub repository listing; `clarity-framework`
exists under that exact name (also `clarity-framework-training` and
`clarity-service`, not audited).

| Repository | Visibility | Markdown files | Tokens (o200k) | Front matter | Records with id | Agent contract | Notes |
|---|---|---|---|---|---|---|---|
| aegis | public | 91 | 143,406 | 52.7% | 39.6% | Praxis | .NET fault-handling library |
| arca | public | 99 | 140,125 | 52.5% | 45.5% | Praxis | |
| clarity-framework | private | 131 | 105,634 | 13.0% | 8.4% | none | aggregates only |
| dokimos | public | 130 | 177,204 | 29.2% | 23.8% | Praxis | |
| echelon-diagnostic-framework | public | 207 | 192,287 | 34.8% | 28.5% | Praxis | MIT licence |
| echelon-engineering-system | public | 7 | 8,510 | 0% | 0% | none | added as representative non-Praxis repo |
| echelon-organization-administration | private | 188 | 545,600 | 0% | 0% | none | aggregates only |
| fides | public | 115 | 199,867 | 62.6% | 52.2% | Praxis | |
| forma | public | 274 | 395,223 | 27.7% | 23.4% | Praxis | |
| mercatus | public | 111 | 214,791 | 43.2% | 32.4% | Praxis | |
| percepta | public | 171 | 205,624 | 33.3% | 26.3% | Praxis | |
| praxis | public | 458 | 787,660 | 55.9% | 53.9% | Praxis (origin) | the repository operating system |
| signal | public | 167 | 303,659 | 43.7% | 36.5% | Praxis | |

Aegis was added beyond the mission list (security capabilities); echelon-engineering-system
as a representative repository without Praxis. Shallow clones were used, so
history-based measures (update frequency, diff complexity) are not available;
see `findings/unresolved-questions.md`.
