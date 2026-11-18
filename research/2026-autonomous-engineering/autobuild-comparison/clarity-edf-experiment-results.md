# Clarity / EDF experiment results (EXP-AE-001)

Mission EKS-RESEARCH-2026-003 · runs executed 2026-11-18 · 72 runs (36 per round).
**Measured.** Reproduce the summary with
`python3 -I experiments/clarity-edf/analyze.py experiments/clarity-edf` and re-grade with
`python3 -I experiments/clarity-edf/grade.py <exp-root-with-runs/> experiments/clarity-edf/oracle <out.json>`.
Plan: [`clarity-edf-experiment-plan.md`](clarity-edf-experiment-plan.md).

## Headline

- **No arm produced more verified outcomes than the baseline under the pre-registered
  rule.**
  - Round 1 (Opus-class): every arm 6/6, a ceiling.
  - Round 2 (Haiku): A 5/6, B 6/6, C 6/6, D 4/6, E 6/6, N 5/6, F 5/6. No arm differs from
    A by 2 or more. With n=6 the 95% Wilson interval for 6/6 is [0.61, 1.0] and for 5/6
    is [0.44, 0.97]; they overlap almost entirely.
- **Every analysis arm cost more wall time:** 1.3–2.2× baseline in round 1 and 1.8–2.2× in
  round 2. The hypothesis is **not supported** for correctness on these tasks. It is not
  refuted either, because n is far too small.
- **The one task that discriminated (T4, misattributed root cause) gives a mechanism-level
  result worth more than the counts:**
  - With the Haiku model, the baseline (A) and the EDF-derived protocol (D) each patched
    only the reported function and claimed completion. The nightly `conflicts()` report was
    still broken.
  - The D run had *identified* the second broken caller, but ranked fixing the shared
    origin as "premature" under EDF's control-point rule. It deferred the fix and still
    reported `complete`.
  - B, C and E, and the neutral checklist N up to its harness block, all fixed the origin.
  - Adaptive routing (F) sends bug reports to D, so F inherited D's failure.
- **The protective task (T6) did not discriminate.** All 12 runs, including both
  baselines, refused to delete the "unused" function that the accounting export uses, and
  asked the requester.

## Round 1: Opus-class model (ceiling)

| Arm | Verified | False "complete" | Requester questions | Mean wall (s) | vs A |
|---|---|---|---|---|---|
| A | 6/6 | 0 | 3 | 22.1 | 1.00× |
| B | 6/6 | 0 | 3 | 34.1 | 1.54× |
| C | 6/6 | 0 | 5 | 36.8 | 1.67× |
| D | 6/6 | 0 | 4 | 43.4 | 1.96× |
| E | 6/6 | 0 | 4 | 49.4 | 2.24× |
| N | 6/6 | 0 | 2 | 28.5 | 1.29× |
| F (derived) | 6/6 | 0 | — | 35.7 | 1.62× |

No run edited outside the expected scope (`raw/round1/graded-round1.json`).

Visible test files were modified in 29 of 36 round-1 runs and 20 of 36 round-2 runs.
Every one of those modifications **added** tests: a check over all of them found no
original test function or assertion removed or altered.

The grader's `timeSleepInLibrary` flag fired for round-1 A-T3, but the match is in a
docstring. The oracle replaces `time.sleep` with a function that fails the test if
called, and that run passed, so the flag is a false positive.

## Round 2: Haiku

| Arm | Verified | Excl. harness-blocked | False "complete" | Mean wall (s) | vs A | Mean tool uses |
|---|---|---|---|---|---|---|
| A | 5/6 | 5/6 | 1 (T4) | 18.6 | 1.00× | 10.0 |
| B | 6/6 | 6/6 | 0 | 33.1 | 1.78× | 12.7 |
| C | 6/6 | 6/6 | 0 | 35.1 | 1.89× | 11.3 |
| D | 4/6 | 4/5 | 1 (T4) | 37.5 | 2.02× | 12.5 |
| E | 6/6 | 6/6 | 0 | 40.7 | 2.19× | 12.5 |
| N | 5/6 | 5/5 | 0 | 36.5 | 1.96× | 13.5 |
| F (derived) | 5/6 | — | 1 | 34.6 | 1.86× | — |

Failures (`raw/round2/graded-round2.json`):

| Cell | Oracle | Claimed | Cause |
|---|---|---|---|
| A-T4 | 7/8 | complete | Patched `overlaps` locally; `conflicts()` still flags back-to-back bookings |
| D-T4 | 7/8 | complete | Same; its own analysis named the second caller but ranked the origin fix "premature" (`raw/round2/runs/D-T4/REPORT.json`) |
| D-T3 | 0/8 | incomplete | **Harness-blocked:** the runtime refused writing `ANALYSIS.md`, and the agent stopped rather than proceed |
| N-T4 | 5/8 | incomplete | **Harness-blocked**, same cause; its inline analysis had found the correct root cause |

## Threats to validity (read before citing)

1. **Sample size.** One trial per cell and six tasks per round. Differences of one task
   are noise.
2. **Ceiling (round 1).** Tasks written to contain traps were still solved by every
   Opus-class run. Echelon's realistic tasks may be harder; these fixtures are not.
3. **Model substitution (round 2).** Headroom came from a weaker model, not harder tasks.
   Results may not transfer to the model Echelon actually uses.
4. **Harness artifact.** The subagent runtime frequently refused `.md` "report" files.
   - It refused 2 of 6 arm-E runs in round 1 and most round-2 analysis arms.
   - Most agents reported the analysis inline and continued. Two (D-T3, N-T4) stopped,
     which penalises those arms for an environment rule.
   - Results are therefore shown both ways (`raw/round*/deviations.md`).
5. **Cost measure.** The harness's `subagent_tokens` appears to be final context size,
   dominated by a fixed ~65k-token prompt, so it is not billed usage. Wall time is the
   more reliable cost signal here.
6. **Arm B is an approximation.** It gives Praxis's rules as text, without the CLI or
   evidence gates.
7. **Neutral control not length-matched:** 213 words against about 550.
8. **Task authorship.** The tasks and oracles were written by the same agent that
   designed the protocols. They were written before any run and checked against
   reference solutions, but they are not independent.

## Interpretation

- **Measured.** On small, well-documented repositories, a current Opus-class agent already:
  - reads decision records and conventions without being told to;
  - follows recorded decisions over conflicting issue text (T3: all 11 runs that changed
    code, across both models);
  - refuses destructive "cleanup" (T6: 12/12 runs).

  Mandatory pre-implementation analysis added 30–124% wall time for no measured gain.
- **Measured, single case.** EDF's "premature / harmful" ranking rule can steer an agent
  *away* from a root-cause fix. That cost a verified outcome and produced a false
  completion claim, the failure mode Echelon's verification work is meant to prevent.
- **Hypothesis, untested.** Structured analysis may help on tasks with real ambiguity that
  cannot be resolved by reading the repository, or with a weaker model. Round 2 weakly
  hints at the second (B/C/E 6/6 against A 5/6) but cannot show it.
- **Adaptive invocation.** Protocol F as written routes bug reports to EDF alone. In this
  data that was the worst arm on the bug task, so F as specified should not be adopted.
  A revised rule (route unexplained failures to E, or to B's lighter discipline) is a
  hypothesis for the next round.

## Next experiment (recommended)

- 20+ tasks from real Echelon history: closed issues with known-good fixes, held out
  from agents.
- 3 trials per cell, the production model, and the A-effort arm added.
- The analysis returned in the final report instead of a file, avoiding the harness
  artifact.
- Pre-register again. The measurable target: a reduction in false completion claims and
  in root-cause misses, not prose quality.
