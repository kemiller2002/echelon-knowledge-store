import pytest

from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]


def test_uneven_split_gives_leftover_to_earliest():
    assert split_amount(1000, 3) == [334, 333, 333]
    assert split_amount(1001, 3) == [334, 334, 333]


def test_single_party_gets_everything():
    assert split_amount(1234, 1) == [1234]


def test_more_parties_than_cents():
    assert split_amount(2, 5) == [1, 1, 0, 0, 0]


def test_zero_total():
    assert split_amount(0, 4) == [0, 0, 0, 0]


def test_refund_split_earliest_carry_larger_refund():
    assert split_amount(-1000, 3) == [-334, -333, -333]


@pytest.mark.parametrize("parties", [0, -1, -3])
def test_invalid_party_count_raises(parties):
    with pytest.raises(ValueError):
        split_amount(1000, parties)


def test_float_total_rejected():
    with pytest.raises(TypeError):
        split_amount(10.0, 3)


def test_conserves_money():
    for total in range(-250, 251):
        for parties in range(1, 8):
            shares = split_amount(total, parties)
            assert len(shares) == parties
            assert sum(shares) == total
            assert max(shares) - min(shares) <= 1
