import copy

from settings.load import load
from settings.merge import merge


def test_lists_replaced_not_concatenated():
    assert merge({"servers": ["a", "b", "c"]}, {"servers": ["x", "y"]}) == {"servers": ["x", "y"]}


def test_none_removes_key_including_nested():
    base = {"a": 1, "db": {"host": "h", "port": 1}}
    assert merge(base, {"a": None, "db": {"port": None}}) == {"db": {"host": "h"}}


def test_none_for_missing_key_is_noop():
    assert merge({"a": 1}, {"b": None}) == {"a": 1}


def test_scalar_replaces_dict_and_dict_replaces_scalar():
    assert merge({"x": {"y": 1}}, {"x": 5}) == {"x": 5}
    assert merge({"x": 5}, {"x": {"y": 1, "z": None}}) == {"x": {"y": 1}}


def test_inputs_not_mutated_and_no_aliasing():
    base = {"db": {"host": "h"}, "l": [1]}
    override = {"db": {"port": 2}, "m": [2], "n": {"k": [3]}}
    b0, o0 = copy.deepcopy(base), copy.deepcopy(override)
    result = merge(base, override)
    assert base == b0 and override == o0
    result["db"]["host"] = "changed"
    result["l"].append(9)
    result["m"].append(9)
    result["n"]["k"].append(9)
    assert base == b0 and override == o0


def test_load_layers():
    defaults = {"db": {"host": "localhost", "port": 5432}, "debug": False}
    env = {"db": {"host": "prod"}}
    user = {"debug": True, "db": {"port": None}}
    assert load([defaults, env, user]) == {"db": {"host": "prod"}, "debug": True}
