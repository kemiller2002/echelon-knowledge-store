from reports.totals import total


def test_total():
    assert total([100, 250]) == 350
