from settings.merge import merge


def test_nested_override():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"host": "prod"}}) == {"db": {"host": "prod", "port": 5432}}


def test_lists_are_replaced_not_concatenated():
    assert merge({"servers": ["a", "b", "c"]}, {"servers": ["x", "y"]}) == {"servers": ["x", "y"]}


def test_none_removes_key_at_any_depth():
    base = {"debug": True, "db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"debug": None, "db": {"port": None}}) == {"db": {"host": "localhost"}}


def test_none_for_missing_key_and_in_new_section():
    assert merge({"a": 1}, {"b": None, "c": {"d": None, "e": 2}}) == {"a": 1, "c": {"e": 2}}


def test_non_dict_replaces_dict_and_vice_versa():
    assert merge({"x": {"y": 1}}, {"x": 5}) == {"x": 5}
    assert merge({"x": 5}, {"x": {"y": 1}}) == {"x": {"y": 1}}


def test_inputs_not_mutated_and_not_aliased():
    import copy

    base = {"db": {"host": "localhost"}, "servers": ["a"]}
    override = {"db": {"port": 1}, "extra": {"list": [1]}}
    base_before, override_before = copy.deepcopy(base), copy.deepcopy(override)
    result = merge(base, override)
    assert base == base_before and override == override_before
    result["db"]["host"] = "changed"
    result["servers"].append("b")
    result["extra"]["list"].append(2)
    assert base == base_before and override == override_before


def test_load_folds_layers_in_order():
    from settings.load import load

    defaults = {"db": {"host": "localhost", "port": 5432}, "debug": False}
    env = {"db": {"host": "db.internal"}}
    user = {"debug": True, "db": {"port": None}}
    assert load([defaults, env, user]) == {"db": {"host": "db.internal"}, "debug": True}
