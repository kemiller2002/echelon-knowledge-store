# Pre-implementation notes

## 1. Task restatement
Implement `merge(base, override)` in `settings/merge.py` so layered settings combine with later layers overriding earlier ones, recursing into nested sections, following ADR-004.

## 2. Files read
- TASK.md: task description.
- settings/__init__.py: empty package marker.
- settings/merge.py: `merge` stub raising NotImplementedError; points to ADR-004.
- settings/load.py: `load` folds layers with `reduce(merge, layers, {})`.
- tests/test_merge.py: one test for nested override.
- docs/DECISIONS.md: ADR-002 (JSON-compatible dicts) and ADR-004 (merge semantics: recursive dicts, lists replaced, None deletes, no mutation).

## 3. Code layout
Package `settings/` (`merge.py`, `load.py`), tests in `tests/`, docs in `docs/`.

## 4. Entry points
`settings.merge.merge`; indirectly `settings.load.load`.

## 5. Coding style
snake_case, full type hints with builtin generics (`dict[str, Any]`), short functions, module docstring, `from typing import Any`.

## 6. Plan
1. Implement `merge`: copy base deeply, then apply each override key.
2. For override value None, remove the key.
3. For dict override over dict base, recurse; dict over non-dict, merge into `{}`.
4. Otherwise deep-copy the override value in.
5. Add tests for ADR-004 rules (lists, None, non-mutation, load).

## 7. Test plan
Run `python3 -m pytest -q -p no:cacheprovider` after implementation and after adding tests.

## 8. Edits
- settings/merge.py
- tests/test_merge.py
- REPORT.json (new)

## 9. Formatting
4-space indent, double quotes, two blank lines between top-level defs, lines under ~100 chars.

## 10. Done check
All tests pass, every ADR-004 rule covered by a test, REPORT.json written.

FINAL
Plan steps: 5
Files to edit: 3
