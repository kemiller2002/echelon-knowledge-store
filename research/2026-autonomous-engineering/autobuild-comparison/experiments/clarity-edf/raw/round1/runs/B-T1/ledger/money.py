"""Money helpers.

All amounts in this package are integers in minor units (cents). Never use float.
"""


def cents(dollars: int, cents_part: int = 0) -> int:
    """Build an amount in cents from whole dollars and cents."""
    return dollars * 100 + cents_part


def total(amounts: list[int]) -> int:
    """Sum amounts; the result is still in cents."""
    return sum(amounts)
