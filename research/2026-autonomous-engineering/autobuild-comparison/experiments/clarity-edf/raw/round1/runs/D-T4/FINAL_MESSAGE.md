<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete — Bug #57 is fixed. The real fault was not in `overlaps` itself but in the function it calls, `intersects` in /root/exp/runs/D-T4/booking/interval.py. That function used `<=`, so bookings that end exactly when the next one starts counted as overlapping. I changed it to `<`, and all 5 tests now pass, including the one that reproduced the bug.

- **Where the fix went:** `overlaps` just passes its arguments to `intersects` and returns the result. The docstring and docs/MODEL.md both say an interval does not include its end time, and `intersects` contradicted that. So I fixed `intersects` and left `overlaps` unchanged.
- **Side effect:** `conflicts()`, which the nightly report uses, also calls `intersects`. It had the same false conflicts and will now stop listing back-to-back bookings. I treated that as the correct behaviour, not a regression.
- **Calendar view:** `merge_ranges` still joins back-to-back bookings into one block, because it checks for that case separately with `adjacent()`.
- **Tests added:** two new tests in /root/exp/runs/D-T4/tests/test_ranges.py. One checks that `conflicts()` reports a real overlap but not a back-to-back pair. The other checks that overlapping ranges are merged.
- **Open point:** bookings with the same start and end time aren't covered by the docs, and I didn't add any handling for them.

Before editing, I wrote the required analysis to /root/exp/runs/D-T4/ANALYSIS.md. It ends with: SUFFICIENT / PROCEED / HIGH, with no key evidence missing. The report is at /root/exp/runs/D-T4/REPORT.json: status "complete", no questions for the requester.

Files changed (all in /root/exp/runs/D-T4):
- booking/interval.py
- tests/test_ranges.py
- ANALYSIS.md
- REPORT.json
