"""Splitting amounts between parties. See docs/CONVENTIONS.md."""


def split_amount(total: int, parties: int) -> list[int]:
    """Split ``total`` cents evenly across ``parties`` shares.

    The shares always sum to ``total``. Leftover cents (when the amount does not
    divide exactly) go one each to the earliest entries. For a negative total
    (a refund), the absolute value is split by the same rule and every share is
    negated, so the earliest entries carry the larger refund.

    Raises ValueError if ``parties`` is zero or negative.
    """
    if parties <= 0:
        raise ValueError(f"parties must be positive, got {parties}")

    sign = -1 if total < 0 else 1
    quotient, remainder = divmod(abs(total), parties)
    shares = [quotient + 1 if i < remainder else quotient for i in range(parties)]
    return [sign * share for share in shares]
