module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxLedger.Ledger

let entry id tenant amount (date: DateOnly) approved =
    { Id = id; TenantId = tenant; Amount = amount; Description = "x"; Date = date
      CreatedBy = "u9"; Approved = approved; ApprovedBy = None; RefundOf = None }

let d (m: int) (day: int) = DateOnly(2026, m, day)

[<Fact>]
let ``balance counts approved entries well before the cut-off`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, balance "t1" (d 3 1) entries)

[<Fact>]
let ``balance excludes entries after the cut-off`` () =
    Assert.Equal(0m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 2 1) true ])

// ---- WI-0002 addEntry
let today = d 6 15
let withDesc desc (e: Entry) = { e with Description = desc }
let withAmt a (e: Entry) = { e with Amount = a }
let fresh = entry "n1" "t1" 10m today false

[<Fact>]
let ``addEntry accepts boundary amount and rejects out of range`` () =
    Assert.True(Result.isOk (addEntry today (withAmt 1000000m fresh) []))
    Assert.Equal(Error InvalidAmount, addEntry today (withAmt 1000000.01m fresh) [])
    Assert.Equal(Error InvalidAmount, addEntry today (withAmt 0m fresh) [])

[<Fact>]
let ``addEntry description length is checked after trimming`` () =
    Assert.True(Result.isOk (addEntry today (withDesc (" " + String('a', 200) + " ") fresh) []))
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc (String('a', 201)) fresh) [])
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc "   " fresh) [])

[<Fact>]
let ``addEntry rejects control characters`` () =
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc "a\tb" fresh) [])
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc "a\nb" fresh) [])

[<Fact>]
let ``addEntry date today accepted tomorrow rejected`` () =
    Assert.True(Result.isOk (addEntry today fresh []))
    Assert.Equal(Error FutureDate, addEntry today { fresh with Date = today.AddDays 1 } [])

[<Fact>]
let ``addEntry rejects duplicate id in any tenant`` () =
    Assert.Equal(Error DuplicateId, addEntry today fresh [ entry "n1" "t2" 1m (d 1 1) true ])

[<Fact>]
let ``addEntry reports first failing rule in DF-FXL-0001 order`` () =
    let bad = { fresh with Amount = 0m; Date = today.AddDays 1 }
    let bad = { bad with Description = "" }
    Assert.Equal(Error InvalidAmount, addEntry today bad [ fresh ])
    Assert.Equal(Error InvalidDescription, addEntry today { bad with Amount = 1m } [ fresh ])
    Assert.Equal(Error FutureDate, addEntry today { bad with Amount = 1m; Description = "ok" } [ fresh ])
    Assert.Equal(Error DuplicateId, addEntry today fresh [ fresh ])

[<Fact>]
let ``addEntry stores trimmed unapproved entry appended last`` () =
    let existing = [ entry "e1" "t1" 1m (d 1 1) true ]
    let input = { fresh with Description = "  rent "; Approved = true; ApprovedBy = Some "boss" }
    match addEntry today input existing with
    | Ok [ first; added ] ->
        Assert.Equal(existing.Head, first)
        Assert.Equal("rent", added.Description)
        Assert.False added.Approved
        Assert.Equal(None, added.ApprovedBy)
        Assert.Equal("n1", added.Id)
    | other -> failwithf "unexpected %A" other

// ---- WI-0004 balance
[<Fact>]
let ``balance includes approved entry dated exactly on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 2 1) [ entry "e1" "t1" 10m (d 2 1) true ])

[<Fact>]
let ``balance excludes unapproved and other tenants on the cut-off date`` () =
    let es = [ entry "e1" "t1" 10m (d 2 1) false; entry "e2" "t2" 4m (d 2 1) true; entry "e3" "t1" 1m (d 2 2) true ]
    Assert.Equal(0m, balance "t1" (d 2 1) es)

// ---- WI-0006 exportCsv
[<Fact>]
let ``exportCsv header only when nothing approved`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t1" 1m (d 1 1) false ])

[<Fact>]
let ``exportCsv formats rows sorted by date then ordinal id`` () =
    let es =
        [ { entry "b" "t1" 5m (d 1 2) true with Description = "x" }
          { entry "B" "t1" 1.5m (d 1 2) true with Description = "y" }
          { entry "z" "t1" -5m (d 1 1) true with Description = "r" }
          entry "o" "t2" 1m (d 1 1) true
          entry "u" "t1" 1m (d 1 1) false ]
    Assert.Equal("id,date,amount,description\nz,2026-01-01,-5.00,r\nB,2026-01-02,1.50,y\nb,2026-01-02,5.00,x\n", exportCsv "t1" es)

[<Fact>]
let ``exportCsv quotes special characters`` () =
    let es =
        [ { entry "a" "t1" 1m (d 1 1) true with Description = "a,b" }
          { entry "b" "t1" 1m (d 1 2) true with Description = "say \"hi\"" }
          { entry "c" "t1" 1m (d 1 3) true with Description = "l1\nl2" }
          { entry "d" "t1" 1m (d 1 4) true with Description = "l1\rl2" } ]
    Assert.Equal(
        "id,date,amount,description\na,2026-01-01,1.00,\"a,b\"\nb,2026-01-02,1.00,\"say \"\"hi\"\"\"\nc,2026-01-03,1.00,\"l1\nl2\"\nd,2026-01-04,1.00,\"l1\rl2\"\n",
        exportCsv "t1" es)

[<Fact>]
let ``exportCsv neutralises formulas before quoting`` () =
    let es =
        [ { entry "a" "t1" 1m (d 1 1) true with Description = "=SUM(A1)" }
          { entry "b" "t1" 1m (d 1 2) true with Description = "-5,x" }
          { entry "c" "t1" 1m (d 1 3) true with Description = "+1" }
          { entry "d" "t1" 1m (d 1 4) true with Description = "@x" } ]
    Assert.Equal(
        "id,date,amount,description\na,2026-01-01,1.00,'=SUM(A1)\nb,2026-01-02,1.00,\"'-5,x\"\nc,2026-01-03,1.00,'+1\nd,2026-01-04,1.00,'@x\n",
        exportCsv "t1" es)

// ---- WI-0008 refund
open FxIdentity.Identity

let now = DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)
let session roles = { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddHours -1.0; LastSeen = now.AddMinutes -1.0 }
let original = entry "o1" "t1" 100m (d 6 1) true
let doRefund s id amt es = refund now s today id "o1" amt es

[<Fact>]
let ``refund expired session`` () =
    let s = { session [ Clerk ] with LastSeen = now.AddMinutes -16.0 }
    Assert.Equal(Error SessionExpired, doRefund s "r1" 10m [ original ])

[<Fact>]
let ``refund needs Clerk or Admin`` () =
    Assert.Equal(Error NotAuthorized, doRefund (session [ Viewer; Approver ]) "r1" 10m [ original ])
    Assert.True(Result.isOk (doRefund (session [ Admin ]) "r1" 10m [ original ]))
    Assert.True(Result.isOk (doRefund (session [ Clerk ]) "r1" 10m [ original ]))

[<Fact>]
let ``refund original must exist in tenant be approved and not a refund`` () =
    let s = session [ Clerk ]
    Assert.Equal(Error NotFound, doRefund s "r1" 10m [])
    Assert.Equal(Error NotFound, doRefund s "r1" 10m [ { original with TenantId = "t2" } ])
    Assert.Equal(Error NotFound, doRefund s "r1" 10m [ { original with Approved = false } ])
    Assert.Equal(Error NotFound, doRefund s "r1" 10m [ { original with RefundOf = Some "x" } ])

[<Fact>]
let ``refund duplicate id and invalid amount`` () =
    let s = session [ Clerk ]
    Assert.Equal(Error DuplicateId, doRefund s "o1" 10m [ original ])
    Assert.Equal(Error InvalidAmount, doRefund s "r1" 0m [ original ])
    Assert.Equal(Error InvalidAmount, doRefund s "r1" -1m [ original ])

[<Fact>]
let ``refund 60 then 40 succeed and further 0.01 rejected`` () =
    let s = session [ Clerk ]
    let r1 = doRefund s "r1" 60m [ original ] |> Result.toOption |> Option.get
    let r2 = doRefund s "r2" 40m r1 |> Result.toOption |> Option.get
    Assert.Equal(Error RefundExceedsOriginal, doRefund s "r3" 0.01m r2)

[<Fact>]
let ``refund larger than original is rejected`` () =
    Assert.Equal(Error RefundExceedsOriginal, doRefund (session [ Clerk ]) "r1" 100.01m [ original ])

[<Fact>]
let ``refund appends the expected entry`` () =
    match doRefund (session [ Clerk ]) "r1" 25m [ original ] with
    | Ok [ _; r ] ->
        Assert.Equal({ Id = "r1"; TenantId = "t1"; Amount = -25m; Description = "Refund of o1"; Date = today
                       CreatedBy = "u1"; Approved = false; ApprovedBy = None; RefundOf = Some "o1" }, r)
    | other -> failwithf "unexpected %A" other

[<Fact>]
let ``refund window 90 days allowed 91 rejected`` () =
    let s = session [ Clerk ]
    Assert.True(Result.isOk (doRefund s "r1" 1m [ { original with Date = today.AddDays -90 } ]))
    Assert.Equal(Error(Unsupported "refund window closed"), doRefund s "r1" 1m [ { original with Date = today.AddDays -91 } ])

[<Fact>]
let ``refund window is checked before duplicate id`` () =
    let old = { original with Date = today.AddDays -91 }
    Assert.Equal(Error(Unsupported "refund window closed"), doRefund (session [ Clerk ]) "o1" 1m [ old ])
