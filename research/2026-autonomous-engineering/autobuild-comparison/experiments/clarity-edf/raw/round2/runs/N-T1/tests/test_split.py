import pytest

from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]


def test_remainder_goes_to_earliest_parties():
    assert split_amount(1000, 3) == [334, 333, 333]


def test_single_party_gets_everything():
    assert split_amount(1234, 1) == [1234]


def test_refund_is_split_by_absolute_value_and_negated():
    assert split_amount(-1000, 3) == [-334, -333, -333]


@pytest.mark.parametrize("total", [0, 1, 7, 999, 1000, -1, -999])
@pytest.mark.parametrize("parties", range(1, 8))
def test_shares_sum_to_total_and_differ_by_at_most_one_cent(total, parties):
    shares = split_amount(total, parties)
    assert len(shares) == parties
    assert sum(shares) == total
    assert max(abs(s) for s in shares) - min(abs(s) for s in shares) <= 1


@pytest.mark.parametrize("parties", [0, -1])
def test_invalid_party_count_raises(parties):
    with pytest.raises(ValueError):
        split_amount(1000, parties)
