"""CSV export consumed by the accounting system (gross amounts, tax included)."""
from reports.totals import legacy_total


def export_row(invoice_id: str, lines: list[int]) -> str:
    return f"{invoice_id},{legacy_total(lines)}"
