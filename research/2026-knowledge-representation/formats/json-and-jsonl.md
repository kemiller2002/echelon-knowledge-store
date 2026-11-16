# JSON and JSONL

Measured (568 records): JSON 1.13× and JSONL 1.10× the tokens of the original
Markdown; metadata-only JSON is 32% larger than front matter, JSONL about equal.
Round trip exact. Trials: JSONL 0.984 (one partial impact answer); agents used
`jq`/python readily.

Bodies become escaped single-line strings: review diffs are unreadable and merge
conflicts are worse (the versioning workstream's git 2.43.0 test: concurrent
appends to one JSONL file conflicted; one-file-per-record did not).

**Verdict:** REJECT as canonical source; ADOPT JSONL as a generated export/index
format for tools.
