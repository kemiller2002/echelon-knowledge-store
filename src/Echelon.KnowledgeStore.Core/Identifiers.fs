namespace Echelon.KnowledgeStore

open System

/// A stable knowledge identifier such as `REQ-0042` or `fides:DEC-0012`.
/// The optional repository qualifier names the owning repository.
type KnowledgeId = private KnowledgeId of repository: string option * local: string

/// Pure functions over knowledge identifiers.
[<RequireQualifiedAccess>]
module KnowledgeId =

    let private isLocalChar (c: char) =
        Char.IsAsciiLetterOrDigit c || c = '-' || c = '_' || c = '.'

    let private isRepoChar (c: char) =
        Char.IsAsciiLetterLower c || Char.IsAsciiDigit c || c = '-'

    let private validLocal (s: string) =
        s.Length > 0
        && s.Length <= 128
        && Char.IsAsciiLetterOrDigit s[0]
        && Seq.forall isLocalChar s

    let private validRepo (s: string) =
        s.Length > 0 && s.Length <= 100 && Seq.forall isRepoChar s

    /// Parses `LOCAL` or `repository:LOCAL`. Returns an error message on invalid input.
    let parse (text: string | null) : Result<KnowledgeId, string> =
        match text |> Option.ofObj |> Option.map (fun t -> t.Trim()) with
        | None
        | Some "" -> Error "identifier is empty"
        | Some t ->
            match t.Split(':') with
            | [| local |] when validLocal local -> Ok(KnowledgeId(None, local))
            | [| repo; local |] when validRepo repo && validLocal local -> Ok(KnowledgeId(Some repo, local))
            | _ -> Error $"invalid identifier '{t}'"

    /// The repository qualifier, if present.
    let repository (KnowledgeId(repo, _)) = repo

    /// The repository-local part.
    let local (KnowledgeId(_, local)) = local

    /// Qualifies an unqualified identifier with its owning repository.
    let qualify (owner: string) (KnowledgeId(repo, local) as id) =
        match repo with
        | Some _ -> id
        | None -> KnowledgeId(Some owner, local)

    /// Canonical text form.
    let format (KnowledgeId(repo, local)) =
        match repo with
        | Some r -> $"{r}:{local}"
        | None -> local
