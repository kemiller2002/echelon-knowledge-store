# Rejected alternatives

See `findings/rejected-ideas.md` for the evidence table. Summary:

| Alternative | Outcome | Reason |
|---|---|---|
| Custom DSL | REJECT | −0.7% tokens, no accuracy gain, new language |
| F# source records | REJECT | +75% metadata tokens, ids become schema, execution risk; FCS checks only shape |
| JSON/JSONL canonical | REJECT | +10–13% tokens, poor diffs, merge conflicts |
| Separate EKS store/pipeline | REJECT | duplicates Praxis (DF-ROS-2026-A001) |
| Graph DB / service | REJECT | no measured need |
| Central repository | REJECT | duplication, privacy |
| BM25 excerpts as delivery | REJECT for relational tasks | 0.47 accuracy |
| Plain Markdown without front matter | REJECT | 0.72–0.76 accuracy |
