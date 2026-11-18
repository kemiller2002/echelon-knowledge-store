# Pre-implementation analysis: layered settings merge

1. Decision. `merge(base, override)` in `settings/merge.py` will return a new dict that deep-merges the override into the base per ADR-004, and will not mutate either input.

2. Problem definition. Requested behaviour (TASK.md): later layers override earlier ones, including inside nested sections; `settings/load.py` folds layers through `merge`. Interpretation (docs/DECISIONS.md, ADR-004, accepted): nested dicts merge recursively; any other override value replaces the base value; lists are replaced, not concatenated; an override value of `None` removes the key; inputs are never mutated. The existing test in `tests/test_merge.py` checks only a one-level nested override.

3. Posture. Impact if wrong: low (one pure function, used only by `settings/load.py`). Correctability: high (single-file change, easy to revert). No escalation trigger applies (no auth, payments, security, privacy, production data, schema, public API, CI, removed functionality, or contradiction with the codebase). Posture: FAST.

4. Bottom line. Implement ADR-004 literally: recursive dict merge, value replacement otherwise, `None` deletes the key, deep copies so inputs are untouched. Add tests for the ADR rules. Probability correct and sufficient: about 90%.

5. Load-bearing assumptions.
- FACT: `load.py` folds with `reduce(merge, layers, {})` and needs `merge` to return a dict (`settings/load.py`).
- FACT: ADR-004 defines the semantics above (`docs/DECISIONS.md`).
- ASSUMPTION: a `None` in a nested override that introduces a new dict should be stripped of its `None` entries too (treat the new subtree as merged into `{}`). Consequence if wrong: a stray `None` would survive in the output; easy to change.
- ASSUMPTION: lists are copied as-is, and `None` entries inside lists are kept. Consequence if wrong: only list contents differ; ADR says lists are replaced wholesale, so this follows the ADR.
- ASSUMPTION: the tests run with the repo root on the import path (`settings.merge` is importable). Consequence if wrong: tests fail on import; visible immediately.

6. Missing information. Whether any caller relies on `None` being kept inside a newly introduced nested dict. Obtainable now by reading the repo: only `settings/load.py` and the test file use `merge`, so no. Nothing else is missing.

7. Evidence check. The ADR and the task text are claims about intended behaviour; the existing test and `load.py` are primary code. Only one caller exists, so no numbers need recomputing. The ADR is the only source for the `None` and mutation rules, and it is one document, not independently confirmed; this is acceptable because the rules are explicit.

8. Strongest rival. Keep `None` values as-is in the output and treat unset as a separate marker. Discriminating evidence: ADR-004 explicitly says `None` removes the key, so the rival contradicts the accepted decision. Rejected.

9. Act or wait. No feasible check would change the approach, so act. Smallest reversible change: implement the function, add tests, run pytest.

10. Reconsider if.
- The pytest run reports a failure in a case covered by ADR-004 (any failing assertion on a documented rule).
- A test reveals that an input dict changed after `merge` is called (identity or equality against a saved copy differs).
- `settings/load.py` raises a `TypeError` or returns a non-dict for any layer list in the tests.
- The new tests require a change to `load.py` or to ADR-004 to pass.

FINAL
Posture: FAST
Action: PROCEED
Probability approach is correct: 90%
Key missing evidence: none
