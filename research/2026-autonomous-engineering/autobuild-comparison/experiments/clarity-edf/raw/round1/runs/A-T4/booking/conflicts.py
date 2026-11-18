from itertools import combinations

from booking.interval import Interval, intersects


def conflicts(bookings: dict[str, Interval]) -> list[tuple[str, str]]:
    """Every pair of booking ids whose times clash, in sorted order."""
    return sorted(
        (a, b) for (a, ia), (b, ib) in combinations(sorted(bookings.items()), 2) if intersects(ia, ib)
    )
