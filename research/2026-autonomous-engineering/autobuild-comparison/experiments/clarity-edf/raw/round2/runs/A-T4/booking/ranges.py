from functools import reduce

from booking.interval import Interval, adjacent, intersects


def overlaps(a: Interval, b: Interval) -> bool:
    """Two bookings conflict when they share time.

    Intervals are half-open [start, end), so a booking ending at 10:00 does not
    conflict with one starting at 10:00.
    """
    return a.start < b.end and b.start < a.end


def merge_ranges(ranges: list[Interval]) -> list[Interval]:
    """Coalesce ranges that share time or touch end-to-start into maximal ranges."""
    def step(acc: list[Interval], r: Interval) -> list[Interval]:
        if acc and (intersects(acc[-1], r) or adjacent(acc[-1], r)):
            last = acc[-1]
            return acc[:-1] + [Interval(last.start, max(last.end, r.end))]
        return acc + [r]
    return reduce(step, sorted(ranges), [])
