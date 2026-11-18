from typing import Callable, TypeVar
from net.backoff import delays
from net.clock import SystemClock
from net.errors import TransientError

T = TypeVar("T")


def retry(operation: Callable[[], T], attempts: int, clock=None) -> T:
    if attempts < 1:
        raise ValueError("attempts must be >= 1")
    clock = clock or SystemClock()
    waits = delays()

    def attempt(n: int) -> T:
        try:
            return operation()
        except TransientError:
            if n >= attempts:
                raise
            clock.sleep(next(waits))
            return attempt(n + 1)
    return attempt(1)
