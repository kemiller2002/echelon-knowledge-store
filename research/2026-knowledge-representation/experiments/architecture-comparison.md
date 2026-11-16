# Architecture comparison

| Architecture | Accuracy (trials) | Agent time | Validation | Cost to build/maintain | Verdict |
|---|---|---|---|---|---|
| A Markdown + front matter, F# validation | 1.00 (T1), 0.99 (T2) | baseline | Praxis `validate` + EKS rules | lowest (exists) | ADOPT (status quo) |
| B JSON/JSONL + F# schemas | 0.98 (T1) | ≈A | same | rewrite of all records | REJECT as source; generated export only |
| C F# source records | not trialled; FCS catches shape/existence only | — | compile errors | ids become schema; executable-looking data | REJECT |
| D compact DSL | 1.00 (T1) | ≈A | parser diagnostics | new language | REJECT (−0.7% tokens) |
| E hybrid typed graph + compiler (compiler-first) | 1.00 (T1), 1.00 after fix (T2) | ½ of A | strongest (22/26 mutations) | ~1,100 lines + calibration; correctness risk | ADOPT **as an extension of Praxis registry build**, staged |
| F simpler: registries (as Praxis) + inverse links + traversal | C in T2: 0.99 | ≈A | Praxis + added rules | ~150–250 lines (reviewer estimate) | strong alternative; the first stage of E |
| Optimised Markdown retrieval (BM25 excerpts) | 0.47 (T2) | < A | none | low | REJECT for relational work |
