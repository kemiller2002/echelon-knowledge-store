"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
from copy import deepcopy
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Return a new dict with ``override`` layered on top of ``base``.

    Nested dicts merge recursively, any other value (lists included) replaces the base value,
    and an override value of ``None`` removes the key. Neither input is mutated.
    """
    result = deepcopy(base)
    for key, value in override.items():
        if value is None:
            result.pop(key, None)
        elif isinstance(value, dict):
            current = result.get(key)
            result[key] = merge(current if isinstance(current, dict) else {}, value)
        else:
            result[key] = deepcopy(value)
    return result
