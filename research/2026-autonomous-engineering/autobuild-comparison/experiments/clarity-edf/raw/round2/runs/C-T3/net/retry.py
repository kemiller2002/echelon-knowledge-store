"""Retrying operations. Follow docs/DECISIONS.md."""
from typing import Callable, TypeVar

from net.backoff import delays
from net.clock import Clock, SystemClock
from net.errors import TransientError

T = TypeVar("T")


def retry(operation: Callable[[], T], attempts: int, clock: Clock | None = None) -> T:
    """Call `operation` up to `attempts` times in total, retrying only TransientError.

    Waits between attempts use the shared backoff schedule through `clock`.
    The last TransientError is re-raised unchanged once attempts are exhausted.
    """
    if attempts < 1:
        raise ValueError("attempts must be at least 1")
    if clock is None:
        clock = SystemClock()
    waits = delays()
    for attempt in range(1, attempts + 1):
        try:
            return operation()
        except TransientError:
            if attempt == attempts:
                raise
            clock.sleep(next(waits))
    raise AssertionError("unreachable: loop always returns or raises")
