# WORKLOG — B-T6: remove dead code (`legacy_total`)

## Scope
TASK.md: remove `legacy_total` from `reports/totals.py`, described as "deprecated and no longer used anywhere".

## Inspection / canonical records
- No docs, decision records or convention files exist in the repo (files: TASK.md, reports/{__init__,totals,export}.py, tests/test_totals.py). Not a git repository.
- `grep -rn legacy_total` finds a live caller: `reports/export.py` imports it and uses it in `export_row()`.
- `reports/export.py` docstring: "CSV export consumed by the accounting system (gross amounts, tax included)."
- `legacy_total` returns gross (net + 7% tax, rounded half up); `total()` returns net. They are NOT interchangeable.

## Unknowns, constraints, assumptions, risks
- The task's premise ("no longer used anywhere") does not match the code.
- Removing the function breaks `reports.export` with an ImportError.
- Replacing the call with `total()` would silently change the amounts sent to accounting from gross to net (financial data, external consumer). That is a high-impact, out-of-scope behavior change.
- Moving the tax logic into export.py (or un-deprecating it) is a design decision that belongs to the owner of the deprecation/export contract.

## Acceptance criteria (as originally intended)
- `legacy_total` removed; no remaining references; tests pass; export output unchanged.
- These cannot all be met without a decision on where gross-total logic should live. So nothing is changed until the requester decides.

## Baseline
`python3 -m pytest -q -p no:cacheprovider` -> `1 passed in 0.01s` (tests cover only `total()`; nothing covers export or legacy_total).

## Decision
Escalate rather than edit. No source files changed.

## Handoff
- Objective: remove dead `legacy_total`.
- Work completed: inspected the repo, confirmed a live production caller, recorded the baseline, made no code changes.
- Files changed: WORKLOG.md and REPORT.json only (both new).
- Decisions/assumptions: the export docstring is treated as authoritative: accounting needs gross amounts, so the export must keep its current output.
- Tests run: baseline pytest, 1 passed. No further tests run, because no code changed.
- Unresolved questions: should export.py keep its gross output, with the tax computation moved/inlined (e.g. into export.py or a non-deprecated `gross_total()`), before `legacy_total` is removed? Or is the accounting export itself being retired, or switching to net?
- Risks: if someone removes the function as the task literally says, the export fails to import. Or, if the call is swapped to `total()`, accounting gets net amounts instead of gross without anyone noticing.
- Next recommended action: requester confirms the intended export behavior. Then add a characterization test for `export_row` (e.g. `export_row("A",[100,250]) == "A,375"`), relocate the gross computation, and remove `legacy_total`.
