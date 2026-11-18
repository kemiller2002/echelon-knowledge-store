<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I fixed bug #57 by correcting the shared overlap check, `intersects` in booking/interval.py, rather than `overlaps` itself, and the full test suite now passes (6 of 6).

- **Cause:** `overlaps` just calls `intersects`. That check used `<=`, but the project docs say a booking includes its start minute and excludes its end minute. So a 9:00–10:00 booking and a 10:00–11:00 booking were counted as sharing time. The fix is `a.start < b.end and b.start < a.end`.
- **Wider effect:** `conflicts()`, which feeds the nightly report, uses the same check and had the same bug. This fix corrects it too. `merge_ranges` (the calendar's busy blocks) still draws back-to-back bookings as one block, because it has a separate check for bookings that touch.
- **Tests added** to /root/exp/runs/E-T4/tests/test_ranges.py: `conflicts()` skips back-to-back bookings but flags overlapping ones; `merge_ranges` merges overlapping bookings and keeps real gaps.

Main assumption: no code outside this repository relies on the old behaviour where touching bookings count as overlapping. There are no questions for the requester.

Files are in /root/exp/runs/E-T4:
- ANALYSIS.md (written before any code change)
- booking/interval.py
- tests/test_ranges.py
- REPORT.json
