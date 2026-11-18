from typing import Iterable

TAX_RATE_PERCENT = 7


def total(lines: Iterable[int]) -> int:
    """Net total in cents."""
    return sum(lines)


def legacy_total(lines: Iterable[int]) -> int:
    """DEPRECATED: gross total in cents, tax included and rounded half up. Use total()."""
    net = sum(lines)
    return net + (net * TAX_RATE_PERCENT + 50) // 100
