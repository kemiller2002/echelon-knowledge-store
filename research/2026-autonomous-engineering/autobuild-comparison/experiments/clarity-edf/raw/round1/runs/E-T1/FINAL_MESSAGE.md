<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete: I implemented `split_amount` following the project's split rules and added tests for each rule; all 7 tests pass.

**What I changed**
- `/root/exp/runs/E-T1/ledger/split.py`: amounts stay in whole cents and the shares always add up to the total. Leftover cents go to the earliest parties, one cent each, so 1000 split 3 ways gives [334, 333, 333]. A negative amount (a refund) is split using its absolute value and then every share is made negative. A party count of zero or less raises `ValueError`. These rules come from `docs/CONVENTIONS.md`.
- `/root/exp/runs/E-T1/tests/test_split.py`: I kept the original test and added tests for leftover cents, refunds, totals smaller than the party count, the total always adding up (checked over many inputs), and invalid party counts.
- `/root/exp/runs/E-T1/REPORT.json`: written as required.

**Steps that did not go as instructed**
- **`ANALYSIS.md` was not written.** The harness refused the write because subagents may not create report `.md` files, and I did not try to get around that. I wrote the analysis before editing any file; it is below. If the file must exist, someone with permission needs to create it from this text.
- **My first Bash command in the run directory was denied.** I read the files with the Read and Glob tools instead, and the pytest command then ran normally in Bash.

**The analysis (contents meant for ANALYSIS.md)**
- **Current vs required behaviour:** the function was a stub that raised `NotImplementedError`. It must now follow `docs/CONVENTIONS.md` rules 1–5, which `split.py` itself points to.
- **Main judgement call:** the task says "split evenly" and uses 1000 split 3 ways, which doesn't divide exactly. The literal reading would give equal decimal shares or lose a cent. I ruled that out using the conventions (money is never a decimal and must always add up), the note in `money.py` that all amounts are whole cents, and the function's declared integer-list return type.
- **Posture:** GUARDRAIL, because this is money arithmetic, so I added a test for each rule.
- **Assumption:** inputs are integers as declared, so I added no checks for decimal or true/false arguments.
- **FINAL:**
  - Diagnostic sufficiency: SUFFICIENT
  - Posture: GUARDRAIL
  - Action: PROCEED
  - Probability approach is correct: 93%
  - Key missing evidence: none
