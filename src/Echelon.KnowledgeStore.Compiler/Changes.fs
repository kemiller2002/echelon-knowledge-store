namespace Echelon.KnowledgeStore.Compiler

open System.Text.Json
open Echelon.KnowledgeStore

/// Outcome of evaluating a proposed knowledge change. There is deliberately no
/// "approved": passing validation makes a change reviewable, not acceptable.
type ChangeVerdict =
    | Rejected of reasons: string list
    | RequiresReview of attention: string list

/// Evaluates a proposed change set (base tree → head tree) as one transaction:
/// either every file is acceptable for review or the whole set is rejected.
[<RequireQualifiedAccess>]
module Changes =

    let private fileMap (c: Compilation) =
        c.Units
        |> List.map (fun u -> $"{u.File.Repository}/{u.File.Path}", u.File.Hash)
        |> Map.ofList

    let private recordKeys (c: Compilation) =
        c.Graph.Records |> Map.keys |> Set.ofSeq

    let private signature (d: Diagnostic) = d.Code, d.Record, d.Message

    let private status (r: KnowledgeRecord) = defaultArg r.Status "unstated"

    let private locatable (r: KnowledgeRecord) =
        r.Provenance.SourceUri |> Option.exists (fun s -> s.Trim() <> "")
        || r.Relations |> List.exists (fun x -> x.Kind = DerivedFrom)

    /// Pure evaluation. `baseC` and `head` must be compiled with the same configuration.
    let evaluate (baseC: Compilation) (head: Compilation) =
        let bf, hf = fileMap baseC, fileMap head
        let added = Set.difference (Set.ofSeq hf.Keys) (Set.ofSeq bf.Keys)
        let removed = Set.difference (Set.ofSeq bf.Keys) (Set.ofSeq hf.Keys)

        let changed =
            Set.intersect (Set.ofSeq hf.Keys) (Set.ofSeq bf.Keys)
            |> Set.filter (fun k -> hf[k] <> bf[k])

        let addedRecords = Set.difference (recordKeys head) (recordKeys baseC)
        let removedRecords = Set.difference (recordKeys baseC) (recordKeys head)

        let changedRecords =
            Set.intersect (recordKeys head) (recordKeys baseC)
            |> Set.filter (fun k -> head.Graph.Records[k] <> baseC.Graph.Records[k])

        let before = baseC.Diagnostics |> List.map signature |> Set.ofList
        let after = head.Diagnostics |> List.map signature |> Set.ofList

        let introduced =
            head.Diagnostics |> List.filter (fun d -> not (before.Contains(signature d)))

        let resolved =
            baseC.Diagnostics |> List.filter (fun d -> not (after.Contains(signature d)))

        let skipped =
            head.Rules
            |> List.filter (fun r -> r.Mandatory && not r.Executed)
            |> List.map _.Rule

        let impacted =
            Set.union changedRecords removedRecords
            |> Seq.collect (fun k ->
                [ baseC; head ]
                |> List.collect (fun c ->
                    c.Precomputed.Impact
                    |> Map.tryFind k
                    |> Option.map (Map.keys >> List.ofSeq)
                    |> Option.defaultValue []))
            |> Set.ofSeq
            |> fun s -> Set.difference s (Set.union changedRecords removedRecords)

        let promotions =
            changedRecords
            |> Set.toList
            |> List.choose (fun k ->
                let b, h = status baseC.Graph.Records[k], status head.Graph.Records[k]

                if b <> h then Some $"{k}: status {b} → {h}" else None)

        let unlocatable =
            Set.union addedRecords changedRecords
            |> Set.toList
            |> List.map (fun k -> head.Graph.Records[k])
            |> List.filter (fun r -> r.Kind = Evidence && status r = "accepted" && not (locatable r))
            |> List.map (fun r ->
                $"{Graph.key r}: accepted evidence whose source cannot be located (no source_uri or derived_from)")

        let errors = introduced |> List.filter (fun d -> d.Level = Level.Error)

        let verdict =
            match
                (errors |> List.map (fun d -> "introduces " + Diagnostic.format d))
                @ (skipped |> List.map (fun r -> $"mandatory rule '{r}' was not executed"))
            with
            | [] ->
                RequiresReview(
                    promotions
                    @ unlocatable
                    @ (if impacted.IsEmpty then
                           []
                       else
                           [ $"{impacted.Count} dependent record(s) may need review" ])
                )
            | reasons -> Rejected reasons

        let body =
            {|
                schema = "eks.change-manifest/0"
                compiler =
                    {|
                        name = Pipeline.compilerName
                        version = Pipeline.compilerVersion
                    |}
                baseInputs = baseC.InputsHash
                headInputs = head.InputsHash
                files =
                    {|
                        added = added |> Set.toList |> List.map (fun f -> {| path = f; sha256 = hf[f] |})
                        removed = removed |> Set.toList |> List.map (fun f -> {| path = f; sha256 = bf[f] |})
                        changed =
                            changed
                            |> Set.toList
                            |> List.map (fun f ->
                                {|
                                    path = f
                                    before = bf[f]
                                    after = hf[f]
                                |})
                    |}
                records =
                    {|
                        added = Set.toList addedRecords
                        removed = Set.toList removedRecords
                        changed = Set.toList changedRecords
                    |}
                diagnosticsIntroduced = introduced |> List.map Diagnostic.format
                diagnosticsResolved = resolved |> List.map Diagnostic.format
                impacted = Set.toList impacted
                verdict =
                    match verdict with
                    | Rejected _ -> "rejected"
                    | RequiresReview _ -> "requires-review"
                reasons =
                    match verdict with
                    | Rejected rs -> rs
                    | RequiresReview a -> a
                note = "No verdict approves a change. Schema-valid changes still require review."
            |}

        verdict, JsonSerializer.Serialize(body, JsonSerializerOptions(WriteIndented = true))
