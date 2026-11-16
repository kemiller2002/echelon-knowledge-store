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

// ---- WI-0001: tenant total (RQ-FXL-0003, DF-FXL-0007) ----

[<Fact>]
let ``approved 0.005 alone totals 0.01`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])

[<Fact>]
let ``approved -0.005 alone totals -0.01`` () =
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" (-0.005m) (d 1 1) true ])

[<Fact>]
let ``unapproved entries and other tenants' entries do not count`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, total "t1" entries)

[<Fact>]
let ``refund entries with negative amounts are included in the total`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" (-4m) (d 1 2) true ]
    Assert.Equal(6m, total "t1" entries)

// ---- WI-0003: entry approval (RQ-FXL-0004, RQ-FXL-0104, RQ-FXID-0002/0004) ----

let now = DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero)

let session (userId: string) (tenant: string) (roles: Role list) (lastSeen: DateTimeOffset) : Session =
    { UserId = userId; TenantId = tenant; Roles = roles; IssuedAt = now.AddHours(-1.0); LastSeen = lastSeen }

let errorOf (r: Result<Entry list, LedgerError>) =
    match r with
    | Error e -> Some e
    | Ok _ -> None

let approvedFlag (id: string) (r: Result<Entry list, LedgerError>) =
    match r with
    | Ok es -> es |> List.find (fun e -> e.Id = id) |> fun e -> Some(e.Approved, e.ApprovedBy)
    | Error _ -> None

[<Fact>]
let ``a session idle for exactly 15 minutes can approve`` () =
    let s = session "u2" "t1" [ Approver ] (now.AddMinutes(-15.0))
    let r = approve now s "e1" [ entry "e1" "t1" 100m (d 1 1) false ]
    Assert.True(Result.isOk r)
    Assert.Equal(Some(true, Some "u2"), approvedFlag "e1" r)

[<Fact>]
let ``a session idle for 15 minutes and 1 second gets SessionExpired`` () =
    let s = session "u2" "t1" [ Approver ] (now.AddMinutes(-15.0).AddSeconds(-1.0))
    let r = approve now s "e1" [ entry "e1" "t1" 100m (d 1 1) false ]
    Assert.Equal(Some SessionExpired, errorOf r)

[<Fact>]
let ``an Admin without Approver gets NotAuthorized`` () =
    let s = session "u2" "t1" [ Admin ] now
    let r = approve now s "e1" [ entry "e1" "t1" 100m (d 1 1) false ]
    Assert.Equal(Some NotAuthorized, errorOf r)

[<Fact>]
let ``an Approver without Admin approving 10000.01 gets NotAuthorized`` () =
    let s = session "u2" "t1" [ Approver ] now
    let r = approve now s "e1" [ entry "e1" "t1" 10000.01m (d 1 1) false ]
    Assert.Equal(Some NotAuthorized, errorOf r)

[<Fact>]
let ``an entry of exactly 10000 needs only Approver`` () =
    let s = session "u2" "t1" [ Approver ] now
    let r = approve now s "e1" [ entry "e1" "t1" 10000m (d 1 1) false ]
    Assert.True(Result.isOk r)

// ---- WI-0005: ledger view (RQ-FXL-0006, DF-FXL-0005) ----

[<Fact>]
let ``an Admin of t1 sees only t1 entries`` () =
    let s = session "u1" "t1" [ Admin ] now
    let entries = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 2m (d 1 1) true; entry "e3" "t1" 3m (d 1 2) false ]
    match view now s entries with
    | Ok es -> Assert.Equal<string list>([ "e1"; "e3" ], es |> List.map _.Id)
    | Error e -> failwithf "unexpected error %A" e

[<Fact>]
let ``an expired session gets SessionExpired`` () =
    let s = session "u1" "t1" [ Admin ] (now.AddMinutes(-16.0))
    Assert.Equal(Some SessionExpired, errorOf (view now s [ entry "e1" "t1" 1m (d 1 1) true ]))
