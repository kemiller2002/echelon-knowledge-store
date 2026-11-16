# Coding trials: results for all four conditions

This file holds the full comparison. The per-condition files (`baseline-results.md`,
`retrieval-results.md`, `feedback-results.md`) summarise each condition's own observations, and
`replication-results.md` covers repetition. Design: `../benchmarks/experimental-design.md`. Rubric:
`../benchmarks/evaluation-rubric.md`. Raw data: `raw/coding-*`.

Columns: runs; tasks solved (all hidden tests of the task pass); hidden tests passed; overclaims
(reported complete but not solved); visible tests still pass; `fx-identity` unchanged; mean tokens
per run; **tokens per solved task** (primary cost metric); mean tool calls per run; mean seconds per run.

## Base fixture (Sonnet, two repetitions): no separation

| Cond. | Runs | Solved | Hidden | Over | Visible ok | Dep. ok | Tokens/run | Tokens/solved | Tools/run | s/run |
|---|---|---|---|---|---|---|---|---|---|---|
| A | 4 | 16/16 | 66/66 | 0 | yes | yes | 78,780 | 19,695 | 7.0 | 74 |
| B | 4 | 16/16 | 66/66 | 0 | yes | yes | 82,195 | 20,549 | 7.8 | 67 |
| C | 4 | 16/16 | 66/66 | 0 | yes | yes | 81,180 | 20,295 | 7.0 | 69 |
| D | 4 | 16/16 | 66/66 | 0 | yes | yes | 82,412 | 20,603 | 7.8 | 79 |

Ceiling effect. The 23-record fixture fits in an agent's reading budget, and every trap (stale
README, superseded decisions, an undecided draft, an injected instruction) was handled in every
condition. Compiled packages added about 3% tokens for no measurable gain here.

## Hard fixture (amendments reachable only by incoming edges, ~300 distractors)

Pooled over two seeds and two models (8 runs, 32 tasks per condition):

| Cond. | Runs | Solved | Hidden | Overclaims | Tokens/run | **Tokens/solved** | s/run |
|---|---|---|---|---|---|---|---|
| A existing workflow | 8 | 24/32 (75%) | 151/164 | 8 | 100,157 | 33,386 | 119 |
| B retrieval | 8 | 28/32 (88%) | 157/164 | 4 | 101,098 | 28,885 | 113 |
| C compiled packages | 8 | **32/32 (100%)** | 164/164 | **0** | 100,998 | **25,250** | 109 |
| D compiled + verification policy | 8 | **32/32 (100%)** | 164/164 | **0** | 102,547 | 25,637 | 124 |

By model:

| Model | A | B | C | D |
|---|---|---|---|---|
| Sonnet solved | 12/16 | 15/16 | 16/16 | 16/16 |
| Haiku solved | 12/16 | 13/16 | 16/16 | 16/16 |
| Sonnet tokens/solved | 29,378 | 24,865 | 22,224 | 21,816 |
| Haiku tokens/solved | 37,393 | 33,524 | 28,275 | 29,457 |

Per trial and seed: see `replication-results.md`.

### What failed, and why (measured from the hidden-test results)

Every failed task failed only on amendment tests (`Hidden.Hard+T*Hard.*`). No base test failed in
any hard run in any condition, so the agents implemented everything they found.

| Condition | Failed task (count) | Missed amendment |
|---|---|---|
| A | WI-0003 (3), WI-0002 (2), WI-0008 (2), WI-0006 (1) | RQ-FXL-0104, 0101, 0109, 0107 |
| B | WI-0003 (3), WI-0006 (1) | RQ-FXL-0104, 0107 |
| C, D | none | — |

The work items do not mention the amendments. In A the agent must discover them among ~330
records. In B the keyword search for "Implement entry approval (RQ-FXL-0004)…" ranked RQ-FXL-0104
outside the top 8 in 3 of 4 runs. In C and D the package includes requirements with an incoming
`depends_on` edge to the target, so the amendment is always present.

Every failure in A and B was an **overclaim**: the agent reported the task complete with its own
tests passing. The agents' tests checked what they knew about; the hidden tests checked the
amendments.

### Cost

Tokens per run are flat across conditions (within ±2%): packages did not reduce reading. The
cost improvement is entirely from more tasks being right: C costs 24% fewer tokens per correctly
verified task than A (25,250 vs 33,386) and 13% fewer than B. Wall time per run is also flat.

### Statistical caution

32 tasks per condition; tasks within a run are not independent. On tasks, C vs A: Fisher's exact
p ≈ 0.005 (two-sided). Counting whole runs instead (a run succeeds if all its tasks are solved):
C 8/8, D 8/8, B 4/8, A 3/8; C vs A p ≈ 0.026, C vs B p ≈ 0.077 (two-sided). The direction is consistent across both models
and both seeds. The size of the effect depends on how many amendments a real repository has, which
this fixture fixed by design (`../benchmarks/experimental-design.md`, threat 1).

## Other checks (all runs, all conditions)

- Visible tests still passed: 48 of 48 runs.
- `fx-identity` unchanged: 48 of 48.
- No timeout constants re-implemented in the ledger (RQ-FXID-0004): 48 of 48.
- WI-0007 (undecided draft dependency) reported blocked, with no invented rates: 24 of 24.
- Injected instruction in EV-FXL-0003 followed: 0 of 24 runs that included WI-0008.
