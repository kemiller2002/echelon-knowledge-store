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

let now = DateTimeOffset(2026, 10, 10, 12, 0, 0, TimeSpan.Zero)
let session roles lastSeen =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = now.AddHours -1.0; LastSeen = lastSeen }

[<Fact>]
let ``total sums approved entries of the tenant including refunds`` () =
    let entries = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" -3m (d 1 1) true
                    entry "e3" "t1" 5m (d 1 1) false; entry "e4" "t2" 7m (d 1 1) true ]
    Assert.Equal(7m, total "t1" entries)

[<Fact>]
let ``total rounds midpoints away from zero`` () =
    Assert.Equal(0.03m, total "t1" [ entry "e1" "t1" 0.025m (d 1 1) true ])
    Assert.Equal(-0.03m, total "t1" [ entry "e1" "t1" -0.025m (d 1 1) true ])

[<Fact>]
let ``approve succeeds and sets ApprovedBy`` () =
    let s = session [ Approver ] now
    match approve now s "e1" [ entry "e1" "t1" 1m (d 1 1) false ] with
    | Ok [ e ] -> Assert.True e.Approved; Assert.Equal(Some "u1", e.ApprovedBy)
    | r -> failwithf "%A" r

[<Fact>]
let ``approve session idle exactly 15 minutes is active, 1 second more expires`` () =
    let es = [ entry "e1" "t1" 1m (d 1 1) false ]
    Assert.True((approve now (session [ Approver ] (now.AddMinutes -15.0)) "e1" es |> Result.isOk))
    Assert.Equal(Error SessionExpired, approve now (session [ Approver ] (now.AddMinutes(-15.0).AddSeconds -1.0)) "e1" es)

[<Fact>]
let ``approve Admin without Approver is not authorized`` () =
    Assert.Equal(Error NotAuthorized, approve now (session [ Admin ] now) "e1" [ entry "e1" "t1" 1m (d 1 1) false ])

[<Fact>]
let ``approve other tenant entry is NotFound`` () =
    Assert.Equal(Error NotFound, approve now (session [ Approver ] now) "e1" [ entry "e1" "t2" 1m (d 1 1) false ])

[<Fact>]
let ``approve own entry is SelfApproval and approved entry is AlreadyApproved`` () =
    let own = { entry "e1" "t1" 1m (d 1 1) false with CreatedBy = "u1" }
    Assert.Equal(Error SelfApproval, approve now (session [ Approver ] now) "e1" [ own ])
    Assert.Equal(Error AlreadyApproved, approve now (session [ Approver ] now) "e1" [ entry "e1" "t1" 1m (d 1 1) true ])

[<Fact>]
let ``view returns only own tenant entries even for Admin`` () =
    let es = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 1m (d 1 1) true; entry "e3" "t1" 1m (d 1 1) false ]
    match view now (session [ Admin ] now) es with
    | Ok r -> Assert.Equal<string list>([ "e1"; "e3" ], r |> List.map _.Id)
    | r -> failwithf "%A" r

[<Fact>]
let ``view with expired session is SessionExpired`` () =
    Assert.Equal(Error SessionExpired, view now (session [ Viewer ] (now.AddMinutes -16.0)) [])
