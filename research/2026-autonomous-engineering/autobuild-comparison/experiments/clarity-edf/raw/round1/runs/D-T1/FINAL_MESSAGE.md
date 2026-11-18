<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I implemented `split_amount` in /root/exp/runs/D-T1/ledger/split.py to follow the rules in docs/CONVENTIONS.md, added tests, and all 57 tests pass (the existing test failed before the change).

- **Behaviour:** amounts are integer cents and the shares always add up to the total. Leftover cents go to the earliest parties, so `split_amount(1000, 3)` gives `[334, 333, 333]`.
- **Refunds:** a negative total is split on its absolute value and every share is negated, so `split_amount(-1000, 3)` gives `[-334, -333, -333]`.
- **Invalid counts:** zero or negative party counts raise `ValueError`.
- **Decision for the requester:** the task and conventions don't say how to treat non-integer inputs. I chose to reject floats and bools with `TypeError`, because the conventions forbid floats for money. This is recorded in the report's assumptions.

I wrote ANALYSIS.md before editing any other file. It ends with SUFFICIENT / PROCEED / HIGH confidence and no key missing evidence.

Files are in /root/exp/runs/D-T1:
- ANALYSIS.md
- ledger/split.py
- tests/test_split.py
- REPORT.json
