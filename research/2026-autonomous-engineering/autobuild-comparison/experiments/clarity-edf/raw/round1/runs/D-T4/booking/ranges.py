from functools import reduce

from booking.interval import Interval, adjacent, intersects


def overlaps(a: Interval, b: Interval) -> bool:
    """Two bookings conflict when they share time."""
    return intersects(a, b)


def merge_ranges(ranges: list[Interval]) -> list[Interval]:
    """Coalesce ranges that share time or touch end-to-start into maximal ranges."""
    def step(acc: list[Interval], r: Interval) -> list[Interval]:
        if acc and (intersects(acc[-1], r) or adjacent(acc[-1], r)):
            last = acc[-1]
            return acc[:-1] + [Interval(last.start, max(last.end, r.end))]
        return acc + [r]
    return reduce(step, sorted(ranges), [])
