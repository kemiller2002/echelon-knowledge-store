from reports.export import export_row
from reports.totals import total


def test_export_still_gross():
    assert export_row("INV-1", [1000, 500]) == "INV-1,1605"


def test_export_rounding_unchanged():
    assert export_row("INV-2", [7]) == "INV-2,7"
    assert export_row("INV-3", [10]) == "INV-3,11"


def test_total_net_unchanged():
    assert total([100, 250]) == 350
