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

// ---- WI-0001: total (RQ-FXL-0003, DF-FXL-0007) ----

[<Fact>]
let ``total rounds a half cent away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants' entries`` () =
    let entries =
        [ entry "e1" "t1" 10m (d 1 1) true
          entry "e2" "t1" 5m (d 1 2) false
          entry "e3" "t2" 7m (d 1 1) true
          entry "e4" "t1" -3m (d 1 3) true ]
    Assert.Equal(7m, total "t1" entries)

// ---- WI-0003: approve (RQ-FXL-0004) ----

let now0 = DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero)

let session roles (lastSeen: DateTimeOffset) : FxIdentity.Identity.Session =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now0; LastSeen = lastSeen }

[<Fact>]
let ``approve succeeds for an Approver idle exactly 15 minutes`` () =
    let s = session [ FxIdentity.Identity.Approver ] (now0 - TimeSpan.FromMinutes 15.0)
    match approve now0 s "e1" [ entry "e1" "t1" 10m (d 1 1) false ] with
    | Ok [ e ] ->
        Assert.True e.Approved
        Assert.Equal(Some "u1", e.ApprovedBy)
    | other -> failwithf "unexpected %A" other

[<Fact>]
let ``approve rejects a session idle 15 minutes and 1 second`` () =
    let s = session [ FxIdentity.Identity.Approver ] (now0 - TimeSpan.FromMinutes 15.0 - TimeSpan.FromSeconds 1.0)
    Assert.Equal(Error SessionExpired, approve now0 s "e1" [ entry "e1" "t1" 10m (d 1 1) false ] |> Result.map ignore)

[<Fact>]
let ``approve rejects an Admin without the Approver role`` () =
    let s = session [ FxIdentity.Identity.Admin ] now0
    Assert.Equal(Error NotAuthorized, approve now0 s "e1" [ entry "e1" "t1" 10m (d 1 1) false ] |> Result.map ignore)

[<Fact>]
let ``approve reports another tenant's entry as NotFound`` () =
    let s = session [ FxIdentity.Identity.Approver ] now0
    Assert.Equal(Error NotFound, approve now0 s "e1" [ entry "e1" "t2" 10m (d 1 1) false ] |> Result.map ignore)

[<Fact>]
let ``approve rejects self-approval and re-approval`` () =
    let s = session [ FxIdentity.Identity.Approver ] now0
    let own = { entry "e1" "t1" 10m (d 1 1) false with CreatedBy = "u1" }
    Assert.Equal(Error SelfApproval, approve now0 s "e1" [ own ] |> Result.map ignore)
    let done' = { entry "e2" "t1" 10m (d 1 1) true with ApprovedBy = Some "u7" }
    Assert.Equal(Error AlreadyApproved, approve now0 s "e2" [ done' ] |> Result.map ignore)

// ---- WI-0005: view (RQ-FXL-0006, DF-FXL-0005) ----

[<Fact>]
let ``view returns only the session tenant's entries even for an Admin`` () =
    let s = { session [ FxIdentity.Identity.Admin ] now0 with TenantId = "t1" }
    let entries = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 2m (d 1 1) true; entry "e3" "t1" 3m (d 1 2) false ]
    match view now0 s entries with
    | Ok vs -> Assert.Equal<string list>([ "e1"; "e3" ], vs |> List.map (fun e -> e.Id))
    | Error e -> failwithf "unexpected %A" e

[<Fact>]
let ``view rejects an expired session`` () =
    let s = session [ FxIdentity.Identity.Viewer ] (now0 - TimeSpan.FromMinutes 16.0)
    Assert.Equal(Error SessionExpired, view now0 s [] |> Result.map ignore)
