# Implementation roadmap

Each phase has an exit criterion measured by the benchmark suite. No phase
migrates content automatically.

| Phase | Deliverable | Exit criterion |
|---|---|---|
| 0 (done) | Research prototype, benchmarks, findings (this PR) | merged after review |
| 1 Validator CLI | `eks validate` over a repository (rules from `Validation.fs`, calibrated); runs as a non-blocking CI job in 2 pilot repositories (praxis, fides) | false-positive rate < 5% on owner review of every finding |
| 2 Generated index | `eks index` → `INDEX.md` + `index.jsonl` with derived inverse links, as CI artefacts; AGENTS.md points agents to it | agent trials: equal accuracy, ≥ 20% fewer tool calls (trials showed −1 call, −30% time with Sonnet) |
| 3 Context packages | `eks context <task> <id>` (implement, evidence, impact, history) | trials on new held-out questions incl. adversarial cases; no accuracy loss; time ≥ 30% lower |
| 4 Cross-repository references | `repo:ID[@version]` fields; manifest harvesting; federated impact query per reader's access | impact analysis precision ≥ 0.9 and recall ≥ 0.9 against a fresh hand-verified gold set (fides, forma, dokimos scenarios) |
| 5 Enforce | blocking rules (dangling refs, cycles, missing replacements, private → public) as required checks | two consecutive months with no override |
| 6 Generated views | decision log, traceability (needs `implements`/`validates`), Dokimos obligations if Dokimos exposes a verified interface | generated views replace hand-written ones in pilot repos; zero drift |

Out of scope unless new evidence: custom DSL, F# source storage, graph database,
central knowledge repository, embedding service.

Owners' decisions needed: dependency graph setting (for dependency review),
branch protection on `main`, licence confirmation, and whether Praxis (which
already owns the front-matter conventions) should host the validator instead of EKS.
