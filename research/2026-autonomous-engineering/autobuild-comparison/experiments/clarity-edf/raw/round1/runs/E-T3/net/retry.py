"""Retrying operations. Follow docs/DECISIONS.md."""
from typing import Callable, Optional, TypeVar

from net.backoff import delays
from net.clock import Clock, SystemClock
from net.errors import TransientError

T = TypeVar("T")


def retry(operation: Callable[[], T], attempts: int, clock: Optional[Clock] = None) -> T:
    """Call `operation` up to `attempts` times in total, returning its first result.

    Only `TransientError` is retried (D-3); any other exception propagates immediately.
    Waits between attempts follow `net.backoff.delays()` (D-2) via the injected clock (D-1).
    After the final failed attempt the last `TransientError` is re-raised unchanged (D-4).
    """
    if attempts < 1:
        raise ValueError(f"attempts must be >= 1, got {attempts!r}")
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
    raise AssertionError("unreachable")  # pragma: no cover
