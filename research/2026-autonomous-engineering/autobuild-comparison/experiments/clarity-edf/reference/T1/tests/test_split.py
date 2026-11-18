from ledger.split import split_amount


def test_even_split():
    assert split_amount(900, 3) == [300, 300, 300]
