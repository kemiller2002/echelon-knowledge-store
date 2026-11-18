from net.clock import FakeClock
from net.retry import retry


def test_returns_result():
    assert retry(lambda: 42, attempts=3, clock=FakeClock()) == 42


import itertools

import pytest

from net.backoff import delays
from net.errors import PermanentError, TransientError


def _flaky(failures, exc_factory=lambda: TransientError("boom"), result="ok"):
    calls = {"n": 0}

    def op():
        calls["n"] += 1
        if calls["n"] <= failures:
            raise exc_factory()
        return result

    return op, calls


def test_retries_transient_then_succeeds_with_backoff_schedule():
    clock = FakeClock()
    op, calls = _flaky(2)
    assert retry(op, attempts=3, clock=clock) == "ok"
    assert calls["n"] == 3
    assert clock.slept == list(itertools.islice(delays(), 2))


def test_exhausted_reraises_last_transient_unchanged_without_trailing_sleep():
    clock = FakeClock()
    errors = []

    def op():
        e = TransientError(len(errors))
        errors.append(e)
        raise e

    with pytest.raises(TransientError) as info:
        retry(op, attempts=4, clock=clock)
    assert info.value is errors[-1]
    assert len(errors) == 4
    assert clock.slept == list(itertools.islice(delays(), 3))


@pytest.mark.parametrize("exc", [PermanentError("400"), ValueError("bug"), RuntimeError("x")])
def test_non_transient_errors_propagate_immediately(exc):
    clock = FakeClock()
    op, calls = _flaky(5, exc_factory=lambda: exc)
    with pytest.raises(type(exc)) as info:
        retry(op, attempts=5, clock=clock)
    assert info.value is exc
    assert calls["n"] == 1
    assert clock.slept == []


def test_single_attempt_never_sleeps():
    clock = FakeClock()
    op, calls = _flaky(1)
    with pytest.raises(TransientError):
        retry(op, attempts=1, clock=clock)
    assert calls["n"] == 1
    assert clock.slept == []


@pytest.mark.parametrize("attempts", [0, -1])
def test_attempts_below_one_is_value_error(attempts):
    with pytest.raises(ValueError):
        retry(lambda: 1, attempts=attempts, clock=FakeClock())
