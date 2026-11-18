"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
import copy
from typing import Any


def _strip_none(value: Any) -> Any:
    """Deep-copy an override value, dropping None-valued keys from nested dicts."""
    if isinstance(value, dict):
        return {k: _strip_none(v) for k, v in value.items() if v is not None}
    return copy.deepcopy(value)


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    """Return a new dict with ``override`` layered on top of ``base`` (ADR-004).

    - Nested dicts merge recursively; any other override value replaces the base value.
    - Lists are replaced, never concatenated.
    - An override value of ``None`` removes the key from the result.
    - Neither input is mutated, and the result shares no mutable objects with them.
    """
    result = copy.deepcopy(base)
    for key, value in override.items():
        if value is None:
            result.pop(key, None)
        elif isinstance(value, dict) and isinstance(result.get(key), dict):
            result[key] = merge(result[key], value)
        else:
            result[key] = _strip_none(value)
    return result
