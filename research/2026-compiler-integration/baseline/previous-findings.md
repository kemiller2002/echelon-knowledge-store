# Previous findings (Phase 1, EKS-RESEARCH-2026-001)

Source: `research/2026-knowledge-representation/` on main at `c0ba624`. This file restates
what Phase 2 relies on and marks each item as measured or hypothesis. Phase 2 does not
rewrite Phase 1 artefacts.

| # | Finding | Kind | Phase 2 relevance |
|---|---|---|---|
| F1 | Markdown + YAML front matter remains the canonical form; registries and packages are generated. Rejected: DSL, F# source, JSON storage, graph DB, central service, central repository. | decision backed by measurements | Phase 2 compiles from Markdown; no storage change is proposed. |
| F2 | The compiler and `eks` CLI produce derived links, dependency closures, diagnostics and task packages deterministically. | measured (unit tests, benchmarks) | Extended with rule-execution records, derived completeness, manifests, work-item packages. |
| F3 | Agent trials (read path): equal accuracy with and without compiled context, about 2× faster agents, no end-to-end token saving. | measured, small n | Re-tested on real coding tasks with hidden tests (this phase). |
| F4 | Write path: the compiler did not prevent fabricated evidence; an explicit integrity policy did (Sonnet 2/2 fabricating → 0/2). | measured, n = 2 per cell | Re-tested with five conditions and two models (this phase). |
| F5 | A diagnostic (EKS0026) existed but was not wired into the pipeline; found only by a wiring test. | measured defect | Generalised into "rules that exist but do not execute"; rule runs are now recorded and checked. |
| F6 | Private repository directory and file names were published in public Git history. | incident | Reviewed and remediated in `../security/privacy-review.md`. |

## What Phase 1 could not establish

- Whether compiled packages change *software* outcomes (correctness of code), as opposed to
  answers to questions about knowledge.
- Cost per correctly verified task (Phase 1 measured tokens and time, not verified outcomes).
- Whether the result holds on more than one model.
- Whether EKS can feed Praxis and Dokimos through their real interfaces.

These are the Phase 2 questions.
