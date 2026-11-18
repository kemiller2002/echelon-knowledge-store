"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
import copy
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Return a new dict with ``override`` layered on top of ``base``.

    Semantics (ADR-004):
    - nested dicts merge recursively;
    - any other override value (including lists) replaces the base value;
    - an override value of ``None`` removes the key from the result;
    - neither input is mutated, and the result shares no mutable objects with them.
    """
    result: dict[str, Any] = {key: copy.deepcopy(value) for key, value in base.items()}
    for key, value in override.items():
        if value is None:
            result.pop(key, None)
        elif isinstance(value, dict):
            existing = result.get(key)
            # Merge into {} when there is no dict to merge with, so `None`
            # markers inside the override are applied (dropped) consistently.
            result[key] = merge(existing if isinstance(existing, dict) else {}, value)
        else:
            result[key] = copy.deepcopy(value)
    return result
