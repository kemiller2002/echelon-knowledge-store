namespace Echelon.KnowledgeStore.Compiler

open System
open System.Security.Cryptography
open System.Text
open System.Text.RegularExpressions
open Echelon.KnowledgeStore

type Visibility =
    | Public
    | Private

/// One input file. `Hash` is SHA-256 of the UTF-8 text.
type SourceFile =
    {
        Repository: string
        Path: string
        Text: string
        Hash: string
    }

type CompileConfig =
    {
        AsOf: DateOnly
        StaleAfterDays: int
        Visibility: Map<string, Visibility>
        /// Audience of the outputs. Public outputs may not contain private knowledge.
        Audience: Visibility
        /// Schema major versions this compiler reads, per record kind (absent = 1).
        SupportedSchema: int
        /// All existing file paths as `repository/path` (any extension), when known.
        /// Enables checking that relative `source_uri` values point to real files.
        ExistingPaths: Set<string> option
        /// Rules switched off (adversarial testing only). A disabled mandatory rule
        /// makes every package INCOMPLETE.
        DisabledRules: Set<string>
        /// Source revision (commit) per repository, recorded in manifests.
        Revisions: Map<string, string>
    }

/// One validation rule's execution record.
type RuleRun =
    {
        Rule: string
        Mandatory: bool
        Executed: bool
        Findings: int
    }

/// Result of parsing one file. Cached by (path, hash) for incremental builds.
type ParsedUnit =
    {
        File: SourceFile
        Record: KnowledgeRecord option
        Diagnostics: Diagnostic list
    }

/// Precomputed (partially evaluated) query results.
type Precomputed =
    {
        Impact: Map<string, Map<string, int>>
        Evidence: Map<string, string list * string list>
        Current: Map<string, string>
        Dependencies: Map<string, Set<string>>
    }

type Compilation =
    {
        Config: CompileConfig
        Units: ParsedUnit list
        Records: KnowledgeRecord list
        Graph: KnowledgeGraph
        Diagnostics: Diagnostic list
        Precomputed: Precomputed
        InputsHash: string
        Rules: RuleRun list
    }

/// Parse cache keyed by `repository/path` and content hash.
type Cache = Map<string, ParsedUnit>

[<RequireQualifiedAccess>]
module Pipeline =

    let compilerName = "eks-compiler"
    let compilerVersion = "0.1.0-research"

    let sha256 (s: string) =
        SHA256.HashData(Encoding.UTF8.GetBytes s)
        |> Convert.ToHexString
        |> _.ToLowerInvariant()

    let sourceFile repo path (text: string) =
        {
            Repository = repo
            Path = path
            Text = text
            Hash = sha256 text
        }

    let private recordDirs =
        Regex(
            @"(^|/)research/(hypotheses|evidence|experiments|decisions|requirements|theories|packages|journals?)/[^/]+\.md$"
        )

    let private fileId =
        Regex(@"^([A-Z][A-Z0-9]{1,9}(?:-[A-Z0-9]{1,12}){0,4}-[A-Z]?\d{2,4}[A-Z]?)(?:--.*)?\.md$")

    let private loc (f: SourceFile) field value =
        Some
            {
                Repository = f.Repository
                Path = f.Path
                Line = Diagnostic.lineOf f.Text field value
                Field = field
            }

    let private filenameRecord (f: SourceFile) (body: string) =
        let m = fileId.Match(f.Path.Split('/') |> Array.last)

        if not m.Success then
            None
        else
            let title =
                body.Split('\n')
                |> Array.tryFind (fun l -> l.StartsWith "# ")
                |> Option.map (fun l -> l.Substring(2).Trim())
                |> Option.defaultValue ""

            Some
                {
                    Id = m.Groups[1].Value
                    Kind = KnowledgeKind.infer f.Path m.Groups[1].Value
                    Title = title
                    Status = None
                    Confidence = Unstated
                    Relations = []
                    Provenance =
                        {
                            Repository = f.Repository
                            Path = f.Path
                            Author = None
                            SourceUri = None
                            Created = None
                            Updated = None
                            Retrieved = None
                        }
                    Body = body.Trim('\n').TrimEnd()
                    Extra = [ "id_source", YScalar "filename" ]
                }

    let private levenshtein (a: string) (b: string) =
        let row0 = Array.init (b.Length + 1) id

        a
        |> Seq.fold
            (fun (prev: int array) ca ->
                let next = Array.zeroCreate (b.Length + 1)
                next[0] <- prev[0] + 1

                for j in 1 .. b.Length do
                    let cost = if ca = b[j - 1] then 0 else 1
                    next[j] <- min (min (next[j - 1] + 1) (prev[j] + 1)) (prev[j - 1] + cost)

                next)
            row0
        |> Array.last

    let private snake (s: string) =
        Regex.Replace(s, "([a-z])([A-Z])", "$1_$2").ToLowerInvariant()

    /// Fields with meaning to EKS (misspellings of these lose information).
    let knownFields =
        [
            "id"
            "title"
            "status"
            "confidence"
            "created"
            "updated"
            "retrieved"
            "schema"
            "tags"
            "owners"
            "version"
            "author_agent"
            "created_by_agent"
            "source_uri"
            "source_title"
            "source_author"
            "source_date"
            "evidence_type"
        ]
        @ (RelationKind.fields |> List.map (fun (f, _, _) -> f))

    let private schemaStatus =
        [
            Hypothesis, [ "proposed"; "active"; "supported"; "rejected"; "superseded"; "withdrawn" ]
            Experiment, [ "proposed"; "active"; "blocked"; "completed"; "cancelled" ]
            Theory,
            [
                "candidate"
                "supported"
                "established"
                "challenged"
                "superseded"
                "rejected"
            ]
        ]

    let private lifecycleStatus =
        set
            [
                "draft"
                "review"
                "accepted"
                "canonical"
                "deprecated"
                "archived"
                "superseded"
                "withdrawn"
                "proposed"
                "active"
                "implemented"
                "completed"
                "complete"
                "verified"
                "rejected"
                "open"
                "blocked"
                "approved"
                "retired"
                "in-progress"
                "planned"
                "cancelled"
                "supported"
            ]

    /// Field-level checks on one record's front matter.
    let private fieldDiagnostics (f: SourceFile) (fm: Yaml) (r: KnowledgeRecord) =
        let key = Graph.key r

        let keys =
            match fm with
            | YMap kvs -> kvs |> List.map fst
            | _ -> []

        let relationFields = RelationKind.fields |> List.map (fun (f, _, _) -> f)

        let lookalikes =
            keys
            |> List.filter (fun k -> not (List.contains k knownFields))
            |> List.choose (fun k ->
                let n = snake k

                let strong =
                    knownFields
                    |> List.tryFind (fun known -> n = known || (known.Length >= 6 && levenshtein n known <= 2))

                let prefix =
                    relationFields
                    |> List.tryFind (fun known -> n.Length >= 4 && known.StartsWith n)

                match strong, prefix with
                | Some known, _ when List.contains known relationFields || n = known -> Some(Level.Error, k, known)
                | _, Some known -> Some(Level.Warning, k, known)
                | Some known, None -> Some(Level.Warning, k, known)
                | None, None -> None)
            |> List.map (fun (level, k, known) ->
                Diagnostic.makeAt
                    level
                    "EKS0022"
                    (Some key)
                    (loc f (Some k) None)
                    $"field '{k}' is not recognised; did you mean '{known}'?")

        let status = r.Status |> Option.map (fun s -> s.Trim().ToLowerInvariant())

        let statusDiag =
            match status, schemaStatus |> List.tryFind (fst >> (=) r.Kind) with
            | Some s, Some(kind, allowed) when not (List.contains s allowed) ->
                [
                    Diagnostic.make
                        "EKS0023"
                        (Some key)
                        (loc f (Some "status") None)
                        $"""status '{s}' is not allowed for {KnowledgeKind.name kind} ({String.concat ", " allowed})"""
                ]
            | Some s, None when not (lifecycleStatus.Contains s) ->
                [
                    Diagnostic.make
                        "EKS0025"
                        (Some key)
                        (loc f (Some "status") None)
                        $"status '{s}' is not an established lifecycle status"
                ]
            | _ -> []

        let confidenceDiag =
            match r.Confidence with
            | Label l ->
                [
                    Diagnostic.make
                        "EKS0024"
                        (Some key)
                        (loc f (Some "confidence") None)
                        $"confidence '{l}' is outside the standard scale"
                ]
            | _ -> []

        let selfRef =
            r.Relations
            |> List.filter (fun x -> x.Target = r.Id && x.Kind <> Mentions)
            |> List.map (fun x ->
                Diagnostic.make
                    "EKS0003"
                    (Some key)
                    (loc f (Some(RelationKind.name x.Kind)) None)
                    $"{RelationKind.name x.Kind} refers to the record itself")

        lookalikes @ statusDiag @ confidenceDiag @ selfRef

    /// Parses one file (pure). Parse failures become diagnostics, never drops.
    let parse (cfg: CompileConfig) (f: SourceFile) : ParsedUnit =
        match FrontMatter.split f.Text with
        | Error e ->
            {
                File = f
                Record = filenameRecord f f.Text
                Diagnostics = [ Diagnostic.make "EKS0011" None (loc f None None) e ]
            }
        | Ok doc ->
            match KnowledgeRecord.ofDocument f.Repository f.Path doc with
            | Ok r ->
                let key = Graph.key r

                let schema =
                    r.Extra
                    |> List.tryFind (fst >> (=) "schema")
                    |> Option.bind (snd >> Yaml.text)
                    |> Option.bind (fun s ->
                        let m = Regex.Match(s, @"(\d+)(?:[-.]\d+)*$")
                        if m.Success then Some(int m.Groups[1].Value) else None)

                let schemaDiag =
                    match schema with
                    | Some major when major > cfg.SupportedSchema ->
                        [
                            Diagnostic.make
                                "EKS0004"
                                (Some key)
                                (loc f (Some "schema") None)
                                $"schema major version {major} is newer than {cfg.SupportedSchema}"
                        ]
                    | _ -> []

                let injection =
                    if Diagnostic.addressesAgents r.Body then
                        [
                            Diagnostic.make
                                "EKS0015"
                                (Some key)
                                (loc f None None)
                                "record text contains instructions addressed to agents"
                        ]
                    else
                        []

                let fields =
                    match doc.FrontMatter with
                    | Some fm -> fieldDiagnostics f fm r
                    | None -> []

                {
                    File = f
                    Record = Some r
                    Diagnostics = schemaDiag @ injection @ fields
                }
            | Error _ when recordDirs.IsMatch f.Path ->
                {
                    File = f
                    Record = filenameRecord f doc.Body
                    Diagnostics =
                        [
                            Diagnostic.make
                                "EKS0001"
                                None
                                (loc f None None)
                                "file in a record directory has no front-matter id"
                        ]
                }
            | Error _ ->
                {
                    File = f
                    Record = filenameRecord f doc.Body
                    Diagnostics = []
                }

    let private ruleCode =
        function
        | "missing-title" -> Some "EKS0005"
        | "missing-status" -> Some "EKS0016"
        | "superseded-without-replacement" -> Some "EKS0013"
        | "replacement-but-active" -> Some "EKS0010"
        | "evidence-without-source" -> Some "EKS0005"
        | "evidence-without-structured-source" -> None
        | "requirement-without-acceptance" -> Some "EKS0009"
        | "stale-evidence" -> Some "EKS0010"
        | "dangling-reference" -> Some "EKS0002"
        | "supersession-asymmetry" -> Some "EKS0021"
        | "evidence-link-asymmetry" -> Some "EKS0012"
        | "unsupported-claim" -> Some "EKS0014"
        | "contested-claim" -> Some "EKS0017"
        | "support-from-superseded-record" -> Some "EKS0007"
        | "dependency-cycle" -> Some "EKS0006"
        | "duplicate-id" -> Some "EKS0018"
        | "cross-repository-id-collision" -> None
        | _ -> None

    /// Precomputes common query results (partial evaluation).
    let precompute (g: KnowledgeGraph) : Precomputed =
        let keys = g.Records |> Map.keys |> List.ofSeq

        // Authority policy: a supersession is effective when the superseded record
        // declares `superseded_by`, or when the superseding record is accepted and
        // the superseded record does not contradict it. A one-sided claim by a
        // draft record never changes the current authority (it is flagged EKS0021).
        let authoritative (k: string) =
            g.Records
            |> Map.tryFind k
            |> Option.bind _.Status
            |> Option.map (fun s ->
                List.contains (s.ToLowerInvariant()) [ "accepted"; "canonical"; "implemented"; "approved" ])
            |> Option.defaultValue false

        let current k =
            let rec go seen k =
                let declaredByOld =
                    Graph.outgoing g k
                    |> List.filter (fun e -> e.Kind = SupersededBy)
                    |> List.map _.Target
                    |> List.sort

                let claimedByNew =
                    Graph.incoming g k
                    |> List.filter (fun e -> e.Kind = Supersedes && authoritative e.Source)
                    |> List.map _.Source
                    |> List.sort

                match declaredByOld @ claimedByNew |> List.filter (fun n -> not (Set.contains n seen)) with
                | n :: _ -> go (Set.add n seen) n
                | [] -> k

            go (Set.singleton k) k

        let deps k =
            let rec go frontier seen =
                match frontier with
                | [] -> seen
                | _ ->
                    let next =
                        frontier
                        |> List.collect (fun x ->
                            Graph.outgoing g x
                            |> List.filter (fun e -> e.Kind = DependsOn || e.Kind = DerivedFrom)
                            |> List.map _.Target)
                        |> List.filter (fun x -> not (Set.contains x seen) && x <> k)
                        |> List.distinct

                    go next (Set.union seen (Set.ofList next))

            go [ k ] Set.empty

        {
            Impact = keys |> List.map (fun k -> k, Graph.impactOf g 12 k) |> Map.ofList
            Evidence = keys |> List.map (fun k -> k, Strict.evidenceFor g k) |> Map.ofList
            Current = keys |> List.map (fun k -> k, current k) |> Map.ofList
            Dependencies = keys |> List.map (fun k -> k, deps k) |> Map.ofList
        }

    /// Diagnostics computed over the whole graph.
    let private corpusDiagnostics
        (cfg: CompileConfig)
        (units: ParsedUnit list)
        (records: KnowledgeRecord list)
        (g: KnowledgeGraph)
        =
        let fileOf =
            units
            |> List.choose (fun u -> u.Record |> Option.map (fun r -> Graph.key r, u.File))
            |> Map.ofList

        let fromValidation =
            Validation.all cfg.AsOf records g
            |> List.choose (fun v ->
                ruleCode v.Rule
                |> Option.map (fun code ->
                    let file = fileOf |> Map.tryFind v.Record

                    let field =
                        match v.Rule with
                        | "dangling-reference" -> v.Detail.Split(' ') |> Array.tryHead
                        | "superseded-without-replacement" -> Some "superseded_by"
                        | "missing-status" -> Some "status"
                        | _ -> None

                    let value =
                        match v.Rule with
                        | "dangling-reference" -> v.Detail.Split(' ') |> Array.tryItem 2
                        | _ -> None

                    Diagnostic.make
                        code
                        (Some v.Record)
                        (file
                         |> Option.map (fun f ->
                             {
                                 Repository = f.Repository
                                 Path = f.Path
                                 Line = Diagnostic.lineOf f.Text field value
                                 Field = field
                             }))
                        v.Detail))

        let ids = records |> List.map _.Id |> Set.ofList

        // Lower- or mixed-case spellings of existing ids: the reference silently fails.
        let unresolvedTyped =
            g.Unresolved
            |> List.filter (fun (_, rel) -> rel.Kind <> RelatedTo && rel.Kind <> Mentions)
            |> List.filter (fun (_, rel) ->
                rel.Target <> rel.Target.ToUpperInvariant()
                && ids.Contains(rel.Target.ToUpperInvariant()))
            |> List.map (fun (src, rel) ->
                let file = fileOf |> Map.tryFind src
                let field = RelationKind.name rel.Kind

                Diagnostic.make
                    "EKS0002"
                    (Some src)
                    (file
                     |> Option.map (fun f ->
                         {
                             Repository = f.Repository
                             Path = f.Path
                             Line = Diagnostic.lineOf f.Text (Some field) (Some rel.Target)
                             Field = Some field
                         }))
                    $"{field} → {rel.Target} does not resolve; ids are case-sensitive (did you mean {rel.Target.ToUpperInvariant()}?)")

        // Same id in several repositories: identical copies are vendored (advisory);
        // differing copies are conflicting authorities (requires review).
        let collisions =
            records
            |> List.groupBy _.Id
            |> List.filter (fun (_, rs) -> (rs |> List.map _.Provenance.Repository |> List.distinct).Length > 1)
            |> List.map (fun (id, rs) ->
                let variants =
                    rs |> List.map (fun r -> sha256 r.Body) |> List.distinct |> List.length

                let repos =
                    rs |> List.map _.Provenance.Repository |> List.distinct |> String.concat ","

                if variants = 1 then
                    Diagnostic.makeAt
                        Level.Advisory
                        "EKS0008"
                        (Some id)
                        None
                        $"{id}: identical vendored copies in {repos}"
                else
                    Diagnostic.make "EKS0008" (Some id) None $"{id}: {variants} different texts in {repos}")

        let misleadingConfidence =
            records
            |> List.filter (fun r ->
                (r.Kind = Hypothesis || r.Kind = Theory)
                && (r.Confidence = High || r.Confidence = VeryHigh))
            |> List.filter (fun r -> Graph.incoming g (Graph.key r) |> List.forall (fun e -> e.Kind <> Supports))
            |> List.map (fun r ->
                Diagnostic.make
                    "EKS0019"
                    (Some(Graph.key r))
                    None
                    $"confidence {Confidence.name r.Confidence} without supporting evidence")

        let statusLag =
            records
            |> List.filter (fun r -> r.Kind = Hypothesis || r.Kind = Theory)
            |> List.choose (fun r ->
                let s, c = Strict.evidenceFor g (Graph.key r)
                let status = (defaultArg r.Status "").ToLowerInvariant()

                match status, s, c with
                | ("active" | "proposed"), _ :: _, [] ->
                    Some(
                        Diagnostic.make
                            "EKS0010"
                            (Some(Graph.key r))
                            None
                            $"status '{status}' although only supporting evidence is linked"
                    )
                | ("active" | "proposed" | "supported"), [], _ :: _ ->
                    Some(
                        Diagnostic.make
                            "EKS0010"
                            (Some(Graph.key r))
                            None
                            $"status '{status}' although only contradicting evidence is linked"
                    )
                | "supported", _ :: _, _ :: _ ->
                    Some(
                        Diagnostic.make
                            "EKS0010"
                            (Some(Graph.key r))
                            None
                            $"status '{status}' although contradicting evidence is also linked"
                    )
                | ("rejected" | "refuted"), _ :: _, [] ->
                    Some(
                        Diagnostic.make
                            "EKS0010"
                            (Some(Graph.key r))
                            None
                            $"status '{status}' although only supporting evidence is linked"
                    )
                | _ -> None)

        let missingSources =
            match cfg.ExistingPaths with
            | None -> []
            | Some paths ->
                records
                |> List.filter (fun r -> r.Kind = Evidence)
                |> List.choose (fun r -> r.Provenance.SourceUri |> Option.map (fun s -> r, s.Trim()))
                |> List.filter (fun (_, s) ->
                    s <> ""
                    && not (Regex.IsMatch(s, @"^[A-Za-z][A-Za-z0-9+.-]*:"))
                    && not (s.StartsWith "#")
                    && (s.Contains "/" || s.Contains "."))
                |> List.filter (fun (r, s) ->
                    let repo = r.Provenance.Repository

                    let dir =
                        r.Provenance.Path.Split('/')
                        |> Array.rev
                        |> Array.skip 1
                        |> Array.rev
                        |> String.concat "/"

                    let target = (s.Split('#')[0])
                    let clean = target.TrimStart('.', '/')

                    let candidates =
                        [
                            $"{repo}/{clean}"
                            System.IO.Path.GetFullPath(System.IO.Path.Combine("/" + repo, dir, target)).TrimStart('/')
                        ]

                    let fromRoot =
                        System.IO.Path.GetFullPath(System.IO.Path.Combine("/" + repo, target)).TrimStart('/')

                    let escapes =
                        not (candidates[1].StartsWith(repo + "/"))
                        || not (fromRoot.StartsWith(repo + "/"))
                    // Paths that leave the repository cannot be verified here; directories count as existing.
                    not escapes
                    && not (
                        candidates
                        |> List.exists (fun c ->
                            paths.Contains c
                            || paths |> Set.exists (fun p -> p.StartsWith(c.TrimEnd('/') + "/")))
                    ))
                |> List.map (fun (r, s) ->
                    Diagnostic.make
                        "EKS0027"
                        (Some(Graph.key r))
                        (fileOf
                         |> Map.tryFind (Graph.key r)
                         |> Option.map (fun f ->
                             {
                                 Repository = f.Repository
                                 Path = f.Path
                                 Line = Diagnostic.lineOf f.Text (Some "source_uri") None
                                 Field = Some "source_uri"
                             }))
                        $"source_uri '{s}' does not exist in the repository")

        let experimentsWithoutHypothesis =
            records
            |> List.filter (fun r -> r.Kind = Experiment)
            |> List.filter (fun r ->
                match Strict.classify ([], []) r with
                | Error es -> es |> List.contains "experiment without a hypothesis"
                | Ok _ -> false)
            |> List.map (fun r -> Diagnostic.make "EKS0026" (Some(Graph.key r)) None $"{r.Id} names no hypothesis")

        let audience =
            match cfg.Audience with
            | Public ->
                records
                |> List.map _.Provenance.Repository
                |> List.distinct
                |> List.filter (fun repo -> cfg.Visibility |> Map.tryFind repo = Some Private)
                |> List.map (fun repo ->
                    Diagnostic.make
                        "EKS0020"
                        None
                        None
                        $"private repository '{repo}' cannot be compiled for a public audience")
            | Private -> []

        // Named rules: (name, mandatory, findings). The evidence-source check is
        // mandatory only when the existing paths are known.
        let rules =
            [
                "core-validation", true, fromValidation
                "case-sensitive-references", true, unresolvedTyped
                "id-collisions", true, collisions
                "confidence-backed-by-evidence", false, misleadingConfidence
                "status-agrees-with-evidence", false, statusLag
                "experiment-names-hypothesis", false, experimentsWithoutHypothesis
                "evidence-source-exists", cfg.ExistingPaths.IsSome, missingSources
                "audience", true, audience
            ]

        let runs =
            rules
            |> List.map (fun (name, mandatory, ds) ->
                let executed =
                    not (cfg.DisabledRules.Contains name)
                    && (name <> "evidence-source-exists" || cfg.ExistingPaths.IsSome)

                {
                    Rule = name
                    Mandatory = mandatory
                    Executed = executed
                    Findings = if executed then List.length ds else 0
                })

        let findings =
            List.zip rules runs
            |> List.collect (fun ((_, _, ds), run) -> if run.Executed then ds else [])

        findings, runs

    let private sortDiagnostics (ds: Diagnostic list) =
        ds
        |> List.sortBy (fun d ->
            d.Code,
            defaultArg d.Record "",
            d.Location
            |> Option.map (fun l -> l.Repository, l.Path, l.Line)
            |> Option.defaultValue ("", "", 0),
            d.Message)

    /// Compiles files, reusing parsed units from `cache` whose content hash is
    /// unchanged. Returns the compilation and the new cache. Deterministic.
    let compile (cfg: CompileConfig) (cache: Cache) (files: SourceFile list) : Compilation * Cache =
        let files = files |> List.sortBy (fun f -> f.Repository, f.Path)

        let units =
            files
            |> List.map (fun f ->
                match cache |> Map.tryFind $"{f.Repository}/{f.Path}" with
                | Some u when u.File.Hash = f.Hash -> u
                | _ -> parse cfg f)

        let records = units |> List.choose _.Record
        let g = Graph.build false records

        let corpusFindings, corpusRuns = corpusDiagnostics cfg units records g
        let parseFindings = units |> List.collect _.Diagnostics

        let parseRun =
            {
                Rule = "parse-and-field-checks"
                Mandatory = true
                Executed = true
                Findings = parseFindings.Length
            }

        let diagnostics = parseFindings @ corpusFindings |> sortDiagnostics

        let inputsHash =
            files
            |> List.map (fun f -> $"{f.Repository}/{f.Path}:{f.Hash}")
            |> String.concat "\n"
            |> sha256

        let comp =
            {
                Config = cfg
                Units = units
                Records = records
                Graph = g
                Diagnostics = diagnostics
                Precomputed = precompute g
                InputsHash = inputsHash
                Rules = parseRun :: corpusRuns
            }

        comp,
        (units
         |> List.map (fun u -> $"{u.File.Repository}/{u.File.Path}", u)
         |> Map.ofList)

    let hasErrors (c: Compilation) =
        c.Diagnostics |> List.exists (fun d -> d.Level = Level.Error)
