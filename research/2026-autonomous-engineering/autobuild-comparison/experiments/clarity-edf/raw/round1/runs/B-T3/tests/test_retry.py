from net.clock import FakeClock
from net.retry import retry


def test_returns_result():
    assert retry(lambda: 42, attempts=3, clock=FakeClock()) == 42


import pytest

from net.backoff import delays
from net.errors import PermanentError, TransientError


def _flaky(failures, result="ok", exc=TransientError):
    calls = {"n": 0}

    def op():
        calls["n"] += 1
        if calls["n"] <= failures:
            raise exc(f"fail {calls['n']}")
        return result

    return op, calls


def test_no_sleep_on_first_success():
    clock = FakeClock()
    retry(lambda: 1, attempts=3, clock=clock)
    assert clock.slept == []


def test_retries_transient_with_backoff_schedule():
    clock = FakeClock()
    op, calls = _flaky(2)
    assert retry(op, attempts=3, clock=clock) == "ok"
    assert calls["n"] == 3
    gen = delays()
    assert clock.slept == [next(gen), next(gen)]


def test_reraises_last_transient_unchanged_after_exhausting():
    clock = FakeClock()
    op, calls = _flaky(10)
    with pytest.raises(TransientError, match="fail 3"):
        retry(op, attempts=3, clock=clock)
    assert calls["n"] == 3
    assert len(clock.slept) == 2


@pytest.mark.parametrize("exc", [PermanentError, ValueError, RuntimeError])
def test_non_transient_propagates_immediately(exc):
    clock = FakeClock()
    op, calls = _flaky(1, exc=exc)
    with pytest.raises(exc):
        retry(op, attempts=5, clock=clock)
    assert calls["n"] == 1
    assert clock.slept == []


@pytest.mark.parametrize("attempts", [0, -1])
def test_attempts_below_one_is_value_error(attempts):
    op, calls = _flaky(0)
    with pytest.raises(ValueError):
        retry(op, attempts=attempts, clock=FakeClock())
    assert calls["n"] == 0


def test_single_attempt_does_not_sleep():
    clock = FakeClock()
    op, _ = _flaky(1)
    with pytest.raises(TransientError):
        retry(op, attempts=1, clock=clock)
    assert clock.slept == []


def test_default_clock_is_system_clock(monkeypatch):
    import time
    slept = []
    monkeypatch.setattr(time, "sleep", slept.append)
    op, _ = _flaky(1)
    assert retry(op, attempts=2) == "ok"
    assert slept == [next(delays())]
