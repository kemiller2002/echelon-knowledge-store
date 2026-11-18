import copy
from functools import reduce

from settings.merge import merge


def test_nested_override():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"host": "prod"}}) == {"db": {"host": "prod", "port": 5432}}


def test_lists_are_replaced_not_concatenated():
    assert merge({"servers": ["a", "b", "c"]}, {"servers": ["d"]}) == {"servers": ["d"]}


def test_none_removes_key():
    assert merge({"a": 1, "b": 2}, {"a": None}) == {"b": 2}


def test_none_removes_nested_key():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"port": None}}) == {"db": {"host": "localhost"}}


def test_none_for_missing_key_is_noop():
    assert merge({"a": 1}, {"b": None}) == {"a": 1}


def test_scalar_replaces_dict_and_dict_replaces_scalar():
    assert merge({"a": {"x": 1}}, {"a": 5}) == {"a": 5}
    assert merge({"a": 5}, {"a": {"x": 1}}) == {"a": {"x": 1}}


def test_inputs_are_not_mutated():
    base = {"db": {"host": "localhost"}, "servers": ["a"]}
    override = {"db": {"port": 1}, "servers": ["b"]}
    base_copy = copy.deepcopy(base)
    override_copy = copy.deepcopy(override)
    result = merge(base, override)
    assert base == base_copy
    assert override == override_copy
    result["servers"].append("z")
    result["db"]["host"] = "changed"
    assert base == base_copy


def test_layers_fold_left_to_right():
    layers = [
        {"db": {"host": "localhost", "port": 5432}, "debug": True},
        {"db": {"host": "staging"}},
        {"debug": None, "db": {"port": 6543}},
    ]
    assert reduce(merge, layers, {}) == {"db": {"host": "staging", "port": 6543}}
