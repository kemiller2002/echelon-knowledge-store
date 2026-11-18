from settings.merge import merge


def test_nested_override():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"host": "prod"}}) == {"db": {"host": "prod", "port": 5432}}
