<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I implemented `split_amount` in /root/exp/runs/C-T1/ledger/split.py following the rules in docs/CONVENTIONS.md, and all 11 tests pass.

- **Behaviour:** amounts are integer cents. The shares always add up to the total, and any leftover cents go to the earliest parties, so `split_amount(1000, 3) == [334, 333, 333]`. Refunds (negative totals) are split on the absolute value and then made negative.
- **Errors:** a party count of zero or less raises `ValueError`. Float or bool inputs raise `TypeError`.
- **Analysis:** I wrote it to /root/exp/runs/C-T1/ANALYSIS.md before changing any code. Posture is FAST, action is PROCEED, and I put 90% on the approach being right.
- **Tests:** I added cases to /root/exp/runs/C-T1/tests/test_split.py for uneven splits, refunds, invalid counts, a zero total, and a sweep checking that the shares always add up to the total.
- **Report:** /root/exp/runs/C-T1/REPORT.json records three assumptions and no open questions:
  - the 1000 in the example is cents;
  - a zero total returns a list of zeros rather than an error;
  - non-integer inputs are rejected with `TypeError`. The task didn't ask for this; I added it to enforce the no-float rule.
