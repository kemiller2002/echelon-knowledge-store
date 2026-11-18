"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
import copy
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Return a new dict with ``override`` applied on top of ``base``.

    Rules (ADR-004):
    - nested dicts merge recursively; any other value replaces the base value;
    - lists are replaced, not concatenated;
    - an override value of ``None`` removes the key;
    - neither input is mutated, and the result shares no mutable state with them.
    """
    result: dict[str, Any] = {key: copy.deepcopy(value) for key, value in base.items()}
    for key, value in override.items():
        if value is None:
            result.pop(key, None)
        elif isinstance(value, dict):
            # Merge onto the existing dict, or onto {} when replacing a non-dict,
            # so that None entries nested in the override are still removed.
            existing = result.get(key)
            result[key] = merge(existing if isinstance(existing, dict) else {}, value)
        else:
            result[key] = copy.deepcopy(value)
    return result
