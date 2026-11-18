# Obvious AutoBuild: Competitive Research

Access date for all sources: 2026-11-18. Product: AutoBuild by Obvious (obvious.ai). The company was formerly linked to Flatfile; sources disagree on the founding date and funding (see section 4).

Evidence status vocabulary: **documented** (vendor docs describe it), **demonstrated** (a public example shows it working), **independently-verified** (a third party tested it), **inferred** (deduction, basis stated), **unknown**.

**Bottom line.** AutoBuild is a spec-driven, multi-agent "software factory." It takes in work, writes or accepts a spec, breaks it into tasks (initiative > feature > executable, one PR per executable), runs agents in per-repo cloud sandboxes, and carries PRs through CI and review to merge. Its controls are a repo-level `.obvious/` contract and workspace-level human-gate settings.

The vendor docs are fairly concrete on config, skills, review scoring and human-merge gates. They are thin on sandbox isolation, the GitHub App's permission scopes, secrets inside sandboxes, deployment, and how self-improvement is versioned.

No independent review, benchmark, Hacker News thread, Reddit thread or YouTube walkthrough was found. The strongest outside evidence is the public GitHub footprint of the `obvious-autobuild[bot]` app, which I measured myself (section 3). It shows real use in production open-source repos, including storybookjs/storybook and chromaui/chromatic-cli.

---

## 1. Capability table

| # | Capability | What is claimed (summary) | Status | Key sources |
|---|---|---|---|---|
| 1 | Requirements / spec generation | Researches the codebase and drafts a "Blueprint" (a spec with plan and tests); "Nothing is built until you approve it." The help center sets out a three-phase spec framework (product, UX, technical). Specs can be written by the user or drafted by the agent. | documented | /autobuild; help: three-phase-spec-framework, blueprints, your-first-initiative |
| 2 | Agent orchestration | An orchestrator plans and hands tasks to worker agents. Hierarchy is initiative > feature > executable ("one task, one agent, one PR"), with dependency ordering and 9 statuses. | demonstrated (docs plus about 5.7k public bot PRs across many repos) | /autobuild; help: what-is-autobuild, how-autobuild-works; GitHub search |
| 3 | Parallel execution and isolation | Each worker runs "in its own cloud sandbox." Independent executables run in parallel. Changes are made "without changing your main branch." | documented (per-repo sandbox snapshot visible in a public PR; isolation technology not disclosed) | /autobuild; help: how-autobuild-works, install-the-github-app; chromatic-cli#1521 |
| 4 | Independent review | Automated review agent with a rubric. `.obvious/review/` holds overlay rules and a scoring model (0–100 mapped to Blocker ≥90, High ≥75, Medium ≥60, internal note 45–59, silent ≤44). Path routing (`none`/`single`/`specialized`), a per-PR "reading guide", and an optional human merge. | documented (how the reviewer is kept independent of the author agent is **unknown**) | help: customizing-code-review, config-yml-reference |
| 5 | Verification and evidence | QA attaches "screenshots, videos, test results, and API responses." Failed checks are auto-fixed and retested. The blog describes a headless browser run against preview deploys, 16-frame "sprites", and semantic diffs. | demonstrated (test and build evidence in public PRs; the visual pipeline is "In Development" on the vendor blog) | /autobuild; build-blog/closed-loop-agents; chromatic-cli#1521 |
| 6 | Repository governance | `.obvious/config.yml` "per-repository policy contract": base branch, merge method, review routing, CI generated-file commands, preview environments, QA paths. Workspace settings `requireHumanMerge` and `requireHumanCreatePR`. Repo config "cannot" weaken platform rules or branch protection. Draft-early PRs cannot be turned off. | demonstrated (bot onboarding PRs add the `.obvious/` contract to real repos) | help: config-yml-reference, workspace-level-settings, give-autobuild-repository-context; chromatic-cli#1521 |
| 7 | Skills and knowledge | `.obvious/skills/<name>/SKILL.md` (frontmatter fields name/description/triggers). `SKILL_CORE.md` is injected every turn. Skills load only from the default branch and are cached for 15 minutes. Agents honor `AGENTS.md`. Knowledge sources: "codebase, past PRs, docs, incidents, and ADRs". User and project memory. | demonstrated (generated SKILL.md in a public PR; versioning and precedence **unknown**) | help: writing-custom-skills, agents/overview; /autobuild; chromatic-cli#1521 |
| 8 | Self-improvement | Nightly memory consolidation. A daily scan of recurring review issues suggests "up to three fixes". Three matching notes produce a drafted rule. Rule, skill and loop changes need approval; memory updates do not. Rules can be "Un-apply"-ed. Scope can be per person, per project or global. | documented (marketing page only; no help article, no version history) | /self-improvement; /autobuild |
| 9 | Cost and human-attention accounting | Spend is tracked "per task, project, and workspace." The build drawer shows cost "when one is recorded." Spend Controls cover budgets, daily throttles, and per-user and per-project allocations. Dashboards show Cost/PR, Autonomy % and a credit leaderboard. A review-SLA dashboard is planned. | documented (attention metrics only shown as illustrative dashboards) | /autobuild; /self-improvement; help: spend-controls, credits; build-blog/orchestration |
| 10 | Human approval workflows | Blueprint approval; custom loops with approval points; an Inbox; decision prompts in the build drawer; human-merge and human-create-PR gates; protected secrets need per-use approval; self-improvement rules need approval. | demonstrated (maintainers approving and merging bot PRs on GitHub) | /autobuild; help: workspace-level-settings, integrations/secrets, agents/plans; storybook#37512 |
| 11 | Model independence | "Fusion" routes each step to a class of model. Models can be pinned per chat, agent, routine or workspace. Listed models: Claude Sonnet/Opus/Haiku 5.5, GPT-6.1 Sol, GLM 5.3 / 5.3 Flash. Max mode costs 2–3x credits. Provider failover. Enterprise offers BYO or custom models and fine-tuning. | documented (Gemini not listed; the default coding model for AutoBuild is **unknown**) | /fusion; /enterprise; /pricing; llm-info |
| 12 | Interface and developer experience | Work comes in from Slack, tickets, triggers or schedules. Autobuild home, build drawer, PRs page, Factory Cockpit, Inbox, desktop and mobile apps. Works with GitHub (GitLab, Bitbucket and Azure are listed). Linear/Jira/Notion/Atlassian integrations. A "babysit" option for stalled PRs. | demonstrated (GitHub-side behavior visible; in-app UI not independently reviewed) | /autobuild; help: what-is-autobuild, your-first-initiative; public PRs |
| 13 | Security boundaries | Security onboarding scan. Per-repo sandboxes. GitHub App limited to selected repos. Scoped secrets, with protected secrets needing approval each use (15 min once, 30 min per session). Secrets "stored separately from prompts". Claims ISO 27001, SOC 2 Type II, GDPR/CCPA, "HIPAA Ready". AES-256 / TLS 1.3, SSO (SAML/OIDC), SCIM, RBAC, MFA, immutable audit logs, IP rules. | documented (certifications are vendor-asserted; sandbox technology, network egress, GitHub App scopes, and whether sandboxed code can read raw secrets are **unknown**) | /security; /enterprise; help: integrations/secrets, install-the-github-app, config-yml-reference |
| 14 | Deployment and continuous maintenance | Preview environments (URL templates, readiness polling, timeouts). Agents own PRs through merge. Scheduled and triggered work. Runs "while your laptop is closed." Pricing page lists an on-prem Enterprise option. | inferred (from the steady public stream of maintenance PRs such as flaky tests and CI fixes; production deploy and release automation are not documented, and `release`/`notifications` keys are reserved and unused) | help: config-yml-reference; /autobuild; /pricing; storybook PR list |

No capability reached **independently-verified**. No third-party evaluation was found.

---

## 2. Self-improvement in depth (capability 8)

Sources: https://www.obvious.ai/self-improvement, https://www.obvious.ai/autobuild. I found no help-center article on this topic. The general agents page covers memory only.

**What feeds it**
- "every chat and rating", plus review comments on PRs.
- Only human input counts: "messages between agents never become memory".

**How feedback becomes a change**
1. **Memory (automatic).** Each night the agent sorts what it learned: it merges duplicates, shortens old notes, and promotes facts that matter across projects to the whole workspace. No human approval is needed.
2. **Rules from repeated ratings.** When ratings keep pointing at the same problem, the agent drafts a fix as a new rule for itself.
3. **Daily review mining.** Obvious looks for recurring review issues each day and proposes "up to three fixes", each linked to the reviews behind it. Example: after reviewers kept flagging "workspace" where the product says "project", it added a "Glossary check" step to the Loop.
4. **Threshold rule.** When three people leave the same note, it becomes a drafted rule.
5. **AutoBuild page.** AutoBuild "proposes skill or loop changes" that "take effect only after you approve them."

**Human approval**
- "every change to its rules waits for a person."
- The owner, or anyone who can edit the agent, can approve, edit or discard each draft on the web.
- "Nothing changes until you approve it" applies to rules. It does **not** apply to memory.

**Versioning and rollback**
- "Un-apply" removes a rule, and the agent rebuilds its instructions without it.
- I found no version history, diff view or audit trail for rule changes.
- It is **unknown** whether approved skill changes are committed to `.obvious/skills/` through a PR, and so get git versioning, or are stored only on the platform.
- Separately, agent "checkpoints" allow multi-step rollback of project artifacts such as documents, sheets and files. The docs do not say they cover code or rules.

**Scope** is set per agent: per person, per project, or global.

**Gaps relevant to competitors**
- No evaluation or regression gate on proposed rules (for example, replaying past tasks) is described.
- No metrics on how good the improvements are.
- The dashboard numbers on the page (PRs/week 62.0, Cost/PR 1,180, Autonomy 71.4%) are illustrative sample values.

---

## 3. My own observation: public GitHub footprint

**Method.** GitHub REST search API, query `author:app/obvious-autobuild is:pr`, run on 2026-11-18. This is not a third-party publication. It is my own measurement of public data, and covers public repos only.

**Volume**
- 5,706 PRs in total: 5,007 merged, 201 closed without merging, 498 open.
- Of PRs that reached a merge-or-close decision, about **96.1% were merged**. This roughly matches the vendor's "96% merge rate" claim.
- Caveats: many of the repos are small or personal, I did not check who merged at scale, and the figure says nothing about code quality.

**Created per month**

| Month (2026) | PRs created |
|---|---|
| Apr | 104 |
| May | 46 |
| Jun | 181 |
| Jul | 11 |
| Aug | 258 |
| Sep | 1,398 |
| Oct | 2,633 |
| Nov 1–18 | 1,075 |

The earliest PR is dated 2026-04-17, in FlatFilers/csvjson-app.

**storybookjs/storybook**
- 143 PRs, 87 merged (about 61% of all PRs, including open ones). That is well below the overall figure.
- Several PRs carry `review:changes-requested`. #37069 drew 55 comments, mostly from the repo's own review bots (Greptile and storybook-contribution-review), which flagged bugs ranging from "Blocking" to "P1". The AutoBuild bot fixed most of them and pushed back on some.
- #37512 was approved and merged by a human maintainer. Greptile noted "Tests were not run" in its own review.

**chromaui/chromatic-cli#1521 (onboarding PR)**
- Adds `.obvious/obvious.md`, `codebase-map.md`, `config.yml` and `skills/local-dev/SKILL.md`.
- Reports install, build, lint, typecheck and tests (1534 passed) and records a sandbox snapshot ID.
- States openly what it did not run: no `chromatic` upload and no screenshots.
- The bot addressed three maintainer comments in a follow-up commit, and the PR was then approved and merged on 2026-10-14.

---

## 4. Pricing, launch and maturity

**Pricing** (https://www.obvious.ai/pricing)
- Standard: $199/seat/month, with 1x Fusion usage.
- Pro: $499/seat/month, with 5x Fusion usage.
- Both include a 7-day trial and differ only in usage.
- Extra credits cost $0.85 each, with volume discounts.
- Enterprise is custom: BYO models, on-prem, and implementation help.
- AutoBuild is listed as part of the workspace and is not priced separately.
- **Inconsistency:** the help page on credits mentions "Team plans", Fast/Deep modes, and 25 free credits that expire after 14 days. That page looks out of date.

**Launch**
- I found no formal launch announcement. The newsroom has no AutoBuild post.
- The "What is Autobuild" help doc was published 2026-05-08 and updated 2026-09-24.
- The first public bot PR is dated 2026-04-17.
- The Obvious platform's public beta began 2026-01-15, following about 8 months of closed beta (newsroom).
- llm-info calls AutoBuild "Obvious's first product". That conflicts with the earlier workspace products (Canvas, Folios, Workbooks).

**Company** (all vendor-stated, and conflicting)
- llm-info: founded 2023, co-founders David Boskovic (CEO) and Jon Bell. It also mentions "Series B, $100M funding", which may refer to Flatfile.
- Newsroom: a $22M Series A led by Benchmark, dated 2026-02-18. This came from search snippets and I did not open the post directly.
- A third-party profile cites a different figure ($50M Series B). Treat all funding figures as unverified.

**Maturity signals**
- Positive: a detailed config reference, real adoption in large open-source repos (Storybook, Chromatic), a fast rise in PR volume from September to November 2026, a status page, and in-person "Frontier Build" events in several cities.
- Caution: several config keys are "reserved and not consumed". Two Build Blog systems are "In Development" (Orchestration Layer, Closed-Loop Agents). There are no third-party reviews and no Hacker News, Reddit or YouTube discussion. Pages contradict each other on the founding year, funding, on-prem availability and plan names.

---

## 5. Quantitative claims (all vendor claims unless noted)

| Claim | Source | Status |
|---|---|---|
| 96% merge rate (decided PRs only) | /autobuild | Vendor claim. Roughly consistent with my public-GitHub measurement (96.1% decided-merge rate, 5,208 decided PRs). Storybook alone is much lower. |
| 6.9x shipping velocity in a month (one team) | /autobuild | Vendor claim, unverified |
| 3 in 4 merged PRs at Obvious made by AutoBuild last month | /autobuild | Vendor claim, unverified |
| "Six months of engineering work … done in 48 hours" (Paragon CTO); the page also says 24 hours; 65 PRs "still climbing" | /case-studies/paragon | Customer testimonial on a vendor page; the baseline is an estimate; inconsistent figures |
| 6,000+ PRs in one project; review SLA under 1 hour | build-blog/orchestration | Vendor claim; feature "In Development" |
| CI: 200 cores, about $1k/month, test suites cut from 10–15 min to 20–30 s, built in one day by AutoBuild | build-blog/ci-engine | Vendor claim |
| Frontier Build: "12 weeks of planned roadmap in 2 days"; every cohort "delivered" | llm-info | Vendor claim |
| Max mode costs 2–3x credits | /fusion; help agents/overview | Documented pricing behavior |

---

## 6. Limitations and criticisms

- **No independent evaluation.** Searches turned up no reviews, benchmarks or community threads about AutoBuild. The competitive page (/vs) says it is "based on live testing" across 8 tasks but publishes no results.
- **Spec dependence.** The vendor's own docs warn that agents implement exactly what the spec says. Skipping the technical spec lets the agent make "architecture decisions you never approved." General commentary on the category (Digg, Hashnode; not about Obvious specifically) argues that one wrong assumption can waste a whole run.
- **Review quality in the wild.** In Storybook, outside review bots found real bugs in AutoBuild PRs, and about 39% of its PRs there are not merged (open or closed).
- **Documentation gaps.** Missing topics: sandbox isolation design; GitHub App permission scopes; whether sandboxed code can read raw secrets; data use for model training (the FAQ question appears with no answer); a trust-center or audit report; versioning of rules, skills and Blueprints; release and deploy automation.
- **Contradictions between pages.** On-prem appears on the pricing page but not the enterprise page. The founding year and funding differ across pages. The credits page describes older plan names and modes.

---

## 7. Sources (accessed 2026-11-18)

| URL | What it said (quotes ≤25 words) |
|---|---|
| https://www.obvious.ai/autobuild | Software factory that runs "research, spec, build, verify, merge". Blueprint: "Nothing is built until you approve it". Orchestrator and workers, each in "its own cloud sandbox". 96% merge rate, 6.9x velocity, 3 in 4 PRs. Fusion; integrations; compliance badges. |
| https://www.obvious.ai/self-improvement | Nightly memory sorting. "suggests up to three fixes". "every change to its rules waits for a person". "Nothing changes until you approve it". "Un-apply" removes a rule. Per-person, per-project or global scope. |
| https://help.obvious.ai/autobuild | Index page. Initiatives/features/executables; GitHub App; `.obvious` directory; three-phase specs; `requireHumanMerge`, `requireHumanCreatePR`. |
| https://help.obvious.ai/autobuild/what-is-autobuild | "Autobuild is Obvious's system for running software development work with AI agents". Executable = "one task, one agent, one PR". Sandbox statuses. Published May 8, 2026; updated Sep 24, 2026. |
| https://help.obvious.ai/autobuild/how-autobuild-works | Dependency ordering; sandbox lets it make changes "without changing your main branch". "Autobuild prepares work for review; it does not replace your judgment." |
| https://help.obvious.ai/autobuild/config-yml-reference | "per-repository policy contract for Obvious agents". "Draft-early PR creation cannot be disabled by config." Repo config is "runtime guidance only". Reserved keys are not consumed. |
| https://help.obvious.ai/autobuild/workspace-level-settings | With these settings, agents "cannot merge a pull request" and "cannot open a pull request". They "take effect immediately across all repositories and all agents". |
| https://help.obvious.ai/autobuild/customizing-code-review | "evaluates every pull request using a generic rubric". Score thresholds from 0 to 100; overlay.md; scoring-model.md. |
| https://help.obvious.ai/autobuild/blueprints | "an agent-built spec or structured document you can return to and keep refining". "Each edit updates the same Blueprint in place". |
| https://help.obvious.ai/autobuild/the-three-phase-spec-framework | Product, UX and technical phases. Skipping the technical spec lets the agent make "architecture decisions you never approved". |
| https://help.obvious.ai/autobuild/writing-custom-skills | SKILL.md with frontmatter. SKILL_CORE.md is "injected directly into the agent's preamble every turn". "Skills on unmerged PR branches are never loaded." |
| https://help.obvious.ai/autobuild/give-autobuild-repository-context | `obvious.md` gives "a short orientation". `.obvious/` "complements AGENTS.md; it does not replace it". |
| https://help.obvious.ai/autobuild/install-the-github-app | The app lets agents "read code, open pull requests, and run automated workflows". Access to all or selected repos. Sandbox sizes and install commands. |
| https://help.obvious.ai/autobuild/your-first-initiative | The spec can be written by the user or drafted by the agent. "Agents start picking up executables immediately." The build drawer shows status, duration and cost. |
| https://help.obvious.ai/integrations/secrets | Protected credentials: "The agent pauses and waits until you approve or deny". Approve once (15 min) or for the session (30 min). Scoped availability. |
| https://help.obvious.ai/account/spend-controls | Monthly budget, daily throttle, and per-user and per-project allocations. Admin only. |
| https://help.obvious.ai/workspace/credits | "25 free credits that expire after 14 days". Mentions Team plans and Fast/Deep modes; appears outdated. |
| https://help.obvious.ai/agents/overview | Agent vs Autobuild selector. "Max mode uses 2-3x the credits of Normal mode". User and project memory. |
| https://help.obvious.ai/agents/plans | Plan approval; Stop-before/after gates; Allow/Review/Block permission gates. Does not mention Autobuild. |
| https://help.obvious.ai/agents/checkpoints | Project snapshots, "Like version history, but for everything in your project". Code and rules are not mentioned. |
| https://help.obvious.ai/changelog | No AutoBuild-specific entries surfaced. Billing and usage entries in Oct–Nov 2026. (https://obvious.ai/whatsnew redirects here.) |
| https://www.obvious.ai/pricing | Standard $199 and Pro $499 per seat per month; 1x and 5x Fusion; extra credits $0.85; Enterprise offers BYO models and on-prem. |
| https://www.obvious.ai/fusion | "routes each step to the right class of model". Lists Claude 5.5 models, GPT-6.1 Sol and GLM 5.3. Max mode 2-3x credits. Provider failover. |
| https://www.obvious.ai/security | ISO 27001, SOC 2 Type II, GDPR/CCPA, HIPAA "Ready". AES-256, TLS 1.3, RBAC, audit logs. FAQ question on training data has no answer shown. |
| https://www.obvious.ai/enterprise | "No vendor lock-in — bring your own models". SAML/OIDC, SCIM, "Immutable audit logs for all actions". Secrets kept separate from prompts. |
| https://www.obvious.ai/vs | Cursor and Claude Code are "Complementary to Obvious". Says it "ran the same 8 tasks across every tool" but shows no results. |
| https://obvious.ai/llm-info | "AutoBuild is an autonomous software factory: agents plan, build, test, and review code to your standards." Founded 2023; funding line is ambiguous. |
| https://obvious.ai/newsroom | 7 posts from Nov 2025 to Mar 2026, including public beta (Jan 15, 2026). No AutoBuild announcement. |
| https://obvious.ai/build-blog | Six engineering posts with no dates shown: CI engine, closed-loop agents, orchestration, type safety, memory. |
| https://obvious.ai/build-blog/orchestration | 6,000+ PRs in a project; review SLA under 1 hour; human review is "the rate-limiting step". Marked "In Development". |
| https://obvious.ai/build-blog/closed-loop-agents | Headless browser against preview deploys; 16-screenshot sprites; observations "surfaced as structured observations". Marked "In Development". |
| https://obvious.ai/build-blog/ci-engine | 200 cores for about $1k/month; ZFS rollback; tests in 20–30 s; says AutoBuild built the system in one day. |
| https://www.obvious.ai/case-studies/paragon | "6 months → 48 hrs". "squeezed six months of work into 24 hours". 65 PRs "still climbing". |
| https://github.com/chromaui/chromatic-cli/pull/1521 | Bot onboarding PR adding the `.obvious/` contract. Tests 1534 passed. Records the sandbox snapshot. Merged Oct 14, 2026 after a maintainer review. |
| https://github.com/storybookjs/storybook/pulls?q=is%3Apr+author%3Aapp%2Fobvious-autobuild | 143 bot PRs; mix of merged, closed and open; some labeled `review:changes-requested`. |
| https://github.com/storybookjs/storybook/pull/37069 | Customer review bots flagged "Blocking" and P1 issues; the bot fixed most and declined some scope changes; PR closed. |
| https://github.com/storybookjs/storybook/pull/37512 | Merged by a human maintainer; Greptile noted "Tests were not run". |
| https://api.github.com/search/issues?q=author:app/obvious-autobuild+is:pr | My own count: 5,706 PRs, 5,007 merged, 201 closed unmerged, 498 open. |
| https://www.thoughtworks.com/radar/techniques/unattended-overnight-agentic-builds | Overnight agentic builds placed in "Trial" for teams with the foundations. Does not mention Obvious. Category context only. |
| https://www.zenml.io/llmops-database/building-autonomous-software-factories-with-ai-agents | About Factory (a different company), not Obvious. Excluded from evidence. |

**Pages that failed or were not usable:** GitHub GraphQL and per-PR REST calls were blocked in this session, so I used search-API counts and WebFetch of PR pages instead. Searches turned up no Hacker News, Reddit, X or YouTube content specific to AutoBuild. I did not open the newsroom Series A post, which is listed only via search results.
