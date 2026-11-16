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

let session roles lastSeen =
    { UserId = "u1"; TenantId = "t1"; Roles = roles; IssuedAt = t0.AddMinutes -30.0; LastSeen = lastSeen }

[<Fact>]
let ``total rounds half away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "e1" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(-0.01m, total "t1" [ entry "e1" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``total ignores unapproved and other tenants`` () =
    let es = [ entry "e1" "t1" 5m (d 1 1) true; entry "e2" "t1" 3m (d 1 1) false; entry "e3" "t2" 9m (d 1 1) true; entry "e4" "t1" -2m (d 1 1) true ]
    Assert.Equal(3m, total "t1" es)

[<Fact>]
let ``approve at exactly 15 minutes idle succeeds, 15m1s expires`` () =
    let es = [ entry "e1" "t1" 5m (d 1 1) false ]
    let ok = approve t0 (session [ Approver ] (t0.AddMinutes -15.0)) "e1" es
    Assert.True(Result.isOk ok)
    Assert.Equal(Error SessionExpired, approve t0 (session [ Approver ] (t0.AddSeconds -901.0)) "e1" es)

[<Fact>]
let ``approve sets approval fields`` () =
    let es = [ entry "e1" "t1" 5m (d 1 1) false ]
    match approve t0 (session [ Approver ] t0) "e1" es with
    | Ok [ e ] -> Assert.True e.Approved; Assert.Equal(Some "u1", e.ApprovedBy)
    | _ -> failwith "unexpected"

[<Fact>]
let ``approve admin without approver is not authorized`` () =
    let es = [ entry "e1" "t1" 5m (d 1 1) false ]
    Assert.Equal(Error NotAuthorized, approve t0 (session [ Admin ] t0) "e1" es)

[<Fact>]
let ``approve other tenant is not found`` () =
    let es = [ entry "e1" "t2" 5m (d 1 1) false ]
    Assert.Equal(Error NotFound, approve t0 (session [ Approver ] t0) "e1" es)

[<Fact>]
let ``approve self and already approved`` () =
    let mine = { entry "e1" "t1" 5m (d 1 1) false with CreatedBy = "u1" }
    Assert.Equal(Error SelfApproval, approve t0 (session [ Approver ] t0) "e1" [ mine ])
    Assert.Equal(Error AlreadyApproved, approve t0 (session [ Approver ] t0) "e1" [ entry "e1" "t1" 5m (d 1 1) true ])

[<Fact>]
let ``approve high value needs approver and admin`` () =
    let hi = [ entry "e1" "t1" 10000.01m (d 1 1) false ]
    Assert.Equal(Error NotAuthorized, approve t0 (session [ Approver ] t0) "e1" hi)
    Assert.True(Result.isOk (approve t0 (session [ Approver; Admin ] t0) "e1" hi))
    Assert.True(Result.isOk (approve t0 (session [ Approver ] t0) "e1" [ entry "e1" "t1" 10000m (d 1 1) false ]))

[<Fact>]
let ``view admin sees only own tenant`` () =
    let es = [ entry "e1" "t1" 1m (d 1 1) true; entry "e2" "t2" 1m (d 1 1) true; entry "e3" "t1" 1m (d 1 1) false ]
    match view t0 (session [ Admin ] t0) es with
    | Ok r -> Assert.Equal<string list>([ "e1"; "e3" ], r |> List.map _.Id)
    | _ -> failwith "unexpected"

[<Fact>]
let ``view expired session`` () =
    Assert.Equal(Error SessionExpired, view t0 (session [ Admin ] (t0.AddMinutes -16.0)) [])
