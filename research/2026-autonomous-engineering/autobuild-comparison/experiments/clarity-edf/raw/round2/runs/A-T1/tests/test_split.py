import pytest

from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]


def test_remainder_goes_to_earliest_entries():
    assert split_amount(1000, 3) == [334, 333, 333]
    assert sum(split_amount(1000, 3)) == 1000


def test_refund_negates_split_of_absolute_value():
    assert split_amount(-1000, 3) == [-334, -333, -333]


def test_single_party_gets_everything():
    assert split_amount(1234, 1) == [1234]


def test_zero_total():
    assert split_amount(0, 4) == [0, 0, 0, 0]


@pytest.mark.parametrize("parties", [0, -1])
def test_invalid_party_count_raises(parties):
    with pytest.raises(ValueError):
        split_amount(100, parties)
