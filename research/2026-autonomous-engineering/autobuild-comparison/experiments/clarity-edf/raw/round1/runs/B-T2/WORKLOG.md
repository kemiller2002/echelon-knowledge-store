# WORKLOG — B-T2: layered settings merge

## Scope
Implement `merge(base, override)` in `settings/merge.py` (called by `settings/load.py` via reduce).
Out of scope: changing `load.py`, ADRs, or settings types.

## Canonical records
- docs/DECISIONS.md ADR-002 (settings are JSON-compatible dicts) and ADR-004 (merge semantics, accepted):
  recursive dict merge; non-dict override replaces; lists replaced not concatenated;
  override `None` removes the key; never mutate inputs.

## Unknowns / assumptions / risks
- A1: `None` inside an override sub-dict whose base counterpart is missing or not a dict: the sub-dict is
  merged against `{}`, so `None` keys are dropped there too (consistent "None means unset" semantics).
- A2: Override dict replacing a non-dict base value (and vice versa) follows "any other value replaces".
- A3: Result is deep-copied from inputs (no shared mutable containers), so callers mutating the result
  cannot corrupt cached layers. Small cost, settings are small.
- A4: `None` as a list element or top-level value inside a list is kept verbatim (only dict values unset).
- Risk: low; pure function, no I/O.

## Acceptance criteria
- Existing test `test_nested_override` passes.
- Each ADR-004 rule covered by a test: recursive merge, scalar replace, list replace, None removes
  (incl. nested and absent keys), inputs not mutated, result not aliased to inputs; `load` folds layers.

## Baseline
`python3 -m pytest -q -p no:cacheprovider` -> 1 failed (test_nested_override: NotImplementedError).

## Handoff
- Objective: implement ADR-004 deep merge in `settings/merge.py`.
- Work completed: implemented `merge` (recursive dict merge, list/scalar replace, `None` unsets, deep-copied
  non-mutating result); added tests covering each ADR-004 rule and `load` layering.
- Files changed: settings/merge.py, tests/test_merge.py, WORKLOG.md (new), REPORT.json (new).
- Decisions/assumptions: A1-A4 above. ADR-004 not changed.
- Tests: `python3 -m pytest -q -p no:cacheprovider` -> 8 passed (baseline: 1 failed).
- Unresolved questions: none blocking; A1 (None dropped inside new sub-dicts) may warrant confirmation.
- Risks: low; deep copy adds small overhead per layer.
- Next action: review; optionally record A1/A3 as clarifications in ADR-004.
