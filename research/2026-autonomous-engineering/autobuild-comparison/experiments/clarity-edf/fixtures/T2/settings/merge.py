"""Deep merge for layered settings. See docs/DECISIONS.md (ADR-004)."""
from typing import Any


def merge(base: dict[str, Any], override: dict[str, Any]) -> dict[str, Any]:
    raise NotImplementedError
