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

let t0 = DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)

let session user tenant roles (idle: TimeSpan) =
    { UserId = user; TenantId = tenant; Roles = roles; IssuedAt = t0 - idle; LastSeen = t0 - idle }

[<Fact>]
let ``total rounds 0.005 away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])

[<Fact>]
let ``total rounds -0.005 away from zero`` () =
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants and includes refunds`` () =
    let es = [ entry "e1" "t1" 10m (d 1 1) true; entry "e2" "t1" 5m (d 1 1) false
               entry "e3" "t2" 7m (d 1 1) true; entry "e4" "t1" -3m (d 1 1) true ]
    Assert.Equal(7m, total "t1" es)

let appr = session "u1" "t1" [ Approver ] TimeSpan.Zero

[<Fact>]
let ``approve succeeds at exactly 15 minutes idle`` () =
    let s = session "u1" "t1" [ Approver ] (TimeSpan.FromMinutes 15.0)
    match approve t0 s "e1" [ entry "e1" "t1" 10m (d 1 1) false ] with
    | Ok [ e ] -> Assert.True e.Approved; Assert.Equal(Some "u1", e.ApprovedBy)
    | r -> failwithf "%A" r

[<Fact>]
let ``approve expired at 15 minutes 1 second`` () =
    let s = session "u1" "t1" [ Approver ] (TimeSpan.FromSeconds 901.0)
    Assert.Equal(Error SessionExpired, approve t0 s "e1" [ entry "e1" "t1" 10m (d 1 1) false ])

[<Fact>]
let ``approve admin without approver not authorized`` () =
    let s = session "u1" "t1" [ Admin ] TimeSpan.Zero
    Assert.Equal(Error NotAuthorized, approve t0 s "e1" [ entry "e1" "t1" 10m (d 1 1) false ])

[<Fact>]
let ``approve other tenant is not found`` () =
    Assert.Equal(Error NotFound, approve t0 appr "e1" [ entry "e1" "t2" 10m (d 1 1) false ])

[<Fact>]
let ``approve self approval and already approved`` () =
    let own = { entry "e1" "t1" 10m (d 1 1) false with CreatedBy = "u1" }
    Assert.Equal(Error SelfApproval, approve t0 appr "e1" [ own ])
    Assert.Equal(Error AlreadyApproved, approve t0 appr "e1" [ entry "e1" "t1" 10m (d 1 1) true ])

[<Fact>]
let ``approve high value needs admin too`` () =
    Assert.Equal(Error NotAuthorized, approve t0 appr "e1" [ entry "e1" "t1" 10000.01m (d 1 1) false ])
    Assert.True(Result.isOk (approve t0 appr "e1" [ entry "e1" "t1" 10000m (d 1 1) false ]))
    let both = session "u1" "t1" [ Approver; Admin ] TimeSpan.Zero
    Assert.True(Result.isOk (approve t0 both "e1" [ entry "e1" "t1" 10000.01m (d 1 1) false ]))

[<Fact>]
let ``view admin sees only own tenant in order`` () =
    let s = session "u1" "t1" [ Admin ] TimeSpan.Zero
    let es = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 1m (d 1 1) true; entry "e3" "t1" 1m (d 1 1) false ]
    match view t0 s es with
    | Ok r -> Assert.Equal<string list>([ "e1"; "e3" ], r |> List.map _.Id)
    | r -> failwithf "%A" r

[<Fact>]
let ``view expired session`` () =
    let s = session "u1" "t1" [ Admin ] (TimeSpan.FromSeconds 901.0)
    Assert.Equal(Error SessionExpired, view t0 s [])
