"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
import copy
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Return a new dict with ``override`` layered over ``base``.

    Rules (ADR-004):
    - nested dicts merge recursively;
    - any other override value replaces the base value (lists included);
    - an override value of ``None`` removes the key from the result;
    - neither input is mutated.
    """
    result: dict[str, Any] = copy.deepcopy(base)
    for key, value in override.items():
        if value is None:
            result.pop(key, None)
        elif isinstance(value, dict):
            existing = result.get(key)
            if isinstance(existing, dict):
                result[key] = merge(existing, value)
            else:
                result[key] = merge({}, value)
        else:
            result[key] = copy.deepcopy(value)
    return result
