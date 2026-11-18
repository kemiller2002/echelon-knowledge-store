class TransientError(Exception):
    """A failure that may succeed if repeated (timeouts, 503s)."""


class PermanentError(Exception):
    """A failure that repeating cannot fix (400s, validation, auth)."""
