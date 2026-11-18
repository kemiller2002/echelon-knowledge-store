# Pre-implementation analysis (protocol C)

## 1. Decision
`ledger.split.split_amount(total, parties)` will return `parties` integer-cent shares that sum exactly to `total`, following docs/CONVENTIONS.md (remainder to earliest, refunds by absolute value then negated, invalid counts raise ValueError).

## 2. Problem definition
- Requested: split a bill evenly between parties, one share per party (TASK.md); example `split_amount(1000, 3)` with no expected output given.
- Interpretation: "evenly" means as evenly as integer cents allow, conserving money. `1000/3` -> `[334, 333, 333]`. Justified by docs/CONVENTIONS.md rules 1-5 and ledger/money.py docstring (integer cents, never float).

## 3. Posture
Impact if wrong: low (small pure function, unreleased, one test). Correctability: high (single function, easily changed). Escalation triggers: payments-adjacent (money arithmetic), but conventions fully specify behaviour; no contradiction between task and codebase. Posture: Fast.

## 4. Bottom line
Implement with `divmod(abs(total), parties)`, give +1 cent to the first `remainder` entries, negate all parts if total < 0, raise ValueError for parties <= 0; add tests for each convention. Probability correct and sufficient: 90%.

## 5. Load-bearing assumptions
- FACT: amounts are integer cents; no floats (docs/CONVENTIONS.md #1, ledger/money.py).
- FACT: parts must sum to total; leftover cents go to earliest entries (CONVENTIONS.md #2, #3).
- FACT: negative totals handled via absolute value then negation (CONVENTIONS.md #4).
- FACT: parties <= 0 raises ValueError (CONVENTIONS.md #5).
- ASSUMPTION: `total` in the example (1000) is cents, not dollars. If wrong, caller would need `cents()`; function behaviour itself is unchanged since the module works only in cents.
- ASSUMPTION: non-int inputs (floats, bool) need not be rejected specially. If wrong, a float total could slip through; I will raise TypeError for non-int to honour "never use float" cheaply.
- ASSUMPTION: total == 0 returns a list of zeros (not an error). If wrong, a small change.

## 6. Missing information
- Whether type-checking of inputs is desired: only from requester; low impact.
- Expected output of the 1000/3 example: not stated; derived from conventions.

## 7. Evidence check
- Conventions doc: claim (doc), but consistent with money.py docstring (separate file) and existing test (primary). Independent-ish sources, same author likely.
- Recompute: 1000 = 3*333 + 1 -> [334, 333, 333], sum 1000. -1000 -> [-334, -333, -333], sum -1000. 900/3 -> [300,300,300] matches existing test.

## 8. Strongest rival
Floor division per party (`[333,333,333]`) or float shares (`333.33`). Discriminating evidence: CONVENTIONS.md #1 and #2 forbid floats and require conservation, so rival is rejected.

## 9. Act or wait
Act. Conventions specify behaviour; change is small and reversible.

## 10. Reconsider if
- Existing test `test_even_split` fails after implementation.
- Any part list fails to sum to total in property-style tests over totals -1000..1000 and parties 1..7.
- A file in the repo is found that contradicts CONVENTIONS.md (e.g. a caller expecting floats).

FINAL
Posture: FAST
Action: PROCEED
Probability approach is correct: 90%
Key missing evidence: whether input type validation is wanted
