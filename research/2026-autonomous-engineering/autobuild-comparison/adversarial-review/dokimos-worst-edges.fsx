#r "/home/user/dokimos/src/Dokimos.Core/bin/Release/net8.0/Dokimos.Core.dll"
open Dokimos.Core.Verification
let sha = "0123456789abcdef0123456789abcdef01234567"
// xUnit theory: 3 InlineData rows, one carries Skip="..." -> VSTest TRX writes outcome="NotExecuted"
let trx =
    "<TestRun xmlns=\"http://microsoft.com/schemas/VisualStudio/TeamTest/2010\"><Results>"
    + "<UnitTestResult testId=\"r1\" testName=\"Rounds(x: 1)\" outcome=\"Passed\"/>"
    + "<UnitTestResult testId=\"r2\" testName=\"Rounds(x: 2)\" outcome=\"Passed\"/>"
    + "<UnitTestResult testId=\"r3\" testName=\"Rounds(x: 3)\" outcome=\"NotExecuted\"/>"
    + "</Results><TestDefinitions>"
    + "<UnitTest id=\"r1\"><TestMethod className=\"App.Tests\" name=\"Rounds\"/></UnitTest>"
    + "<UnitTest id=\"r2\"><TestMethod className=\"App.Tests\" name=\"Rounds\"/></UnitTest>"
    + "<UnitTest id=\"r3\"><TestMethod className=\"App.Tests\" name=\"Rounds\"/></UnitTest>"
    + "</TestDefinitions></TestRun>"
let run id = { Suite = "S"; Format = Trx; Path = "r.trx"; SourceSha = sha; RunId = id }
match TestResults.parseTrx trx with
| Ok rows ->
    printfn "parsed rows: %A" rows
    let r = Coverage.admit sha [ run "1", rows ]
    printfn "theory with one skipped row -> %A" (r.Accepted.["S"].["dotnet:App.Tests.Rounds"])
| Error e -> printfn "%s" e
// Same evidence, rows listed in different order before the fix would have flipped; now stable:
printfn "worst [Passed; NotExecuted] = %A" (TestResults.worst [Passed; NotExecuted])
// Format inconsistency: Playwright list parser vs coverage merge for pass+skip of one title
let log = "  ✓  1 [chromium] › e2e/a.spec.ts:3:5 › logs in (1.2s)\n  -  1 [chromium] › e2e/a.spec.ts:3:5 › logs in\n"
printfn "playwright-list pass+skip     -> %A" (TestResults.parsePlaywrightList "" log)
printfn "coverage mergeWorst pass+skip -> %A" (TestResults.mergeWorst [ "id", Passed; "id", Skipped ])
// Omitting the failing run from the caller-written manifest still verifies:
let r2 = Coverage.admit sha [ run "2", [ "dotnet:App.Tests.Rounds", Passed ] ]
printfn "manifest lists only the passing rerun -> %A" (r2.Accepted.["S"].["dotnet:App.Tests.Rounds"])
