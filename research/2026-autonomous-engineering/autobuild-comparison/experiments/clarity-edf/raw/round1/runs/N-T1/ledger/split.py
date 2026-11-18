"""Splitting amounts between parties. See docs/CONVENTIONS.md."""


def split_amount(total: int, parties: int) -> list[int]:
    """Split total cents evenly; leftover cents go to the earliest parties."""
    if parties <= 0:
        raise ValueError("parties must be a positive integer")
    share, remainder = divmod(abs(total), parties)
    parts = [share + 1 if i < remainder else share for i in range(parties)]
    if total < 0:
        return [-part for part in parts]
    return parts
