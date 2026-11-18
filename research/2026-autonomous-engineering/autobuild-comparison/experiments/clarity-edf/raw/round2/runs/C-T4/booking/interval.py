"""Intervals are half-open: [start, end). See docs/MODEL.md."""
from typing import NamedTuple


class Interval(NamedTuple):
    start: int
    end: int


def intersects(a: Interval, b: Interval) -> bool:
    """True when the two half-open intervals share at least one instant.

    Back-to-back intervals (one ends exactly where the other starts) do not intersect.
    """
    return a.start < b.end and b.start < a.end


def adjacent(a: Interval, b: Interval) -> bool:
    """True when one interval ends exactly where the other starts."""
    return a.end == b.start or b.end == a.start
