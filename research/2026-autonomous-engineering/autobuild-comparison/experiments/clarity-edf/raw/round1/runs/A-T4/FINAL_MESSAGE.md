<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete — I fixed bug #57 at its source: `intersects()` in `booking/interval.py` now uses strict `<`, so back-to-back bookings no longer count as overlapping in `overlaps()` or `conflicts()`.

- **Why there and not in `overlaps()`:** `overlaps()` just calls `intersects()`. That function compared the intervals as if their end times were included, using `<=`. `docs/MODEL.md` says an interval excludes its end time, so 9:00–10:00 and 10:00–11:00 were wrongly flagged as a conflict.
- **Behaviour change:** `conflicts()`, which feeds the nightly report, had the same bug. The report will now stop listing back-to-back bookings.
- **Calendar view:** `merge_ranges()` still joins back-to-back bookings into one block, because it checks for touching ranges separately with `adjacent()`.
- **Tests:** I added `tests/test_conflicts.py` with two regression tests. All 5 tests now pass, including the one that reproduced the bug.

`/root/exp/runs/A-T4/REPORT.json` is written with status "complete" and these assumptions recorded.

Files changed:
- /root/exp/runs/A-T4/booking/interval.py
- /root/exp/runs/A-T4/tests/test_conflicts.py
