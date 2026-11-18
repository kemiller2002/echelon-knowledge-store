#r "/home/user/praxis/src/Praxis.Infrastructure/bin/Release/net10.0/Praxis.Domain.dll"
#r "/home/user/praxis/src/Praxis.Infrastructure/bin/Release/net10.0/Praxis.Application.dll"
#r "/home/user/praxis/src/Praxis.Infrastructure/bin/Release/net10.0/Praxis.Infrastructure.dll"
open System.IO
open Praxis.Domain.Work
open Praxis.Infrastructure.Work
let parent = Path.Combine(__SOURCE_DIRECTORY__, "prx-bypass")
if Directory.Exists parent then Directory.Delete(parent, true)
let root = Path.Combine(parent, "repo")
let outside = Path.Combine(parent, "outside")
Directory.CreateDirectory root |> ignore
Directory.CreateDirectory outside |> ignore
File.WriteAllText(Path.Combine(outside, "forged-tests.txt"), "forged: 100 passed")
// 1. symlink to a directory outside the repo; cite a file *through* it
Directory.CreateSymbolicLink(Path.Combine(root, "evidence"), outside) |> ignore
// 2. symlink on the final entry (the case the fix handles)
File.CreateSymbolicLink(Path.Combine(root, "direct-link.txt"), Path.Combine(outside, "forged-tests.txt")) |> ignore
// 3. nested: in-repo dir -> link chain
Directory.CreateDirectory(Path.Combine(root, "a")) |> ignore
Directory.CreateSymbolicLink(Path.Combine(root, "a", "b"), "../../outside") |> ignore
// 4. untracked / ignored file inside the worktree (not in any commit)
Directory.CreateDirectory(Path.Combine(root, "bin")) |> ignore
File.WriteAllText(Path.Combine(root, "bin", "never-committed.txt"), "x")
// 5. repository root itself / directory citation
// 6. root reached via a symlink: in-repo link to in-repo file
let realRoot = Path.Combine(parent, "real")
Directory.CreateDirectory realRoot |> ignore
File.WriteAllText(Path.Combine(realRoot, "t.txt"), "ok")
File.CreateSymbolicLink(Path.Combine(realRoot, "alias.txt"), "t.txt") |> ignore
let linkedRoot = Path.Combine(parent, "linked-root")
Directory.CreateSymbolicLink(linkedRoot, realRoot) |> ignore
let repo = FileEvidenceRepository.create root
for p in [ "evidence/forged-tests.txt"; "direct-link.txt"; "a/b/forged-tests.txt"; "bin/never-committed.txt"; "."; "" ] do
    printfn "%-32s -> %A" p (repo.Observe { Type = "tests"; Path = p })
let repo2 = FileEvidenceRepository.create linkedRoot
for p in [ "t.txt"; "alias.txt" ] do
    printfn "[root via symlink] %-14s -> %A" p (repo2.Observe { Type = "tests"; Path = p })
