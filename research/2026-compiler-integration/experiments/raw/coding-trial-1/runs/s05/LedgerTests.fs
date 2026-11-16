module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxLedger.Ledger
open FxIdentity.Identity

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

[<Fact>]
let ``balance includes entries dated on the cut-off`` () =
    Assert.Equal(10m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 1 1) true ])

let today = d 6 1

[<Fact>]
let ``addEntry stores trimmed unapproved entry`` () =
    let e = { entry "n1" "t1" 5m (d 1 1) true with Description = "  hi  "; ApprovedBy = Some "a" }
    let existing = [ entry "e1" "t1" 1m (d 1 1) true ]
    match addEntry today e existing with
    | Ok [ x; y ] ->
        Assert.Equal("e1", x.Id)
        Assert.Equal("hi", y.Description)
        Assert.False y.Approved
        Assert.Equal(None, y.ApprovedBy)
    | r -> failwithf "%A" r

[<Fact>]
let ``addEntry validation order and rules`` () =
    let ex = [ entry "e1" "t1" 1m (d 1 1) true ]
    let bad = { entry "e1" "t1" 0m (d 12 1) true with Description = " " }
    Assert.Equal(Error InvalidAmount, addEntry today bad ex)
    Assert.Equal(Error InvalidAmount, addEntry today { bad with Amount = 1000001m } ex)
    Assert.Equal(Error InvalidDescription, addEntry today { bad with Amount = 1m } ex)
    Assert.Equal(Error InvalidDescription, addEntry today { bad with Amount = 1m; Description = String('a', 201) } ex)
    Assert.Equal(Error FutureDate, addEntry today { bad with Amount = 1m; Description = "x" } ex)
    Assert.Equal(Error DuplicateId, addEntry today { bad with Amount = 1m; Description = "x"; Date = today } ex)
    Assert.True(Result.isOk (addEntry today { bad with Id = "z"; Amount = 1000000m; Description = String('a', 200); Date = today } ex))

[<Fact>]
let ``exportCsv format`` () =
    let es =
        [ { entry "b" "t1" 5m (d 1 2) true with Description = "a,\"b\"" }
          entry "a" "t1" -5m (d 1 2) true
          entry "c" "t1" 1.5m (d 1 1) true
          entry "u" "t1" 1m (d 1 1) false
          entry "o" "t2" 1m (d 1 1) true ]
    Assert.Equal("id,date,amount,description\nc,2026-01-01,1.50,x\na,2026-01-02,-5.00,x\nb,2026-01-02,5.00,\"a,\"\"b\"\"\"\n", exportCsv "t1" es)
    Assert.Equal("id,date,amount,description\n", exportCsv "t9" es)

let now = DateTimeOffset(2026, 6, 1, 12, 0, 0, TimeSpan.Zero)
let sess roles = { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now; LastSeen = now }

[<Fact>]
let ``refund rules`` () =
    let orig = entry "o" "t1" 100m (d 1 1) true
    let r id amt es = refund now (sess [ Clerk ]) today id "o" amt es
    let ok = function Ok es -> es | e -> failwithf "%A" e
    let es = [ orig ] |> r "r1" 60m |> ok |> r "r2" 40m |> ok
    Assert.Equal(Error RefundExceedsOriginal, r "r3" 0.01m es)
    Assert.Equal(Error RefundExceedsOriginal, r "r1" 101m [ orig ] |> Result.map (fun _ -> []) |> fun _ -> r "rx" 101m [ orig ])
    Assert.Equal(Error DuplicateId, r "o" 1m [ orig ])
    Assert.Equal(Error InvalidAmount, r "r9" 0m [ orig ])
    Assert.Equal(Error NotFound, r "r9" 1m [ { orig with Approved = false } ])
    Assert.Equal(Error NotFound, refund now (sess [ Clerk ]) today "r9" "r1" 1m es)
    Assert.Equal(Error NotAuthorized, refund now (sess [ Approver ]) today "r9" "o" 1m [ orig ])
    Assert.Equal(Error SessionExpired, refund now { sess [ Clerk ] with LastSeen = now.AddMinutes -16.0 } today "r9" "o" 1m [ orig ])
    let last = List.last es
    Assert.Equal(-40m, last.Amount)
    Assert.Equal("Refund of o", last.Description)
    Assert.Equal(Some "o", last.RefundOf)
    Assert.False last.Approved
