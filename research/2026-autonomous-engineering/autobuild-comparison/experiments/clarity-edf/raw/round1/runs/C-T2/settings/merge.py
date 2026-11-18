"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
import copy
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Return a new dict with ``override`` layered on top of ``base``.

    ADR-004 semantics:
    - nested dicts merge recursively; any other override value replaces the base value;
    - lists are replaced, never concatenated;
    - an override value of ``None`` removes the key from the result;
    - neither input is mutated, and the result shares no mutable objects with them.
    """
    result = copy.deepcopy(base)
    for key, value in override.items():
        if value is None:
            result.pop(key, None)
        elif isinstance(value, dict) and isinstance(result.get(key), dict):
            result[key] = merge(result[key], value)
        elif isinstance(value, dict):
            # No dict to merge into: still apply the rules (drop None values) inside it.
            result[key] = merge({}, value)
        else:
            result[key] = copy.deepcopy(value)
    return result
