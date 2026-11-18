"""Splitting amounts between parties. See docs/CONVENTIONS.md."""


def split_amount(total: int, parties: int) -> list[int]:
    """Split ``total`` cents into ``parties`` shares that sum to ``total``.

    The leftover cents go one each to the earliest entries. A negative total
    (a refund) is split by applying the same rule to its absolute value and
    negating every part.
    """
    if parties < 1:
        raise ValueError(f"parties must be at least 1, got {parties}")
    sign = -1 if total < 0 else 1
    base, remainder = divmod(abs(total), parties)
    shares = [base + 1 if i < remainder else base for i in range(parties)]
    return [sign * share for share in shares]
