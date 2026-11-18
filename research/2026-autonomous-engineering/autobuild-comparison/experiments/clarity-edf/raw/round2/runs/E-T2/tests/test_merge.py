import copy

from settings.load import load
from settings.merge import merge


def test_nested_override():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"host": "prod"}}) == {"db": {"host": "prod", "port": 5432}}


def test_deeply_nested_override_keeps_siblings():
    base = {"a": {"b": {"c": 1, "d": 2}, "e": 3}}
    assert merge(base, {"a": {"b": {"c": 9}}}) == {"a": {"b": {"c": 9, "d": 2}, "e": 3}}


def test_scalar_replaces_dict_and_dict_replaces_scalar():
    assert merge({"x": {"y": 1}}, {"x": 5}) == {"x": 5}
    assert merge({"x": 5}, {"x": {"y": 1}}) == {"x": {"y": 1}}


def test_lists_are_replaced_not_concatenated():
    base = {"servers": ["a", "b"]}
    assert merge(base, {"servers": ["c"]}) == {"servers": ["c"]}


def test_none_removes_top_level_key():
    base = {"debug": True, "name": "app"}
    assert merge(base, {"debug": None}) == {"name": "app"}


def test_none_removes_nested_key():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"port": None}}) == {"db": {"host": "localhost"}}


def test_none_for_absent_key_is_a_no_op():
    assert merge({"a": 1}, {"missing": None}) == {"a": 1}


def test_inputs_are_not_mutated():
    base = {"db": {"host": "localhost"}, "servers": ["a"]}
    override = {"db": {"port": 1}, "servers": ["b"]}
    base_before = copy.deepcopy(base)
    override_before = copy.deepcopy(override)
    merge(base, override)
    assert base == base_before
    assert override == override_before


def test_result_does_not_alias_inputs():
    base = {"db": {"host": "localhost"}}
    override = {"servers": ["a"]}
    result = merge(base, override)
    result["db"]["host"] = "changed"
    result["servers"].append("b")
    assert base == {"db": {"host": "localhost"}}
    assert override == {"servers": ["a"]}


def test_load_folds_layers_in_order():
    defaults = {"db": {"host": "localhost", "port": 5432}, "debug": True}
    environment = {"db": {"host": "staging"}}
    user = {"debug": None, "db": {"port": 6000}}
    assert load([defaults, environment, user]) == {"db": {"host": "staging", "port": 6000}}
