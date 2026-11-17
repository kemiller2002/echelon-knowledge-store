module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxIdentity.Identity
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

// ---- WI-0001: tenant total (RQ-FXL-0003) ----

[<Fact>]
let ``total rounds approved 0.005 away from zero to 0.01`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])

[<Fact>]
let ``total rounds approved -0.005 away from zero to -0.01`` () =
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total excludes unapproved entries and other tenants' entries`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, total "t1" entries)

// ---- WI-0003: approval (RQ-FXL-0004, amendment RQ-FXL-0104) ----

let now = DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero)

let session (roles: Role list) (lastSeen: DateTimeOffset) : Session =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddHours(-1.0); LastSeen = lastSeen }

let assertErr (expected: LedgerError) (actual: Result<Entry list, LedgerError>) =
    Assert.Equal<Result<Entry list, LedgerError>>(Error expected, actual)

[<Fact>]
let ``approve accepts a session idle for exactly 15 minutes`` () =
    let s = session [ Approver ] (now.AddMinutes(-15.0))
    let es = [ entry "e1" "t1" 100m (d 10 1) false ]
    let expected = [ { es.Head with Approved = true; ApprovedBy = Some "u1" } ]
    Assert.Equal<Result<Entry list, LedgerError>>(Ok expected, approve now s "e1" es)

[<Fact>]
let ``approve rejects a session idle for 15 minutes 1 second with SessionExpired`` () =
    let s = session [ Approver ] (now.AddMinutes(-15.0).AddSeconds(-1.0))
    assertErr SessionExpired (approve now s "e1" [ entry "e1" "t1" 100m (d 10 1) false ])

[<Fact>]
let ``approve rejects an Admin without Approver with NotAuthorized`` () =
    let s = session [ Admin ] now
    assertErr NotAuthorized (approve now s "e1" [ entry "e1" "t1" 100m (d 10 1) false ])

[<Fact>]
let ``approve rejects an Approver without Admin for an amount of 10000.01 with NotAuthorized`` () =
    let s = session [ Approver ] now
    assertErr NotAuthorized (approve now s "e1" [ entry "e1" "t1" 10000.01m (d 10 1) false ])

[<Fact>]
let ``approve accepts an amount of exactly 10000 with Approver only`` () =
    let s = session [ Approver ] now
    let es = [ entry "e1" "t1" 10000m (d 10 1) false ]
    let expected = [ { es.Head with Approved = true; ApprovedBy = Some "u1" } ]
    Assert.Equal<Result<Entry list, LedgerError>>(Ok expected, approve now s "e1" es)

// ---- WI-0005: ledger view (RQ-FXL-0006) ----

[<Fact>]
let ``view shows an Admin of t1 only t1 entries`` () =
    let s = session [ Admin ] now
    let es = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t2" 7m (d 1 2) true; entry "e3" "t1" 3m (d 1 3) false ]
    let expected = [ es.[0]; es.[2] ]
    Assert.Equal<Result<Entry list, LedgerError>>(Ok expected, view now s es)

[<Fact>]
let ``view returns SessionExpired for an expired session`` () =
    let s = session [ Admin ] (now.AddMinutes(-16.0))
    assertErr SessionExpired (view now s [ entry "e1" "t1" 10m (d 1 1) true ])
