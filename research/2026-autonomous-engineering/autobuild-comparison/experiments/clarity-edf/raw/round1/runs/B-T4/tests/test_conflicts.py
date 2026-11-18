from booking.conflicts import conflicts
from booking.interval import Interval, intersects


def test_intersects_is_half_open():
    assert not intersects(Interval(540, 600), Interval(600, 660))
    assert not intersects(Interval(600, 660), Interval(540, 600))
    assert intersects(Interval(540, 601), Interval(600, 660))
    assert intersects(Interval(540, 660), Interval(570, 600))


def test_conflicts_ignores_back_to_back_but_reports_clashes():
    bookings = {
        "a": Interval(540, 600),
        "b": Interval(600, 660),
        "c": Interval(630, 700),
    }
    assert conflicts(bookings) == [("b", "c")]
