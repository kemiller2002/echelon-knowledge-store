# Decisions

## ADR-002: settings are plain dictionaries
Settings are JSON-compatible dictionaries: dict, list, str, int, float, bool, None.

## ADR-004: merge semantics (accepted)
- Nested dictionaries merge recursively; any other value in the override replaces the base value.
- Lists are replaced, never concatenated: a user who lists two servers means exactly two servers.
- An override value of `None` removes the key from the result (this is how a layer unsets a default).
- `merge` never mutates either input; layers are reused by callers and cached.
