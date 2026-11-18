from booking.conflicts import conflicts
from booking.interval import Interval


def test_back_to_back_bookings_are_not_conflicts():
    assert conflicts({"a": Interval(540, 600), "b": Interval(600, 660)}) == []


def test_overlapping_bookings_are_conflicts():
    assert conflicts({"a": Interval(540, 600), "b": Interval(570, 630)}) == [("a", "b")]
