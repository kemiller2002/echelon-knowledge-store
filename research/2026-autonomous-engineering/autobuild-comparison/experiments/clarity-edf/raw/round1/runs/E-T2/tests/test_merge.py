import copy

from settings.load import load
from settings.merge import merge


def test_nested_override():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"host": "prod"}}) == {"db": {"host": "prod", "port": 5432}}


def test_deeply_nested_and_new_keys():
    base = {"a": {"b": {"c": 1, "d": 2}}, "x": 1}
    override = {"a": {"b": {"c": 10, "e": 3}}, "y": 2}
    assert merge(base, override) == {"a": {"b": {"c": 10, "d": 2, "e": 3}}, "x": 1, "y": 2}


def test_lists_are_replaced_not_concatenated():
    base = {"servers": ["a", "b", "c"]}
    assert merge(base, {"servers": ["x", "y"]}) == {"servers": ["x", "y"]}


def test_none_removes_key_top_level_and_nested():
    base = {"debug": True, "db": {"host": "h", "password": "p"}}
    assert merge(base, {"debug": None, "db": {"password": None}}) == {"db": {"host": "h"}}


def test_none_for_missing_key_is_noop():
    assert merge({"a": 1}, {"b": None}) == {"a": 1}


def test_non_dict_replaces_dict_and_vice_versa():
    assert merge({"a": {"b": 1}}, {"a": 5}) == {"a": 5}
    assert merge({"a": 5}, {"a": {"b": 1}}) == {"a": {"b": 1}}


def test_falsy_values_override():
    assert merge({"a": 1, "b": "x", "c": True}, {"a": 0, "b": "", "c": False}) == {
        "a": 0,
        "b": "",
        "c": False,
    }


def test_inputs_not_mutated_and_result_not_aliased():
    base = {"db": {"host": "h"}, "servers": ["a"], "keep": {"k": [1]}}
    override = {"db": {"port": 1}, "extra": {"list": [2]}}
    base_copy, override_copy = copy.deepcopy(base), copy.deepcopy(override)

    result = merge(base, override)
    result["db"]["host"] = "changed"
    result["servers"].append("z")
    result["keep"]["k"].append(9)
    result["extra"]["list"].append(9)

    assert base == base_copy
    assert override == override_copy


def test_load_folds_layers_in_order():
    defaults = {"db": {"host": "localhost", "port": 5432}, "debug": False, "servers": ["a"]}
    env = {"db": {"host": "env-host"}, "debug": True}
    user = {"db": {"port": 6000}, "debug": None, "servers": ["u1", "u2"]}
    assert load([defaults, env, user]) == {
        "db": {"host": "env-host", "port": 6000},
        "servers": ["u1", "u2"],
    }
    assert defaults == {"db": {"host": "localhost", "port": 5432}, "debug": False, "servers": ["a"]}
