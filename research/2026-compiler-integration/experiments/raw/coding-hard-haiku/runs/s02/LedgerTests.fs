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
let ``total rounds a positive half-cent away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])

[<Fact>]
let ``total rounds a negative half-cent away from zero`` () =
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved entries and other tenants`` () =
    let entries =
        [ entry "e1" "t1" 10m (d 1 1) true
          entry "e2" "t1" 5m (d 1 1) false
          entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, total "t1" entries)

[<Fact>]
let ``total includes refunds as negative amounts`` () =
    Assert.Equal(60m, total "t1" [ entry "e1" "t1" 100m (d 1 1) true; entry "e2" "t1" -40m (d 1 2) true ])

// ---- WI-0003: entry approval (RQ-FXL-0004, RQ-FXL-0104) ----

let t0 = DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero)

let sessionWith (roles: Role list) : Session =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = t0; LastSeen = t0 }

let approver = sessionWith [ Approver ]

[<Fact>]
let ``approve accepts a session idle for exactly 15 minutes`` () =
    let now = t0.AddMinutes 15.0
    match approve now approver "e1" [ entry "e1" "t1" 10m (d 1 1) false ] with
    | Ok [ e ] ->
        Assert.True(e.Approved)
        Assert.Equal(Some "u1", e.ApprovedBy)
    | other -> failwithf "unexpected result: %A" other

[<Fact>]
let ``approve rejects a session idle for 15 minutes and 1 second`` () =
    let now = t0.AddMinutes(15.0).AddSeconds 1.0
    Assert.Equal(Error SessionExpired, approve now approver "e1" [ entry "e1" "t1" 10m (d 1 1) false ] |> Result.map ignore)

[<Fact>]
let ``approve rejects an Admin without the Approver role`` () =
    let admin = sessionWith [ Admin ]
    Assert.Equal(Error NotAuthorized, approve t0 admin "e1" [ entry "e1" "t1" 10m (d 1 1) false ] |> Result.map ignore)

[<Fact>]
let ``approve rejects an Approver without Admin for an amount above 10000`` () =
    Assert.Equal(Error NotAuthorized, approve t0 approver "e1" [ entry "e1" "t1" 10000.01m (d 1 1) false ] |> Result.map ignore)

[<Fact>]
let ``approve accepts an amount of exactly 10000 with only the Approver role`` () =
    Assert.True(Result.isOk (approve t0 approver "e1" [ entry "e1" "t1" 10000m (d 1 1) false ]))

[<Fact>]
let ``approve reports an entry of another tenant as NotFound`` () =
    Assert.Equal(Error NotFound, approve t0 approver "e1" [ entry "e1" "t2" 10m (d 1 1) false ] |> Result.map ignore)

[<Fact>]
let ``approve rejects approving an entry the session user created`` () =
    let entries = [ { entry "e1" "t1" 10m (d 1 1) false with CreatedBy = "u1" } ]
    Assert.Equal(Error SelfApproval, approve t0 approver "e1" entries |> Result.map ignore)

[<Fact>]
let ``approve rejects an entry that is already approved`` () =
    Assert.Equal(Error AlreadyApproved, approve t0 approver "e1" [ entry "e1" "t1" 10m (d 1 1) true ] |> Result.map ignore)

// ---- WI-0005: ledger view (RQ-FXL-0006) ----

let adminT1 = { UserId = "a1"; TenantId = "t1"; Roles = [ Admin ]; IssuedAt = t0; LastSeen = t0 }

[<Fact>]
let ``view returns only the session tenant's entries in original order`` () =
    let entries =
        [ entry "e1" "t2" 1m (d 1 1) true
          entry "e2" "t1" 2m (d 1 2) false
          entry "e3" "t1" 3m (d 1 3) true ]
    match view t0 adminT1 entries with
    | Ok visible -> Assert.Equal<string list>([ "e2"; "e3" ], visible |> List.map _.Id)
    | Error e -> failwithf "unexpected error: %A" e

[<Fact>]
let ``view returns SessionExpired for an expired session`` () =
    let expired = { adminT1 with LastSeen = t0.AddMinutes(-16.0) }
    Assert.Equal(Error SessionExpired, view t0 expired [] |> Result.map ignore)
