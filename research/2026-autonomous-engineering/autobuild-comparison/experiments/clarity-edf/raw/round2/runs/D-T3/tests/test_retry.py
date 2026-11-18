from net.clock import FakeClock
from net.retry import retry


def test_returns_result():
    assert retry(lambda: 42, attempts=3, clock=FakeClock()) == 42
