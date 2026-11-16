namespace Echelon.KnowledgeStore

open System
open System.Text.Json

/// Renders records as F# source (format D). Writer only: reading F# source
/// safely needs the F# compiler's parser; evaluating it would execute code.
[<RequireQualifiedAccess>]
module FSharpSource =

    let private str (s: string) =
        if s.Contains "\"\"\"" || s.EndsWith "\"" then
            JsonSerializer.Serialize(
                s,
                JsonSerializerOptions(Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping)
            )
        else
            "\"\"\"" + s + "\"\"\""

    let private shortStr (s: string) = JsonSerializer.Serialize s

    let private opt f =
        function
        | Some v -> $"Some({f v})"
        | None -> "None"

    let private date (d: DateOnly) =
        $"DateOnly({d.Year}, {d.Month}, {d.Day})"

    let private kindCase =
        function
        | Other s -> $"Other {shortStr s}"
        | k -> sprintf "%A" k

    let private relCase (k: RelationKind) = sprintf "%A" k

    let ofRecord (r: KnowledgeRecord) =
        let p = r.Provenance

        let rels =
            r.Relations
            |> List.map (fun x -> $"{{ Kind = {relCase x.Kind}; Target = {shortStr x.Target} }}")
            |> String.concat "; "

        let extra =
            r.Extra
            |> List.map (fun (k, v) ->
                let items = Yaml.strings v |> List.map shortStr |> String.concat "; "
                $"{shortStr k}, [ {items} ]")
            |> String.concat "; "

        $"""    {{ Id = {shortStr r.Id}
      Kind = {kindCase r.Kind}
      Title = {shortStr r.Title}
      Status = {opt shortStr r.Status}
      Confidence = {sprintf "%A" r.Confidence}
      Relations = [ {rels} ]
      Evidence = [ {extra} ]
      Provenance =
        {{ Repository = {shortStr p.Repository}
          Path = {shortStr p.Path}
          Author = {opt shortStr p.Author}
          SourceUri = {opt shortStr p.SourceUri}
          Created = {opt date p.Created}
          Updated = {opt date p.Updated}
          Retrieved = {opt date p.Retrieved} }}
      Body = {str r.Body} }}"""

    let write (records: KnowledgeRecord list) =
        "module Knowledge\n\nopen System\nopen Echelon.KnowledgeStore\n\nlet records =\n  [\n"
        + (records |> List.map ofRecord |> String.concat "\n")
        + "\n  ]\n"
