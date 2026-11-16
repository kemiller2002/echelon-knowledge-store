# EKS-RESEARCH-2026-002: compiler integration and autonomous development validation

Question: do compiled, validated, task-specific knowledge packages make autonomous agents produce
more correct software, with fewer missed requirements and lower total cost, than reading repository
documentation?

Short answer (measured, on a synthetic benchmark): not when knowledge is easy to find; yes when
requirements are amended by records the work item does not mention. There, packages gave 32/32
verified tasks against 24/32 for the existing workflow, at 24% lower cost per verified task. Start
with `strategy/executive-report.md`.

Status: **PARTIALLY COMPLETE** (no long-running tasks; no real-repository tasks).

## Contents

| Folder | What it holds |
|---|---|
| `baseline/` | revisions used, Phase 1 findings relied on, risks carried in and out |
| `architecture/` | verified Praxis and Dokimos interfaces, task-package contract, manifests, incremental compilation |
| `evidence-integrity/` | threat model, source validation, semantic support, five-condition experiment |
| `benchmarks/` | coding task catalog, rubric, design, held-out policy |
| `experiments/` | per-condition results, replication; `raw/` holds every score, assignment, usage record, agent report and agent-written source |
| `knowledge-updates/` | write path, update correctness (impact precision/recall), conflicts |
| `security/` | privacy incident review and remediation; adversarial results |
| `strategy/` | decision gates, staged roadmap, executive report |
| `continuation/` | machine-readable status and next steps |

## Reproducing

```
# build
dotnet build Echelon.KnowledgeStore.sln -c Release
# work-item package and manifest for the fixture
dotnet run --project tools/Echelon.KnowledgeStore.Cli -c Release -- work-item fx-ledger WI-0003 --root benchmarks/coding-fixture/repos --as-of 2026-10-10
# coding trial sandboxes (base or hard), then scoring after agents finish
EKS_CODING_ROOT=/path EKS_CODING_VARIANT=hard EKS_CODING_REPS=1 EKS_CODING_SEED=20261117 \
  dotnet run --project benchmarks/Echelon.KnowledgeStore.Bench -c Release -- coding-prepare
EKS_CODING_ROOT=/path dotnet run --project benchmarks/Echelon.KnowledgeStore.Bench -c Release -- coding-score
# evidence-integrity trial
EKS_EVIDENCE_ROOT=/path dotnet run --project benchmarks/Echelon.KnowledgeStore.Bench -c Release -- evidence-prepare   # then evidence-score
# incremental compilation (reads the public repositories under EKS_CORPUS_ROOT)
dotnet run --project benchmarks/Echelon.KnowledgeStore.Bench -c Release -- incremental
```

Agent runs are stochastic; preparation, sandboxes and scoring are deterministic per seed.
