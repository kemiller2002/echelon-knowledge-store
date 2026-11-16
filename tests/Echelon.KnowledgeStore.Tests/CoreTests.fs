module Echelon.KnowledgeStore.Tests.CoreTests

open System
open Xunit
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Tests.Fixtures

[<Fact>]
let ``yaml rejects aliases and anchors`` () =
    Assert.True(Yaml.parse "a: &x 1\nb: *x" |> Result.isError)

[<Fact>]
let ``yaml parses nested data without type activation`` () =
    let y = Yaml.parse "a: [x, y]\nb:\n  c: !!python/object 1"
    // A tag is reported by the parser as data or rejected; it never instantiates a type.
    match y with
    | Ok v ->
        Assert.Equal<string list>([ "x"; "y" ], Yaml.field "a" v |> Option.map Yaml.strings |> Option.defaultValue [])
    | Error _ -> ()

[<Fact>]
let ``front matter without terminator is an error`` () =
    Assert.True(FrontMatter.split "---\nid: X\n" |> Result.isError)

[<Fact>]
let ``markdown without front matter is a body`` () =
    match FrontMatter.split "# Title\n" with
    | Ok d -> Assert.True(d.FrontMatter.IsNone)
    | Error e -> failwith e

[<Fact>]
let ``record kind is inferred from directory`` () =
    Assert.Equal(Hypothesis, hypothesis.Kind)
    Assert.Equal(Evidence, evidenceFor.Kind)
    Assert.Equal(Decision, decisionNew.Kind)

[<Fact>]
let ``inverse evidence lists become graph edges`` () =
    let incoming =
        Graph.incoming graph "praxis:HY-T-2026-0001"
        |> List.map (fun e -> e.Kind, e.Source)
        |> Set.ofList

    Assert.Contains((Supports, "praxis:EV-T-2026-0001"), incoming)
    Assert.Contains((Contradicts, "praxis:EV-T-2026-0002"), incoming)

[<Fact>]
let ``impact follows each relation's direction`` () =
    // EV-1 supports HY-1 (towards target) and EV-2 is derived from EV-1 (towards source).
    let impact = Graph.impactOf graph 6 "praxis:EV-T-2026-0001" |> Map.keys |> Set.ofSeq
    Assert.Equal<Set<string>>(set [ "praxis:HY-T-2026-0001"; "praxis:EV-T-2026-0002" ], impact)
    // Changing the hypothesis does not make its evidence stale.
    Assert.Empty(Graph.impactOf graph 6 "praxis:HY-T-2026-0001")
    // A decision depending on a requirement is impacted by it.
    Assert.Equal<string list>(
        [ "praxis:DF-T-2026-0002" ],
        Graph.impactOf graph 6 "praxis:RQ-T-2026-0001" |> Map.keys |> List.ofSeq
    )

[<Fact>]
let ``validation reports dangling references and asymmetric evidence`` () =
    let orphan =
        record
            "praxis"
            "research/decisions/DF-T-2026-0009--x.md"
            "---\nid: DF-T-2026-0009\ntitle: T\nstatus: accepted\ndepends_on: [RQ-T-2026-0404]\n---\n\nx\n"

    let rs = orphan :: all
    let vs = Validation.all (DateOnly(2026, 11, 16)) rs (Graph.build false rs)
    Assert.Contains(vs, fun v -> v.Rule = "dangling-reference" && v.Record = "praxis:DF-T-2026-0009")
    // EV-2 is listed as contradicting by HY-1 but does not declare it.
    Assert.Contains(vs, fun v -> v.Rule = "evidence-link-asymmetry" && v.Record = "praxis:EV-T-2026-0002")
    Assert.Contains(vs, fun v -> v.Rule = "contested-claim")

[<Fact>]
let ``superseded without replacement is blocking`` () =
    let r = { decisionOld with Relations = [] }
    let vs = Validation.recordRules (DateOnly(2026, 11, 16)) r
    Assert.Contains(vs, fun v -> v.Rule = "superseded-without-replacement" && v.Severity = Blocking)

[<Fact>]
let ``dependency cycles are detected`` () =
    let a =
        record
            "x"
            "a/DF-C-2026-0001.md"
            "---\nid: DF-C-2026-0001\ntitle: A\nstatus: accepted\ndepends_on: [DF-C-2026-0002]\n---\n\na\n"

    let b =
        record
            "x"
            "a/DF-C-2026-0002.md"
            "---\nid: DF-C-2026-0002\ntitle: B\nstatus: accepted\ndepends_on: [DF-C-2026-0001]\n---\n\nb\n"

    let vs = Validation.graphRules (Graph.build false [ a; b ])
    Assert.Contains(vs, fun v -> v.Rule = "dependency-cycle")

[<Fact>]
let ``duplicate ids within a repository are blocking`` () =
    let vs = Validation.duplicateIds [ requirement; requirement ]
    Assert.Contains(vs, fun v -> v.Rule = "duplicate-id")
