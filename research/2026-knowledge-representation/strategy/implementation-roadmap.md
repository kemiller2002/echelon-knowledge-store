# Implementation roadmap

Each stage extends the Praxis pipeline (or ships as a library Praxis calls — owners'
decision) and has a measured exit criterion. No content is migrated automatically.

| Stage | Deliverable | Exit criterion |
|---|---|---|
| 0 (done) | Research prototype, compiler, benchmarks, findings (PR #1) | review and merge |
| 1 | Add derived inverse links, closures and EKS rules to `praxis registry build`/`validate` as **non-blocking** reports in praxis and fides | owner-reviewed false-positive rate < 5%; mutation suite ≥ 22/26 |
| 2 | Manifest with content hashes; `verify` refusing stale registries | zero stale-answer incidents in CI over a month |
| 3 | `context`, `evidence`, `impact` commands; AGENTS.md points agents to them | on new held-out tasks including multi-step implementation tasks and two other model families: accuracy ≥ raw Markdown, wall time ≥ 30% lower |
| 4 | Agent write path: diagnostics fed back to agents editing records | repair rate measured; ≥ 80% of injected mistakes fixed without human help |
| 5 | Cross-repository references `repo:ID[@version]` + manifest harvesting; federated impact per reader access | impact precision and recall ≥ 0.9 on a fresh hand-verified gold set |
| 6 | Blocking rules (errors only) as required checks | two months without overrides |
| 7 | Generated views (decision logs, traceability) replace hand-written ones; Dokimos obligations if an interface is agreed | zero drift in pilot repositories |

Guardrails at every stage: catalogue-wiring test, preservation oracles, determinism
and warm = cold tests, raw sources remain readable by agents, no private-derived
artefact in a public location.

Owner decisions needed: enable Dependency graph (dependency review check), branch
protection on `main`, licence confirmation, where the compiler lives (Praxis vs EKS),
whether to keep the separate echelon-knowledge-store repository after stage 1.
