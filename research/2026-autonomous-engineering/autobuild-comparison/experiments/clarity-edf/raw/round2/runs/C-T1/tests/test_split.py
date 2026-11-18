import pytest

from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]


def test_remainder_goes_to_earliest_entries():
    assert split_amount(1000, 3) == [334, 333, 333]


def test_parts_always_sum_to_total():
    for total in range(0, 201):
        for parties in range(1, 21):
            parts = split_amount(total, parties)
            assert len(parts) == parties
            assert sum(parts) == total


def test_refund_uses_absolute_value_and_negates_parts():
    assert split_amount(-1000, 3) == [-334, -333, -333]
    assert split_amount(-900, 3) == [-300, -300, -300]


def test_zero_total():
    assert split_amount(0, 4) == [0, 0, 0, 0]


def test_single_party_gets_whole_amount():
    assert split_amount(777, 1) == [777]


@pytest.mark.parametrize("parties", [0, -1, -5])
def test_invalid_party_count_raises(parties):
    with pytest.raises(ValueError):
        split_amount(100, parties)


def test_non_integer_party_count_raises():
    with pytest.raises(ValueError):
        split_amount(100, 2.0)
