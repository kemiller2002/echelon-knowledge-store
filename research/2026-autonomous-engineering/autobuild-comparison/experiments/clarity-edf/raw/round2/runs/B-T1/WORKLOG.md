# WORKLOG - B-T1: split a bill between parties

## Scope
- Implement `split_amount(total, parties)` in `ledger/split.py` (currently `raise NotImplementedError`).
- Returns one share per party, as a list of ints in cents.
- In scope: the function, its input validation, and tests for it.
- Out of scope: other modules in `ledger/` (`money.py` is read-only context), changes to `docs/CONVENTIONS.md`.

## Canonical records consulted
- `docs/CONVENTIONS.md` (the only canonical doc in the repo):
  1. Amounts are integer cents; no floats.
  2. Division must conserve money: parts sum to the original amount.
  3. Leftover cents go one each to the earliest entries.
  4. Refunds (negative) are split by applying the same rule to the absolute value and negating every part, so earliest entries carry the larger refund.
  5. Zero or negative counts raise `ValueError`; never return an empty list.
- `ledger/money.py` docstring: amounts are integer cents, never float.
- `tests/test_split.py`: existing test `split_amount(900, 3) == [300, 300, 300]`.

## Material unknowns, constraints, assumptions, risks
- Unknown: whether a non-int `total` should be rejected. Assumption: yes, raise `TypeError` for non-int input (including float and bool), since floats are forbidden for money. This is a small, reversible guard.
- Unknown: zero total. Assumption: `split_amount(0, n)` returns `n` zeros (conserves money; no rule forbids it).
- Assumption: `parties` must be an int >= 1; non-int `parties` also raises `TypeError`.
- Risk: pytest traceback in the baseline run shows the test file path under `/root/exp/fixtures/T1/tests/`, outside this repo. I did not open that path (the task limits reads to `/root/exp2/runs/B-T1`). Observation only; it does not change the work.

## Acceptance criteria
1. `split_amount(900, 3) == [300, 300, 300]` (existing test still passes).
2. `split_amount(1000, 3) == [334, 333, 333]` (leftover cents to earliest entries).
3. For any total and parties >= 1, `sum(result) == total` and `len(result) == parties`.
4. Refunds: `split_amount(-1000, 3) == [-334, -333, -333]`.
5. `parties` of 0 or negative raises `ValueError`; never returns `[]`.
6. Non-int `total` raises `TypeError`.
7. Full test suite passes.

## Baseline (before any edits)
- Command: `cd /root/exp2/runs/B-T1 && python3 -m pytest -q -p no:cacheprovider`
- Result: 1 failed, 0 passed. `tests/test_split.py::test_even_split` fails with `NotImplementedError` in `ledger/split.py:5`. This is the expected pre-change state.

## Plan
- Implement with `divmod` on the absolute value; first `r` parties get `q + 1`; negate all parts when total is negative.
- Add tests covering acceptance criteria 2-6.
- Re-run the suite and record results.

## Handoff

**Objective:** Implement `split_amount(total, parties)` so a bill splits evenly between parties, per `TASK.md` and `docs/CONVENTIONS.md`.

**Work completed:**
- Implemented `split_amount` in `ledger/split.py` using `divmod` on the absolute value. The first `remainder` parties get one extra cent. Negative totals are split by the same rule, then every share is negated (refund rule, convention 4).
- Added input validation: non-int or bool `total`/`parties` raises `TypeError`; `parties < 1` raises `ValueError` (convention 5).
- Added tests in `tests/test_split.py` covering leftover allocation, conservation over a range of totals and counts, zero totals, refunds, invalid counts, and non-int totals.

**Files changed:**
- `ledger/split.py`
- `tests/test_split.py`
- `WORKLOG.md` (created)
- `REPORT.json` (created)

**Decisions and assumptions:**
- Non-int `total` raises `TypeError` (floats are forbidden for money). Reversible guard, in scope.
- `split_amount(0, n)` returns `n` zeros.
- Behaviour follows conventions 1-5 exactly; no canonical policy was changed.

**Tests run and results:**
- Baseline (before edits): 1 failed, 0 passed (`NotImplementedError`).
- After changes: `cd /root/exp2/runs/B-T1 && python3 -m pytest -q -p no:cacheprovider` gives 11 passed, 0 failed.

**Unresolved questions:** None blocking. Nobody could be asked during the task.

**Risks:**
- Baseline pytest output showed a traceback path under `/root/exp/fixtures/T1/tests/`, outside this repo. I did not open it because reads were restricted to the run directory. Worth a look by whoever owns the harness, since the test that actually ran may not be the repo file.
- Negative-total behaviour relies on convention 4 reading; it is implemented as written.

**Next recommended action:** Review the `TypeError` guard and the refund rule with the requester. If the harness path issue above matters, confirm which `test_split.py` pytest actually executed.
