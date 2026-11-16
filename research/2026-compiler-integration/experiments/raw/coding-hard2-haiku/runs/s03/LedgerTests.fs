module FxLedger.Tests.LedgerTests

open System
open Xunit
open FxIdentity.Identity
open FxLedger.Ledger

let entry id tenant amount (date: DateOnly) approved =
    { Id = id; TenantId = tenant; Amount = amount; Description = "x"; Date = date
      CreatedBy = "u9"; Approved = approved; ApprovedBy = None; RefundOf = None }

let d (m: int) (day: int) = DateOnly(2026, m, day)

let t0 = DateTimeOffset(2026, 11, 1, 9, 0, 0, TimeSpan.Zero)

let sessionWith (userId: string) (tenant: string) (roles: Role list) =
    { UserId = userId; TenantId = tenant; Roles = roles; IssuedAt = t0; LastSeen = t0 }

let approver = sessionWith "u1" "t1" [ Approver ]

[<Fact>]
let ``balance counts approved entries well before the cut-off`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, balance "t1" (d 3 1) entries)

[<Fact>]
let ``balance excludes entries after the cut-off`` () =
    Assert.Equal(0m, balance "t1" (d 1 1) [ entry "e1" "t1" 10m (d 2 1) true ])

// WI-0001 (RQ-FXL-0003)
[<Fact>]
let ``total rounds approved amounts half away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved entries and other tenants`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 2) false; entry "e3" "t2" 7m (d 1 1) true ]
    Assert.Equal(10m, total "t1" entries)

// WI-0003 (RQ-FXL-0004)
[<Fact>]
let ``approve sets Approved and ApprovedBy for an approver`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) false ]
    match approve t0 approver "e1" entries with
    | Ok [ e ] ->
        Assert.True(e.Approved)
        Assert.Equal(Some "u1", e.ApprovedBy)
        Assert.Equal(10m, e.Amount)
    | other -> failwithf "unexpected result: %A" other

[<Fact>]
let ``approve accepts a session idle for exactly 15 minutes`` () =
    let s = { approver with LastSeen = t0 }
    Assert.True(Result.isOk (approve (t0.AddMinutes 15.0) s "e1" [ entry "e1" "t1" 10m (d 1 1) false ]))

[<Fact>]
let ``approve rejects a session idle for 15 minutes and 1 second`` () =
    let now = t0.AddMinutes(15.0).AddSeconds(1.0)
    Assert.Equal(Error SessionExpired, approve now approver "e1" [ entry "e1" "t1" 10m (d 1 1) false ] |> Result.map ignore)

[<Fact>]
let ``admin without approver role is not authorized to approve`` () =
    let admin = sessionWith "u1" "t1" [ Admin ]
    Assert.Equal(Error NotAuthorized, approve t0 admin "e1" [ entry "e1" "t1" 10m (d 1 1) false ] |> Result.map ignore)

[<Fact>]
let ``approving an entry of another tenant reports NotFound`` () =
    Assert.Equal(Error NotFound, approve t0 approver "e1" [ entry "e1" "t2" 10m (d 1 1) false ] |> Result.map ignore)

[<Fact>]
let ``approver cannot approve their own entry`` () =
    let own = { entry "e1" "t1" 10m (d 1 1) false with CreatedBy = "u1" }
    Assert.Equal(Error SelfApproval, approve t0 approver "e1" [ own ] |> Result.map ignore)

[<Fact>]
let ``approving an already approved entry reports AlreadyApproved`` () =
    Assert.Equal(Error AlreadyApproved, approve t0 approver "e1" [ entry "e1" "t1" 10m (d 1 1) true ] |> Result.map ignore)

// WI-0005 (RQ-FXL-0006)
[<Fact>]
let ``view returns only the session tenant's entries in original order`` () =
    let entries = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 2m (d 1 2) true; entry "e3" "t1" 3m (d 1 3) false ]
    match view t0 approver entries with
    | Ok result -> Assert.Equal<string list>([ "e1"; "e3" ], result |> List.map (fun e -> e.Id))
    | Error err -> failwithf "unexpected error: %A" err

[<Fact>]
let ``admin sees only its own tenant's entries`` () =
    let admin = sessionWith "u1" "t1" [ Admin ]
    let entries = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 2m (d 1 2) true ]
    match view t0 admin entries with
    | Ok result -> Assert.Equal<string list>([ "e1" ], result |> List.map (fun e -> e.Id))
    | Error err -> failwithf "unexpected error: %A" err

[<Fact>]
let ``view of an expired session reports SessionExpired`` () =
    Assert.Equal(Error SessionExpired, view (t0.AddMinutes 16.0) approver [] |> Result.map ignore)
