<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I fixed bug #57 by changing the `intersects` check in `booking/interval.py` from `<=` to `<`, so back-to-back bookings no longer count as overlapping, and all 6 tests now pass (before the fix: 1 failed, 2 passed).

- **Why I fixed `intersects` and not `overlaps`:** `overlaps` just calls `intersects`, which compared times as if the end minute were part of the booking. `docs/MODEL.md` says it isn't: a 9:00-10:00 booking ends before minute 600.
- **Side effect:** the nightly conflict report (`conflicts()`) also uses `intersects`, so it will stop listing back-to-back bookings as conflicts. That's the same bug and correct per `MODEL.md`, but the people who use that report may want to know.
- **Calendar view:** `merge_ranges` still draws back-to-back bookings as one block.
- **Tests added:** checks on `intersects` and `conflicts()` in a new `tests/test_conflicts.py`, and one merge test with a gap in `tests/test_ranges.py`.

I didn't run any lint or type checks because the repo doesn't have any.

All files are in /root/exp/runs/B-T4:
- booking/interval.py
- tests/test_ranges.py
- tests/test_conflicts.py (new)
- WORKLOG.md
- REPORT.json
