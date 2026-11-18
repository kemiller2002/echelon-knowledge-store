import pytest
from ledger.split import split_amount


def test_conserves_and_remainder_to_earliest():
    assert split_amount(1000, 3) == [334, 333, 333]


def test_two_cent_remainder():
    assert split_amount(1001, 3) == [334, 334, 333]


def test_returns_ints():
    assert all(type(x) is int for x in split_amount(1000, 3))


def test_refund_symmetric():
    assert split_amount(-1000, 3) == [-334, -333, -333]


def test_zero_total():
    assert split_amount(0, 2) == [0, 0]


@pytest.mark.parametrize("bad", [0, -2])
def test_invalid_parties(bad):
    with pytest.raises(ValueError):
        split_amount(100, bad)


def test_more_parties_than_cents():
    assert split_amount(2, 3) == [1, 1, 0]
