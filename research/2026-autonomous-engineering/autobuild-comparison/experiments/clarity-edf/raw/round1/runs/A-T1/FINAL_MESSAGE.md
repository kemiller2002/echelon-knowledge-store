<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I implemented `split_amount` in `/root/exp/runs/A-T1/ledger/split.py` following `docs/CONVENTIONS.md`, added tests in `/root/exp/runs/A-T1/tests/test_split.py`, and all 8 tests pass.

- **Splitting:** amounts are whole cents and the shares always add up to the total. Leftover cents go to the earliest parties, one each, so `split_amount(1000, 3)` gives `[334, 333, 333]`.
- **Refunds:** a negative total is split by its absolute value and every share is negated, so `-1000` across 3 gives `[-334, -333, -333]`.
- **Bad input:** a party count of zero or less raises `ValueError`.
- **My additions:** beyond what was asked, floats and booleans raise `TypeError`, since the conventions say money is never a float. A zero total returns one zero per party.

The new tests cover uneven splits, more parties than cents, a zero total, refunds, bad party counts and float rejection. I wrote `/root/exp/runs/A-T1/REPORT.json` with these assumptions recorded and no questions for the requester.
