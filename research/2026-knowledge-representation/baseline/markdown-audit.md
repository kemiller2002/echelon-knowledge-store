# Markdown audit

Quantitative results: `experiments/baseline-results.md` and
`experiments/results/{baseline,validation,ablation}.md`. Qualitative audit and
agent workflows: `current-agent-workflows.md`. Summary of the mission's baseline
measurements:

| Mission measure | Result |
|---|---|
| Total Markdown files / bytes / tokens | 2,149 / 15.2 MB / 3.42M (o200k) |
| Average document size | 1,591 tokens (max 90,414) |
| Duplicate content | 580 duplicate file copies (25.4% of tokens); 8,519 duplicate paragraphs |
| Cross-document references | 1,426 relative links; 1,317 resolved record relations (83% untyped) |
| Broken links | 205 (14.4%); public repositories 4.1% |
| Contradictory information | 1 contested claim; 5 claims whose status lags evidence; 29 ids defined in several repositories (11 with different content per audit) |
| Outdated information | 5 of 6 index documents omit records; 3 supports from superseded records; Signal CURRENT-STATE still "newly initialized" (audit) |
| Missing provenance | 49 evidence records cite sources only via links; 68% of files have no id |
| Repeated agent instructions | Praxis governance/framework payload byte-identical in 10 repositories; AGENTS.md 76–94% identical |
| Retrieval effort | relational questions need 8–20 tool calls with front matter, 18–24 without (trials) |
| Agent context utilisation | 9.6k–22.3k tokens mandatory startup reading per Praxis repository (bytes/4) |
| Knowledge update frequency, git diff complexity | not measured (shallow clones) |
