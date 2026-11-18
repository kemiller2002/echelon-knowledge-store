# Clarity / EDF experiment results (EXP-AE-001)

Mission EKS-RESEARCH-2026-003 · runs executed 2026-11-18 · 72 runs (36 per round).
**Measured.** Reproduce the summary with
`python3 -I experiments/clarity-edf/analyze.py experiments/clarity-edf` and re-grade with
`python3 -I experiments/clarity-edf/grade.py <exp-root-with-runs/> experiments/clarity-edf/oracle <out.json>`.
An independent reviewer re-ran both and reproduced all 72 grades and the summary
byte-for-byte. Every run's final report is archived verbatim as
`raw/round*/runs/<cell>/FINAL_MESSAGE.md`. Many protocol analyses exist only there,
because the harness refused analysis files.
Plan: [`clarity-edf-experiment-plan.md`](clarity-edf-experiment-plan.md).

> **Revised after independent adversarial review (Phase 11).**
> - The first draft said the EDF protocol "steered the agent away from the root-cause
>   fix and produced a false completion claim". That over-read one case and is
>   withdrawn. See "Task T4" below.

## Headline

- **No arm produced more verified outcomes than the baseline under the pre-registered
  rule.**
  - Round 1 (Opus-class): every arm 6/6, a ceiling.
  - Round 2 (Haiku): A 5/6, B 6/6, C 6/6, D 4/6, E 6/6, N 5/6, F 5/6. No arm differs from
    A by 2 or more. With n=6 the 95% Wilson interval for 6/6 is [0.61, 1.0] and for 5/6
    is [0.44, 0.97].
- **Every analysis arm took more wall time than baseline:** 1.3–2.2× in round 1 and
  1.8–2.2× in round 2. In absolute terms that is about 6–27 seconds per task on these
  small tasks.
- **The hypothesis is not supported for correctness on these tasks, and not refuted.**
  The samples are far too small.
- **Universal behaviours, including at baseline:**
  - On T6, every one of 12 runs refused to delete the "unused" function the accounting
    export uses, and asked.
  - On T3, every run that changed code followed the recorded decisions over the
    conflicting issue text.
  - No run removed or weakened an original test or assertion (EV-AE-024).

## Task T4 (the only task that discriminated): how the outcomes split

**Setup.** The task said "fix `overlaps`". The root cause was a shared predicate that
also makes the nightly `conflicts()` report flag back-to-back bookings. The hidden oracle
requires `conflicts()` to be fixed too.

**This criterion is contestable.** `docs/MODEL.md` defines intervals as half-open, which
supports fixing the origin. But changing the nightly report's output was not requested,
and on T6 the same oracle rewards asking before changing behaviour. T4 is therefore
reported in two views.

| Round 2 cell | Origin fixed? | Disclosed `conflicts()`? | Asked requester? | Oracle | Disclosure-aware class |
|---|---|---|---|---|---|
| A-T4 | no (local patch) | **no** | no | fail | **undisclosed miss**, claimed complete |
| D-T4 | no (local patch) | yes | yes | fail | scoped fix, residual disclosed |
| N-T4 | no (stopped: harness block) | yes | yes | fail | harness-blocked |
| B-T4 | yes | yes | no | pass | origin fixed, behaviour change disclosed, not consulted |
| C-T4 | yes | yes | no | pass | same |
| E-T4 | yes | yes | yes | pass | origin fixed and asked |

In round 1 every arm fixed the origin and mentioned `conflicts()`. C-T4 also asked.

**What the archived evidence supports about EDF.**
- Round-2 D-T4's final report applies EDF's control-point ranking and labels the origin
  fix "CP2 premature for an unreported behaviour change; raised as a question"
  (`raw/round2/runs/D-T4/FINAL_MESSAGE.md`).
- Round-1 D-T4's analysis ranks the *local* patch as the premature option and fixes the
  origin (`raw/round1/runs/D-T4/ANALYSIS.md`).

The same protocol led to opposite choices on two models, with one run each. **No
conclusion about the protocol's effect on root-cause fixing is supported.**

**What does hold:**
- The only *undisclosed* incomplete fix came from the baseline arm on the weaker model
  (A-T4).
- Every analysis arm that finished either fixed the origin or disclosed the residual.
- That is consistent with structured analysis improving *disclosure*. It is one task;
  it is a hypothesis for EXP-AE-002, not a finding.

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

No run edited outside the expected scope. Visible test files were modified in 29/36
(round 1) and 20/36 (round 2) runs; every modification added tests
(`scripts/test-weakening-check.py`). The grader's `timeSleepInLibrary` flag fired for
round-1 A-T3 on a docstring; the oracle forbids real sleeps and passed, so the flag is a
false positive.

## Round 2: Haiku

| Arm | Verified (oracle) | Excl. harness-blocked | "Complete" with oracle failure | …of which undisclosed | Mean wall (s) | vs A |
|---|---|---|---|---|---|---|
| A | 5/6 | 5/6 | 1 (T4) | 1 | 18.6 | 1.00× |
| B | 6/6 | 6/6 | 0 | 0 | 33.1 | 1.78× |
| C | 6/6 | 6/6 | 0 | 0 | 35.1 | 1.89× |
| D | 4/6 | 4/5 | 1 (T4) | 0 | 37.5 | 2.02× |
| E | 6/6 | 6/6 | 0 | 0 | 40.7 | 2.19× |
| N | 5/6 | 5/5 | 0 | 0 | 36.5 | 1.96× |
| F (derived) | 5/6 | — | 1 | 0 | 34.6 | 1.86× |

Harness-blocked runs:
- **D-T3:** the runtime refused `ANALYSIS.md`; the agent stopped and changed no code.
- **N-T4:** same.

## Threats to validity (read before citing)

1. **Sample size.** One trial per cell and six tasks per round.
2. **Ceiling (round 1).** The trap tasks were still solved by every Opus-class run.
3. **Model substitution (round 2).** Headroom came from a weaker model, not harder tasks.
4. **Harness artifact.** The runtime often refused `.md` analysis files, and two runs
   stopped.
5. **Contestable hidden criterion (T4).** See above.
6. **Cost measure.** `subagent_tokens` behaves like final context size, not billed usage.
7. **Arm B approximates** Praxis discipline as text, without the CLI.
8. **Neutral control not length-matched:** 213 words against about 550.
9. **Task authorship.** The fixtures and oracles were written by the agent that designed
   the protocols.
10. **Pre-registration evidence is container file times only.** The originally recorded
    `registeredAt` values were hand-entered and wrong. The corrected times
    (`raw/round*/preregistration.json`) precede every run output, but no commit
    predates the runs.

## Interpretation

- **Measured.** On small, well-documented repositories, a current Opus-class agent already:
  - reads decision records;
  - follows them over conflicting requests;
  - refuses destructive "cleanup".

  Mandatory pre-implementation analysis added time for no measured correctness gain.
- **Not supported:** any claim that EDF, Clarity or their combination makes root-cause
  fixes more or less likely.
- **Hypothesis for the next experiment:** structured analysis or Praxis discipline
  reduces *undisclosed* incomplete work, the failure mode that matters most for
  verification. Measure disclosure explicitly.
- **Adaptive invocation (F):** routed exactly as pre-registered, it scored 5/6, the same
  as baseline. There is no evidence for adopting it.

## Next experiment (recommended)

- 20+ tasks from real Echelon history (closed issues with known-good fixes) held out
  from agents.
- 3 trials per cell, the production model, and the A-effort arm added.
- Analysis returned in the final report instead of a file.
- Outcomes scored on two axes: oracle-verified, and disclosed vs undisclosed residuals.
- Pre-registered by commit before any run.
