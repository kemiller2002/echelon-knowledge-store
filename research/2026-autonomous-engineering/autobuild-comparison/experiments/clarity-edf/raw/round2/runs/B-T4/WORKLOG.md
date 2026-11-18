# WORKLOG - Bug #57: back-to-back bookings reported as conflict

## Scope
- Bug: booking 9:00-10:00 then 10:00-11:00 is reported as a conflict.
- TASK.md asks to fix `overlaps` in `booking/ranges.py`.
- Canonical policy: docs/MODEL.md says intervals are half-open `[start, end)`; a 540-600 booking does not contain minute 600.

## Unknowns, constraints, assumptions, risks
- `overlaps` delegates to `booking.interval.intersects`, which uses closed comparisons (`<=`). That contradicts the half-open model.
- `booking.conflicts.conflicts()` (nightly report) also uses `intersects`, so it has the same false positive. Fixing only `overlaps` would leave it wrong.
- `merge_ranges` uses `intersects OR adjacent`. Changing `intersects` keeps merging back-to-back ranges because `adjacent` still covers touching ranges.
- Assumption: zero-length intervals (start == end) contain no instant under half-open semantics, so they intersect nothing. Previously they intersected themselves. No test or doc covers this.
- Risk: behavior of `conflicts()` changes (intended by the model). Reversible, one-line.

## Acceptance criteria
- `tests/test_ranges.py::test_back_to_back_bookings_do_not_overlap` passes.
- Existing tests still pass.
- Genuine overlaps (including containment and partial overlap) still report as overlapping.
- `conflicts()` does not flag back-to-back bookings.

## Baseline
- Command: `cd /root/exp2/runs/B-T4 && python3 -m pytest -q -p no:cacheprovider`
- Result: 1 failed, 2 passed. Failing: `test_back_to_back_bookings_do_not_overlap`.
- Note: the pytest traceback cites a path under /root/exp/fixtures/T4/tests/. I did not open it. The repo's own tests/test_ranges.py is the file under test.

## Plan
- Fix root cause in `booking/interval.py::intersects` to use strict comparison (`a.start < b.end and b.start < a.end`), matching the half-open model.
- Add a regression test for `conflicts()` with back-to-back bookings.

## Handoff
- **Objective:** Fix bug #57 so back-to-back bookings (9:00-10:00, 10:00-11:00) are not reported as a conflict.
- **Work completed:** Changed `intersects` in `booking/interval.py` from closed (`<=`) to half-open (`<`) comparisons, matching docs/MODEL.md. This fixes `overlaps` (which delegates to it) and also `conflicts()` (nightly report), which had the same false positive. Added `test_conflicts_ignores_back_to_back_bookings` in `tests/test_ranges.py`.
- **Files changed:** `booking/interval.py`, `tests/test_ranges.py`, `WORKLOG.md`, `REPORT.json`.
- **Decisions and assumptions:**
  - Fixed the root cause in `intersects` rather than only in `overlaps`, because the model is half-open and `conflicts()` shares the primitive.
  - `merge_ranges` is unchanged in behavior: `adjacent()` still merges touching ranges, so the calendar view still draws back-to-back bookings as one block.
  - Zero-length intervals now intersect nothing (half-open semantics). Not covered by tests or docs.
- **Tests run and results:**
  - Baseline: 1 failed, 2 passed.
  - After change: `cd /root/exp2/runs/B-T4 && python3 -m pytest -q -p no:cacheprovider` gives 4 passed.
  - Not run: no separate lint or type checks exist in the repo.
- **Unresolved questions:** None blocking. Behavior change to `conflicts()` follows from the canonical model; flag if the nightly report was meant to keep closed-interval semantics.
- **Risks:** Low. The change is one comparison per operator and reversible. The zero-length edge case is the only semantic shift beyond the bug.
- **Next recommended action:** Merge after a review of the `conflicts()` behavior change, and consider adding a zero-length interval policy to docs/MODEL.md.
