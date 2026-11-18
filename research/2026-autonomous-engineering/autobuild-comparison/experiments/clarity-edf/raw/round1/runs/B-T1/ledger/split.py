"""Splitting amounts between parties. See docs/CONVENTIONS.md."""


def split_amount(total: int, parties: int) -> list[int]:
    """Split ``total`` (integer cents) evenly between ``parties``.

    Follows docs/CONVENTIONS.md: the parts always sum to ``total``; leftover cents go to the
    earliest parties, one cent each; a negative total (refund) is split by its absolute value
    and every part negated; a non-positive party count raises ``ValueError``.
    """
    for name, value in (("total", total), ("parties", parties)):
        if isinstance(value, bool) or not isinstance(value, int):
            raise TypeError(f"{name} must be an int (cents), got {type(value).__name__}")
    if parties <= 0:
        raise ValueError(f"parties must be positive, got {parties}")

    sign = -1 if total < 0 else 1
    base, remainder = divmod(abs(total), parties)
    return [sign * (base + (1 if i < remainder else 0)) for i in range(parties)]
