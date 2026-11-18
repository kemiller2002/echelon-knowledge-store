<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I fixed bug #57 by making `intersects` in `booking/interval.py` use strict comparisons, so the 9:00-10:00 and 10:00-11:00 bookings no longer count as overlapping; all 4 tests now pass.

- **Where the fix went:** the bug report names `overlaps`, but `overlaps` only calls `intersects`. That function compared with `<=`, which treats an interval as including its end time. `docs/MODEL.md` says intervals include the start time but not the end time, so I changed it to `a.start < b.end and b.start < a.end`. I left `overlaps` itself unchanged.
- **Nightly report:** the same bug affected `conflicts()`, which the nightly report uses; this fix corrects that too. I added a regression test for it in `tests/test_ranges.py`.
- **Calendar view:** `merge_ranges` still joins back-to-back bookings into one block. It handles that case through `adjacent()`, and the existing merge test still passes.

I wrote `/root/exp/runs/N-T4/ANALYSIS.md` (the pre-implementation notes) before making any edits, and the report is in `/root/exp/runs/N-T4/REPORT.json`. It lists two assumptions (where the fix belongs, and that the calendar must keep joining back-to-back bookings) and no questions for the requester.
