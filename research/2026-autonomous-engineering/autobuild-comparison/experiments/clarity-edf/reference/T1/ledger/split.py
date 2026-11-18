"""Splitting amounts between parties. See docs/CONVENTIONS.md."""


def split_amount(total: int, parties: int) -> list[int]:
    if parties <= 0:
        raise ValueError("parties must be positive")
    sign = -1 if total < 0 else 1
    share, extra = divmod(abs(total), parties)
    return [sign * (share + (1 if i < extra else 0)) for i in range(parties)]
