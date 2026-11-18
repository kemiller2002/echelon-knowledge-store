# Pre-implementation notes

## 1. Task restatement
Implement `split_amount(total, parties)` in `ledger/split.py` to divide an integer-cent amount evenly into one share per party, following docs/CONVENTIONS.md (conserve money, leftover cents to earliest entries, refunds by absolute value then negated, invalid counts raise ValueError).

## 2. Files read
- TASK.md: task description.
- ledger/__init__.py: empty package marker.
- ledger/split.py: stub `split_amount` raising NotImplementedError.
- ledger/money.py: cents helpers; states amounts are int cents, never float.
- tests/test_split.py: one test, even split of 900 into 3.
- docs/CONVENTIONS.md: money-division rules 1-5.

## 3. Code layout
Package `ledger/` (money.py, split.py), tests in `tests/`, docs in `docs/`.

## 4. Entry points
`ledger.split.split_amount`.

## 5. Coding style
snake_case, full type hints with builtin generics (`list[int]`), short functions, one-line docstrings, no imports beyond need.

## 6. Plan
1. Validate `parties` > 0 else raise ValueError.
2. Split abs(total) with divmod; give one extra cent to the first `remainder` parties.
3. Negate every part if total is negative.
4. Add tests for remainder, refunds, zero total, and invalid counts.

## 7. Test plan
Run `cd /root/exp/runs/N-T1 && python3 -m pytest -q -p no:cacheprovider` after implementing and after adding tests.

## 8. Edits
- ledger/split.py
- tests/test_split.py
- REPORT.json (new)

## 9. Formatting
4-space indent, double quotes, two blank lines between top-level defs, lines under ~100 chars.

## 10. Done check
All tests pass, including 1000/3 -> [334, 333, 333], -1000/3 -> [-334, -333, -333], ValueError for 0/negative parties; parts always sum to total.

FINAL
Plan steps: 4
Files to edit: 3
