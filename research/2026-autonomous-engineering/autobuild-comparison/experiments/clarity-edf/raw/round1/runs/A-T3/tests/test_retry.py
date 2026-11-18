from net.clock import FakeClock
from net.retry import retry


def test_returns_result():
    assert retry(lambda: 42, attempts=3, clock=FakeClock()) == 42


import pytest

from net.errors import PermanentError, TransientError


def _failing(times, exc=TransientError, result="ok"):
    calls = {"n": 0}

    def op():
        calls["n"] += 1
        if calls["n"] <= times:
            raise exc(f"fail {calls['n']}")
        return result

    return op, calls


def test_retries_transient_then_succeeds_with_backoff():
    clock = FakeClock()
    op, calls = _failing(2)
    assert retry(op, attempts=3, clock=clock) == "ok"
    assert calls["n"] == 3
    assert clock.slept == [0.1, 0.2]


def test_reraises_last_transient_error_after_all_attempts():
    clock = FakeClock()
    op, calls = _failing(5)
    with pytest.raises(TransientError, match="fail 3"):
        retry(op, attempts=3, clock=clock)
    assert calls["n"] == 3
    assert clock.slept == [0.1, 0.2]


def test_permanent_error_not_retried():
    clock = FakeClock()
    op, calls = _failing(1, exc=PermanentError)
    with pytest.raises(PermanentError):
        retry(op, attempts=3, clock=clock)
    assert calls["n"] == 1
    assert clock.slept == []


def test_other_exceptions_not_retried():
    clock = FakeClock()
    op, calls = _failing(1, exc=ValueError)
    with pytest.raises(ValueError):
        retry(op, attempts=3, clock=clock)
    assert calls["n"] == 1


def test_single_attempt_does_not_sleep():
    clock = FakeClock()
    op, calls = _failing(1)
    with pytest.raises(TransientError):
        retry(op, attempts=1, clock=clock)
    assert clock.slept == []


@pytest.mark.parametrize("attempts", [0, -1])
def test_invalid_attempts(attempts):
    with pytest.raises(ValueError):
        retry(lambda: 1, attempts=attempts, clock=FakeClock())
