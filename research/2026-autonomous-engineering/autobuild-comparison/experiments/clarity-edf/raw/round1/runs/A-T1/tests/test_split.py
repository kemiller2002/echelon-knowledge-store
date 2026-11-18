from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]


import pytest


def test_uneven_split_gives_leftover_to_earliest():
    assert split_amount(1000, 3) == [334, 333, 333]
    assert sum(split_amount(1000, 3)) == 1000


def test_more_parties_than_cents():
    assert split_amount(2, 5) == [1, 1, 0, 0, 0]


def test_zero_total():
    assert split_amount(0, 3) == [0, 0, 0]


def test_refund_split():
    assert split_amount(-1000, 3) == [-334, -333, -333]


@pytest.mark.parametrize("parties", [0, -1])
def test_invalid_parties(parties):
    with pytest.raises(ValueError):
        split_amount(1000, parties)


def test_float_total_rejected():
    with pytest.raises(TypeError):
        split_amount(10.0, 2)
