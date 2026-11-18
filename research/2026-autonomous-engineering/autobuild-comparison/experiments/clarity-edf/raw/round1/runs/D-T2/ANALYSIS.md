# Pre-implementation analysis (EDF, protocol D v1)

## 1. System Context
- Primary System: `settings/merge.py::merge(base, override)`.
- Context: `settings/load.py::load` folds layers (defaults, environment, user) with `reduce(merge, layers, {})`; `tests/test_merge.py`; `docs/DECISIONS.md` ADR-002 (JSON-compatible values) and ADR-004 (accepted merge semantics). Callers beyond `load.py`: unknown. No git history, no CI config present.
- Focus: functional semantics of the merge (recursion, replacement, list handling, `None` deletion, non-mutation).

## 2. Outcome
- Current: `merge` raises `NotImplementedError`; `load` therefore fails for any non-empty layer list.
- Required: `merge` returns a new dict where nested dicts merge recursively, any other override value replaces the base value, lists are replaced (not concatenated), an override value of `None` removes the key, and neither input is mutated (ADR-004).

## 3. Manifestations
- Current: `pytest` -> 1 failed (`test_nested_override`, NotImplementedError) (observed).
- Failure signals after change: that test failing; list concatenation; `None` appearing in results; inputs changed after a call; shared nested objects between result and inputs (later mutation of result leaking into cached layers).

## 4. Origin Network
- Leading hypothesis: function is an unimplemented stub (direct observation). 
- Rivals: "no real problem" - ruled out (raises). "Intentional tradeoff" - no; ADR-004 is accepted and describes intended behaviour. "Cause outside component" - `load.py` is correct for a left fold; ruled out. "Test artifact" - test matches ADR-004; ruled out.
- Contributing human/organisational factor: the test suite covers only one ADR-004 rule; the other rules (lists, None, non-mutation) are specified only in prose, so a naive implementation could pass tests while violating the ADR.

## 5. Propagation
- `merge` -> `load` -> every consumer of loaded settings (unknown). Shared state: layers are reused and cached by callers (ADR-004), so any mutation or aliasing of nested objects would propagate into caches and later loads. Implementation must deep-copy values it carries into the result.
- Side effects outside scope: none expected; no other module changed.

## 6. Evidence
- Direct: read `merge.py` (stub), `load.py` (reduce fold), test file, ADR-004; ran pytest (1 failed).
- Causal link "stub -> failing test/load": causal (mechanism: `raise NotImplementedError`; rivals ruled out above).
- Inference: `None` deletion should apply at every nesting level and also strip `None` inside override sections that do not exist in base (consistent reading of "override value of None removes the key"). Not stated explicitly -> assumption.

## 7. Control Points
1. Implement `merge` per ADR-004 in `settings/merge.py` - acts at the origin; resolves outcome fully.
2. Add tests for list replacement, `None` deletion, non-mutation, type replacement - acts on verification; protects the ADR contract.
3. Change `load.py` or the existing test - not needed; would act outside the origin.

## 8. Control Point Ranking
1. CP1 - preferred (changes the established mechanism; high influence, low cost, reversible).
2. CP2 - preferred (cheap, strengthens checks, no harm under any hypothesis).
3. CP3 - harmful/unnecessary (weakening/altering a correct test or caller).

## 9. Confidence
High - specification is explicit (ADR-004) and the code is tiny and fully read.

## 10. Diagnostic Sufficiency
Sufficient: further investigation would not change the action.

## 11. Unknowns
- Other callers of `merge` (none in repo).
- Whether `None` inside a brand-new nested section should be stripped (assumed yes).
- Whether a `None` value in the base layer (e.g. defaults) should be kept (assumed kept; ADR only speaks of override values).

## 12. Next Responsible Action
Implement `merge` per ADR-004 with deep copies; add tests for each ADR-004 rule.
Verification: before = 1 failed; after = all tests pass, including new list/None/non-mutation tests. Recovery: revert `settings/merge.py` and the added tests (two files).

FINAL
Diagnostic sufficiency: SUFFICIENT
Action: PROCEED
Confidence: HIGH
Key missing evidence: none
