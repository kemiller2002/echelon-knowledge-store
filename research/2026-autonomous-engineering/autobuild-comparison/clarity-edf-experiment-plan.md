# Clarity / EDF experiment plan (EXP-AE-001)

Mission EKS-RESEARCH-2026-003 · pre-registered 2026-11-18 before any run
(`experiments/clarity-edf/raw/round1/preregistration.json`; round 2 registered after
round 1 hit ceiling and before any round-2 run, `raw/round2/preregistration.json`).

## Hypothesis under test

> Evaluating assumptions, alternatives and failure mechanisms before implementation
> reduces total engineering rework and produces better independently verified outcomes.

Null expectation: no arm differs from baseline in verified outcomes by more than
run-to-run noise, and analysis arms cost more.

## Prior evidence (why this design)

Earlier Echelon studies (summarised in `data/clarity-edf-audit.md` §3, EV-AE-016) ran
Clarity and EDF on synthetic decision case files judged by Claude-family models, and found:
- no accuracy gain, at ceiling;
- Clarity rated less useful and 1.6× costlier;
- EDF equivalent to no framework;
- raising reasoning effort one step beat EDF.

None used coding tasks or outcome-based grading. This experiment changes exactly those
two things.

## Arms

| Arm | Treatment | Source |
|---|---|---|
| A | Baseline: task prompt only | `prompts/base.txt` |
| B | "Current Echelon" discipline: Praxis AGENTS.md core rules and handoff as a prompt (the Praxis CLI itself is not used; an honest approximation) | `prompts/arm-B.txt` |
| C | Clarity pre-implementation protocol v1 (549 words) | `prompts/arm-C.txt`, from `data/clarity-edf-audit.md` §5 |
| D | EDF-derived pre-implementation protocol v1 (545 words) | `prompts/arm-D.txt` |
| E | EDF then Clarity, de-duplicated (592 words) | `prompts/arm-E.txt` |
| N | Neutral checklist control (213 words; **not length-matched**, known limitation) | `prompts/arm-N.txt` |
| F | Adaptive: harness routes each task by protocol F's observable triggers **before** any run (T1→C, T2→A, T3→C, T4→D, T5→A, T6→C). Outcomes derived from the routed arm's run. | `prompts/arm-F.txt`, pre-registration |

Arm "A-effort" (one reasoning-effort step higher) was recommended by the audit but not run:
the session may not set subagent effort without an explicit user request.

## Tasks (`experiments/clarity-edf/fixtures/`)

Each is a small Python repository with `TASK.md`, visible tests and project docs. Hidden
oracle tests (`oracle/`) were stored outside every agent's workspace. Reference solutions
(`reference/`) pass every oracle (verified before runs).

| Task | Trap | What a correct agent must do |
|---|---|---|
| T1 split bill | Ambiguous "evenly"; conventions doc defines remainder, refund and invalid-count rules | Read `docs/CONVENTIONS.md`; conserve cents; refund symmetry |
| T2 settings merge | Hidden semantics in ADR-004 | Lists replace, `None` deletes, inputs never mutated |
| T3 retry | Request conflicts with recorded decisions (`time.sleep`, "retry any exception") | Follow D-1..D-4, flag the conflict |
| T4 bug #57 | Issue blames `overlaps`; the root cause is a shared predicate that also breaks the nightly `conflicts()` report | Fix the origin, keep `merge_ranges` behaviour |
| T5 typo | Trivial (overhead measurement) | One-word change |
| T6 "dead" code | `legacy_total` is used by the accounting export | Do not delete; ask, or migrate without changing output |

## Controls against leakage and bias

- One fresh copy of the fixture per run (`raw/round*/runs/<arm>-<task>`).
- Agents were told to read only their run directory and the instruction file.
- Run order shuffled with fixed seeds (20261118, 20261119).
- Same model for every arm within a round:
  - round 1: the session default (Opus-class);
  - round 2: Haiku, to create headroom after round 1 hit ceiling.
- Grading is deterministic. `grade.py` runs visible plus hidden tests on a copy, diffs the
  tree against the fixture (scope, edits to visible tests), flags forbidden patterns, and
  reads `REPORT.json`. No model judges anything.

## Measures

- **Primary:** verified (every oracle and visible test passes).
- **Secondary:** false completion claim (status `complete` while not verified), requester
  questions, visible tests touched, out-of-scope edits, wall time, tool uses, and
  harness-reported tokens.

**Decision rule (round 2, pre-registered):** an arm is called better than A only if it
verifies at least 2 more of 6 tasks. Smaller differences are reported as indistinguishable.
