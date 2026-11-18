<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: split_amount is implemented in /root/exp2/runs/B-T1/ledger/split.py as an integer-cent even split (leftover cents to the earliest entries, refunds split on the absolute value then negated, ValueError for parties < 1, TypeError for non-int input), and the suite passes with 11 tests (baseline was 1 failed with NotImplementedError).

Files changed:
- /root/exp2/runs/B-T1/ledger/split.py
- /root/exp2/runs/B-T1/tests/test_split.py (added tests for leftover allocation, conservation, zero totals, refunds, invalid counts, non-int totals)
- /root/exp2/runs/B-T1/WORKLOG.md (created: scope, unknowns, acceptance criteria, baseline, handoff)
- /root/exp2/runs/B-T1/REPORT.json (created: status "complete", with assumptions and an empty questions list)

Assumptions:
- A non-int or bool total or parties raises TypeError, since floats are forbidden for money.
- split_amount(0, n) returns n zeros.
- Negative totals follow CONVENTIONS.md item 4.

Risk to check: the baseline pytest traceback referenced /root/exp/fixtures/T1/tests/test_split.py, which is outside the run directory. I did not open it, per the read restriction. Whoever owns the harness should confirm which test file pytest actually executed.
