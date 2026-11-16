module Echelon.KnowledgeStore.Bench.Tokens

open System.IO
open Microsoft.ML.Tokenizers

/// A tokenizer family used for cost measurement.
type Family = { Name: string; Count: string -> int }

let private tiktoken (encoding: string) =
    let t = TiktokenTokenizer.CreateForEncoding encoding

    {
        Name = encoding
        Count = fun s -> t.CountTokens s
    }

/// o200k_base (GPT-4o/GPT-5 family), cl100k_base (GPT-4 family), and a Llama
/// SentencePiece model when `benchmarks/out/tok/llama.model` exists.
/// Claude's tokenizer is not public; it is not measured.
let families (llamaModel: string) : Family list =
    let llama =
        if File.Exists llamaModel then
            use s = File.OpenRead llamaModel
            let t = LlamaTokenizer.Create s

            [
                {
                    Name = "llama-sp"
                    Count = fun s -> t.CountTokens s
                }
            ]
        else
            []

    [ tiktoken "o200k_base"; tiktoken "cl100k_base" ] @ llama

/// The primary family used where a single number is reported.
let primary (fs: Family list) = List.head fs
