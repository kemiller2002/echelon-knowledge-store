<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: Fixed bug #57 by making booking.interval.intersects half-open (a.start < b.end and b.start < a.end) per docs/MODEL.md, which corrects both overlaps() and the nightly conflicts() report; the test suite went from 1 failed, 2 passed to 4 passed.

Files changed: /root/exp2/runs/B-T4/booking/interval.py, /root/exp2/runs/B-T4/tests/test_ranges.py (added test_conflicts_ignores_back_to_back_bookings), /root/exp2/runs/B-T4/WORKLOG.md, /root/exp2/runs/B-T4/REPORT.json.

Assumptions: fixed the root cause in intersects rather than only overlaps; merge_ranges behavior is unchanged; zero-length intervals now intersect nothing (not covered by tests or docs); conflicts() now also ignores back-to-back bookings, as the model implies.

Note: the baseline pytest traceback cited a path under /root/exp/fixtures/T4/tests/. I did not open it; I only worked on the repo's own tests/test_ranges.py.
