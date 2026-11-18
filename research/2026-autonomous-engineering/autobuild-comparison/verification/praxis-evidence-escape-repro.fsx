#r "/home/user/praxis/src/Praxis.Infrastructure/bin/Release/net10.0/Praxis.Domain.dll"
#r "/home/user/praxis/src/Praxis.Infrastructure/bin/Release/net10.0/Praxis.Application.dll"
#r "/home/user/praxis/src/Praxis.Infrastructure/bin/Release/net10.0/Praxis.Infrastructure.dll"
open System.IO
open Praxis.Domain.Work
open Praxis.Infrastructure.Work
let parent = Path.Combine(Path.GetTempPath(), "prx-repro")
let root = Path.Combine(parent, "repo")
Directory.CreateDirectory root |> ignore
File.WriteAllText(Path.Combine(parent, "outside.txt"), "forged")
let repo = FileEvidenceRepository.create root
for p in [ Path.Combine(parent, "outside.txt"); "../outside.txt"; "/etc/hostname"; "." ] do
    printfn "%-40s -> %A" p (repo.Observe { Type = "tests"; Path = p })
