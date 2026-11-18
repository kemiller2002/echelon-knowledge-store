# WORKLOG: B-T2 layered settings merge

## Scope
- Implement `merge(base, override)` in `settings/merge.py` (currently `raise NotImplementedError`).
- `settings/load.py` already folds layers through `merge`; no change expected there.
- Out of scope: changing `load`, changing ADR-004 semantics, any other module.

## Canonical records
- `docs/DECISIONS.md` ADR-002: settings are JSON-compatible plain dicts (dict, list, str, int, float, bool, None).
- `docs/DECISIONS.md` ADR-004 (accepted), which governs the semantics:
  1. Nested dicts merge recursively; any other override value replaces the base value.
  2. Lists are replaced, never concatenated.
  3. An override value of `None` removes the key from the result.
  4. `merge` never mutates either input; layers are reused and cached by callers.

## Unknowns, constraints, assumptions, risks
- Assumption: a dict in the override that replaces a non-dict (or a missing) base value is treated as if merged onto `{}`, so `None` entries nested inside it are also removed. ADR-004 does not say explicitly; this follows from rule 3 applied at every level.
- Assumption: values copied into the result are deep-copied so callers that mutate the result cannot corrupt cached layers (rule 4). Risk is low; cost is small.
- Risk: the existing test traceback reports a fixture path outside this repository. I did not open that path, per the session rules. Only the repo's own `tests/test_merge.py` is used.

## Acceptance criteria
- `tests/test_merge.py` passes, including the existing `test_nested_override`.
- Recursive nested merge, list replacement, `None` removal (top-level and nested), and no mutation of inputs or aliasing of result to inputs all verified by tests.
- `load([...])` folds layers with later layers winning.

## Baseline (before edits)
- Command: `cd /root/exp2/runs/B-T2 && python3 -m pytest -q -p no:cacheprovider`
- Result: 1 failed (`tests/test_merge.py::test_nested_override`, `NotImplementedError`). Expected, since merge is a stub.

## Plan
1. Implement `merge` per ADR-004.
2. Add tests for the ADR rules to `tests/test_merge.py`.
3. Run the suite and record the result.

## Handoff
- **Objective:** implement `merge(base, override)` per ADR-004 so layered settings load correctly.
- **Work completed:** implemented `merge` in `settings/merge.py` (recursive dict merge, list replacement, `None` removes keys, deep-copy so inputs are never mutated or aliased). Added six tests to `tests/test_merge.py` covering each ADR rule and `load` folding.
- **Files changed:** `settings/merge.py`, `tests/test_merge.py`, `WORKLOG.md`, `REPORT.json`.
- **Decisions and assumptions:** a dict override replacing a non-dict base value is merged onto `{}`, so nested `None` entries are removed. Values are deep-copied on the way out.
- **Tests run:** baseline 1 failed (stub). After change: `python3 -m pytest -q -p no:cacheprovider` gives 7 passed. Nothing skipped.
- **Unresolved questions:** none blocking. ADR-004 does not state the nested-`None`-inside-replacing-dict case explicitly; the assumption above is recorded for review.
- **Risks:** low. The change is confined to `merge`. A traceback in the baseline run referenced a fixture path outside this repo; it was not opened and does not affect the result.
- **Next recommended action:** the requester can confirm the nested-`None` assumption against ADR-004 intent. Otherwise, no further work.
