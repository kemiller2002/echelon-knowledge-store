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

open FxIdentity.Identity

let today = d 6 1
let ok r = match r with Ok v -> v | Error e -> failwithf "unexpected %A" e
let withDesc desc amount date id = { entry id "t1" amount date false with Description = desc }

[<Fact>]
let ``balance includes entries dated on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 1m (d 1 2) true ])

[<Fact>]
let ``addEntry amount bounds`` () =
    Assert.True(Result.isOk (addEntry today (withDesc "a" 1000000m today "n") []))
    Assert.Equal(Error InvalidAmount, addEntry today (withDesc "a" 1000000.01m today "n") [])
    Assert.Equal(Error InvalidAmount, addEntry today (withDesc "a" 0m today "n") [])

[<Fact>]
let ``addEntry description bounds and control characters`` () =
    Assert.True(Result.isOk (addEntry today (withDesc (" " + String('a', 200) + " ") 1m today "n") []))
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc (String('a', 201)) 1m today "n") [])
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc "  " 1m today "n") [])
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc "a\tb" 1m today "n") [])
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc "a\nb" 1m today "n") [])

[<Fact>]
let ``addEntry date, duplicate and validation order`` () =
    Assert.True(Result.isOk (addEntry today (withDesc "a" 1m today "n") []))
    Assert.Equal(Error FutureDate, addEntry today (withDesc "a" 1m (d 6 2) "n") [])
    Assert.Equal(Error DuplicateId, addEntry today (withDesc "a" 1m today "e1") [ entry "e1" "t2" 1m today true ])
    Assert.Equal(Error InvalidAmount, addEntry today (withDesc "" 0m (d 6 2) "e1") [ entry "e1" "t1" 1m today true ])
    Assert.Equal(Error InvalidDescription, addEntry today (withDesc "" 1m (d 6 2) "e1") [ entry "e1" "t1" 1m today true ])
    Assert.Equal(Error FutureDate, addEntry today (withDesc "a" 1m (d 6 2) "e1") [ entry "e1" "t1" 1m today true ])

[<Fact>]
let ``addEntry stores trimmed unapproved entry appended last`` () =
    let e0 = entry "e0" "t1" 1m today true
    let r = addEntry today { withDesc "  rent " 5m today "n" with Approved = true; ApprovedBy = Some "x" } [ e0 ] |> ok
    Assert.Equal(2, r.Length)
    Assert.Equal(e0, r.Head)
    let n = List.last r
    Assert.Equal("rent", n.Description)
    Assert.False n.Approved
    Assert.Equal(None, n.ApprovedBy)

[<Fact>]
let ``exportCsv header only for empty tenant`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "e1" "t2" 1m today true; entry "e2" "t1" 1m today false ])

[<Fact>]
let ``exportCsv quoting, formulas, order and amounts`` () =
    let es =
        [ { entry "b" "t1" 2m (d 1 2) true with Description = "a,\"b\"" }
          { entry "a" "t1" -5m (d 1 2) true with Description = "=SUM(A1)" }
          { entry "c" "t1" 1.5m (d 1 1) true with Description = "-x,y" } ]
    let expected =
        "id,date,amount,description\n"
        + "c,2026-01-01,1.50,\"'-x,y\"\n"
        + "a,2026-01-02,-5.00,'=SUM(A1)\n"
        + "b,2026-01-02,2.00,\"a,\"\"b\"\"\"\n"
    Assert.Equal(expected, exportCsv "t1" es)

let now = DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
let clerk = { UserId = "u1"; TenantId = "t1"; Roles = [ Clerk ]; IssuedAt = now; LastSeen = now }
let orig = entry "o" "t1" 100m (d 5 1) true
let refundOk amt es = refund now clerk today "r" "o" amt es

[<Fact>]
let ``refund session and role checks`` () =
    Assert.Equal(Error SessionExpired, refund now { clerk with LastSeen = now - TimeSpan.FromMinutes 16.0 } today "r" "o" 1m [ orig ])
    Assert.True(Result.isOk (refund now { clerk with LastSeen = now - TimeSpan.FromMinutes 15.0 } today "r" "o" 1m [ orig ]))
    Assert.Equal(Error NotAuthorized, refund now { clerk with Roles = [ Viewer; Approver ] } today "r" "o" 1m [ orig ])
    Assert.True(Result.isOk (refund now { clerk with Roles = [ Admin ] } today "r" "o" 1m [ orig ]))

[<Fact>]
let ``refund original lookup`` () =
    Assert.Equal(Error NotFound, refundOk 1m [])
    Assert.Equal(Error NotFound, refundOk 1m [ { orig with TenantId = "t2" } ])
    Assert.Equal(Error NotFound, refundOk 1m [ { orig with Approved = false } ])
    Assert.Equal(Error NotFound, refundOk 1m [ { orig with RefundOf = Some "z" } ])

[<Fact>]
let ``refund duplicate id and amount`` () =
    Assert.Equal(Error DuplicateId, refund now clerk today "o" "o" 1m [ orig ])
    Assert.Equal(Error InvalidAmount, refundOk 0m [ orig ])
    Assert.Equal(Error InvalidAmount, refundOk -1m [ orig ])

[<Fact>]
let ``refund cannot exceed original`` () =
    let r1 = refund now clerk today "r1" "o" 60m [ orig ] |> ok
    let r2 = refund now clerk today "r2" "o" 40m r1 |> ok
    Assert.Equal(Error RefundExceedsOriginal, refund now clerk today "r3" "o" 0.01m r2)
    Assert.Equal(Error RefundExceedsOriginal, refundOk 100.01m [ orig ])

[<Fact>]
let ``refund stored form`` () =
    let r = refundOk 25m [ orig ] |> ok
    let n = List.last r
    Assert.Equal({ Id = "r"; TenantId = "t1"; Amount = -25m; Description = "Refund of o"; Date = today
                   CreatedBy = "u1"; Approved = false; ApprovedBy = None; RefundOf = Some "o" }, n)

[<Fact>]
let ``refund window of 90 days`` () =
    let o = { orig with Date = today.AddDays -90 }
    Assert.True(Result.isOk (refundOk 1m [ o ]))
    Assert.Equal(Error(Unsupported "refund window closed"), refundOk 1m [ { orig with Date = today.AddDays -91 } ])
    // window check precedes duplicate-id check
    Assert.Equal(Error(Unsupported "refund window closed"), refund now clerk today "o" "o" 1m [ { orig with Date = today.AddDays -91 } ])
