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

// --- WI-0001: tenant total (RQ-FXL-0003) ---

[<Fact>]
let ``total rounds half away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total excludes unapproved entries and other tenants`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, total "t1" entries)

// --- WI-0003: approval (RQ-FXL-0004) ---

let t0 = DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero)

let session roles =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = t0; LastSeen = t0 }

let pending = [ entry "e1" "t1" 10m (d 1 1) false ]

[<Fact>]
let ``approve sets Approved and ApprovedBy for an active approver`` () =
    match approve (t0.AddMinutes 1.0) (session [ Approver ]) "e1" pending with
    | Ok [ e ] ->
        Assert.True(e.Approved)
        Assert.Equal(Some "u1", e.ApprovedBy)
    | other -> failwithf "unexpected: %A" other

[<Fact>]
let ``approve allows exactly 15 minutes idle and rejects 15 minutes 1 second`` () =
    let s = session [ Approver ]
    Assert.True(Result.isOk (approve (t0.AddMinutes 15.0) s "e1" pending))
    Assert.Equal(Error SessionExpired, approve (t0.AddMinutes(15.0).AddSeconds 1.0) s "e1" pending |> Result.map ignore)

[<Fact>]
let ``admin without approver gets NotAuthorized`` () =
    Assert.Equal(Error NotAuthorized, approve (t0.AddMinutes 1.0) (session [ Admin ]) "e1" pending |> Result.map ignore)

[<Fact>]
let ``approve rejects self approval and other tenants as NotFound`` () =
    let own = [ { entry "e1" "t1" 10m (d 1 1) false with CreatedBy = "u1" } ]
    Assert.Equal(Error SelfApproval, approve (t0.AddMinutes 1.0) (session [ Approver ]) "e1" own |> Result.map ignore)
    let other = [ entry "e2" "t2" 10m (d 1 1) false ]
    Assert.Equal(Error NotFound, approve (t0.AddMinutes 1.0) (session [ Approver ]) "e2" other |> Result.map ignore)

[<Fact>]
let ``approve rejects an already approved entry`` () =
    let done' = [ { entry "e1" "t1" 10m (d 1 1) true with ApprovedBy = Some "u7" } ]
    Assert.Equal(Error AlreadyApproved, approve (t0.AddMinutes 1.0) (session [ Approver ]) "e1" done' |> Result.map ignore)

// --- WI-0005: ledger view (RQ-FXL-0006) ---

[<Fact>]
let ``view returns only the session tenant's entries in original order`` () =
    let entries = [ entry "e1" "t2" 1m (d 1 1) true; entry "e2" "t1" 2m (d 1 2) false; entry "e3" "t1" 3m (d 1 3) true ]
    match view (t0.AddMinutes 1.0) (session [ Admin ]) entries with
    | Ok visible -> Assert.Equal<string list>([ "e2"; "e3" ], visible |> List.map _.Id)
    | Error e -> failwithf "unexpected: %A" e

[<Fact>]
let ``view with an expired session gets SessionExpired`` () =
    Assert.Equal(Error SessionExpired, view (t0.AddMinutes 16.0) (session [ Viewer ]) [] |> Result.map ignore)
