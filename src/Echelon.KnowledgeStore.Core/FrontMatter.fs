namespace Echelon.KnowledgeStore

open System

/// A Markdown document split into optional YAML front matter and body.
type MarkdownDocument =
    {
        FrontMatter: Yaml option
        FrontMatterText: string option
        Body: string
    }

[<RequireQualifiedAccess>]
module FrontMatter =

    let private normalise (text: string) = text.Replace("\r\n", "\n")

    /// Splits front matter delimited by `---` lines. Never throws; a front-matter
    /// block that fails to parse is reported as an error.
    let split (text: string) : Result<MarkdownDocument, string> =
        let t = normalise text

        if not (t.StartsWith "---\n") then
            Ok
                {
                    FrontMatter = None
                    FrontMatterText = None
                    Body = t
                }
        else
            let lines = t.Split('\n')

            lines
            |> Array.skip 1
            |> Array.tryFindIndex (fun l -> l.TrimEnd() = "---" || l.TrimEnd() = "...")
            |> function
                | None -> Error "unterminated front matter"
                | Some i ->
                    let fm = String.Join("\n", lines[1..i])
                    let body = String.Join("\n", lines[i + 2 ..])

                    Yaml.parse fm
                    |> Result.map (fun y ->
                        {
                            FrontMatter = Some y
                            FrontMatterText = Some fm
                            Body = body
                        })
