"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
import copy
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Return a new dict with ``override`` layered on top of ``base``.

    - Nested dicts merge recursively.
    - Any other override value (including lists) replaces the base value.
    - An override value of None removes the key from the result.
    - Neither input is mutated; the result shares no mutable state with them.
    """
    result: dict[str, Any] = {key: copy.deepcopy(value) for key, value in base.items()}
    for key, value in override.items():
        if value is None:
            result.pop(key, None)
        elif isinstance(value, dict):
            existing = result.get(key)
            # Merging into {} when the base has no dict here still drops
            # None-valued keys from the override, per ADR-004.
            result[key] = merge(existing if isinstance(existing, dict) else {}, value)
        else:
            result[key] = copy.deepcopy(value)
    return result
