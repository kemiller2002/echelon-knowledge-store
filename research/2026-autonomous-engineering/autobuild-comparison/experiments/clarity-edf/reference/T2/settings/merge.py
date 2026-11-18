import copy
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    def combine(key: str) -> Any:
        b, o = base.get(key), override.get(key)
        if key not in override:
            return copy.deepcopy(b)
        if isinstance(b, dict) and isinstance(o, dict):
            return merge(b, o)
        return copy.deepcopy(o)
    keys = list(base) + [k for k in override if k not in base]
    return {k: v for k in keys if not (k in override and override[k] is None) for v in [combine(k)]}
