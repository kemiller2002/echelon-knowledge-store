# Literature: Agent Memory, RAG, GraphRAG, Hybrid Retrieval, Caching, Compression

Mission EKS-RESEARCH-2026-001, workstream E. Compiled 2026-11-16.

Conventions. **[Cited]** is a fact reported by the retrieved source. **[Inference]** is my reasoning, not stated by a source. Source IDs map to `evidence/sources-agents.csv`. Where I could only read an abstract or a search-result snippet, the claim is marked "abstract only" or "snippet only". Numbers are as reported by the authors and are not reproduced by us. Vendor-authored results are flagged.

## 0. Bottom line for the reader in a hurry

1. Retrieval structure matters less than task type. Vanilla RAG with reranking beat most graph methods on simple fact lookup; graph methods won on multi-hop reasoning and summarization, at large token cost (SRC-AG-043, SRC-AG-042).
2. For code and repository knowledge, a well-designed agent harness using grep/glob matched or beat embedding retrieval in the sources found, but those sources are mostly blogs or secondary summaries (SRC-AG-029, SRC-AG-030, SRC-AG-031).
3. Hybrid lexical+dense retrieval, with rank fusion or reranking, is the best-supported default for document retrieval (SRC-AG-046, SRC-AG-047).
4. Agent-memory claims are mostly vendor-authored and mutually contested (Mem0 vs Letta). Treat headline numbers as unverified (SRC-AG-035, SRC-AG-037).
5. No retrieved source tests typed/structured knowledge (schemas, typed records) against Markdown for coding-agent task success. That is the gap EKS fills.

## 1. Retrieval-augmented generation and chunking

### 1.1 Chunking
- **[Cited]** Vectara-affiliated authors compared fixed-size and semantic chunking on document retrieval, evidence retrieval and answer generation. They report that the computational cost of semantic chunking is not justified by consistent gains; on realistic (non-stitched) datasets fixed-size chunking consistently did as well or better (SRC-AG-049; seen via search result, full text not read). The authors work for a RAG vendor, so "independent" is only partly true.
- **[Cited]** Anthropic describes chunks "usually no more than a few hundred tokens" and says chunk size, boundaries and overlap affect retrieval, with no optimal size named. Its Contextual Retrieval prepends about 50-100 tokens of document-level context to each chunk (SRC-AG-046).
- **[Cited, vendor-reported]** Contextual embeddings cut the top-20 retrieval failure rate by 35% (5.7% to 3.7%); contextual embeddings plus contextual BM25 by 49% (to 2.9%); adding a reranker by 67% (to 1.9%). One-time cost is about $1.02 per million document tokens under stated assumptions with prompt caching (SRC-AG-046). These are Anthropic's own measurements; I found no independent replication.
- **[Inference]** For EKS, the unit of knowledge is a typed record (requirement, ADR, finding), not an arbitrary text window. Chunking research mostly addresses unstructured prose. Typed records sidestep boundary choice but inherit the lesson that a record stripped of its context (project, status, supersession) retrieves poorly. Contextual-retrieval-style prefixes are a natural analogue to front matter.

### 1.2 Agentic / just-in-time retrieval
- **[Cited]** Anthropic recommends agents keep lightweight identifiers (file paths, stored queries) and load content at runtime via tools, with a hybrid of upfront retrieval and autonomous exploration often best. Trade-off: slower, but fresher and cheaper in context (SRC-AG-012).
- **[Cited, secondary]** A practitioner write-up of an Augment talk says grep and find were sufficient on SWE-bench, agent persistence offset weaker tools, and embeddings matter more for large codebases, unstructured content (Slack, docs) and third-party code. It gives no benchmark scores (SRC-AG-029).
- **[Cited, listing/abstract level]** A small-model ablation on SWE-bench Verified (grep only, plus embeddings, plus graph tools, all) reports gains from embedding/graph tools of 17-21 points for 1.7B-4B models, falling to 4-5 points at 32B; each extra hop in multi-hop localization cut odds of success to 0.50-0.67 of the previous level regardless of tools (SRC-AG-030; read via a listing page, not the paper).
- **[Cited, secondary, unverified]** Blog posts attribute to Claude Code's creator the statement that early versions used a local vector database and agentic search worked better (SRC-AG-031; search-result summary only; primary sources not reached).
- **[Inference]** Findings on code localization do not transfer automatically to requirements/ADR knowledge: code has lexical anchors (identifiers) that grep exploits, while decision rationale often lacks literal overlap with the query. NoLiMa-style evidence (workstream F file) suggests literal-match advantages shrink when queries are paraphrases.

## 2. Agent memory architectures

Taxonomy note. CoALA proposes modular memory components for language agents (SRC-AG-034; abstract only, so I did not retrieve its working/episodic/semantic/procedural definitions). The mapping below is **[Inference]** using common usage:

| Memory kind | Typical realization | Relevance to EKS |
|---|---|---|
| Working | the context window | what the compiler outputs |
| Episodic | logs of past interactions or runs | agent run traces |
| Semantic/declarative | facts, requirements, ADRs | core EKS content |
| Procedural | skills, instructions, AGENTS.md | agent-instruction records |

- **MemGPT** **[Cited]**: "virtual context management" borrowed from OS memory hierarchies; the LLM moves data between fast and slow tiers and uses interrupts. Evaluated on large-document analysis and multi-session chat; the abstract reports no numbers (SRC-AG-032).
- **Generative Agents** **[Cited]**: complete natural-language record of experience, reflection that synthesizes higher-level memories, and retrieval-driven planning; ablations show observation, planning and reflection each contribute to believability, with no numbers on the abstract page (SRC-AG-033). Note the metric is believability of simulated behavior, not task success.
- **Letta** **[Cited]**: current docs describe labeled memory blocks and a git-tracked "MemFS" memory (SRC-AG-038). I did not retrieve its archival/recall documentation.
- **LongMemEval** **[Cited]**: 500 questions across five abilities (extraction, multi-session reasoning, temporal reasoning, knowledge update, abstention); commercial assistants and long-context LLMs show about a 30% accuracy drop over sustained interaction. Proposed fixes: session decomposition, fact-augmented key expansion, time-aware query expansion (SRC-AG-039).
- **Mem0** **[Cited, vendor-authored]**: on LOCOMO, 26% relative improvement in LLM-as-judge over OpenAI's memory, 91% lower p95 latency and over 90% token-cost saving versus full context; graph variant about 2% above base (SRC-AG-035).
- **Zep/Graphiti** **[Cited, vendor-authored]**: temporal knowledge graph; DMR 94.8% vs 93.4% for MemGPT; LongMemEval accuracy up to 18.5% higher with 90% lower latency than baselines (SRC-AG-036).
- **Contested comparisons** **[Cited]**: Letta reports 74.0% on LoCoMo for a gpt-4o-mini agent using plain files plus grep/search tools, versus 68.5% reported by Mem0 for its graph variant, and says Mem0 did not clarify its MemGPT comparison. Letta itself caveats that the setup is one model, partly scaffolded, and that LoCoMo may be a poor memory measure (SRC-AG-037). Both sides are vendors.
- **[Inference]** The most transferable signal is that a simple filesystem plus search tools was competitive with specialized memory layers on one benchmark. That supports a "compile to files the agent can grep" baseline arm in EKS experiments, and argues against assuming a bespoke store is needed.
- **[Inference]** Agent memory benchmarks are conversational. None retrieved evaluates software-engineering knowledge (requirements/ADR recall in code tasks).

## 3. GraphRAG, LightRAG and when graphs beat vector/BM25

- **GraphRAG (Microsoft)** **[Cited]**: LLM extracts an entity graph, pre-summarizes communities, answers global questions by map-reduce over summaries; evaluated for comprehensiveness and diversity on global sensemaking over roughly 1M-token corpora. Abstract gives no win rates (SRC-AG-040). Designed for corpus-wide questions, not fact lookup.
- **LightRAG** **[Cited]**: graph structure plus vectors, dual-level (low/high) retrieval, and an incremental update algorithm intended to integrate new data without full rebuild. Abstract claims improved accuracy and efficiency without numbers (SRC-AG-041).
- **Independent evaluation 1: RAG vs GraphRAG** (Han et al.) **[Cited]** (SRC-AG-042; HTML version, Llama-3.1-8B unless noted):
  - NQ single-hop F1: RAG 64.78, Community-GraphRAG (Local) 63.01, HippoRAG2 61.03. HotpotQA multi-hop F1: HippoRAG2 63.01, Community-GraphRAG (Local) 61.66, RAG 60.04.
  - MultiHop-RAG accuracy: HippoRAG2 70.27, RAG 67.02. By query type, temporal: RAG 30.70 vs Community-GraphRAG 50.60; null: RAG 96.01 vs 80.07.
  - 13.6% of queries were answered correctly only by GraphRAG and 11.6% only by RAG, so the methods are complementary; running both ("integration") improved the best baseline by 6.4% at higher cost.
  - Token-matched RAG scored 71.01 vs GraphRAG 71.17, suggesting input length explains much of the gap. Graph quality mattered: with a 70B reader, GraphRAG on a GPT-4o-built graph scored 75.08 vs 71.17 with a GPT-4o-mini-built graph.
  - Indexing time on MultiHop-RAG: 135 s (RAG) vs 5,560 s (Community-GraphRAG) and 7,702 s (KG-GraphRAG).
  - LLM-judge position bias was large: GPT-4o favored GraphRAG-Global 59% in one order and 0% when order was swapped on one comparison.
- **Independent evaluation 2: GraphRAG-Bench "When to use graphs in RAG"** **[Cited]** (SRC-AG-043; first ~100k characters read). The abstract states GraphRAG frequently underperforms vanilla RAG on many real-world tasks. Reported numbers on the novel dataset:
  - Simple fact retrieval: RAG with reranking 60.92% vs MS-GraphRAG 49.29%; context relevance 77.8% vs 27.3%.
  - Complex reasoning: MS-GraphRAG 50.93% vs RAG+rerank 42.93%; HippoRAG2 53.38%. Summarization: 64.40% vs 51.30%.
  - On the medical dataset the gap on fact retrieval nearly vanished (HippoRAG2 66.28% vs 64.73%).
  - Average tokens per query: vanilla RAG 879; HippoRAG2 1,008; RAPTOR 3,441; LightRAG 100,832; MS-GraphRAG global 331,375.
- **Industry case** **[Cited, authors' own deployment]**: a knowledge graph over structured support tickets (preserving intra-ticket structure and inter-ticket links) reported 77.6% MRR gain over a text baseline and 28.6% lower median resolution time after about six months at LinkedIn (SRC-AG-045). This is the closest retrieved analogue to EKS: structure was in the source data, not extracted by an LLM.
- **GraphRAG-Bench (Xiao et al.)** SRC-AG-044: abstract only; no baseline comparison retrieved.
- **[Inference]** Pattern across sources: graphs help when the question needs relations or aggregation across items; they hurt or cost much more when a single passage answers it. EKS records already carry explicit typed links (supersedes, implements, depends-on), which removes the LLM extraction step whose quality the Han et al. result shows matters. This favours graph-style traversal over typed links for relational queries and plain retrieval for lookups.

## 4. Hybrid retrieval, fusion and reranking

- **RRF** **[Cited]**: score = sum over rankers of 1/(rank + k); Azure documents k=60 as a value that performs well; fusion is unsupervised (SRC-AG-047). Cormack et al. (SIGIR 2009) report RRF beat the best single system and Condorcet fuse; TREC Robust MAP 0.3686 vs 0.3652 for Condorcet, so margins can be small (SRC-AG-048; search-result summary, full text not read).
- **[Cited]** Anthropic reports BM25 plus embeddings beats embeddings alone but gives no standalone figure; adding contextual BM25 and reranking gave the failure-rate reductions in section 1.1 (SRC-AG-046).
- **[Inference]** Identifier-heavy corpora (requirement IDs, ADR numbers, repo names) are exactly where BM25 is strong, so a lexical arm is cheap insurance for EKS. I did not find a benchmark of hybrid retrieval on engineering-decision corpora.

## 5. Semantic caching

- **[Cited]** GPTCache's own docs acknowledge false positives on hits and false negatives on misses. MeanCache reports GPTCache had 54 false hits on queries that should reach the LLM, versus 3 for MeanCache, attributing it to ignoring context (SRC-AG-050; search-result snippet only). Another snippet cites a test-generation study finding incorrect hits at a 0.9 similarity threshold.
- **[Inference]** Semantic caches are tuned for repeated user questions. For coding agents, near-identical queries with different repository state would return stale or wrong answers, so exact-key caching on compiled context (hash of task + record versions) is safer. Not tested by any source I retrieved.

## 6. Knowledge compression

- **[Cited]** LLMLingua reports up to 20x prompt compression with little performance loss on GSM8K, BBH, ShareGPT and Arxiv-March23 (SRC-AG-051; abstract only).
- **[Cited]** Chroma reports focused ~300-token prompts consistently beat full ~113k-token prompts on LongMemEval-derived tasks (SRC-AG-002).
- **[Cited]** Anthropic reports compaction examples shrinking histories from about 100k to 2-3k tokens, and vendor-measured gains of 29% (context editing) and 39% (with memory tool) on an internal agentic search evaluation, plus 84% token reduction on a 100-turn task (SRC-AG-014, SRC-AG-015). Methodology not disclosed.
- **[Inference]** Compression in LLMLingua is token-level and lossy; for requirements with normative wording ("shall"), lossy compression is risky. Compile-time selection (dropping irrelevant records) is lossless per record and is the safer EKS form of compression.

## 7. Incremental updates and staleness

- **[Cited]** LightRAG claims incremental graph updates (SRC-AG-041). Graphiti claims dynamic synthesis of new data with temporal handling (SRC-AG-036, abstract only). Mechanism details not retrieved for either.
- **[Cited]** Cursor and Copilot maintain indexes that update incrementally (Copilot: first index of a large repo up to about 60 s, later updates typically within seconds) (SRC-AG-022). Cursor describes a local "Instant Grep" index (SRC-AG-024). Claude Code reads files at use time rather than from a prebuilt index (SRC-AG-012, SRC-AG-031 secondary).
- **[Inference]** For EKS, a content-hash per record and per compiled context gives deterministic invalidation; LLM-extracted graphs (GraphRAG) re-pay extraction cost on change, typed records do not.

## 8. Gaps

- No independent study of typed/structured knowledge versus Markdown for coding-agent task success.
- No retrieved comparison of hybrid retrieval on ADR/requirements corpora.
- Memory-system numbers are vendor-authored and conflicting.
- Several items rest on abstracts or search snippets (flagged above).

## Implications for EKS (falsifiable hypotheses)

H-M1. On EKS tasks needing one record (e.g. "what is the retention requirement for X"), BM25-or-hybrid retrieval over typed records will match or beat a graph-traversal arm on accuracy at fewer tokens; falsified if the graph arm exceeds the lookup arm by more than the pre-registered margin on single-record tasks. (Basis: SRC-AG-043.)
H-M2. On tasks requiring 2+ linked records (e.g. ADR supersession chains, requirement-to-implementation tracing), traversal over explicit typed links will beat flat top-k retrieval by a pre-registered margin; falsified if not. (Basis: SRC-AG-042, SRC-AG-043; [Inference] that explicit links remove the extraction-quality problem.)
H-M3. A grep-over-files arm (agent searches the compiled Markdown tree) will perform within the noise band of an embedding arm on identifier-bearing queries and worse on paraphrased queries. Falsified if embeddings show no advantage on paraphrased queries. (Basis: SRC-AG-029, SRC-AG-011.)
H-M4. Adding a lexical arm (BM25) to a dense arm via RRF improves recall@k on EKS queries containing record IDs; falsified if recall@k does not improve. (Basis: SRC-AG-046, SRC-AG-047.)
H-M5. A task-specific compiled context (selected records only) beats loading the full relevant repository knowledge at equal correctness requirement while using fewer input tokens; falsified if full-load achieves higher success. (Basis: SRC-AG-002, SRC-AG-043 token costs.)
H-M6. Content-hash-keyed exact caching of compiled contexts yields zero stale-context errors in a changing-repo test, while a semantic (embedding-similarity) cache produces measurable stale or wrong hits; falsified if the semantic cache shows no stale hits at the chosen threshold. (Basis: SRC-AG-050 [snippet].)
H-M7. A flat-file baseline (plain Markdown plus grep tools) is within a pre-registered margin of any bespoke memory/store arm on EKS tasks; falsified if the bespoke arm wins by more than the margin. (Basis: SRC-AG-037, vendor-authored, contested.)
