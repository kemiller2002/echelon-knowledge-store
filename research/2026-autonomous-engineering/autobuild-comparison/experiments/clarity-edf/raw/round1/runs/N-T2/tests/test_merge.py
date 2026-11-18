from settings.merge import merge


def test_nested_override():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"host": "prod"}}) == {"db": {"host": "prod", "port": 5432}}


def test_scalar_replaces_dict_and_dict_replaces_scalar():
    assert merge({"a": {"x": 1}}, {"a": 2}) == {"a": 2}
    assert merge({"a": 2}, {"a": {"x": 1}}) == {"a": {"x": 1}}


def test_lists_are_replaced_not_concatenated():
    base = {"servers": ["a", "b", "c"]}
    assert merge(base, {"servers": ["x", "y"]}) == {"servers": ["x", "y"]}


def test_none_removes_key_including_nested():
    base = {"debug": True, "db": {"host": "h", "port": 1}}
    assert merge(base, {"debug": None, "db": {"port": None}, "missing": None}) == {
        "db": {"host": "h"}
    }


def test_inputs_not_mutated_and_result_not_aliased():
    base = {"db": {"host": "h"}, "tags": ["a"]}
    override = {"db": {"port": 1}, "extra": {"items": [1]}}
    result = merge(base, override)
    assert base == {"db": {"host": "h"}, "tags": ["a"]}
    assert override == {"db": {"port": 1}, "extra": {"items": [1]}}
    result["db"]["host"] = "changed"
    result["tags"].append("b")
    result["extra"]["items"].append(2)
    assert base == {"db": {"host": "h"}, "tags": ["a"]}
    assert override == {"db": {"port": 1}, "extra": {"items": [1]}}


def test_load_folds_layers_in_order():
    from settings.load import load

    defaults = {"db": {"host": "localhost", "port": 5432}, "debug": False}
    env = {"db": {"host": "env"}}
    user = {"debug": None, "db": {"port": 6000}}
    assert load([defaults, env, user]) == {"db": {"host": "env", "port": 6000}}
