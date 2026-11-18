"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
import copy
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Return a new dict with `override` applied on top of `base`.

    Rules (ADR-004):
    - Nested dicts merge recursively.
    - Any other override value replaces the base value (lists are replaced, not concatenated).
    - An override value of None removes the key from the result.
    - Neither input is mutated; the result shares no mutable objects with the inputs.
    """
    result: dict[str, Any] = {key: copy.deepcopy(value) for key, value in base.items()}
    for key, value in override.items():
        if value is None:
            result.pop(key, None)
        elif isinstance(value, dict):
            existing = result.get(key)
            if isinstance(existing, dict):
                result[key] = merge(existing, value)
            else:
                # No dict to merge into: build it fresh so that None entries are still dropped.
                result[key] = merge({}, value)
        else:
            result[key] = copy.deepcopy(value)
    return result
