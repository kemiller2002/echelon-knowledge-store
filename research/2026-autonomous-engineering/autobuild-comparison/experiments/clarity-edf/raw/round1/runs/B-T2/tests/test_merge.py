import copy

from settings.load import load
from settings.merge import merge


def test_nested_override():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"host": "prod"}}) == {"db": {"host": "prod", "port": 5432}}


def test_scalar_replace_and_new_keys():
    assert merge({"a": 1, "b": 2}, {"b": 3, "c": 4}) == {"a": 1, "b": 3, "c": 4}


def test_lists_are_replaced_not_concatenated():
    base = {"servers": ["a", "b", "c"]}
    assert merge(base, {"servers": ["x", "y"]}) == {"servers": ["x", "y"]}


def test_none_removes_key_top_level_and_nested():
    base = {"a": 1, "db": {"host": "h", "port": 1}}
    assert merge(base, {"a": None, "db": {"port": None}}) == {"db": {"host": "h"}}


def test_none_for_absent_key_is_noop():
    assert merge({"a": 1}, {"b": None, "c": {"d": None, "e": 2}}) == {"a": 1, "c": {"e": 2}}


def test_type_changes_replace():
    assert merge({"a": {"x": 1}}, {"a": 5}) == {"a": 5}
    assert merge({"a": 5}, {"a": {"x": 1}}) == {"a": {"x": 1}}


def test_inputs_not_mutated_and_result_not_aliased():
    base = {"db": {"host": "h", "opts": [1]}, "keep": {"k": [1]}}
    override = {"db": {"port": 1, "tags": ["t"]}, "gone": None}
    base_copy, override_copy = copy.deepcopy(base), copy.deepcopy(override)
    result = merge(base, override)
    assert base == base_copy and override == override_copy
    result["db"]["opts"].append(2)
    result["db"]["tags"].append("u")
    result["keep"]["k"].append(2)
    assert base == base_copy and override == override_copy


def test_load_folds_layers_in_order():
    defaults = {"db": {"host": "localhost", "port": 5432}, "debug": False, "servers": ["a"]}
    env = {"db": {"host": "env"}, "debug": True}
    user = {"db": {"port": None}, "servers": ["u1", "u2"]}
    assert load([defaults, env, user]) == {"db": {"host": "env"}, "debug": True, "servers": ["u1", "u2"]}
