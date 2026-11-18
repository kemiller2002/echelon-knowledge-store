from dataclasses import dataclass, field
from typing import Protocol
import time


class Clock(Protocol):
    def sleep(self, seconds: float) -> None: ...


class SystemClock:
    def sleep(self, seconds: float) -> None:
        time.sleep(seconds)


@dataclass
class FakeClock:
    """Records requested sleeps instead of waiting. Used by tests."""
    slept: list[float] = field(default_factory=list)

    def sleep(self, seconds: float) -> None:
        self.slept.append(seconds)
