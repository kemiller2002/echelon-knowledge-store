# Markdown (baseline A)

**Measured.** 2,149 files, 3.42M tokens. With typed front matter removed ("plain
Markdown"), agent accuracy on relational questions fell to 0.72 (Sonnet) and 0.76
(Haiku), with twice the tool calls (`experiments/comparative-results.md`). Plain
Markdown is 13% smaller than the original files: that is the whole cost of the
metadata that buys ~25 accuracy points.

**Strengths:** universal agent support (every major coding agent reads Markdown
instruction files natively; `literature/context-engineering.md`), excellent diffs
and review, zero learning cost.

**Weaknesses (measured):** relations in prose are ambiguous (agents guessed status
and support); nothing checks references; hand-maintained views drift.

**Verdict:** Markdown is not the problem; *untyped* Markdown is. Keep it as the body format.
