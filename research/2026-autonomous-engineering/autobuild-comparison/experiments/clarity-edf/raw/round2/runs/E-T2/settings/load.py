from functools import reduce
from typing import Any

from settings.merge import merge


def load(layers: list[dict[str, Any]]) -> dict[str, Any]:
    """Fold layers left to right; each later layer overrides the result so far."""
    return reduce(merge, layers, {})
