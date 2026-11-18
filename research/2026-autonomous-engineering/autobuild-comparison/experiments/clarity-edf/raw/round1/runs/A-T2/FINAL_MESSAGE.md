<!-- Agent's final report, extracted verbatim from the run transcript (model output, not verified). -->

complete — I implemented `merge()` in `/root/exp/runs/A-T2/settings/merge.py` following ADR-004, added `/root/exp/runs/A-T2/tests/test_merge_semantics.py`, and all 7 tests pass; the full report is in `/root/exp/runs/A-T2/REPORT.json`.

How `merge()` behaves:
- **Nested dicts** merge recursively.
- **Lists and other values** in the override replace the base value outright; lists are never combined.
- **`None` in the override** removes that key from the result.
- **Inputs:** neither one is changed. The result is deep-copied, so later edits to it can't change the original layers.

The new tests cover each of those rules plus a three-layer `load()` call.

Assumptions I made where the docs were silent (also listed in `REPORT.json`):
- A `None` override for a key the base doesn't have does nothing.
- If an override dict lands where the base has no dict, it is merged into an empty dict. Any `None` entries inside it are dropped rather than stored as `None`.
- A non-dict override replaces a dict in the base entirely.
- A `None` already sitting in the base is kept; only a `None` in the override means "remove".
