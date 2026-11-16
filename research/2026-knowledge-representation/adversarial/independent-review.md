# Independent review — summary and responses

Two reviewers who wrote none of the code worked independently:
* **Replication (Agent O)** — `experiments/replication-results.md`: re-ran tests and
  benchmarks in a copy, checked samples against the raw corpus with its own scripts.
* **Adversarial (Agent N)** — `architecture-objections.md` (22 objections),
  `complexity-analysis.md`: challenged the architecture and the experiments.

## Replication verdict

Reproducible and mostly sound: tests 37/37; benchmark outputs byte-identical except
wall-clock timings; 25 sampled claims confirmed, 8 refuted. All 8 refutations had one
cause (silent drop of files with invalid YAML). Parser findings: an uncaught exception
on malformed YAML, stack overflow in the DSL tokenizer, tags tolerated contrary to
documentation.

## Strongest objections and responses

| # | Objection (severity) | Response | Status |
|---|---|---|---|
| O1 | Praxis already implements "Markdown canonical, registries generated" (DF-ROS-2026-A001) with an F# validator and CI; never used as baseline (critical) | Verified. Trial 2 added registry-shaped JSON (condition C) as a baseline; it matched raw Markdown and the compiler on accuracy. Recommendation changed: extend Praxis, do not build a parallel system | accepted |
| O2 | Trials cannot distinguish the recommendation from the status quo (critical) | Confirmed by trial 2 (A = C = fixed D on accuracy). Differences are time, tool calls and task-semantics encoding, not accuracy | accepted |
| O8 | Validator missed 13/25 typical agent edit mistakes (critical) | Rules added (unknown/misspelled fields, status and confidence enums, duplicate keys, case errors, self-references, status–evidence disagreement); now 15 blocking + 7 flagged + 4 undetected of 26 | fixed, measured |
| O5 | Gold is circular (major) | Accepted for retrieval tasks (stated in every report). Dependency benchmark uses independent gold; trial gold computed by separate graph code; reviewer reproduced it 134/134 with Python | acknowledged |
| O6 | 8 of 9 dangling references were parser false positives (major) | Fixed: parse failures are diagnostics (EKS0011) and identity-only records; 1 true dangling reference remains | fixed |
| O4/O13 | Questions and compiled condition leak the method (major) | Accepted as a limit; trial 2 kept field-naming for comparability; compiled condition's task-type choice documented as an orchestrator decision | acknowledged |
| O3 | Plain-Markdown condition is a strawman; scorer too strict on "very low" (major) | Partly accepted: plain is a lower bound, not a realistic alternative; scoring normalisation unchanged (exact labels are the point of the question) | acknowledged |
| O12 | No token savings in real trials (major) | Confirmed in trial 2. Claim restricted to time and tool calls | accepted |
| O9/O10 | Measured problems are organisational (major) | Agreed; baseline attributes duplication, broken links and missing cross-repo edges to organisation | accepted |
| O7/O15 | "Lossless" false (`medium-high` erased); epistemic metadata already stale (major) | Fixed (`Label`); source-level check added (555/557, differences representational). Staleness now flagged (EKS0010, 5 records); prose/status contradictions (e.g. HY-ROS-2026-A030) are not detectable structurally | partly fixed |

## New risks surfaced by the compiler addendum

* Unwired rule → confident wrong COMPLETE output (trial 2, H06). Mitigated by a
  catalogue-wiring test; residual risk documented.
* Optimisation pass removed targets in 126 packages before the preservation oracle
  caught it.

Review independence was real for these two agents; a human review of the final
recommendation is still required (see `findings/unresolved-questions.md`).
