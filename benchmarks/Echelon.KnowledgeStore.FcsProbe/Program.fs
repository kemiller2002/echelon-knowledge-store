module Echelon.KnowledgeStore.FcsProbe.Program

open System
open System.Diagnostics
open System.IO
open FSharp.Compiler.CodeAnalysis
open FSharp.Compiler.Diagnostics
open FSharp.Compiler.Text
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Compiler
open Echelon.KnowledgeStore.FcsProbe

let private checker = FSharpChecker.Create()

/// Parses and type-checks source text as a script. Nothing is compiled to an
/// assembly or executed. Returns (error count, error messages, milliseconds).
let check (source: string) =
    let file = "/tmp/knowledge.fsx"
    let text = SourceText.ofString source
    let sw = Stopwatch.StartNew()

    let options, _ =
        checker.GetProjectOptionsFromScript(file, text, assumeDotNetFramework = false, useSdkRefs = true)
        |> Async.RunSynchronously

    let _, answer =
        checker.ParseAndCheckFileInProject(file, 0, text, options)
        |> Async.RunSynchronously

    sw.Stop()

    let errors =
        match answer with
        | FSharpCheckFileAnswer.Succeeded r ->
            r.Diagnostics
            |> Array.filter (fun d -> d.Severity = FSharpDiagnosticSeverity.Error)
        | FSharpCheckFileAnswer.Aborted -> [||]

    errors.Length,
    errors
    |> Array.truncate 3
    |> Array.map (fun d -> $"FS{d.ErrorNumber:D4}: {d.Message}")
    |> List.ofArray,
    sw.Elapsed.TotalMilliseconds

[<EntryPoint>]
let main argv =
    let root =
        if argv.Length > 0 then
            argv[0]
        else
            "/home/user/kemiller2002"

    let repos =
        [
            "aegis"
            "arca"
            "dokimos"
            "echelon-diagnostic-framework"
            "echelon-engineering-system"
            "fides"
            "forma"
            "mercatus"
            "percepta"
            "praxis"
            "signal"
        ]

    let excluded (p: string) =
        let q = "/" + p

        [
            "/.git/"
            "/node_modules/"
            "/bin/"
            "/obj/"
            "/vendor/"
            "/archive/"
            "/tests/"
            "/fixtures/"
            "/templates/"
            "/starter/"
        ]
        |> List.exists q.Contains

    let files =
        repos
        |> List.collect (fun repo ->
            let dir = Path.Combine(root, repo)

            Directory.EnumerateFiles(dir, "*.md", SearchOption.AllDirectories)
            |> Seq.map (fun f -> Path.GetRelativePath(dir, f).Replace('\\', '/'))
            |> Seq.filter (excluded >> not)
            |> Seq.sort
            |> Seq.map (fun p -> Pipeline.sourceFile repo p (File.ReadAllText(Path.Combine(dir, p))))
            |> List.ofSeq)

    let cfg =
        {
            AsOf = DateOnly(2026, 11, 16)
            StaleAfterDays = 180
            Visibility = Map.empty
            Audience = Private
            SupportedSchema = 1
            ExistingPaths = None
        }

    let c, _ = Pipeline.compile cfg Map.empty files
    // One record per id (vendored copies collapsed) — F# needs unique case names.
    let rs = c.Records |> List.distinctBy _.Id |> List.sortBy _.Id
    let ids = rs |> List.map _.Id
    let known = Set.ofList ids
    let body, skipped = Generate.records known rs
    let source = Generate.prelude ids + body
    let lines = source.Split('\n').Length

    let warm = check (Generate.prelude [ "X-1" ] + "\nlet records : Record list = []\n")
    let n0, e0, t0 = check source
    let n1, _, t1 = check source

    let mutate (name: string) (f: string -> string) =
        let s = f source
        let n, es, ms = check s

        name,
        (if s = source then "mutation did not apply"
         elif n > 0 then "COMPILE ERROR"
         else "accepted"),
        (es |> List.tryHead |> Option.defaultValue ""),
        ms

    let firstRecord = "{ Id = HY_FE_2026_B006;"

    let replaceIn (marker: string) (a: string) (b: string) (s: string) =
        let i = s.IndexOf marker
        let j = s.IndexOf("}", i)
        s.Substring(0, i) + s.Substring(i, j - i).Replace(a, b) + s.Substring j

    let results =
        [
            mutate "status typo (Proposed → Propsed)" (replaceIn firstRecord "Some(Proposed)" "Some(Propsed)")
            mutate
                "relation id typo (B006 → B060)"
                (replaceIn "{ Id = EV_FE_2026_B003;" "HY_FE_2026_B006" "HY_FE_2026_B060")
            mutate "relation id lower-cased" (replaceIn "{ Id = EV_FE_2026_B003;" "HY_FE_2026_B006" "hy_fe_2026_b006")
            mutate "field typo (Supports → Support)" (replaceIn firstRecord "Supports =" "Support =")
            mutate "confidence invalid (VeryLow → Hgih)" (replaceIn firstRecord "Some VeryLow" "Some Hgih")
            mutate
                "status set to Supported despite contradiction"
                (replaceIn firstRecord "Some(Proposed)" "Some(Supported)")
            mutate
                "self reference in SupersededBy"
                (replaceIn firstRecord "SupersededBy = [  ]" "SupersededBy = [ HY_FE_2026_B006 ]")
            mutate
                "evidence moved from Contradicts to Supports"
                (replaceIn
                    "{ Id = EV_FE_2026_B004;"
                    "Supports = [  ]; Contradicts = [ HY_FE_2026_B006 ]"
                    "Supports = [ HY_FE_2026_B006 ]; Contradicts = [  ]")
            mutate
                "dependency cycle (A ← B, B ← A)"
                (replaceIn "{ Id = DF_ROS_2026_A042;" "DerivedFrom = [  ]" "DerivedFrom = [ DF_ROS_2026_A059 ]")
        ]

    printfn "## Layer A: F# compiler as validator of knowledge-as-F#\n"

    printfn
        "Records: %d (unique ids), generated source lines: %d, relation targets skipped (not record ids): %d"
        rs.Length
        lines
        skipped

    printfn "Warm-up check (empty corpus): %.0f ms" (let _, _, t = warm in t)
    printfn "Type-check full corpus: %d errors, first run %.0f ms, second run %.0f ms" n0 t0 t1
    e0 |> List.iter (printfn "  %s")
    printfn "\n| Mutation | Result | First error | Check ms |\n|---|---|---|---|"

    results
    |> List.iter (fun (n, r, e, ms) ->
        printfn
            "| %s | %s | %s | %.0f |"
            n
            r
            (e.Replace("|", "/").Replace("\n", " ")
             |> fun s -> s.Substring(0, min 90 s.Length))
            ms)

    0
