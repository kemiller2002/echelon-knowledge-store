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

// WI-0001: tenant total (RQ-FXL-0003)

[<Fact>]
let ``total rounds a positive half-cent midpoint away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])

[<Fact>]
let ``total rounds a negative half-cent midpoint away from zero`` () =
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved entries and other tenants`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, total "t1" entries)

// WI-0003: approval (RQ-FXL-0004, RQ-FXL-0104)

let t0 = DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero)

let session roles =
    { FxIdentity.Identity.UserId = "u1"; FxIdentity.Identity.TenantId = "t1"; FxIdentity.Identity.Roles = roles
      FxIdentity.Identity.IssuedAt = t0; FxIdentity.Identity.LastSeen = t0 }

let approver = session [ FxIdentity.Identity.Approver ]

[<Fact>]
let ``approve accepts a session idle for exactly 15 minutes`` () =
    let r = approve (t0.AddMinutes 15.0) approver "e1" [ entry "e1" "t1" 10m (d 1 1) false ]
    Assert.True(Result.isOk r)

[<Fact>]
let ``approve rejects a session idle for 15 minutes and 1 second`` () =
    let r = approve (t0.AddMinutes(15.0).AddSeconds 1.0) approver "e1" [ entry "e1" "t1" 10m (d 1 1) false ]
    Assert.Equal(Error SessionExpired, r)

[<Fact>]
let ``approve rejects an Admin without Approver`` () =
    let admin = session [ FxIdentity.Identity.Admin ]
    Assert.Equal(Error NotAuthorized, approve t0 admin "e1" [ entry "e1" "t1" 10m (d 1 1) false ])

[<Fact>]
let ``approve reports an entry of another tenant as NotFound`` () =
    Assert.Equal(Error NotFound, approve t0 approver "e1" [ entry "e1" "t2" 10m (d 1 1) false ])

[<Fact>]
let ``approve rejects self-approval`` () =
    let own = { entry "e1" "t1" 10m (d 1 1) false with CreatedBy = "u1" }
    Assert.Equal(Error SelfApproval, approve t0 approver "e1" [ own ])

[<Fact>]
let ``approve rejects an entry that is already approved`` () =
    Assert.Equal(Error AlreadyApproved, approve t0 approver "e1" [ entry "e1" "t1" 10m (d 1 1) true ])

[<Fact>]
let ``approve sets Approved and ApprovedBy on success`` () =
    match approve t0 approver "e1" [ entry "e1" "t1" 10m (d 1 1) false; entry "e2" "t1" 5m (d 1 2) false ] with
    | Ok [ a; b ] ->
        Assert.True(a.Approved)
        Assert.Equal(Some "u1", a.ApprovedBy)
        Assert.False(b.Approved)
    | other -> failwithf "unexpected %A" other

[<Fact>]
let ``high-value approval of 10000.01 needs Admin as well as Approver`` () =
    Assert.Equal(Error NotAuthorized, approve t0 approver "e1" [ entry "e1" "t1" 10000.01m (d 1 1) false ])

[<Fact>]
let ``approval of exactly 10000 needs only Approver`` () =
    Assert.True(Result.isOk (approve t0 approver "e1" [ entry "e1" "t1" 10000m (d 1 1) false ]))

[<Fact>]
let ``high-value approval succeeds for a session with Approver and Admin`` () =
    let both = session [ FxIdentity.Identity.Approver; FxIdentity.Identity.Admin ]
    Assert.True(Result.isOk (approve t0 both "e1" [ entry "e1" "t1" 10000.01m (d 1 1) false ]))

// WI-0005: ledger view (RQ-FXL-0006, DF-FXL-0005)

[<Fact>]
let ``view returns only the session tenant's entries for an Admin`` () =
    let admin = session [ FxIdentity.Identity.Admin ]
    let entries = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 2m (d 1 1) true; entry "e3" "t1" 3m (d 1 2) false ]
    match view t0 admin entries with
    | Ok visible -> Assert.Equal<string list>([ "e1"; "e3" ], visible |> List.map _.Id)
    | Error e -> failwithf "unexpected %A" e

[<Fact>]
let ``view of an expired session gets SessionExpired`` () =
    let late = t0.AddMinutes 15.0 |> fun t -> t.AddSeconds 1.0
    Assert.Equal(Error SessionExpired, view late approver [ entry "e1" "t1" 1m (d 1 1) true ])
