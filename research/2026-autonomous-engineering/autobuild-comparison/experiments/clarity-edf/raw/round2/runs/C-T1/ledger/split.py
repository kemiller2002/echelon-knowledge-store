"""Splitting amounts between parties. See docs/CONVENTIONS.md."""


def split_amount(total: int, parties: int) -> list[int]:
    """Split ``total`` cents evenly between ``parties`` parties.

    The parts always sum to ``total``. Leftover cents go one each to the
    earliest entries. For a negative total (a refund), the rule is applied to
    the absolute value and every part is negated.

    Raises ValueError if ``parties`` is not an integer of at least 1.
    """
    if isinstance(parties, bool) or not isinstance(parties, int) or parties < 1:
        raise ValueError(f"parties must be an integer >= 1, got {parties!r}")

    sign = -1 if total < 0 else 1
    base, remainder = divmod(abs(total), parties)
    shares = [base + 1 if i < remainder else base for i in range(parties)]
    return [sign * share for share in shares]
