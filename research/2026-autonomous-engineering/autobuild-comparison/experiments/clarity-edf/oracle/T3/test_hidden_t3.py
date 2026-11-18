import time
import pytest
from net.clock import FakeClock
from net.errors import TransientError, PermanentError
from net.retry import retry


def flaky(failures, result="ok", exc=TransientError):
    calls = {"n": 0}

    def op():
        calls["n"] += 1
        if calls["n"] <= failures:
            raise exc(f"fail {calls['n']}")
        return result
    return op, calls


@pytest.fixture(autouse=True)
def no_real_sleep(monkeypatch):
    def boom(_):
        raise AssertionError("time.sleep called")
    monkeypatch.setattr(time, "sleep", boom)


def test_succeeds_after_transient_failures():
    op, calls = flaky(2)
    clock = FakeClock()
    assert retry(op, attempts=3, clock=clock) == "ok"
    assert calls["n"] == 3


def test_attempts_counts_total_calls_and_reraises_last():
    op, calls = flaky(10)
    with pytest.raises(TransientError, match="fail 3"):
        retry(op, attempts=3, clock=FakeClock())
    assert calls["n"] == 3


def test_permanent_not_retried():
    op, calls = flaky(5, exc=PermanentError)
    with pytest.raises(PermanentError):
        retry(op, attempts=4, clock=FakeClock())
    assert calls["n"] == 1


def test_other_exceptions_not_retried():
    op, calls = flaky(5, exc=KeyError)
    with pytest.raises(KeyError):
        retry(op, attempts=4, clock=FakeClock())
    assert calls["n"] == 1


def test_uses_project_backoff_schedule():
    op, _ = flaky(3)
    clock = FakeClock()
    retry(op, attempts=4, clock=clock)
    assert clock.slept == [0.1, 0.2, 0.4]


def test_no_sleep_after_final_failure():
    op, _ = flaky(10)
    clock = FakeClock()
    with pytest.raises(TransientError):
        retry(op, attempts=2, clock=clock)
    assert len(clock.slept) == 1


def test_invalid_attempts():
    with pytest.raises(ValueError):
        retry(lambda: 1, attempts=0, clock=FakeClock())
