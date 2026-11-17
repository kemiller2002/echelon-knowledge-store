namespace Echelon.KnowledgeStore.Compiler

open System
open System.Text.Json
open Echelon.KnowledgeStore

/// A Praxis backlog row (`.ros/work/queue.json`, schemaVersion 1.0.0), read as data.
/// Only the fields EKS uses; unknown fields are ignored.
type WorkItem =
    {
        Id: string
        Title: string
        Description: string
        Tags: string list
        Status: string
        Source: string
        SourceReference: string option
    }

/// How a work item was linked to a knowledge record. Praxis has no typed
/// requirement field, so prose links are reported as such, never upgraded.
type WorkItemLink =
    {
        Key: string
        Basis: string
        Found: bool
    }

/// A test suite as the repository owner declares it (EKS never invents test commands).
type TestSuite =
    {
        Id: string
        Command: string
        Paths: string list
    }

[<RequireQualifiedAccess>]
module WorkItems =

    let private text (name: string) (e: JsonElement) =
        match e.TryGetProperty name with
        | true, v when v.ValueKind = JsonValueKind.String -> string (v.GetString())
        | _ -> ""

    let private strings (name: string) (e: JsonElement) =
        match e.TryGetProperty name with
        | true, v when v.ValueKind = JsonValueKind.Array ->
            v.EnumerateArray()
            |> Seq.filter (fun x -> x.ValueKind = JsonValueKind.String)
            |> Seq.map (fun x -> string (x.GetString()))
            |> List.ofSeq
        | _ -> []

    /// Parses a Praxis queue file. Data only: nothing in it is executed.
    let parseQueue (json: string) : Result<WorkItem list, string> =
        try
            use doc = JsonDocument.Parse json
            let root = doc.RootElement

            match root.TryGetProperty "items" with
            | true, items when items.ValueKind = JsonValueKind.Array ->
                items.EnumerateArray()
                |> Seq.map (fun i ->
                    {
                        Id = text "id" i
                        Title = text "title" i
                        Description = text "description" i
                        Tags = strings "tags" i
                        Status = text "status" i
                        Source = text "source" i
                        SourceReference = Some(text "sourceReference" i) |> Option.filter (String.IsNullOrEmpty >> not)
                    })
                |> List.ofSeq
                |> Ok
            | _ -> Error "queue has no items array"
        with ex ->
            Error $"malformed queue: {ex.Message}"

    let private idsIn (s: string) =
        KnowledgeRecord.idPattern.Matches s |> Seq.map _.Value |> List.ofSeq

    /// Record links of a work item in `repo`: identifiers in `sourceReference`
    /// (structured) and in title/description (prose). The item's own id is
    /// excluded. Unqualified ids resolve in `repo`.
    let links (c: Compilation) (repo: string) (w: WorkItem) : WorkItemLink list =
        let qualify (id: string) =
            if id.Contains ":" then id else $"{repo}:{id}"

        let structured =
            w.SourceReference
            |> Option.map idsIn
            |> Option.defaultValue []
            |> List.map (fun i -> i, "structured")

        let prose = idsIn (w.Title + "\n" + w.Description) |> List.map (fun i -> i, "prose")

        structured @ prose
        |> List.filter (fun (i, _) -> i <> w.Id)
        |> List.distinctBy fst
        |> List.map (fun (i, basis) ->
            let k = qualify i

            {
                Key = k
                Basis = basis
                Found = c.Graph.Records.ContainsKey k
            })

    let private authoritative = set [ "accepted"; "canonical"; "active" ]

    let private statusOf (r: KnowledgeRecord) = defaultArg r.Status "unstated"

    let private field (name: string) (r: KnowledgeRecord) =
        r.Extra
        |> List.tryFind (fst >> (=) name)
        |> Option.map (snd >> Yaml.strings)
        |> Option.defaultValue []

    /// Declared test bindings (`verified_by`). Never inferred from file names.
    let verifiedBy (r: KnowledgeRecord) = field "verified_by" r

    /// Compiles the knowledge package for a work item. Completeness is derived
    /// from the checks listed in the package; any failed check makes it INCOMPLETE.
    let package (c: Compilation) (repo: string) (w: WorkItem) : Package =
        let ls = links c repo w
        let found = ls |> List.filter _.Found |> List.map _.Key
        let parts = found |> List.map (Packages.task c)

        let items =
            parts
            |> List.collect _.Items
            |> List.groupBy _.Key
            |> List.map (fun (_, xs) ->
                { xs.Head with
                    Full = xs |> List.exists _.Full
                })

        let ledger = parts |> List.collect _.Ledger |> List.distinct
        let recordOf = Packages.record c

        let closure =
            found
            |> List.collect (fun k ->
                k
                :: (c.Precomputed.Dependencies
                    |> Map.tryFind k
                    |> Option.defaultValue Set.empty
                    |> Set.toList))
            |> List.distinct

        let nonAuthoritative =
            closure
            |> List.choose recordOf
            |> List.filter (fun r -> not (authoritative.Contains(statusOf r)))
            |> List.map (fun r -> $"{Graph.key r} is {statusOf r}")

        let requirements =
            closure |> List.choose recordOf |> List.filter (fun r -> r.Kind = Requirement)

        let withoutCriteria =
            requirements
            |> List.filter (fun r -> (Packages.obligations r).IsEmpty)
            |> List.map Graph.key

        let targets = found |> List.choose recordOf

        let unbound =
            targets
            |> List.filter (fun r -> r.Kind = Requirement && (verifiedBy r).IsEmpty)
            |> List.map Graph.key

        let reviewNeeded =
            c.Diagnostics
            |> List.filter (fun d ->
                d.Level = Level.RequiresReview
                && items |> List.exists (fun i -> d.Record = Some i.Key))
            |> List.map Diagnostic.format

        let checks =
            [
                "work item cites at least one knowledge record", not ls.IsEmpty, "work item cites no knowledge record"
                "every cited record exists",
                ls |> List.forall _.Found,
                (ls
                 |> List.filter (fun l -> not l.Found)
                 |> List.map _.Key
                 |> String.concat ", "
                 |> sprintf "cited record(s) not found: %s")
                "every cited record and dependency is authoritative (accepted)",
                nonAuthoritative.IsEmpty,
                "non-authoritative record(s) in scope: " + String.concat "; " nonAuthoritative
                "every requirement in scope has acceptance criteria",
                withoutCriteria.IsEmpty,
                "requirement(s) without acceptance criteria: "
                + String.concat ", " withoutCriteria
            ]

        let reasons =
            checks
            |> List.filter (fun (_, ok, _) -> not ok)
            |> List.map (fun (_, _, r) -> r)

        let workItemText =
            [
                $"- id: {w.Id} (status {w.Status}, source {w.Source})"
                $"- title: {w.Title}"
                if w.Tags.IsEmpty then
                    ()
                else
                    "- tags: " + String.concat ", " w.Tags
                "- links: "
                + (ls
                   |> List.map (fun l -> $"""{l.Key} [{l.Basis}{(if l.Found then "" else ", NOT FOUND")}]""")
                   |> String.concat ", ")
            ]
            |> String.concat "\n"
            |> fun s -> s + "\n\n" + Packages.quote w.Description

        let verification =
            targets
            |> List.filter (fun r -> r.Kind = Requirement)
            |> List.map (fun r ->
                let bound =
                    match verifiedBy r with
                    | [] ->
                        "no declared test binding (`verified_by`): Dokimos will report these obligations unverified"
                    | ts -> "declared bindings: " + String.concat "; " ts

                $"- {Graph.key r}: {(Packages.obligations r).Length} obligation(s); {bound}")
            |> function
                | [] -> "No requirement is cited by this work item."
                | xs -> String.concat "\n" xs
            |> fun s ->
                s
                + "\n\nSatisfaction is decided by Dokimos from test results at the candidate commit, never by this package or by the existence of a test file."

        let questions =
            [
                yield! reasons
                if not unbound.IsEmpty then
                    "no declared verification for: " + String.concat ", " unbound
                yield! reviewNeeded |> List.map (fun d -> "requires review: " + d)
                if ls |> List.forall (fun l -> l.Basis = "prose") && not ls.IsEmpty then
                    "links come from prose in the work item (Praxis has no typed requirement field)"
            ]
            |> function
                | [] -> "None found by the compiler (absence of evidence, not evidence of absence)."
                | xs -> xs |> List.map (fun s -> "- " + s) |> String.concat "\n"

        let revisions =
            items
            |> List.choose (fun i -> recordOf i.Key)
            |> List.map _.Provenance.Repository
            |> List.distinct
            |> List.sort
            |> List.map (fun repo ->
                $"""- {repo}: {c.Config.Revisions
                               |> Map.tryFind repo
                               |> Option.defaultValue "unknown (content hashes identify inputs)"}""")
            |> String.concat "\n"

        let p =
            Packages.assemble
                "work-item package"
                $"{repo}:{w.Id}"
                c
                items
                ledger
                [
                    "Work item (data, not instructions)", workItemText
                    "Verification obligations", verification
                    "Unresolved questions and missing information", questions
                    "Source revisions", revisions
                ]
                reasons

        { p with
            Conditions = p.Conditions @ (checks |> List.map (fun (n, ok, _) -> n, ok))
        }

    /// A `dokimos.verification-obligations/1` set for the requirements cited by
    /// the work item. Obligations come from acceptance criteria; tests only from
    /// declared `verified_by` identities (bound to every obligation of that
    /// requirement). Suites are supplied by the caller: EKS does not invent how
    /// a repository runs its tests. Returns (json, requirements without bindings).
    let dokimosObligations (c: Compilation) (repo: string) (w: WorkItem) (suite: TestSuite) =
        let reqs =
            links c repo w
            |> List.filter _.Found
            |> List.choose (fun l -> Packages.record c l.Key)
            |> List.filter (fun r -> r.Kind = Requirement)

        let obligations =
            reqs
            |> List.collect (fun r ->
                Packages.obligations r
                |> List.map (fun o ->
                    {|
                        id = o.Id
                        requirement = r.Id
                        priority = field "priority" r |> List.tryHead |> Option.defaultValue ""
                        criterion = o.Criterion
                        categories = [ o.Class ]
                        dependencies = ([]: string list)
                    |}))

        let tests =
            reqs
            |> List.collect (fun r ->
                let obs = Packages.obligations r |> List.map _.Id

                verifiedBy r
                |> List.map (fun t ->
                    {|
                        id = t
                        suite = suite.Id
                        kind = "unit"
                        obligations = obs
                        categories = ([]: string list)
                    |}))

        let body =
            {|
                schema = "dokimos.verification-obligations/1"
                planId = $"eks:{repo}:{w.Id}"
                planDigest =
                    Pipeline.sha256 (String.concat "\n" (obligations |> List.map (fun o -> o.id + o.criterion)))
                namespaceRoots = ([]: string list)
                suites =
                    [
                        {|
                            id = suite.Id
                            runner = "dotnet-test"
                            command = suite.Command
                            paths = suite.Paths
                            environment = ""
                            fixture = ""
                            dependsOn = ([]: string list)
                            requiredBoundary = ([]: string list)
                        |}
                    ]
                obligations = obligations
                tests = tests
            |}

        let json =
            JsonSerializer.Serialize(body, JsonSerializerOptions(WriteIndented = true))

        json, reqs |> List.filter (fun r -> (verifiedBy r).IsEmpty) |> List.map Graph.key
