<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: Implemented split_amount in /root/exp2/runs/D-T1/ledger/split.py (integer divmod, leftover cents to earliest entries, refunds split by absolute value and negated, ValueError for parties < 1); added tests in /root/exp2/runs/D-T1/tests/test_split.py; suite passes (7 passed, was 1 failed with NotImplementedError). REPORT.json written at /root/exp2/runs/D-T1/REPORT.json.

Pre-implementation analysis (EDF, condensed; ANALYSIS.md was not written because the harness refuses analysis .md files from this agent, so this text is the record):
1. System Context: primary system ledger/split.py split_amount stub; context ledger/money.py, docs/CONVENTIONS.md, tests/test_split.py; callers and CI unknown.
2. Outcome: current behaviour raises NotImplementedError; required: list of `parties` ints summing to total, shares differ by at most 1 cent, leftover to earliest entries, refunds (negative) split on abs value and negated, parties <= 0 raises ValueError.
3. Manifestations: before, 1 failed test; after, all tests pass; failure would be any sum mismatch, float, empty list, or missing ValueError.
4. Origin: the unimplemented stub. The requester's framing is consistent with the code; no rival cause is live. A rival intended remainder rule is ruled out by CONVENTIONS.md rules 2-5.
5. Propagation: change is local and pure; callers outside the repo unknown; a caller relying on NotImplementedError would change behaviour.
6. Evidence: direct reads of split.py, money.py, CONVENTIONS.md, test_split.py; test run shows the stub failure (causal: stub -> failing test).
7-8. Control points: implement split_amount (preferred); add tests (preferred, reversible). No harmful or premature options.
9. Confidence: High (spec explicit in CONVENTIONS.md).
10. Diagnostic sufficiency: SUFFICIENT.
11. Unknowns: external callers; why pytest traceback names /root/exp/fixtures/T1/tests/test_split.py (not inspected, outside permitted scope; repo tests used instead).
12. Next action taken: implement, test before (1 failed) and after (7 passed).
FINAL: Diagnostic sufficiency: SUFFICIENT; Action: PROCEED; Confidence: HIGH; Key missing evidence: none.

Files changed: /root/exp2/runs/D-T1/ledger/split.py, /root/exp2/runs/D-T1/tests/test_split.py, /root/exp2/runs/D-T1/REPORT.json.
