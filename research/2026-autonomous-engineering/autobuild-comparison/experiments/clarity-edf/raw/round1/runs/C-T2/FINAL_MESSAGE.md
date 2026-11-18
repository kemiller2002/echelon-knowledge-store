<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I implemented `merge` in /root/exp/runs/C-T2/settings/merge.py following ADR-004 and added tests for each of its rules; all 7 tests pass.

How it now behaves:
- **Nested settings** merge level by level.
- **Lists and other values** in a later layer replace the earlier value; lists are not combined.
- **A value of `None`** in a later layer removes that key.
- **Inputs** are never changed, and the result shares nothing with them.

The analysis is in /root/exp/runs/C-T2/ANALYSIS.md (posture FAST, action PROCEED, 90%) and the report is in /root/exp/runs/C-T2/REPORT.json.

Where the spec was silent, I decided as follows and recorded it in the report:
- `None` removes a key at any depth, and does nothing if the key isn't there.
- A `None` already in the earlier layer is kept as a value.
- A section in a later layer replaces a plain value in the earlier one, and any `None` values inside that section are dropped.

There are no questions for the requester.
