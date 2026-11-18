from itertools import islice

import pytest

from net.backoff import delays
from net.clock import FakeClock
from net.errors import PermanentError, TransientError
from net.retry import retry


def flaky(failures, result="ok", exc=TransientError):
    calls = {"n": 0}

    def op():
        calls["n"] += 1
        if calls["n"] <= failures:
            raise exc(f"fail {calls['n']}")
        return result

    return op, calls


def test_returns_result():
    assert retry(lambda: 42, attempts=3, clock=FakeClock()) == 42


def test_success_first_try_does_not_sleep():
    clock = FakeClock()
    op, calls = flaky(0)
    assert retry(op, attempts=3, clock=clock) == "ok"
    assert calls["n"] == 1
    assert clock.slept == []


def test_retries_transient_then_succeeds_with_backoff_schedule():
    clock = FakeClock()
    op, calls = flaky(2)
    assert retry(op, attempts=5, clock=clock) == "ok"
    assert calls["n"] == 3
    assert clock.slept == list(islice(delays(), 2))


def test_exhaustion_reraises_last_transient_unchanged():
    clock = FakeClock()
    op, calls = flaky(10)
    with pytest.raises(TransientError, match="fail 3"):
        retry(op, attempts=3, clock=clock)
    assert calls["n"] == 3
    assert clock.slept == list(islice(delays(), 2))  # no sleep after last attempt


def test_permanent_error_not_retried():
    clock = FakeClock()
    op, calls = flaky(1, exc=PermanentError)
    with pytest.raises(PermanentError):
        retry(op, attempts=3, clock=clock)
    assert calls["n"] == 1
    assert clock.slept == []


def test_other_exceptions_not_retried():
    clock = FakeClock()
    op, calls = flaky(1, exc=ValueError)
    with pytest.raises(ValueError):
        retry(op, attempts=3, clock=clock)
    assert calls["n"] == 1
    assert clock.slept == []


def test_single_attempt():
    clock = FakeClock()
    op, calls = flaky(1)
    with pytest.raises(TransientError):
        retry(op, attempts=1, clock=clock)
    assert calls["n"] == 1
    assert clock.slept == []


@pytest.mark.parametrize("attempts", [0, -1])
def test_attempts_below_one_is_value_error(attempts):
    op, calls = flaky(0)
    with pytest.raises(ValueError):
        retry(op, attempts=attempts, clock=FakeClock())
    assert calls["n"] == 0
