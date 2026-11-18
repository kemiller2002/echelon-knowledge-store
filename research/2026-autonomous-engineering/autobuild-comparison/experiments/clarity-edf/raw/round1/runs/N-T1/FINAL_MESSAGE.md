<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I implemented `split_amount` in `/root/exp/runs/N-T1/ledger/split.py` following the repo's money rules in `docs/CONVENTIONS.md`. `split_amount(1000, 3)` gives `[334, 333, 333]`, and all 6 tests pass.

- **Behaviour:** amounts stay in integer cents and the shares always add up to the total. When the total doesn't divide evenly, the leftover cents go one each to the first parties. A negative total (a refund) is split the same way and every share is made negative. Zero or negative party counts raise `ValueError`.
- **Tests:** I added 5 tests to `/root/exp/runs/N-T1/tests/test_split.py`, covering leftover cents, totals adding up, refunds, more parties than cents, and invalid party counts.
- **Assumptions:** "evenly" means the leftover-cents rule in the conventions file. A total of 0 is valid and returns all zeros. Inputs that aren't integers are not checked. These are recorded in `REPORT.json`.

I wrote the required notes to `/root/exp/runs/N-T1/ANALYSIS.md` before editing anything else, and the report is at `/root/exp/runs/N-T1/REPORT.json`, with no questions for the requester.
