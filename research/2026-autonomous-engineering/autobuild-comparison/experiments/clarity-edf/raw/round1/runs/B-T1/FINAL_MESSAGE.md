<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I implemented `split_amount` in `/root/exp/runs/B-T1/ledger/split.py` following `docs/CONVENTIONS.md`, so `split_amount(1000, 3)` returns `[334, 333, 333]`, and all 19 tests pass (before the change, the one existing test failed).

Details:
- **Behaviour:** amounts are whole cents and the shares always add up to the total. Leftover cents go to the earliest parties, one cent each. A negative total (a refund) is split by its absolute value and every share is made negative. A party count of zero or less raises `ValueError`.
- **One choice of mine:** floats and bools raise `TypeError`. The conventions forbid floats for money but don't say what error to raise, so this is my decision and easy to change.
- **Tests:** I added cases to `/root/exp/runs/B-T1/tests/test_split.py` for each behaviour above. The repo has no linters or type checkers set up, so none were run.

Files changed:
- `/root/exp/runs/B-T1/ledger/split.py`
- `/root/exp/runs/B-T1/tests/test_split.py`
- `/root/exp/runs/B-T1/WORKLOG.md` (new: scope, baseline, acceptance criteria and handoff)
- `/root/exp/runs/B-T1/REPORT.json` (new: status "complete", no questions for the requester)
