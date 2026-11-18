"""Splitting amounts between parties. See docs/CONVENTIONS.md."""


def split_amount(total: int, parties: int) -> list[int]:
    """Split ``total`` (integer cents) into ``parties`` shares as evenly as possible.

    Follows docs/CONVENTIONS.md:
    - shares are integer cents and always sum to ``total``;
    - leftover cents go to the earliest shares, one cent each;
    - negative totals (refunds) are split by absolute value and every share negated;
    - ``parties`` of zero or less raises ``ValueError``.
    """
    if parties <= 0:
        raise ValueError(f"parties must be a positive integer, got {parties}")

    sign = -1 if total < 0 else 1
    base, remainder = divmod(abs(total), parties)
    shares = [base + 1 if i < remainder else base for i in range(parties)]
    return [sign * share for share in shares]
