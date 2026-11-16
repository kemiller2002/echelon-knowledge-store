module Echelon.KnowledgeStore.Tests.WorkItemTests

open System
open Xunit
open Echelon.KnowledgeStore
open Echelon.KnowledgeStore.Compiler

let private cfg =
    {
        AsOf = DateOnly(2026, 11, 16)
        StaleAfterDays = 180
        Visibility = Map.empty
        Audience = Private
        SupportedSchema = 1
        ExistingPaths = None
        DisabledRules = Set.empty
        Revisions = Map.ofList [ "app", "abc123" ]
    }

let private req id status deps extra =
    Pipeline.sourceFile
        "app"
        $"research/requirements/{id}--r.md"
        $"---\nid: {id}\ntitle: R {id}\nstatus: {status}\ncreated: 2026-10-01\ndepends_on: [{deps}]\n{extra}---\n\n# R\n\nText.\n\n## Acceptance\n\n- Returns 1 for input 1.\n"

let private dec id status =
    Pipeline.sourceFile
        "app"
        $"research/decisions/{id}--d.md"
        $"---\nid: {id}\ntitle: D {id}\nstatus: {status}\ncreated: 2026-10-01\n---\n\nWhy.\n"

let private compile files =
    Pipeline.compile cfg Map.empty files |> fst

let private queue =
    """{"schemaVersion":"1.0.0","repository":"app","nextSeq":4,"items":[
  {"id":"WI-0001","title":"Do RQ-APP-0001","description":"","tags":["code"],"status":"ready","source":"manual","sourceReference":null,"unknown":1},
  {"id":"WI-0002","title":"Do RQ-APP-0002","description":"see RQ-APP-0009","tags":[],"status":"ready","source":"manual","sourceReference":null},
  {"id":"WI-0003","title":"Tidy up","description":"","tags":[],"status":"ready","source":"manual","sourceReference":"RQ-APP-0001"}]}"""

let private items =
    match WorkItems.parseQueue queue with
    | Ok ws -> ws
    | Error e -> failwith e

let private item id = items |> List.find (fun w -> w.Id = id)

let private corpus =
    compile
        [
            req "RQ-APP-0001" "accepted" "DF-APP-0001" "verified_by: [\"dotnet:App.Tests.T.one\"]\n"
            dec "DF-APP-0001" "accepted"
            req "RQ-APP-0002" "accepted" "DF-APP-0002" ""
            dec "DF-APP-0002" "draft"
        ]

[<Fact>]
let ``queue parses as data and ignores unknown fields`` () =
    Assert.Equal(3, items.Length)
    Assert.Equal(Some "RQ-APP-0001", (item "WI-0003").SourceReference)

[<Fact>]
let ``links record their basis`` () =
    let ls = WorkItems.links corpus "app" (item "WI-0003")
    Assert.Equal<string list>([ "app:RQ-APP-0001" ], ls |> List.map _.Key)
    Assert.Equal("structured", ls.Head.Basis)
    Assert.Equal("prose", (WorkItems.links corpus "app" (item "WI-0001")).Head.Basis)

[<Fact>]
let ``complete package lists dependencies, obligations and revisions`` () =
    let p = WorkItems.package corpus "app" (item "WI-0001")
    Assert.Equal(Complete, p.Status)
    Assert.Contains("app:DF-APP-0001", p.Text)
    Assert.Contains("dotnet:App.Tests.T.one", p.Text)
    Assert.Contains("app: abc123", p.Text)
    Assert.True(p.Conditions |> List.forall snd)

[<Fact>]
let ``draft dependency and missing record make the package incomplete`` () =
    let p = WorkItems.package corpus "app" (item "WI-0002")

    match p.Status with
    | Incomplete reasons ->
        Assert.Contains(reasons, fun r -> r.Contains "DF-APP-0002 is draft")
        Assert.Contains(reasons, fun r -> r.Contains "app:RQ-APP-0009")
    | Complete -> failwith "expected INCOMPLETE"

    Assert.Contains(p.Conditions, fun (_, ok) -> not ok)

[<Fact>]
let ``work item without links is never complete`` () =
    let w =
        { item "WI-0001" with
            Title = "Tidy up"
        }

    Assert.NotEqual(Complete, (WorkItems.package corpus "app" w).Status)

[<Fact>]
let ``completeness agrees with conditions`` () =
    items
    |> List.map (WorkItems.package corpus "app")
    |> List.iter (fun p -> Assert.Equal(p.Conditions |> List.forall snd, (p.Status = Complete)))

[<Fact>]
let ``dokimos obligations bind only declared tests`` () =
    let suite =
        {
            Id = "unit"
            Command = "dotnet test"
            Paths = [ "tests" ]
        }

    let json, unbound = WorkItems.dokimosObligations corpus "app" (item "WI-0001") suite
    Assert.Contains("\"schema\": \"dokimos.verification-obligations/1\"", json)
    Assert.Contains("dotnet:App.Tests.T.one", json)
    Assert.Empty unbound

    let json2, unbound2 =
        WorkItems.dokimosObligations corpus "app" (item "WI-0002") suite

    Assert.Equal<string list>([ "app:RQ-APP-0002" ], unbound2)
    Assert.Contains("\"tests\": []", json2)

[<Fact>]
let ``change manifest rejects new errors and never approves`` () =
    let baseFiles =
        [ req "RQ-APP-0001" "accepted" "DF-APP-0001" ""; dec "DF-APP-0001" "accepted" ]

    let baseC = compile baseFiles

    let evidence status extra =
        Pipeline.sourceFile
            "app"
            "research/evidence/EV-APP-0001--e.md"
            $"---\nid: EV-APP-0001\ntitle: E\nstatus: {status}\nsupports: [DF-APP-0001]\n{extra}---\n\nData.\n"

    match Changes.evaluate baseC (compile (baseFiles @ [ evidence "accepted" "source_title: A meeting\n" ])) with
    | RequiresReview attention, json ->
        Assert.Contains(attention, fun a -> a.Contains "cannot be located")
        Assert.Contains("\"requires-review\"", json)
    | Rejected rs, _ -> failwithf "unexpected rejection %A" rs

    match Changes.evaluate baseC (compile (baseFiles @ [ evidence "accepted" "" ])) with
    | Rejected reasons, _ -> Assert.Contains(reasons, fun r -> r.Contains "EKS0005")
    | RequiresReview _, _ -> failwith "a change that introduces an error must be rejected"

    let promoted =
        [ req "RQ-APP-0001" "accepted" "DF-APP-0001" ""; dec "DF-APP-0001" "proposed" ]

    match Changes.evaluate baseC (compile promoted) with
    | RequiresReview attention, json ->
        Assert.Contains(attention, fun a -> a.Contains "status accepted → proposed")
        Assert.DoesNotContain("\"approved\"", json)
    | Rejected rs, _ -> failwithf "unexpected rejection %A" rs
