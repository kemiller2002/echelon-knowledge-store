"""Retrying operations. Follow docs/DECISIONS.md."""
from typing import Callable, TypeVar

from net.backoff import delays
from net.clock import Clock, SystemClock
from net.errors import TransientError

T = TypeVar("T")


def retry(operation: Callable[[], T], attempts: int, clock: Clock | None = None) -> T:
    """Call `operation` up to `attempts` times, backing off between transient failures.

    `attempts` counts the first call. Only TransientError is retried; any other
    exception propagates immediately. The last TransientError is re-raised unchanged.
    """
    if attempts < 1:
        raise ValueError(f"attempts must be >= 1, got {attempts}")
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
    raise AssertionError("unreachable")
