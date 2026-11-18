"""Splitting amounts between parties. See docs/CONVENTIONS.md."""


def split_amount(total: int, parties: int) -> list[int]:
    """Split ``total`` cents evenly between ``parties``.

    The parts always sum to ``total``. Leftover cents go to the earliest
    parties, one cent each. Negative totals (refunds) are split by applying
    the same rule to the absolute value and negating every part.
    Raises ``ValueError`` if ``parties`` is zero or negative.
    """
    if isinstance(total, bool) or not isinstance(total, int):
        raise TypeError("total must be an integer amount in cents")
    if isinstance(parties, bool) or not isinstance(parties, int):
        raise TypeError("parties must be an integer")
    if parties <= 0:
        raise ValueError("parties must be a positive integer")

    sign = -1 if total < 0 else 1
    base, remainder = divmod(abs(total), parties)
    return [sign * (base + (1 if i < remainder else 0)) for i in range(parties)]
