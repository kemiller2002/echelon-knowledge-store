import pytest

from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]


def test_uneven_split_gives_leftover_to_earliest():
    assert split_amount(1000, 3) == [334, 333, 333]
    assert split_amount(1001, 3) == [334, 334, 333]


def test_split_conserves_total():
    for total in range(-250, 251):
        for parties in range(1, 8):
            shares = split_amount(total, parties)
            assert len(shares) == parties
            assert sum(shares) == total
            assert all(isinstance(s, int) for s in shares)


def test_refund_split_negates_parts():
    assert split_amount(-1000, 3) == [-334, -333, -333]


def test_fewer_cents_than_parties():
    assert split_amount(2, 3) == [1, 1, 0]
    assert split_amount(0, 3) == [0, 0, 0]


@pytest.mark.parametrize("parties", [0, -1])
def test_invalid_party_count_raises(parties):
    with pytest.raises(ValueError):
        split_amount(1000, parties)
