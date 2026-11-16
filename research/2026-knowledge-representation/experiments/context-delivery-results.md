# Task-specific context delivery results

## Measured

| Measure | Raw corpus | Compiled package |
|---|---|---|
| Material size (16 questions) | 2.70M tokens available (Markdown) | 51,517 tokens total, mean 3.2k per question |
| Accuracy, Sonnet | 1.000 | 1.000 |
| Accuracy, Haiku | 1.000 | 1.000 |
| Wall time per run (8 questions), Sonnet | 43.7 s | 22.1 s |
| Wall time per run, Haiku | 101.9 s | 34.6 s |
| Tool calls per run, Haiku | 18.5 | 12.0 |
| Tokens per run, Sonnet / Haiku (incl. fixed overhead) | 77.5k / 106.9k | 75.4k / 116.7k |

Deterministic: cards deliver the same answer sets as full bodies with ~10× fewer
tokens (evidence: 343 vs 4,408; impact: 1,677 vs 14,779 per task).

Budgeting never drops content silently: items are first reduced to cards, then
dropped and listed under `OMITTED` (unit-tested). No trial package needed truncation
(budget 12k tokens).

## Retrieval failure risk

Description-based questions resolve the target by BM25 before compiling. On the
59 deterministic lookup-by-description tasks, the hybrid BM25 → graph pipeline
found the target in 43 cases (0.73). In the trials all three description
questions were answered correctly because the right record was among the five
candidate cards. A compiled package is only as good as its target resolution:
packages list candidates, never a single silent guess.

## Conclusion

Compiled delivery preserves accuracy and reduces time and tool calls. Token
savings at the agent level were not observed because of fixed per-agent overhead.
They should appear when a package replaces large raw reads inside a longer
session; that is untested.
