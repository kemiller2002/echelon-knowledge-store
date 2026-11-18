import pytest

from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]


def test_remainder_goes_to_earliest():
    assert split_amount(1000, 3) == [334, 333, 333]


def test_split_conserves_total():
    for total in (0, 1, 7, 1000, 12345):
        for parties in range(1, 8):
            assert sum(split_amount(total, parties)) == total


def test_refund_split():
    assert split_amount(-1000, 3) == [-334, -333, -333]


def test_more_parties_than_cents():
    assert split_amount(2, 4) == [1, 1, 0, 0]


def test_invalid_parties():
    for parties in (0, -1):
        with pytest.raises(ValueError):
            split_amount(100, parties)
