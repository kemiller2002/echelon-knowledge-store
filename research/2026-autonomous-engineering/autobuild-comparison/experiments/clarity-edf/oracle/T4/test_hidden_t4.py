from booking.interval import Interval
from booking.ranges import overlaps, merge_ranges
from booking.conflicts import conflicts


def test_adjacent_no_overlap_both_orders():
    assert not overlaps(Interval(600, 660), Interval(540, 600))


def test_real_overlap_and_containment():
    assert overlaps(Interval(540, 660), Interval(570, 600))
    assert overlaps(Interval(540, 600), Interval(599, 700))


def test_nightly_report_ignores_back_to_back():
    assert conflicts({"a": Interval(540, 600), "b": Interval(600, 660), "c": Interval(650, 700)}) == [("b", "c")]


def test_merge_still_joins_adjacent_and_overlapping():
    assert merge_ranges([Interval(540, 600), Interval(600, 660), Interval(650, 700), Interval(800, 900)]) == [
        Interval(540, 700), Interval(800, 900)]


def test_merge_keeps_gaps():
    assert merge_ranges([Interval(540, 600), Interval(601, 660)]) == [Interval(540, 600), Interval(601, 660)]
