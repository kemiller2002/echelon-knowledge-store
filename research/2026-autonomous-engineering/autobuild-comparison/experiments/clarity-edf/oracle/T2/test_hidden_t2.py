import copy
from settings.merge import merge
from settings.load import load


def test_lists_replaced():
    assert merge({"servers": ["a", "b"]}, {"servers": ["c"]}) == {"servers": ["c"]}


def test_none_removes_key():
    assert merge({"a": 1, "b": 2}, {"b": None}) == {"a": 1}


def test_nested_none_removes():
    assert merge({"x": {"y": 1, "z": 2}}, {"x": {"z": None}}) == {"x": {"y": 1}}


def test_inputs_not_mutated():
    base = {"x": {"y": 1}, "l": [1]}
    over = {"x": {"z": 2}, "l": [2]}
    b0, o0 = copy.deepcopy(base), copy.deepcopy(over)
    merge(base, over)
    assert base == b0 and over == o0


def test_result_does_not_alias_inputs():
    base = {"x": {"y": 1}}
    out = merge(base, {})
    out["x"]["y"] = 99
    assert base["x"]["y"] == 1


def test_dict_replaces_scalar_and_scalar_replaces_dict():
    assert merge({"a": 1}, {"a": {"b": 2}}) == {"a": {"b": 2}}
    assert merge({"a": {"b": 2}}, {"a": 3}) == {"a": 3}


def test_three_levels():
    assert merge({"a": {"b": {"c": 1, "d": 2}}}, {"a": {"b": {"d": 3}}}) == {"a": {"b": {"c": 1, "d": 3}}}


def test_load_layers():
    assert load([{"a": 1, "l": [1, 2]}, {"l": [3]}, {"a": None}]) == {"l": [3]}
