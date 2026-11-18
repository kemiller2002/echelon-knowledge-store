from itertools import islice

import pytest

from net.backoff import delays
from net.clock import FakeClock
from net.errors import PermanentError, TransientError
from net.retry import retry


def test_returns_result():
    assert retry(lambda: 42, attempts=3, clock=FakeClock()) == 42


def test_retries_transient_errors_until_success():
    results = iter([TransientError("a"), TransientError("b"), "ok"])

    def operation():
        item = next(results)
        if isinstance(item, Exception):
            raise item
        return item

    clock = FakeClock()
    assert retry(operation, attempts=3, clock=clock) == "ok"
    assert clock.slept == [0.1, 0.2]


def test_backoff_uses_shared_schedule():
    clock = FakeClock()
    calls = []

    def operation():
        calls.append(1)
        raise TransientError("down")

    with pytest.raises(TransientError):
        retry(operation, attempts=4, clock=clock)
    assert len(calls) == 4
    assert clock.slept == list(islice(delays(), 3))


def test_no_sleep_after_last_attempt_and_last_error_reraised():
    clock = FakeClock()
    calls = []

    def operation():
        calls.append(1)
        raise TransientError(f"fail {len(calls)}")

    with pytest.raises(TransientError, match="fail 3"):
        retry(operation, attempts=3, clock=clock)
    assert len(calls) == 3
    assert len(clock.slept) == 2


def test_permanent_error_is_not_retried():
    clock = FakeClock()
    calls = []

    def operation():
        calls.append(1)
        raise PermanentError("bad request")

    with pytest.raises(PermanentError):
        retry(operation, attempts=5, clock=clock)
    assert len(calls) == 1
    assert clock.slept == []


def test_other_exceptions_are_not_retried():
    clock = FakeClock()

    def operation():
        raise ValueError("bug")

    with pytest.raises(ValueError):
        retry(operation, attempts=5, clock=clock)
    assert clock.slept == []


def test_attempts_below_one_is_value_error():
    with pytest.raises(ValueError):
        retry(lambda: 1, attempts=0, clock=FakeClock())
