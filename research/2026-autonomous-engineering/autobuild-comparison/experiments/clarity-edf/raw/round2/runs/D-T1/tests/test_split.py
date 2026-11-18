import pytest

from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]


def test_remainder_goes_to_earliest_entries():
    assert split_amount(1000, 3) == [334, 333, 333]
    assert split_amount(10, 4) == [3, 3, 2, 2]


def test_shares_conserve_total():
    for total in range(0, 200):
        for parties in range(1, 12):
            shares = split_amount(total, parties)
            assert len(shares) == parties
            assert sum(shares) == total


def test_refund_is_split_by_absolute_value_and_negated():
    assert split_amount(-1000, 3) == [-334, -333, -333]
    assert split_amount(-10, 4) == [-3, -3, -2, -2]


def test_zero_total_gives_zero_shares():
    assert split_amount(0, 3) == [0, 0, 0]


@pytest.mark.parametrize("parties", [0, -1])
def test_invalid_party_count_raises(parties):
    with pytest.raises(ValueError):
        split_amount(100, parties)
