import pytest

from net.clock import FakeClock
from net.errors import PermanentError, TransientError
from net.retry import retry


def test_returns_result():
    assert retry(lambda: 42, attempts=3, clock=FakeClock()) == 42


def test_retries_transient_then_succeeds_with_backoff_waits():
    clock = FakeClock()
    outcomes = [TransientError("503"), TransientError("timeout"), "ok"]
    calls = []

    def op():
        calls.append(1)
        item = outcomes.pop(0)
        if isinstance(item, Exception):
            raise item
        return item

    assert retry(op, attempts=3, clock=clock) == "ok"
    assert len(calls) == 3
    assert clock.slept == [0.1, 0.2]


def test_exhausted_attempts_reraise_last_transient_error_unchanged():
    clock = FakeClock()
    errors = [TransientError("a"), TransientError("b"), TransientError("c")]
    calls = []

    def op():
        calls.append(1)
        raise errors[len(calls) - 1]

    with pytest.raises(TransientError) as info:
        retry(op, attempts=3, clock=clock)
    assert info.value is errors[2]
    assert len(calls) == 3  # attempts counts total calls, including the first
    assert clock.slept == [0.1, 0.2]  # no wait after the final attempt


def test_permanent_error_is_not_retried():
    clock = FakeClock()
    calls = []

    def op():
        calls.append(1)
        raise PermanentError("400")

    with pytest.raises(PermanentError):
        retry(op, attempts=5, clock=clock)
    assert len(calls) == 1
    assert clock.slept == []


def test_other_exceptions_are_not_retried():
    calls = []

    def op():
        calls.append(1)
        raise KeyError("bug")

    with pytest.raises(KeyError):
        retry(op, attempts=5, clock=FakeClock())
    assert len(calls) == 1


@pytest.mark.parametrize("attempts", [0, -1])
def test_attempts_below_one_is_value_error(attempts):
    calls = []
    with pytest.raises(ValueError):
        retry(lambda: calls.append(1), attempts=attempts, clock=FakeClock())
    assert calls == []


def test_single_attempt_does_not_sleep():
    clock = FakeClock()

    def op():
        raise TransientError("x")

    with pytest.raises(TransientError):
        retry(op, attempts=1, clock=clock)
    assert clock.slept == []
