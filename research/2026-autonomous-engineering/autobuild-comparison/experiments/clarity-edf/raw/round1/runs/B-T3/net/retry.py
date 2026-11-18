"""Retrying operations. Follow docs/DECISIONS.md."""
from typing import Callable, Optional, TypeVar

from net.backoff import delays
from net.clock import Clock, SystemClock
from net.errors import TransientError

T = TypeVar("T")


def retry(operation: Callable[[], T], attempts: int, clock: Optional[Clock] = None) -> T:
    """Call `operation` up to `attempts` times in total, retrying only `TransientError`.

    Waits between attempts follow `net.backoff.delays()` via the injected clock (D-1, D-2).
    Any other exception propagates immediately (D-3). After the final failed attempt the
    last `TransientError` is re-raised unchanged (D-4).
    """
    if attempts < 1:
        raise ValueError("attempts must be >= 1")
    if clock is None:
        clock = SystemClock()
    schedule = delays()
    for attempt in range(1, attempts + 1):
        try:
            return operation()
        except TransientError:
            if attempt == attempts:
                raise
            clock.sleep(next(schedule))
    raise AssertionError("unreachable")
