import pytest

from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]


def test_remainder_goes_to_earliest_entries():
    assert split_amount(1000, 3) == [334, 333, 333]
    assert split_amount(1001, 3) == [334, 334, 333]
    assert split_amount(2, 5) == [1, 1, 0, 0, 0]


def test_single_party_gets_everything():
    assert split_amount(1234, 1) == [1234]


def test_zero_total():
    assert split_amount(0, 4) == [0, 0, 0, 0]


def test_refund_is_negated_split_of_absolute_value():
    assert split_amount(-1000, 3) == [-334, -333, -333]
    assert split_amount(-2, 5) == [-1, -1, 0, 0, 0]


@pytest.mark.parametrize("total", [-1001, -7, -1, 0, 1, 7, 99, 1000, 123457])
@pytest.mark.parametrize("parties", [1, 2, 3, 7, 10])
def test_conserves_money_and_shares_are_ints(total, parties):
    shares = split_amount(total, parties)
    assert len(shares) == parties
    assert sum(shares) == total
    assert all(type(s) is int for s in shares)
    assert max(shares) - min(shares) <= 1


@pytest.mark.parametrize("parties", [0, -1, -5])
def test_invalid_party_count_raises(parties):
    with pytest.raises(ValueError):
        split_amount(1000, parties)


@pytest.mark.parametrize("total, parties", [(10.0, 2), (10, 2.0), (True, 1), (10, True)])
def test_non_integer_inputs_rejected(total, parties):
    with pytest.raises(TypeError):
        split_amount(total, parties)
