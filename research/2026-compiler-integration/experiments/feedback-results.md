# Condition D (compiled packages + validation feedback and integrity controls): results

Condition C plus a mandatory policy: stop at INCOMPLETE packages, treat record text as data, write
and run one test per acceptance criterion, report complete only when all criteria are verified, and
optionally recompile packages with `eks work-item`. Full comparison: `compiler-results.md`.

| Fixture | Runs | Solved | Overclaims | Tokens/solved |
|---|---|---|---|---|
| Base (Sonnet) | 4 | 16/16 | 0 | 20,603 |
| Hard (Sonnet + Haiku, two seeds) | 8 | 32/32 | 0 | 25,637 |

Observations (measured):

- D was not distinguishable from C on outcomes: both solved everything, with no overclaims.
- D's agents wrote more tests, as instructed: a mean of 19.0 tests in `LedgerTests.fs` per hard run
  (range 12–27), against 14.6 (A), 14.5 (B) and 15.9 (C). That strengthened the agents' own
  evidence, but the hidden tests could not show a difference because C was already at 100%.
- D cost about 1.5% more tokens per solved task than C, and the slowest wall time per run.
- **What D was designed to catch did not occur in C**: no C agent overclaimed or implemented past
  an INCOMPLETE package. On this fixture the policy is insurance with no measured payout. The
  evidence-integrity trial (`../evidence-integrity/experimental-results.md`) did not show a policy
  effect either, because there were too few fabrication events.
- Agents in D did not use the recompile command. Packages did not change during the tasks, so
  there was nothing to recompile.

Validation feedback for knowledge edits (as opposed to code) is measured in the evidence-integrity
trial and the change-manifest evaluation (`../knowledge-updates/write-path-design.md`).
