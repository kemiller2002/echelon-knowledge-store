import copy

from settings.load import load
from settings.merge import merge


def test_nested_override():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"host": "prod"}}) == {"db": {"host": "prod", "port": 5432}}


def test_deeply_nested_override():
    base = {"a": {"b": {"c": 1, "d": 2}}}
    assert merge(base, {"a": {"b": {"d": 3}}}) == {"a": {"b": {"c": 1, "d": 3}}}


def test_lists_are_replaced_not_concatenated():
    base = {"servers": ["a", "b", "c"]}
    assert merge(base, {"servers": ["x", "y"]}) == {"servers": ["x", "y"]}


def test_none_removes_top_level_key():
    base = {"debug": True, "port": 80}
    assert merge(base, {"debug": None}) == {"port": 80}


def test_none_removes_nested_key():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"port": None}}) == {"db": {"host": "localhost"}}


def test_none_for_missing_key_adds_nothing():
    base = {"a": 1}
    assert merge(base, {"missing": None, "nested": {"x": None}}) == {"a": 1, "nested": {}}


def test_dict_replaces_non_dict_base_value():
    base = {"logging": "off"}
    assert merge(base, {"logging": {"level": "info"}}) == {"logging": {"level": "info"}}


def test_scalar_replaces_dict():
    base = {"cache": {"ttl": 60}}
    assert merge(base, {"cache": "disabled"}) == {"cache": "disabled"}


def test_inputs_are_not_mutated():
    base = {"db": {"host": "localhost"}, "servers": ["a"]}
    override = {"db": {"port": 5432}, "servers": ["b"]}
    base_copy = copy.deepcopy(base)
    override_copy = copy.deepcopy(override)
    result = merge(base, override)
    assert base == base_copy
    assert override == override_copy
    result["db"]["host"] = "changed"
    result["servers"].append("c")
    assert base == base_copy


def test_three_layer_fold_matches_load():
    defaults = {"db": {"host": "localhost", "port": 5432}, "debug": True}
    environment = {"db": {"host": "staging"}}
    user = {"debug": None, "db": {"port": 6543}}
    assert load([defaults, environment, user]) == {"db": {"host": "staging", "port": 6543}}
