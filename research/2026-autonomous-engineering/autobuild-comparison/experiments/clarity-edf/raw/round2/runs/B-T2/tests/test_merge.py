from settings.merge import merge


def test_nested_override():
    base = {"db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"db": {"host": "prod"}}) == {"db": {"host": "prod", "port": 5432}}


def test_lists_are_replaced_not_concatenated():
    assert merge({"servers": ["a", "b"]}, {"servers": ["c"]}) == {"servers": ["c"]}


def test_none_removes_top_level_and_nested_keys():
    base = {"debug": True, "db": {"host": "localhost", "port": 5432}}
    assert merge(base, {"debug": None, "db": {"port": None}}) == {"db": {"host": "localhost"}}


def test_none_inside_dict_replacing_scalar_is_removed():
    assert merge({"db": "sqlite"}, {"db": {"host": "x", "port": None}}) == {"db": {"host": "x"}}


def test_scalar_replaces_dict_and_dict_replaces_scalar():
    assert merge({"a": {"x": 1}}, {"a": 2}) == {"a": 2}
    assert merge({"a": 2}, {"a": {"x": 1}}) == {"a": {"x": 1}}


def test_inputs_are_not_mutated_and_result_does_not_alias_them():
    base = {"db": {"hosts": ["a"]}}
    override = {"db": {"ports": [1]}}
    result = merge(base, override)
    result["db"]["hosts"].append("z")
    result["db"]["ports"].append(2)
    assert base == {"db": {"hosts": ["a"]}}
    assert override == {"db": {"ports": [1]}}


def test_layers_fold_left_to_right():
    from settings.load import load

    layers = [
        {"db": {"host": "localhost", "port": 5432}, "debug": True},
        {"db": {"host": "staging"}},
        {"debug": None, "db": {"port": 6000}},
    ]
    assert load(layers) == {"db": {"host": "staging", "port": 6000}}
