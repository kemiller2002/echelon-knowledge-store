"""Splitting amounts between parties. See docs/CONVENTIONS.md."""


def split_amount(total: int, parties: int) -> list[int]:
    """Split ``total`` cents evenly between ``parties``.

    The shares always sum to ``total``. Leftover cents go one each to the
    earliest entries. A negative ``total`` (a refund) is split by applying the
    same rule to its absolute value and negating every share.
    """
    if isinstance(total, bool) or not isinstance(total, int):
        raise TypeError("total must be an integer number of cents")
    if isinstance(parties, bool) or not isinstance(parties, int):
        raise TypeError("parties must be an integer")
    if parties < 1:
        raise ValueError("parties must be at least 1")

    sign = -1 if total < 0 else 1
    base, leftover = divmod(abs(total), parties)
    shares = [base + 1 if i < leftover else base for i in range(parties)]
    return [sign * share for share in shares]
