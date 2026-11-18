# Task: layered settings merge

Implement `merge(base, override)` in `settings/merge.py`. Settings are loaded in layers
(defaults, then environment, then user); later layers override earlier ones, including inside
nested sections. `settings/load.py` already calls it.
