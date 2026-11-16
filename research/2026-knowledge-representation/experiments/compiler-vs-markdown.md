# Compiler-first vs Markdown (mandatory comparison, addendum §9)

Held-out trial 2: 12 new questions (task context, evidence bundle, impact,
cross-repository impact, current authority, missing links, contested claims,
status lag, superseded support, description lookup) plus two adversarial
fixtures (a draft record claiming to supersede an accepted decision; an evidence
record carrying an injected instruction). Same model tier (Claude Sonnet via Claude
Code subagents), same tools, same instructions, two batches of six per condition.
Gold computed by direct graph walks, independently of the compiler code.
Raw: `results/trial2-*.json|md`, answers in `results/trial2-answers/`.

| Condition | What the agent gets | Mean score | Exact | Tokens / run* | Tool calls / run | Wall s / run |
|---|---|---|---|---|---|---|
| A raw Markdown | 1,620 files (2.70M tokens available), tools | 0.991 | 11/12 | 74.4k | 8.0 | 31.3 |
| B optimised retrieval | top-8 BM25 sections per question (44.6k tokens total) | 0.472 | 5/12 | 82.4k | 6.5 | 23.5 |
| C structured (registry-shaped JSON, as Praxis generates) | 66 JSON files (178k tokens), tools | 0.991 | 11/12 | 73.6k | 7.5 | 32.8 |
| D compiler-first, as run | one compiled output per question (12.1k tokens total) | 0.917 | 11/12 | 75.4k | 5.0 | 14.7 |
| D compiler-first, after fix | as D, EKS0026 wired | **1.000** | **12/12** | 75.4k | 5.0 | 15.2 |

\* includes a fixed ~65–70k-token agent prompt.

## Findings

1. **Accuracy:** A, C and fixed D are at ceiling. The compiler did not make a capable
   tool-using agent more accurate than raw Markdown with front matter, or than
   registry JSON.
2. **Time and tool calls:** D halves wall time (≈15 s vs 31–33 s) and needs fewer
   tool calls (5 vs 7.5–8), consistent with trial 1.
3. **Optimised Markdown retrieval fails on relational questions** (0.47): excerpts
   lack metadata, and BM25 does not retrieve the records a relation question needs.
   Retrieval of text is not a substitute for resolving structure.
4. **The compiler's own correctness is the new single point of failure.** In the
   as-run D condition, a rule that was defined but not wired returned an empty list
   labelled COMPLETE; the agent trusted it and answered wrongly (H06 = 0). A and C
   agents, computing from the source, got it right. After the fix (and a regression
   test that every catalogued code is wired) D scored 12/12.
5. **Adversarial fixtures:** every condition that saw metadata resisted the injected
   instruction and the draft supersession (H07, H08 = 1.0 in A, B, C, D). With the
   rule stated in the question, capable agents applied it themselves. The compiler's
   policy and EKS0015/EKS0021 flags made it explicit but did not change outcomes here.
6. **Unsupported conclusions:** B agents answered "none" on four relational questions
   where the excerpts were silent — absence of retrieved evidence read as evidence of
   absence. D packages state completeness explicitly ("all 592 records checked"),
   which is exactly what made the H06 defect dangerous.

## Limits

One model tier, n = 12 per condition, one run per batch (plus one re-run). Ceiling
effects dominate A, C, D. Questions name the relation fields to use. D was given the
right output type per question (an orchestrator decision).
