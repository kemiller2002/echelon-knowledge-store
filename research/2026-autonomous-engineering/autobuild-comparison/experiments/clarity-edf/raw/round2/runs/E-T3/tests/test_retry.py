import pytest

from net.clock import FakeClock
from net.errors import PermanentError, TransientError
from net.retry import retry


def test_returns_result():
    assert retry(lambda: 42, attempts=3, clock=FakeClock()) == 42


def test_retries_transient_then_succeeds_with_backoff_sleeps():
    clock = FakeClock()
    calls = []

    def op():
        calls.append(1)
        if len(calls) < 3:
            raise TransientError("503")
        return "ok"

    assert retry(op, attempts=5, clock=clock) == "ok"
    assert len(calls) == 3
    assert clock.slept == [0.1, 0.2]


def test_sleep_schedule_is_capped():
    clock = FakeClock()

    def op():
        raise TransientError("timeout")

    with pytest.raises(TransientError):
        retry(op, attempts=10, clock=clock)
    assert clock.slept == [0.1, 0.2, 0.4, 0.8, 1.6, 3.2, 5.0, 5.0, 5.0]


def test_reraises_last_transient_error_unchanged():
    clock = FakeClock()
    errors = []

    def op():
        err = TransientError(f"fail {len(errors)}")
        errors.append(err)
        raise err

    with pytest.raises(TransientError) as info:
        retry(op, attempts=3, clock=clock)
    assert info.value is errors[-1]
    assert len(errors) == 3
    assert len(clock.slept) == 2  # no sleep after the final attempt


def test_permanent_error_propagates_without_retry():
    clock = FakeClock()
    calls = []

    def op():
        calls.append(1)
        raise PermanentError("400")

    with pytest.raises(PermanentError):
        retry(op, attempts=5, clock=clock)
    assert len(calls) == 1
    assert clock.slept == []


def test_other_exceptions_propagate_without_retry():
    clock = FakeClock()
    calls = []

    def op():
        calls.append(1)
        raise KeyError("bug")

    with pytest.raises(KeyError):
        retry(op, attempts=5, clock=clock)
    assert len(calls) == 1


def test_attempts_below_one_is_value_error():
    with pytest.raises(ValueError):
        retry(lambda: 1, attempts=0, clock=FakeClock())


def test_single_attempt_does_not_sleep():
    clock = FakeClock()

    def op():
        raise TransientError("x")

    with pytest.raises(TransientError):
        retry(op, attempts=1, clock=clock)
    assert clock.slept == []
