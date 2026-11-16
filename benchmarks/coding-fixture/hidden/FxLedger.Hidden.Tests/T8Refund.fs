module Hidden.T8

open Xunit
open Hidden.Common
open FxIdentity.Identity
open FxLedger.Ledger

let clerk = session "clerk" "t1" [ Clerk ]
let original = entry "o" "t1" 100m (d 9 1) true
let ledger = [ original; entry "u" "t1" 50m (d 9 1) false; entry "x" "t2" 100m (d 9 1) true ]
let refundOf amount = refund t0 clerk today "r1" "o" amount ledger

[<Fact>]
let ``creates refund entry`` () =
    let result = ok (refundOf 60m)
    let expected =
        { Id = "r1"; TenantId = "t1"; Amount = -60m; Description = "Refund of o"; Date = today
          CreatedBy = "clerk"; Approved = false; ApprovedBy = None; RefundOf = Some "o" }
    Assert.Equal<Entry list>(ledger @ [ expected ], result)

[<Fact>]
let ``cumulative ceiling`` () =
    let after60 = ok (refundOf 60m)
    let after100 = ok (refund t0 clerk today "r2" "o" 40m after60)
    Assert.True(refund t0 clerk today "r3" "o" 0.01m after100 |> isError RefundExceedsOriginal)
    Assert.True(refundOf 100.01m |> isError RefundExceedsOriginal)

[<Fact>]
let ``non positive amount`` () =
    Assert.True(refundOf 0m |> isError InvalidAmount)
    Assert.True(refundOf -5m |> isError InvalidAmount)

[<Fact>]
let ``roles and session`` () =
    Assert.True(refund t0 (session "a" "t1" [ Admin ]) today "r1" "o" 1m ledger |> Result.isOk)
    Assert.True(refund t0 (session "a" "t1" [ Approver ]) today "r1" "o" 1m ledger |> isError NotAuthorized)
    Assert.True(refund (t0.AddMinutes 20.0) clerk today "r1" "o" 1m ledger |> isError SessionExpired)

[<Fact>]
let ``original must be approved own tenant non refund`` () =
    Assert.True(refund t0 clerk today "r1" "u" 1m ledger |> isError NotFound)
    Assert.True(refund t0 clerk today "r1" "x" 1m ledger |> isError NotFound)
    let withRefund = ok (refundOf 10m)
    Assert.True(refund t0 clerk today "r2" "r1" 1m withRefund |> isError NotFound)

[<Fact>]
let ``duplicate refund id`` () =
    Assert.True(refund t0 clerk today "o" "o" 1m ledger |> isError DuplicateId)
