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
    assert merge({"servers": ["a", "b"]}, {"servers": ["c"]}) == {"servers": ["c"]}


def test_none_removes_key():
    assert merge({"a": 1, "b": {"c": 2, "d": 3}}, {"a": None, "b": {"c": None}}) == {
        "b": {"d": 3}
    }


def test_none_in_new_nested_dict_is_dropped():
    assert merge({}, {"a": {"b": None, "c": 1}}) == {"a": {"c": 1}}


def test_none_for_missing_key_is_noop():
    assert merge({"a": 1}, {"zzz": None}) == {"a": 1}


def test_inputs_are_not_mutated():
    base = {"db": {"host": "localhost"}, "list": [1, 2]}
    override = {"db": {"port": 1}, "list": [3]}
    base_copy = {"db": {"host": "localhost"}, "list": [1, 2]}
    override_copy = {"db": {"port": 1}, "list": [3]}
    result = merge(base, override)
    assert base == base_copy
    assert override == override_copy
    result["db"]["host"] = "changed"
    result["list"].append(99)
    assert base == base_copy
    assert override == override_copy


def test_result_does_not_share_nested_objects_with_override():
    override = {"db": {"port": 1}}
    result = merge({}, override)
    result["db"]["port"] = 2
    assert override == {"db": {"port": 1}}


def test_load_folds_layers_left_to_right():
    layers = [
        {"db": {"host": "localhost", "port": 5432}, "debug": True},
        {"db": {"host": "staging"}},
        {"debug": None, "db": {"port": 6543}},
    ]
    assert load(layers) == {"db": {"host": "staging", "port": 6543}}
