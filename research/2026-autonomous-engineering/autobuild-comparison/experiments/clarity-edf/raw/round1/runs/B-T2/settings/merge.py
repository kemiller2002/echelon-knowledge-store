"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
import copy
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Return a new dict with ``override`` layered on top of ``base`` (ADR-004).

    - Nested dicts merge recursively; any other override value replaces the base value.
    - Lists are replaced, never concatenated.
    - An override value of ``None`` removes the key from the result.
    - Neither input is mutated, and the result shares no mutable containers with them.
    """
    result: dict[str, Any] = {}
    for key, value in base.items():
        if key not in override:
            result[key] = copy.deepcopy(value)
    for key, value in override.items():
        if value is None:
            continue
        if isinstance(value, dict):
            prior = base.get(key)
            result[key] = merge(prior if isinstance(prior, dict) else {}, value)
        else:
            result[key] = copy.deepcopy(value)
    return result
