from net.clock import FakeClock
from net.retry import retry


def test_returns_result():
    assert retry(lambda: 42, attempts=3, clock=FakeClock()) == 42


import pytest

from net.errors import PermanentError, TransientError


def _flaky(failures, exc=TransientError, result="ok"):
    calls = {"n": 0}

    def op():
        calls["n"] += 1
        if calls["n"] <= failures:
            raise exc(f"fail {calls['n']}")
        return result

    return op, calls


def test_retries_transient_then_succeeds_with_backoff():
    clock = FakeClock()
    op, calls = _flaky(2)
    assert retry(op, attempts=3, clock=clock) == "ok"
    assert calls["n"] == 3
    assert clock.slept == [0.1, 0.2]


def test_no_sleep_on_first_success():
    clock = FakeClock()
    retry(lambda: 1, attempts=5, clock=clock)
    assert clock.slept == []


def test_reraises_last_transient_error_unchanged():
    clock = FakeClock()
    errors = []

    def op():
        e = TransientError(len(errors))
        errors.append(e)
        raise e

    with pytest.raises(TransientError) as info:
        retry(op, attempts=3, clock=clock)
    assert info.value is errors[-1]
    assert len(errors) == 3
    assert clock.slept == [0.1, 0.2]


@pytest.mark.parametrize("exc", [PermanentError, ValueError, KeyError])
def test_non_transient_propagates_immediately(exc):
    clock = FakeClock()
    op, calls = _flaky(1, exc=exc)
    with pytest.raises(exc):
        retry(op, attempts=3, clock=clock)
    assert calls["n"] == 1
    assert clock.slept == []


@pytest.mark.parametrize("attempts", [0, -1])
def test_attempts_below_one_is_value_error(attempts):
    with pytest.raises(ValueError):
        retry(lambda: 1, attempts=attempts, clock=FakeClock())


def test_backoff_follows_project_schedule_and_cap():
    clock = FakeClock()
    op, _ = _flaky(100)
    with pytest.raises(TransientError):
        retry(op, attempts=9, clock=clock)
    assert clock.slept == [0.1, 0.2, 0.4, 0.8, 1.6, 3.2, 5.0, 5.0]


def test_default_clock_is_system_clock(monkeypatch):
    import net.clock

    slept = []
    monkeypatch.setattr(net.clock.time, "sleep", slept.append)
    op, _ = _flaky(1)
    assert retry(op, attempts=2) == "ok"
    assert slept == [0.1]
