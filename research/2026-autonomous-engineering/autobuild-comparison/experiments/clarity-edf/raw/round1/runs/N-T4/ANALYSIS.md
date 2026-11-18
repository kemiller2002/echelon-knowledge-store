# Pre-implementation notes

## 1. Task restatement
Fix bug #57: back-to-back bookings (e.g. 9:00-10:00 and 10:00-11:00) are wrongly reported as overlapping; `overlaps` must return False for intervals that only touch.

## 2. Files read
- TASK.md: bug #57 description.
- booking/__init__.py: empty package marker.
- booking/interval.py: `Interval` NamedTuple plus `intersects` and `adjacent` predicates; docstring says half-open.
- booking/ranges.py: `overlaps` (delegates to `intersects`) and `merge_ranges`.
- booking/conflicts.py: `conflicts()` pairs clashing bookings using `intersects`.
- tests/test_ranges.py: three tests for `overlaps` and `merge_ranges`; the back-to-back one fails.
- docs/MODEL.md: intervals are half-open [start, end); nightly report uses `conflicts()`, calendar uses `merge_ranges()`.

## 3. Code layout
One package `booking` (interval primitives -> ranges, conflicts), a `tests` folder with pytest tests, and `docs`.

## 4. Entry points
`booking.ranges.overlaps`; root cause is `booking.interval.intersects` (uses `<=`, treating intervals as closed), which is also used by `booking.conflicts.conflicts` and `booking.ranges.merge_ranges`.

## 5. Coding style
snake_case, type hints on all functions, one-line docstrings, very short functions, absolute `from booking.x import y` imports.

## 6. Plan
1. Change `intersects` to strict comparisons (`a.start < b.end and b.start < a.end`) to match the half-open model.
2. Leave `overlaps` delegating to `intersects`.
3. Confirm `merge_ranges` still merges back-to-back ranges via `adjacent`.
4. Add a regression test that `conflicts()` does not report back-to-back bookings.

## 7. Test plan
Run `cd /root/exp/runs/N-T4 && python3 -m pytest -q -p no:cacheprovider` before the change (already done: 1 failure) and after the change.

## 8. Edits
- booking/interval.py
- tests/test_ranges.py
- REPORT.json (required output)

## 9. Formatting
4-space indent, double quotes, two blank lines between top-level definitions, lines under ~110 chars.

## 10. Done check
All tests pass, including the back-to-back test, the new conflicts test, and the merge test; REPORT.json written.

FINAL
Plan steps: 4
Files to edit: 2
