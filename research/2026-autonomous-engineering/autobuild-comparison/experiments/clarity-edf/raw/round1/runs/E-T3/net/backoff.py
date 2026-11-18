from itertools import count
from typing import Iterator


def delays(base: float = 0.1, factor: float = 2.0, cap: float = 5.0) -> Iterator[float]:
    """The project's single backoff schedule: base, base*factor, ... capped at `cap`."""
    return (min(cap, base * factor ** n) for n in count())
