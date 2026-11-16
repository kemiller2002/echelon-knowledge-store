namespace Echelon.KnowledgeStore

open System

type Severity =
    | Blocking
    | Advisory
    | Informational

/// A rule violation. Validity is structural: a record with no violations is
/// well-formed, not true.
type Violation =
    {
        Rule: string
        Severity: Severity
        Record: string
        Detail: string
    }

[<RequireQualifiedAccess>]
module Validation =

    let private violation rule severity record detail =
        {
            Rule = rule
            Severity = severity
            Record = record
            Detail = detail
        }

    let private statusIs (values: string list) (r: KnowledgeRecord) =
        r.Status
        |> Option.map (fun s -> values |> List.contains (s.Trim().ToLowerInvariant()))
        |> Option.defaultValue false

    let private targets kind (r: KnowledgeRecord) =
        r.Relations |> List.filter (fun x -> x.Kind = kind) |> List.map _.Target

    /// Rules over a single record.
    let recordRules (asOf: DateOnly) (r: KnowledgeRecord) : Violation list =
        let k = Graph.key r

        [
            if String.IsNullOrWhiteSpace r.Title then
                violation "missing-title" Blocking k "record has no title"
            if r.Status.IsNone && not (r.Extra |> List.exists (fst >> (=) "id_source")) then
                violation "missing-status" Advisory k "record has no status"
            if statusIs [ "superseded" ] r && List.isEmpty (targets SupersededBy r) then
                violation "superseded-without-replacement" Blocking k "status is superseded but superseded_by is empty"
            if
                not (List.isEmpty (targets SupersededBy r))
                && not (statusIs [ "superseded"; "deprecated"; "retired" ] r)
            then
                violation "replacement-but-active" Advisory k $"superseded_by is set but status is {r.Status}"
            if
                r.Kind = Evidence
                && r.Provenance.SourceUri.IsNone
                && not (r.Extra |> List.exists (fun (f, _) -> f = "source_title" || f = "sources"))
                && not (r.Relations |> List.exists (fun x -> x.Kind = DerivedFrom || x.Kind = RelatedTo))
            then
                violation
                    "evidence-without-source"
                    Blocking
                    k
                    "evidence has no source, derived_from or related_documents"
            if
                r.Kind = Evidence
                && r.Provenance.SourceUri.IsNone
                && not (r.Extra |> List.exists (fst >> (=) "source_title"))
            then
                violation
                    "evidence-without-structured-source"
                    Advisory
                    k
                    "evidence source given only as related links or prose"
            if
                r.Kind = Requirement
                && not (r.Body.Contains("Acceptance", StringComparison.OrdinalIgnoreCase))
            then
                violation "requirement-without-acceptance" Advisory k "no acceptance criteria found in body"
            match r.Kind, r.Provenance.Retrieved with
            | Evidence, Some d when d.AddDays 180 < asOf ->
                violation "stale-evidence" Advisory k $"evidence retrieved {d}"
            | _ -> ()
        ]

    let private cycles (g: KnowledgeGraph) (kinds: RelationKind list) : string list list =
        let next k =
            Graph.outgoing g k
            |> List.filter (fun e -> List.contains e.Kind kinds)
            |> List.map _.Target

        let rec dfs path k =
            if List.contains k path then
                [ k :: (path |> List.takeWhile ((<>) k)) @ [ k ] |> List.rev ]
            else
                next k |> List.collect (dfs (k :: path))

        g.Records
        |> Map.keys
        |> Seq.collect (dfs [])
        |> Seq.map (fun c -> c |> List.distinct |> List.sort)
        |> Seq.distinct
        |> List.ofSeq

    /// Rules over the whole graph (references, symmetry, cycles, epistemic support).
    let graphRules (g: KnowledgeGraph) : Violation list =
        let declared (src: string) kind (dst: string) =
            g.Records
            |> Map.tryFind src
            |> Option.map (fun r ->
                r.Relations
                |> List.exists (fun x -> x.Kind = kind && (x.Target = (Map.find dst g.Records).Id || x.Target = dst)))
            |> Option.defaultValue false

        let dangling =
            g.Unresolved
            |> List.filter (fun (_, rel) -> rel.Kind <> Mentions)
            |> List.filter (fun (_, rel) ->
                KnowledgeRecord.idPattern.IsMatch rel.Target && not (rel.Target.Contains "/"))
            |> List.map (fun (src, rel) ->
                violation "dangling-reference" Blocking src $"{RelationKind.name rel.Kind} → {rel.Target} not found")

        let supersession =
            g.Edges
            |> List.collect (fun e ->
                match e.Kind with
                | Supersedes when not (declared e.Target SupersededBy e.Source) ->
                    [
                        violation
                            "supersession-asymmetry"
                            Advisory
                            e.Target
                            $"{e.Source} supersedes it but superseded_by omits it"
                    ]
                | SupersededBy when not (declared e.Target Supersedes e.Source) ->
                    [
                        violation
                            "supersession-asymmetry"
                            Advisory
                            e.Target
                            $"{e.Source} names it as replacement but supersedes omits it"
                    ]
                | _ -> [])

        let evidenceSymmetry =
            g.Records
            |> Map.toList
            |> List.collect (fun (k, r) ->
                KnowledgeRecord.inverseRelations r
                |> List.choose (fun (source, rel) ->
                    g.Records
                    |> Map.tryFindKey (fun _ sr ->
                        sr.Id = source && sr.Provenance.Repository = r.Provenance.Repository)
                    |> Option.filter (fun sk -> not (declared sk rel.Kind k))
                    |> Option.map (fun sk ->
                        violation
                            "evidence-link-asymmetry"
                            Advisory
                            sk
                            $"{r.Id} lists it as {RelationKind.name rel.Kind} evidence but it does not declare {RelationKind.name rel.Kind} {r.Id}")))

        let unsupported =
            g.Records
            |> Map.toList
            |> List.filter (fun (_, r) ->
                (r.Kind = Hypothesis || r.Kind = Theory)
                && statusIs [ "supported"; "accepted"; "confirmed"; "validated"; "canonical" ] r)
            |> List.filter (fun (k, _) -> Graph.incoming g k |> List.forall (fun e -> e.Kind <> Supports))
            |> List.map (fun (k, r) ->
                violation "unsupported-claim" Blocking k $"status {r.Status} but no supporting evidence")

        let contested =
            g.Records
            |> Map.toList
            |> List.filter (fun (k, _) ->
                let kinds = Graph.incoming g k |> List.map _.Kind
                List.contains Supports kinds && List.contains Contradicts kinds)
            |> List.map (fun (k, _) ->
                violation "contested-claim" Informational k "has both supporting and contradicting evidence")

        let supersededSupport =
            g.Edges
            |> List.filter (fun e -> e.Kind = Supports || e.Kind = Contradicts)
            |> List.filter (fun e -> statusIs [ "superseded" ] g.Records[e.Source])
            |> List.map (fun e ->
                violation
                    "support-from-superseded-record"
                    Advisory
                    e.Target
                    $"{e.Source} is superseded but still {RelationKind.name e.Kind} it")

        let cyc =
            cycles g [ DependsOn; DerivedFrom; Supersedes ]
            |> List.map (fun c -> violation "dependency-cycle" Blocking (List.head c) (String.Join(" → ", c)))

        dangling
        @ supersession
        @ evidenceSymmetry
        @ unsupported
        @ contested
        @ supersededSupport
        @ cyc

    /// Duplicate ids within a repository and ids shared across repositories.
    let duplicateIds (records: KnowledgeRecord list) : Violation list =
        records
        |> List.groupBy _.Id
        |> List.collect (fun (rid, rs) ->
            let repos = rs |> List.map _.Provenance.Repository
            let within = repos |> List.countBy id |> List.filter (snd >> (<) 1)

            let repoList = String.concat "," (List.distinct repos)

            [
                for repo, n in within do
                    violation "duplicate-id" Blocking $"{repo}:{rid}" $"{n} records share this id"
                if (List.distinct repos).Length > 1 then
                    violation "cross-repository-id-collision" Advisory rid $"defined in {repoList}"
            ])

    let all (asOf: DateOnly) (records: KnowledgeRecord list) (g: KnowledgeGraph) =
        (records |> List.collect (recordRules asOf))
        @ duplicateIds records
        @ graphRules g
