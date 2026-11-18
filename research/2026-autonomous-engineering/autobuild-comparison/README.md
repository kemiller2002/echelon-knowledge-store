# EKS-RESEARCH-2026-003: autonomous engineering improvement, with Obvious AutoBuild as reference

**Question:** can Echelon Foundry produce more correct, independently verified
engineering outcomes with less total cost, less human attention and fewer downstream
failures?

**Short answer (measured, small samples):**
- **Not by adding analysis frameworks to the loop.** In 72 controlled runs, mandatory
  Clarity or EDF analysis gave no measured correctness gain over a baseline agent and
  cost 30–124% more wall time.
- **The measurable lever is preventing false "complete" claims.** Echelon's verification
  design already resists them, but it is enabled in 0 of 13 repositories.
- This mission fixed two verification defects that let false or foreign evidence count
  as verified.
- It added a governed contract for learning from failures.

Start with [`final-report.md`](final-report.md).

Status: **PARTIALLY COMPLETE**. All phases were executed. The effectiveness questions
need larger, real-task experiments (`implementation-roadmap.md` R6–R8).

## Contents

| File | Phase |
|---|---|
| `final-report.md` | Answer, implemented changes, PRs, tests, blockers, adversarial review |
| `competitive-analysis.md`, `capability-matrix.md` | 1: AutoBuild comparison |
| `data/baseline-group-*.json` | 0: baseline inventory of 14 systems |
| `verification-audit.md`, `verification/` | 2: verification hardening |
| `execution-contract-design.md` | 3: typed execution contract |
| `governed-learning-design.md` | 4: governed self-improvement |
| `knowledge-compilation-results.md` | 5: compiled vs Markdown knowledge |
| `clarity-edf-experiment-plan.md`, `clarity-edf-experiment-results.md`, `experiments/clarity-edf/` | 6: Clarity / EDF experiments |
| `economic-analysis.md` | 7: cost and attention |
| `implementation-roadmap.md` | 8 and 9: control plane plan, prioritised roadmap |
| `adaptation-recommendations.md` | adopt / improve / reject |
| `evidence-register.md` | every `EV-AE-*` claim and its source |
| `continuation/research-status.json` | machine-readable state for a successor |

## Reproducing

```
cd experiments/clarity-edf
python3 -I analyze.py .        # results-summary.json from raw/
python3 -I economics.py .      # economics.json
python3 -I ../../scripts/test-weakening-check.py .
# Re-grade a run tree (directory with runs/<arm>-<task>/ and fixtures/):
python3 -I grade.py <exp-root> oracle <out.json>
```

Agent runs themselves are not deterministic. `raw/` keeps every run's final tree,
`REPORT.json` and analysis file (where the harness allowed one).

## Deviation from repository convention (for reviewer decision)

`AGENTS.md` requires F# for implementation code in `src/`, `tests/`, `benchmarks/` and
`tools/`, and this repository has no Python. The experiment harness here (`grade.py`,
`analyze.py`, `economics.py`, `scripts/*.py`) is Python because:
- the task fixtures are Python repositories;
- the grader drives `pytest` over them.

It lives under `research/`, not in the F#-only directories, and it is not application
code. If the owner prefers, the three scripts can be ported to an F# tool under
`benchmarks/`; the fixtures themselves would stay Python.
