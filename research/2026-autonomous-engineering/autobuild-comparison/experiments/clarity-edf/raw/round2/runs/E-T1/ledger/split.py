"""Splitting amounts between parties. See docs/CONVENTIONS.md."""


def split_amount(total: int, parties: int) -> list[int]:
    """Split ``total`` cents evenly between ``parties``.

    The shares always sum to ``total``. Leftover cents go one each to the
    earliest entries. For a negative total (a refund) the rule is applied to
    the absolute value and every part is negated.
    """
    if parties <= 0:
        raise ValueError(f"parties must be a positive integer, got {parties}")
    sign = -1 if total < 0 else 1
    base, extra = divmod(abs(total), parties)
    shares = [base + 1 if i < extra else base for i in range(parties)]
    return [sign * share for share in shares]
