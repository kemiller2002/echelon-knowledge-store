"""Retrying operations. Follow docs/DECISIONS.md."""
from typing import Callable, TypeVar

T = TypeVar("T")


def retry(operation: Callable[[], T], attempts: int, clock=None) -> T:
    raise NotImplementedError
