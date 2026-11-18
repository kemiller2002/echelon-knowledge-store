import pytest

from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]


def test_task_example_uneven_split_conserves_and_favours_earliest():
    assert split_amount(1000, 3) == [334, 333, 333]


@pytest.mark.parametrize("total,parties", [(0, 1), (0, 4), (1, 3), (7, 7), (101, 4), (99999, 13), (-1000, 3), (-1, 2)])
def test_conservation_and_length(total, parties):
    parts = split_amount(total, parties)
    assert len(parts) == parties
    assert sum(parts) == total
    assert all(isinstance(p, int) for p in parts)
    assert max(parts) - min(parts) <= 1


def test_fewer_cents_than_parties():
    assert split_amount(2, 5) == [1, 1, 0, 0, 0]


def test_refund_earliest_carry_larger_refund():
    assert split_amount(-1000, 3) == [-334, -333, -333]
    assert split_amount(-2, 3) == [-1, -1, 0]


@pytest.mark.parametrize("parties", [0, -1, -5])
def test_invalid_party_count_raises(parties):
    with pytest.raises(ValueError):
        split_amount(1000, parties)


@pytest.mark.parametrize("total,parties", [(10.0, 2), (1000, 2.0), (True, 1), (1000, True)])
def test_non_integer_inputs_rejected(total, parties):
    with pytest.raises(TypeError):
        split_amount(total, parties)
