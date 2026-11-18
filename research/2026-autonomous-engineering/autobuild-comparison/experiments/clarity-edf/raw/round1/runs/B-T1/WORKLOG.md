# WORKLOG — B-T1: split a bill between parties

## Scope
Implement `split_amount(total, parties)` in `ledger/split.py` (TASK.md). Add tests. No other behaviour changes.

## Canonical records inspected
- `docs/CONVENTIONS.md` (referenced from `ledger/split.py` docstring): integer cents; conservation;
  leftover cents to earliest entries; refunds split on absolute value then negated; invalid counts raise ValueError.
- `ledger/money.py`: all amounts are integer cents, never float.
- `tests/test_split.py`: one existing test (even split).

## Unknowns / assumptions / risks
- TASK example `split_amount(1000, 3)` does not divide evenly. Per CONVENTIONS rule 3 the result is
  `[334, 333, 333]` (amounts interpreted as cents per money.py / CONVENTIONS rule 1).
- Non-integer inputs (floats, bools) are not specified; I reject floats/non-ints with TypeError to honour
  "never use float" (low-risk, reversible). bool is an int subclass; rejected too for parties/total for safety.
- Risk: low; new function only, no callers yet.

## Acceptance criteria
- Returns exactly `parties` integer shares; sum equals `total` (conservation).
- Leftover cents go to earliest entries, one cent each.
- Negative totals: split abs value, negate each part (earliest carry larger refund).
- `parties <= 0` raises ValueError; never returns empty list.
- Existing test passes; new tests cover the above.

## Baseline
`python3 -m pytest -q -p no:cacheprovider` -> 1 failed (test_even_split: NotImplementedError).

## Handoff
- Objective: implement `split_amount` per TASK.md and docs/CONVENTIONS.md.
- Work completed: implemented integer-cent even split with conservation, earliest-first leftover cents,
  refund handling (abs-then-negate), ValueError for parties <= 0, TypeError for non-int/bool inputs; added tests.
- Files changed: `ledger/split.py`, `tests/test_split.py`, `WORKLOG.md` (new), `REPORT.json` (new).
- Decisions/assumptions: `split_amount(1000, 3)` -> `[334, 333, 333]` (cents, rule 3). Non-int inputs raise
  TypeError (not specified by conventions; reversible). total == 0 returns zeros (valid, non-empty).
- Tests run: `python3 -m pytest -q -p no:cacheprovider` -> 19 passed (baseline: 1 failed). No linters/type checkers
  are configured in the repo; none run.
- Unresolved questions: none blocking. Requester may confirm whether TypeError on floats/bools is desired.
- Risks: low; new function with no existing callers.
- Next recommended action: review and merge; consider adding the TypeError rule to CONVENTIONS.md if accepted.
