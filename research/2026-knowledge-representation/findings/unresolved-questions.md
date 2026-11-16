# Unresolved questions

| Question | Why unresolved | Next experiment |
|---|---|---|
| Does compiled delivery help other model families (GPT, Gemini, open models)? | only Claude Sonnet/Haiku tiers tested | rerun trials 1–2 with two other families |
| Does it help on long, multi-step coding tasks (where packages replace large reads mid-session)? | trials were short Q&A; fixed overhead dominates tokens | SWE-style implementation tasks with and without `eks context` |
| Does compiler feedback improve agent edits? | write trial: no structural breakage in any run; Haiku 0.16 → 0.73 with feedback, but n = 1 per cell; Sonnet fabricated evidence with and without feedback (`experiments/write-path-results.md`) | n ≥ 5 per cell with integrity scoring; a source-verification review gate |
| Human review burden of diagnostics and generated views | not measured | owner review of first `eks validate` reports in two pilot repositories |
| Will owners maintain cross-repository typed references? | 0 exist today | backfill the 29 verified hard dependencies in two repositories, measure drift after a month |
| Does embedding retrieval fix description lookup (BM25 hybrid 0.73)? | no embedding model available offline here | add a local embedding model; compare |
| Should EKS live inside Praxis (`praxis registry build`) or as a library Praxis calls? | owners' decision; reviewer estimates extension at <15% of the cost | design review with Praxis owners |
| Status vs prose contradictions (e.g. status active, text "not supported") | not structurally detectable | model-assisted review rule (RequiresReview), measured for precision |
| Update frequency, diff complexity of knowledge | shallow clones | deep clones of two repositories |
| Requirement-contract integration with Dokimos | Dokimos exposes only a fatal diagnostic envelope; no obligation interface found | ask Dokimos owners; prototype one obligation gate |
| Test-plan generation, source-linked research summaries (mission tasks 7, 12) | not benchmarked | add to trial 3 |
