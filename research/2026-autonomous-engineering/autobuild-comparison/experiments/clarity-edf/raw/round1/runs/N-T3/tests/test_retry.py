import pytest

from net.backoff import delays
from net.clock import FakeClock
from net.errors import PermanentError, TransientError
from net.retry import retry


def flaky(failures: int, result=42, error=TransientError):
    calls = []

    def operation():
        calls.append(1)
        if len(calls) <= failures:
            raise error(f"failure {len(calls)}")
        return result

    return operation, calls


def test_returns_result():
    assert retry(lambda: 42, attempts=3, clock=FakeClock()) == 42


def test_success_does_not_sleep():
    clock = FakeClock()
    retry(lambda: 1, attempts=3, clock=clock)
    assert clock.slept == []


def test_retries_transient_errors_with_backoff_schedule():
    clock = FakeClock()
    operation, calls = flaky(failures=2)
    assert retry(operation, attempts=3, clock=clock) == 42
    assert len(calls) == 3
    schedule = delays()
    assert clock.slept == [next(schedule), next(schedule)]


def test_reraises_last_transient_error_after_all_attempts():
    clock = FakeClock()
    operation, calls = flaky(failures=10)
    with pytest.raises(TransientError, match="failure 3"):
        retry(operation, attempts=3, clock=clock)
    assert len(calls) == 3
    assert len(clock.slept) == 2


def test_permanent_error_propagates_immediately():
    clock = FakeClock()
    operation, calls = flaky(failures=1, error=PermanentError)
    with pytest.raises(PermanentError):
        retry(operation, attempts=3, clock=clock)
    assert len(calls) == 1
    assert clock.slept == []


def test_other_exceptions_propagate_immediately():
    clock = FakeClock()
    operation, calls = flaky(failures=1, error=KeyError)
    with pytest.raises(KeyError):
        retry(operation, attempts=3, clock=clock)
    assert len(calls) == 1


def test_single_attempt_does_not_retry():
    clock = FakeClock()
    operation, calls = flaky(failures=1)
    with pytest.raises(TransientError):
        retry(operation, attempts=1, clock=clock)
    assert len(calls) == 1
    assert clock.slept == []


@pytest.mark.parametrize("attempts", [0, -1])
def test_invalid_attempts_raise_value_error(attempts):
    with pytest.raises(ValueError):
        retry(lambda: 1, attempts=attempts, clock=FakeClock())
