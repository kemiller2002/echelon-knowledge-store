import pytest

from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]


def test_leftover_cents_go_to_earliest_entries():
    assert split_amount(1000, 3) == [334, 333, 333]
    assert split_amount(10, 4) == [3, 3, 2, 2]


def test_shares_conserve_total_and_count():
    for total in range(0, 200):
        for parties in range(1, 12):
            shares = split_amount(total, parties)
            assert len(shares) == parties
            assert sum(shares) == total


def test_zero_total():
    assert split_amount(0, 3) == [0, 0, 0]


def test_refund_applies_rule_to_absolute_value_and_negates():
    assert split_amount(-1000, 3) == [-334, -333, -333]
    for total in range(-200, 0):
        for parties in range(1, 12):
            shares = split_amount(total, parties)
            assert sum(shares) == total
            assert shares == [-s for s in split_amount(-total, parties)]


@pytest.mark.parametrize("parties", [0, -1, -3])
def test_invalid_count_raises_value_error(parties):
    with pytest.raises(ValueError):
        split_amount(100, parties)


@pytest.mark.parametrize("total", [10.0, "10", None])
def test_non_int_total_raises_type_error(total):
    with pytest.raises(TypeError):
        split_amount(total, 2)
