from booking.interval import Interval
from booking.ranges import overlaps, merge_ranges


def test_overlapping_bookings_overlap():
    assert overlaps(Interval(540, 600), Interval(570, 630))


def test_back_to_back_bookings_do_not_overlap():
    assert not overlaps(Interval(540, 600), Interval(600, 660))


def test_merge_back_to_back():
    assert merge_ranges([Interval(600, 660), Interval(540, 600)]) == [Interval(540, 660)]


def test_merge_overlapping_and_separate():
    assert merge_ranges([Interval(540, 600), Interval(570, 630), Interval(700, 720)]) == [
        Interval(540, 630),
        Interval(700, 720),
    ]


def test_conflicts_ignores_back_to_back_but_reports_overlap():
    from booking.conflicts import conflicts

    bookings = {"a": Interval(540, 600), "b": Interval(600, 660), "c": Interval(630, 690)}
    assert conflicts(bookings) == [("b", "c")]
