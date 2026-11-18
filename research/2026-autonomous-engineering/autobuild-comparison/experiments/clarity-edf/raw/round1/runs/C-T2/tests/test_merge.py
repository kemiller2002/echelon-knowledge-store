from settings.merge import merge


def test_nested_override():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"host": "prod"}}) == {"db": {"host": "prod", "port": 5432}}


def test_scalar_and_new_keys():
    assert merge({"a": 1, "b": 2}, {"b": 3, "c": 4}) == {"a": 1, "b": 3, "c": 4}


def test_lists_are_replaced_not_concatenated():
    base = {"servers": ["a", "b", "c"]}
    assert merge(base, {"servers": ["x", "y"]}) == {"servers": ["x", "y"]}


def test_none_removes_key_top_level_and_nested():
    base = {"a": 1, "db": {"host": "h", "port": 1}}
    assert merge(base, {"a": None, "db": {"port": None}}) == {"db": {"host": "h"}}
    assert merge({"a": 1}, {"missing": None}) == {"a": 1}


def test_dict_replaces_non_dict_and_vice_versa():
    assert merge({"x": 1}, {"x": {"y": 2}}) == {"x": {"y": 2}}
    assert merge({"x": {"y": 2}}, {"x": 5}) == {"x": 5}


def test_inputs_not_mutated_and_result_not_aliased():
    base = {"db": {"host": "h", "opts": [1]}, "keep": {"k": [1]}}
    override = {"db": {"host": "p", "tags": ["t"]}, "drop": None}
    base_copy = {"db": {"host": "h", "opts": [1]}, "keep": {"k": [1]}}
    override_copy = {"db": {"host": "p", "tags": ["t"]}, "drop": None}
    result = merge(base, override)
    assert base == base_copy and override == override_copy
    result["keep"]["k"].append(2)
    result["db"]["tags"].append("u")
    assert base == base_copy and override == override_copy


def test_load_folds_layers():
    from settings.load import load
    defaults = {"db": {"host": "localhost", "port": 5432}, "debug": False}
    env = {"db": {"host": "env"}}
    user = {"debug": True, "db": {"port": None}}
    assert load([defaults, env, user]) == {"db": {"host": "env"}, "debug": True}
