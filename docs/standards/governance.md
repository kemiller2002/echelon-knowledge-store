# Governance

## Branches

- `main` is the default branch and is always green.
- Research and experiments happen on branches (`research/<topic>`, `claude/<task>`),
  merged by pull request.
- Never force-push; never rewrite published history.

## Required checks (to configure as branch protection on `main`)

| Check | Workflow job |
|---|---|
| Formatting (Fantomas) | `CI / format` |
| Build and unit/schema tests | `CI / build-and-test` |
| Vulnerable packages | `CI / security` |
| Dependency review | `Dependency review / dependency-review` |

Branch protection itself must be configured by a repository administrator
(Settings → Branches): require the checks above, require one approving review,
dismiss stale approvals, and block force pushes and deletion.

## Pull requests

Use the template. Research PRs must state which results are measured and how to
reproduce them. Benchmark output is retained as workflow artifacts (30 days) and
committed results live under `research/`.
