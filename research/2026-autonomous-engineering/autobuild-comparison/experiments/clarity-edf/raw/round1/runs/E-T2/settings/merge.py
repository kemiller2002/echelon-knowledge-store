"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
from copy import deepcopy
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Return a new dict with ``override`` layered on top of ``base``.

    Rules (ADR-004):
    - Nested dictionaries merge recursively; any other override value replaces the base value.
    - Lists are replaced, never concatenated.
    - An override value of ``None`` removes the key from the result.
    - Neither input is mutated, and the result shares no mutable objects with them.
    """
    result: dict[str, Any] = {key: deepcopy(value) for key, value in base.items()}
    for key, value in override.items():
        if value is None:
            result.pop(key, None)
        elif isinstance(value, dict):
            current = base.get(key)
            result[key] = merge(current if isinstance(current, dict) else {}, value)
        else:
            result[key] = deepcopy(value)
    return result
