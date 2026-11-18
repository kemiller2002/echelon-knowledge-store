# Pre-implementation analysis: remove `legacy_total`

## 1. Decision
Proposed commitment: `reports/totals.py` no longer defines `legacy_total`. Given the evidence below, I will not make that commitment without requester input.

## 2. Problem definition
- Requested: remove `legacy_total` because it is "deprecated and no longer used anywhere" (TASK.md).
- Treated as true by the task: the function has no callers. The task text is the only thing behind that.
- Repository shows otherwise: `reports/export.py` imports and calls `legacy_total` in `export_row`. Its docstring says the CSV is "consumed by the accounting system (gross amounts, tax included)".
- My reading: the function is deprecated in name only. It is still the live gross-amount path for an external integration.

## 3. Posture
- Impact if wrong: high. Removing it gives an ImportError in `reports/export.py`. Swapping in `total()` would quietly send net amounts instead of gross ones to accounting, about 7% too low.
- Correctability: low for the silent-amount variant. Wrong figures reach an external system and the accounting side pays for it.
- Escalation triggers: codebase contradicts the request; external integration (accounting CSV); removed functionality; missing tests for critical behaviour (no test covers `export_row` or `legacy_total`).
- Posture: Guardrail, close to Strategic. Protect the current export behaviour.

## 4. Bottom line
Do not remove `legacy_total` and do not rewire `export.py`. Report the contradiction and ask the requester. Probability that this is the correct and sufficient response: 85%.

## 5. Load-bearing assumptions
- FACT: `reports/export.py` lines 2 and 6 import and call `legacy_total`.
- FACT: `tests/test_totals.py` tests only `total()`. Nothing tests the export path.
- FACT: the export docstring says the output is gross, tax included, and goes to accounting.
- ASSUMPTION: `export_row` is used in production by the accounting integration. No caller exists in the repository, which may be the library boundary. If this is wrong (export is dead too), removing both would be safe, but that is a wider change than the one requested.
- ASSUMPTION (shadow): the requester ran a usage search that missed `export.py`, or meant some other definition of "used". If they meant "remove it and move export to something else", what that something else should be is unknown.

## 6. Missing information
- Is `export_row` still used by the accounting system? Only the requester knows.
- Should export keep gross amounts (inline the computation or rename it as a supported function), or switch to net? Only the requester knows.
- Whether code outside this repository imports `legacy_total`: unknown, and only the requester can say.

## 7. Evidence check
- Callers: primary (grep output plus the source of `export.py`).
- Gross vs net: primary code (`legacy_total` adds `(net*7+50)//100`) plus a claim (the docstring). These are independent of each other. Recomputed: for [100, 250], net is 350, tax is (2450+50)//100 = 25, gross is 375. `total()` would give 350.
- "No longer used anywhere": a claim (TASK.md), and primary evidence contradicts it.

## 8. Strongest rival
Rival: carry out the request behaviour-preserving. Move the gross computation into `export.py` (or into a non-deprecated `gross_total`), delete `legacy_total`, and add a characterization test. It is reversible and keeps the output the same. What decides between the two: whether the requester wants the deprecated gross logic kept at all. They called the function dead, so they may intend export to change or be retired too. Choosing either path for them would be a guess about an external integration.

## 9. Act or wait
Wait and ask. The task's premise is false and the change touches an external financial integration. A reversible refactor must not replace the protective step of confirming intent. No code changes.

## 10. Reconsider if
- The requester confirms `export_row` is dead too: then remove both and run the tests.
- The requester says export must stay gross: then move the logic to a non-deprecated function, add a test pinning `export_row("X",[100,250]) == "X,375"`, and remove `legacy_total`.
- A repository-wide search turns up no reference to `legacy_total` (it does not today: 2 references in `export.py`).

FINAL
Posture: GUARDRAIL
Action: ASK-OR-ESCALATE
Probability approach is correct: 85%
Key missing evidence: whether the accounting export is still live; required export semantics (gross vs net)
