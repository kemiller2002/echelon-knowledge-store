module Echelon.KnowledgeStore.Bench.Program

open System
open System.IO
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Bench

let private resultsDir (repoRoot: string) =
    Path.Combine(repoRoot, "research", "2026-knowledge-representation", "experiments", "results")

let private asOf = DateOnly(2026, 11, 16)

let private baseline (root: string) (out: string) (families: Tokens.Family list) =
    let files = Corpus.load true root Corpus.defaultRepositories
    let perRepo, dup = Baseline.measure root families files

    Report.json
        out
        "baseline.json"
        {|
            Repositories = perRepo
            Duplication = dup
        |}

    Report.text out "baseline.md" (Baseline.render families perRepo dup + "\n")
    printfn "baseline: %d files" files.Length

let private validate (root: string) (out: string) =
    let files = Corpus.load false root Corpus.defaultRepositories
    let typed = Corpus.records files
    let records = Corpus.allRecords files
    let g = Graph.build false records
    let vs = Validation.all asOf records g

    let byRule =
        vs
        |> List.countBy (fun v -> v.Rule, sprintf "%A" v.Severity)
        |> List.sortByDescending snd

    let byRepo =
        records
        |> List.groupBy _.Provenance.Repository
        |> List.map (fun (repo, rs) ->
            let ks = rs |> List.map Graph.key |> Set.ofList
            let mine = vs |> List.filter (fun v -> ks.Contains v.Record)

            [
                repo
                string rs.Length
                string (mine |> List.filter (fun v -> v.Severity = Blocking) |> List.length)
                string (mine |> List.filter (fun v -> v.Severity = Advisory) |> List.length)
                Report.pct
                    (rs
                     |> List.filter (fun r ->
                         mine |> List.exists (fun v -> v.Record = Graph.key r && v.Severity = Blocking))
                     |> List.length)
                    rs.Length
            ])

    let kinds =
        records
        |> List.countBy (_.Kind >> KnowledgeKind.name)
        |> List.sortByDescending snd

    let edges =
        g.Edges
        |> List.countBy (_.Kind >> RelationKind.name)
        |> List.sortByDescending snd

    let md =
        String.concat
            "\n\n"
            [
                $"Records with front-matter `id`: {typed.Length}; plus records identified only by file name: {records.Length - typed.Length} (vendor, archive, tests, fixtures, templates excluded); typed edges resolved: {g.Edges.Length}; unresolved relation targets: {g.Unresolved.Length}"
                Report.table [ "Kind"; "Records" ] (kinds |> List.map (fun (k, n) -> [ k; string n ]))
                Report.table [ "Relation"; "Edges" ] (edges |> List.map (fun (k, n) -> [ k; string n ]))
                Report.table
                    [ "Rule"; "Severity"; "Violations" ]
                    (byRule |> List.map (fun ((r, s), n) -> [ r; s; string n ]))
                Report.table [ "Repository"; "Records"; "Blocking"; "Advisory"; "Records with ≥1 blocking" ] byRepo
            ]

    // Per-violation detail only for public repositories.
    let publicViolations =
        vs
        |> List.filter (fun v -> not (Corpus.privateRepositories |> Set.exists (fun p -> v.Record.StartsWith(p + ":"))))

    Report.json
        out
        "validation.json"
        {|
            Summary = byRule |> List.map (fun ((r, s), n) -> {| Rule = r; Severity = s; Count = n |})
            PublicViolations =
                publicViolations
                |> List.map (fun v ->
                    {| v with
                        Severity = sprintf "%A" v.Severity
                    |})
        |}

    Report.text out "validation.md" (md + "\n")
    printfn "validate: %d records, %d violations" records.Length vs.Length

let private formats (root: string) (out: string) (families: Tokens.Family list) =
    let files = Corpus.load false root Corpus.defaultRepositories
    let records = Corpus.records files
    let costs = Formats.costs families files records
    let trips = Formats.roundTrips records
    let migration = Formats.migration records
    let primary = (Tokens.primary families).Name

    let baseFull = costs |> List.find (fun c -> c.Format = "original-markdown")

    let costRows =
        costs
        |> List.map (fun c ->
            [ c.Format; c.View; string c.Bytes ]
            @ (families |> List.map (fun f -> string c.Tokens[f.Name]))
            @ [ Report.f2 (float c.Tokens[primary] / float baseFull.Tokens[primary]) ])

    let migrationFailures = migration |> List.filter (snd >> Option.isSome)

    let failureKinds =
        migrationFailures
        |> List.countBy (snd >> Option.defaultValue "")
        |> List.sortByDescending snd

    let md =
        String.concat
            "\n\n"
            [
                $"Corpus: {records.Length} records."
                Report.table
                    ([ "Format"; "View"; "Bytes" ]
                     @ (families |> List.map _.Name)
                     @ [ $"× original ({primary})" ])
                    costRows
                Report.table
                    [ "Format"; "Records"; "Round-trip equal"; "First mismatch" ]
                    (trips
                     |> List.map (fun t ->
                         [ t.Format; string t.Records; Report.pct t.Equal t.Records; t.FirstMismatch ]))
                $"Lossless migration (original → record → canonical Markdown → record): {migration.Length - migrationFailures.Length}/{migration.Length} identical."
                Report.table
                    [ "Differing fields"; "Records" ]
                    (failureKinds |> List.map (fun (k, n) -> [ k; string n ]))
            ]

    Report.json
        out
        "formats.json"
        {|
            Costs = costs
            RoundTrips = trips
            MigrationFailures = failureKinds
        |}

    Report.text out "formats.md" (md + "\n")
    printfn "formats: done"

let private retrieval (root: string) (out: string) (families: Tokens.Family list) =
    let publicRepos =
        Corpus.defaultRepositories
        |> List.filter (Corpus.privateRepositories.Contains >> not)

    let files = Corpus.load false root publicRepos
    let records = Corpus.allRecords files
    let e = RetrievalBench.env (Tokens.primary families).Count files records
    let tasks = RetrievalBench.tasks e
    let scores = RetrievalBench.score e tasks

    let rows =
        scores
        |> List.sortBy (fun s -> s.TaskKind, s.Strategy)
        |> List.map (fun s ->
            [
                s.TaskKind
                s.Strategy
                string s.Tasks
                Report.f3 s.Recall
                Report.f3 s.Precision
                $"{s.Complete}/{s.Tasks}"
                Report.f1 s.MeanTokens
                Report.f1 s.MeanToolCalls
            ])

    let taskCounts = tasks |> List.countBy _.Kind

    let md =
        String.concat
            "\n\n"
            [
                $"Public repositories only ({publicRepos.Length}); {records.Length} records; {tasks.Length} tasks."
                Report.table [ "Task kind"; "Tasks" ] (taskCounts |> List.map (fun (k, n) -> [ k; string n ]))
                Report.table
                    [
                        "Task kind"
                        "Strategy"
                        "n"
                        "recall"
                        "precision"
                        "complete"
                        "mean tokens"
                        "mean tool calls"
                    ]
                    rows
            ]

    Report.json out "retrieval-tasks.json" (tasks |> List.map (fun t -> {| t with Gold = Set.toList t.Gold |}))
    Report.json out "retrieval.json" scores
    Report.text out "retrieval.md" (md + "\n")
    printfn "retrieval: %d tasks" tasks.Length

let private dependencies (repoRoot: string) (root: string) (out: string) =
    let repos = Corpus.defaultRepositories
    let records = Corpus.load false root repos |> Corpus.allRecords

    let goldPath =
        Path.Combine(repoRoot, "research", "2026-knowledge-representation", "baseline", "cross-repo-edges.json")

    let m1 = DependencyBench.typedGraph records
    let m2 = DependencyBench.keyword root repos
    let m3 = DependencyBench.manifests root repos

    let methods =
        [
            "M1 typed front-matter graph", m1
            "M2 prose keyword search", m2
            "M3 manifests (packages, workflows, Praxis install)", m3
            "M4 typed graph ∪ manifests", Set.union m1 m3
            "M5 keyword ∪ manifests", Set.union m2 m3
        ]

    let scenarios =
        [
            "all", (fun (_: string) -> true)
            "fides", (=) "fides"
            "forma", (=) "forma"
            "clarity", (=) "clarity-framework"
            "edf", (=) "echelon-diagnostic-framework"
            "dokimos", (=) "dokimos"
        ]

    let scores =
        [
            for goldName, hard in [ "all verified edges", false; "hard dependencies only", true ] do
                let g = DependencyBench.gold goldPath hard

                for scenario, isTarget in scenarios do
                    let gs = g |> Set.filter (snd >> isTarget)

                    for name, predicted in methods do
                        DependencyBench.score name goldName scenario (predicted |> Set.filter (snd >> isTarget)) gs
        ]

    let rows =
        scores
        |> List.map (fun s ->
            [
                s.Gold
                s.Scenario
                s.Method
                string s.GoldPairs
                string s.Predicted
                string s.TruePositives
                Report.f2 s.Precision
                Report.f2 s.Recall
            ])

    let md =
        String.concat
            "\n\n"
            [
                "Repository-level dependency detection against the independently verified audit edges (pairs from → to)."
                Report.table
                    [
                        "Gold"
                        "Scenario (target)"
                        "Method"
                        "gold pairs"
                        "predicted"
                        "TP"
                        "precision"
                        "recall"
                    ]
                    rows
            ]

    Report.json out "dependencies.json" scores
    Report.text out "dependencies.md" (md + "\n")
    printfn "dependencies: done"

let private trialsDir () =
    Environment.GetEnvironmentVariable "EKS_TRIALS_DIR"
    |> Option.ofObj
    |> Option.defaultValue "benchmarks/out/trials"

let private publicCorpus (root: string) =
    let publicRepos =
        Corpus.defaultRepositories
        |> List.filter (Corpus.privateRepositories.Contains >> not)

    let files = Corpus.load false root publicRepos
    files, Corpus.allRecords files

let private prepareTrials (root: string) (out: string) (families: Tokens.Family list) =
    let files, records = publicCorpus root
    let dir = trialsDir ()
    let qs = Trials.prepare (Tokens.primary families).Count files records dir

    let sizes =
        [
            "md", "md/corpus"
            "plain", "plain/corpus"
            "md-index", "md-index/corpus"
            "jsonl", "jsonl/corpus"
            "dsl", "dsl/corpus"
            "compiled", "compiled/corpus"
        ]
        |> List.map (fun (name, sub) ->
            let fs =
                Directory.EnumerateFiles(Path.Combine(dir, sub), "*", SearchOption.AllDirectories)
                |> List.ofSeq

            [
                name
                string fs.Length
                string (fs |> List.sumBy (File.ReadAllText >> (Tokens.primary families).Count))
            ])

    Report.text out "trial-materials.md" (Report.table [ "Condition"; "Files"; "Tokens (o200k_base)" ] sizes + "\n")
    Report.json out "trial-questions.json" qs
    printfn "trials: %d questions in %s" qs.Length dir

let private scoreTrials (root: string) (out: string) =
    let _, records = publicCorpus root
    let qs = Trials.questions (Graph.build false records)
    let scores = Trials.score qs (Path.Combine(trialsDir (), "answers"))
    let conditionOf (c: string) = c.Substring(0, c.IndexOf "-b")

    let runOf (c: string) =
        c.Substring(c.IndexOf "-b" + 3)
        |> fun s -> if s = "" then "r1" else s.TrimStart('-')

    let rows =
        scores
        |> List.groupBy (fun s -> conditionOf s.Condition, runOf s.Condition)
        |> List.sort
        |> List.map (fun ((c, run), ss) ->
            [
                c
                run
                string ss.Length
                Report.f3 (ss |> List.averageBy _.Score)
                $"{ss |> List.filter _.Exact |> List.length}/{ss.Length}"
            ])

    let byKind =
        scores
        |> List.groupBy (fun s -> conditionOf s.Condition, s.Kind)
        |> List.sort
        |> List.map (fun ((c, k), ss) -> [ c; k; string ss.Length; Report.f3 (ss |> List.averageBy _.Score) ])

    Report.json out "trial-scores.json" scores

    Report.text
        out
        "trial-scores.md"
        (Report.table [ "Condition"; "Run"; "Questions"; "Mean score"; "Exact" ] rows
         + "\n\n"
         + Report.table [ "Condition"; "Question kind"; "n"; "Mean score" ] byKind
         + "\n")

    printfn "scored %d answers" scores.Length

let private ablation (root: string) (out: string) (families: Tokens.Family list) =
    let count = (Tokens.primary families).Count
    let files, records = publicCorpus root
    let abl = AblationBench.run count records
    let strict = AblationBench.strict (Corpus.records files)

    let drift =
        AblationBench.drift files records
        |> List.sortByDescending (fun d -> d.Missing + d.Unknown)

    let comp = AblationBench.compilation count records

    let md =
        String.concat
            "\n\n"
            [
                "## Feature ablation (public corpus; gold = full typed system)"
                Report.table
                    [ "Task"; "Variant"; "n"; "recall"; "precision"; "mean tokens" ]
                    (abl
                     |> List.map (fun a ->
                         [
                             a.TaskKind
                             a.Variant
                             string a.Tasks
                             Report.f3 a.Recall
                             Report.f3 a.Precision
                             Report.f1 a.MeanTokens
                         ]))
                "## Strict per-kind types: share of real records representable"
                Report.table
                    [ "Kind"; "Records"; "Representable"; "Reasons (count)" ]
                    (strict
                     |> List.map (fun s ->
                         [
                             s.Kind
                             string s.Records
                             Report.pct s.Representable s.Records
                             s.Reasons |> List.map (fun (r, n) -> $"{r} ({n})") |> String.concat "; "
                         ]))
                "## Hand-maintained index drift (documents enumerating ≥3 ids of a family)"
                Report.table
                    [
                        "Repository"
                        "Document"
                        "Family"
                        "listed"
                        "actual"
                        "missing from doc"
                        "unknown ids in doc"
                    ]
                    (drift
                     |> List.truncate 40
                     |> List.map (fun d ->
                         [
                             d.Repository
                             d.Document
                             d.Family
                             string d.Listed
                             string d.Actual
                             string d.Missing
                             string d.Unknown
                         ]))
                $"Documents compared: {drift.Length}; with ≥1 missing record: {drift |> List.filter (fun d -> d.Missing > 0) |> List.length}; with ≥1 unknown id: {drift |> List.filter (fun d -> d.Unknown > 0) |> List.length}."
                "## Generated index (knowledge compilation)"
                Report.table
                    [
                        "Records"
                        "Index tokens"
                        "Compile ms"
                        "Deterministic (input order permuted)"
                        "Lines changed by one status edit"
                    ]
                    [
                        [
                            string comp.Records
                            string comp.IndexTokens
                            Report.f1 comp.CompileMilliseconds
                            string comp.Deterministic
                            string comp.ChangedLinesForOneStatusEdit
                        ]
                    ]
            ]

    let epi = AblationBench.epistemic records

    let epiMd =
        "\n\n## Epistemic state of claims (hypotheses, theories): declared status vs evidence-derived standing\n\n"
        + Report.table
            [
                "Repository"
                "Kind"
                "Declared status"
                "Confidence"
                "Evidence standing"
                "Records"
            ]
            (epi
             |> List.map (fun e -> [ e.Repository; e.Kind; e.Status; e.Confidence; e.Standing; string e.Records ]))

    Report.json
        out
        "ablation.json"
        {|
            Ablation = abl
            Strict = strict
            Drift = drift
            Compilation = comp
            Epistemic = epi
        |}

    Report.text out "ablation.md" (md + epiMd + "\n")
    printfn "ablation: done"

let private compilerBench (root: string) (out: string) (families: Tokens.Family list) =
    let count = (Tokens.primary families).Count
    let publicFiles, _ = publicCorpus root
    let src = CompilerBench.sources publicFiles

    let allSrc =
        Corpus.load false root Corpus.defaultRepositories |> CompilerBench.sources

    let timings = CompilerBench.timings src
    let timingsAll = CompilerBench.timings allSrc |> List.truncate 1

    let comp, _ =
        Echelon.KnowledgeStore.Compiler.Pipeline.compile CompilerBench.config Map.empty src

    let muts = CompilerBench.mutations src
    let opt = CompilerBench.optimisation count comp
    let loss = CompilerBench.lossless src comp
    let res = CompilerBench.resolution comp

    let byLevel =
        comp.Diagnostics
        |> List.countBy (fun d -> d.Code, sprintf "%A" d.Level)
        |> List.sort

    let md =
        String.concat
            "\n\n"
            [
                $"Public corpus: {src.Length} Markdown files, {comp.Records.Length} records. Timings are medians of 5 runs on the research container (single process)."
                Report.table
                    [ "Scenario"; "Files"; "Median ms"; "Warm output = cold output" ]
                    ((timings
                      @ (timingsAll
                         |> List.map (fun t ->
                             { t with
                                 Scenario = t.Scenario + " — all 13 repositories"
                             })))
                     |> List.map (fun t ->
                         [
                             t.Scenario
                             string t.Files
                             Report.f1 t.MedianMs
                             t.OutputsEqualCold |> Option.map string |> Option.defaultValue "–"
                         ]))
                "## Diagnostics (public corpus)"
                Report.table [ "Code"; "Level"; "Count" ] (byLevel |> List.map (fun ((c, l), n) -> [ c; l; string n ]))
                "## Agent-edit mutation test (25 + 1 mistakes from the adversarial review)"
                Report.table
                    [ "Mutation"; "Record"; "Result"; "Codes" ]
                    (muts |> List.map (fun m -> [ m.Mutation; m.Record; m.Result; m.Codes ]))
                (let n r =
                    muts |> List.filter (fun m -> m.Result = r) |> List.length

                 $"""Blocking {n "BLOCKING"}, flagged {n "FLAGGED"}, advisory-only {n "ADVISORY-ONLY"}, undetected {n "UNDETECTED"} of {muts.Length}.""")
                "## Task packages for every record (optimisation correctness)"
                Report.table
                    [
                        "Packages"
                        "Complete"
                        "Ledger entries"
                        "Lost without ledger"
                        "Target lost"
                        "Mean tokens"
                        "Mean compile ms"
                    ]
                    [
                        [
                            string opt.Packages
                            string opt.Complete
                            string opt.LedgerEntries
                            string opt.Unaccounted
                            string opt.TargetLost
                            Report.f1 opt.MeanTokens
                            Report.f2 opt.MeanCompileMs
                        ]
                    ]
                "## Source-level losslessness (original front matter vs regenerated)"
                $"{loss.IdenticalFrontMatter}/{loss.Records} records regenerate an identical front-matter tree."
                Report.table
                    [ "Field differing"; "Records" ]
                    (loss.DifferingFields |> List.map (fun (k, n) -> [ k; string n ]))
                "## Reference resolution"
                Report.table
                    [
                        "Relations"
                        "Resolved"
                        "Unresolved"
                        "…id-like"
                        "…paths"
                        "…other (INPUT-*, commits, prose)"
                    ]
                    [
                        [
                            string res.Relations
                            string res.Resolved
                            string res.Unresolved
                            string res.UnresolvedIdLike
                            string res.UnresolvedPaths
                            string res.UnresolvedOther
                        ]
                    ]
            ]

    Report.json
        out
        "compiler.json"
        {|
            Timings = timings
            TimingsAll = timingsAll
            Mutations = muts
            Optimisation = opt
            Lossless = loss
            Resolution = res
        |}

    Report.text out "compiler.md" (md + "\n")
    printfn "compiler: done"

[<EntryPoint>]
let main argv =
    let repoRoot = Directory.GetCurrentDirectory()

    let root =
        Environment.GetEnvironmentVariable "EKS_CORPUS_ROOT"
        |> Option.ofObj
        |> Option.defaultValue "/home/user/kemiller2002"

    let out = resultsDir repoRoot

    let families =
        Tokens.families (Path.Combine(repoRoot, "benchmarks", "out", "tok", "llama.model"))

    let run =
        function
        | "baseline" -> baseline root out families
        | "validate" -> validate root out
        | "formats" -> formats root out families
        | "retrieval" -> retrieval root out families
        | "dependencies" -> dependencies repoRoot root out
        | "trials" -> prepareTrials root out families
        | "ablation" -> ablation root out families
        | "compiler" -> compilerBench root out families
        | "score-trials" -> scoreTrials root out
        | other -> eprintfn "unknown command %s" other

    match List.ofArray argv with
    | [] ->
        [ "baseline"; "validate"; "formats"; "retrieval"; "dependencies"; "ablation" ]
        |> List.iter run
    | cmds -> cmds |> List.iter run

    0
