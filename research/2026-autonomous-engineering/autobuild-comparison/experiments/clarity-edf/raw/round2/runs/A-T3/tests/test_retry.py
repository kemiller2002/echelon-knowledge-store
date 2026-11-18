from itertools import islice

import pytest

from net.backoff import delays
from net.clock import FakeClock
from net.errors import PermanentError, TransientError
from net.retry import retry


def test_returns_result():
    assert retry(lambda: 42, attempts=3, clock=FakeClock()) == 42


def test_retries_transient_then_succeeds():
    calls = []
    clock = FakeClock()

    def op():
        calls.append(1)
        if len(calls) < 3:
            raise TransientError("503")
        return "ok"

    assert retry(op, attempts=3, clock=clock) == "ok"
    assert len(calls) == 3
    assert clock.slept == [0.1, 0.2]


def test_waits_follow_shared_backoff_schedule():
    clock = FakeClock()

    def op():
        raise TransientError("timeout")

    with pytest.raises(TransientError):
        retry(op, attempts=4, clock=clock)
    assert clock.slept == list(islice(delays(), 3))


def test_reraises_last_transient_error_unchanged():
    err = TransientError("still down")
    calls = []

    def op():
        calls.append(1)
        raise err

    with pytest.raises(TransientError) as info:
        retry(op, attempts=3, clock=FakeClock())
    assert info.value is err
    assert len(calls) == 3


def test_permanent_error_propagates_immediately():
    calls = []
    clock = FakeClock()

    def op():
        calls.append(1)
        raise PermanentError("400")

    with pytest.raises(PermanentError):
        retry(op, attempts=5, clock=clock)
    assert len(calls) == 1
    assert clock.slept == []


def test_other_exception_propagates_immediately():
    calls = []

    def op():
        calls.append(1)
        raise KeyError("bug")

    with pytest.raises(KeyError):
        retry(op, attempts=5, clock=FakeClock())
    assert len(calls) == 1


@pytest.mark.parametrize("attempts", [0, -1])
def test_attempts_below_one_is_value_error(attempts):
    with pytest.raises(ValueError):
        retry(lambda: 1, attempts=attempts, clock=FakeClock())
