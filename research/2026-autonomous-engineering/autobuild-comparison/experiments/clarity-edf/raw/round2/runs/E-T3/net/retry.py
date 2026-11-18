"""Retrying operations. Follow docs/DECISIONS.md."""
from typing import Callable, TypeVar

from net.backoff import delays
from net.clock import Clock, SystemClock
from net.errors import TransientError

T = TypeVar("T")


def retry(operation: Callable[[], T], attempts: int, clock: Clock | None = None) -> T:
    """Call `operation` up to `attempts` times in total (D-4).

    Only TransientError is retried (D-3); any other exception propagates immediately.
    Waits between attempts use net.backoff.delays() through the injected clock (D-1, D-2).
    After the last failed attempt the last TransientError is re-raised unchanged.
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
