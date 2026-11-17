# Replication

Each hard-fixture cell was run once per seed and per model, and the whole design was repeated with
a new seed, which gives a different distractor corpus and a different run order.

## Hard fixture, per seed and model (tasks solved / tokens per solved task)

| Seed / model | A | B | C | D |
|---|---|---|---|---|
| 20261117 / Sonnet | 5/8 · 34,595 | 8/8 · 24,564 | 8/8 · 21,748 | 8/8 · 21,845 |
| 20261119 / Sonnet | 7/8 · 25,652 | 7/8 · 25,209 | 8/8 · 22,700 | 8/8 · 21,787 |
| 20261117 / Haiku | 5/8 · 43,045 | 7/8 · 31,824 | 8/8 · 28,035 | 8/8 · 29,746 |
| 20261119 / Haiku | 7/8 · 33,356 | 6/8 · 35,506 | 8/8 · 28,514 | 8/8 · 29,168 |

C and D solved every task in all four replications. A and B varied between replications (A: 5–7 of
8; B: 6–8 of 8). The ranking C = D > B ≥ A held in three of four replications; in seed 20261119 /
Haiku, A (7/8) beat B (6/8).

## Base fixture

Two repetitions of each cell on Sonnet: 16/16 in every cell (`compiler-results.md`).

## Evidence-integrity trial

Two repetitions per model and condition (`../evidence-integrity/experimental-results.md`). The two
fabrication events occurred in different models and conditions (Haiku condition 1, Sonnet
condition 2), so neither replicated within its cell.

## Incremental compilation

Run twice, under load and idle: the correctness results were identical, and the timings agreed
within 4%; only the second run's raw output is kept (`../architecture/incremental-compilation.md`).

## Phase 1 claims re-tested

| Phase 1 claim | Phase 2 result |
|---|---|
| Compiled context gives equal accuracy, no token saving | Confirmed on the easy (base) fixture. On the hard fixture accuracy improved (100% vs 75%) and tokens per solved task fell 24%, while tokens per run stayed flat. |
| An explicit integrity policy removed fabrication (Sonnet 2/2 → 0/2) | Not replicated: fabrication was rare in every condition (2 of 20 runs). Inconclusive. |
| Compiler validation does not prevent fabrication | Confirmed: title-only fabricated evidence passes every validation level. The change manifest's "source cannot be located" item, added after seeing these two cases, flags them; it has not been tested on new data. |
