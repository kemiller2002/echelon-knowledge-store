# Baseline repository revisions

Mission EKS-RESEARCH-2026-002 starts from these immutable revisions. Every experiment
in this phase reads them read-only (Praxis, Dokimos, Percepta) or runs in sandbox copies
(the synthetic fixtures). Nothing in Praxis or Dokimos was modified.

| Repository | Role in this phase | Revision | How it was read |
|---|---|---|---|
| echelon-knowledge-store | compiler, CLI, benchmarks, deliverables | `c0ba6242516d88db934946748056cbc7eea12a92` (main, merge of PR #3) | branch `claude/echelon-knowledge-store-research-horhs0`, PR #4 |
| praxis | work-item pipeline, completion gates | `12bf9951253fc15e6973c2d3f5b88076f4556245` | read-only clone |
| dokimos | verification obligations, coverage | `6ea86246cb13f141201bc5cae5d84ebe9831d02b` (version 0.11.1) | read-only clone; CLI built into a scratch directory |
| percepta | public corpus member | `4a53688238f205fdde474cc340378a89ddea0c09` | read-only clone |

The public corpus used for compiler-scale measurements (incremental compilation, rule
regressions) is the set of public repositories cloned in the workspace: praxis, fides,
signal, forma, percepta, dokimos, aegis, arca, mercatus. Private repositories are not
read by any Phase 2 experiment; see `../security/privacy-review.md`.

## Tooling

- .NET SDK 10.0.404 (`global.json` 10.0.400, `rollForward: latestFeature`).
- Dokimos targets .NET 8; its CLI was run with `DOTNET_ROLL_FORWARD=Major` on the
  .NET 10 runtime because no .NET 8 runtime is installed. Results are recorded as
  produced under roll-forward.
- Models used for agent trials: Claude Sonnet and Claude Haiku (subagents of the
  research session). Model identifiers are deliberately not recorded in the repository.

## Fixtures created in this phase (synthetic, public)

- `benchmarks/coding-fixture/repos/fx-identity`, `.../fx-ledger`: two invented
  repositories with Praxis-shaped work items and knowledge records.
- `benchmarks/coding-fixture/hidden`: held-out evaluation tests (see
  `../benchmarks/held-out-policy.md` for the contamination caveat).
- `benchmarks/coding-fixture/hard`: amendments, decoys and a distractor generator.
- `benchmarks/coding-fixture/evidence/sources`: source documents for the
  evidence-integrity experiment.

None of the fixtures copies private repository content.
