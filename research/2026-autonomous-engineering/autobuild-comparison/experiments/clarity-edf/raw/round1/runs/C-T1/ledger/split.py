"""Splitting amounts between parties. See docs/CONVENTIONS.md."""


def split_amount(total: int, parties: int) -> list[int]:
    """Split ``total`` (integer cents) into ``parties`` shares as evenly as possible.

    The shares always sum to ``total``. Leftover cents go to the earliest shares,
    one cent each. Negative totals (refunds) are split by absolute value and every
    share is negated, so the earliest shares carry the larger refund.

    Raises ``ValueError`` if ``parties`` is zero or negative, and ``TypeError`` if
    either argument is not an integer.
    """
    if isinstance(total, bool) or not isinstance(total, int):
        raise TypeError("total must be an integer amount in cents")
    if isinstance(parties, bool) or not isinstance(parties, int):
        raise TypeError("parties must be an integer")
    if parties <= 0:
        raise ValueError("parties must be a positive integer")

    base, remainder = divmod(abs(total), parties)
    shares = [base + 1 if i < remainder else base for i in range(parties)]
    if total < 0:
        shares = [-share for share in shares]
    return shares
