"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
import copy
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Return a new dict with override applied on top of base.

    Nested dicts merge recursively, lists and scalars are replaced, and an
    override value of None removes the key. Neither input is mutated.
    """
    result = {key: copy.deepcopy(value) for key, value in base.items()}
    for key, value in override.items():
        if value is None:
            result.pop(key, None)
        elif isinstance(value, dict):
            base_value = base.get(key)
            if not isinstance(base_value, dict):
                base_value = {}
            result[key] = merge(base_value, value)
        else:
            result[key] = copy.deepcopy(value)
    return result
