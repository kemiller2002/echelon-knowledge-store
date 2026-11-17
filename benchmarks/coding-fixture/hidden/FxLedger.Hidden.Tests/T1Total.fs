module Hidden.T1

open Xunit
open Hidden.Common
open FxLedger.Ledger

[<Fact>]
let ``sums approved entries of the tenant only`` () =
    let es = [ entry "a" "t1" 10.25m (d 1 1) true; entry "b" "t1" 5m (d 1 2) false; entry "c" "t2" 7m (d 1 1) true ]
    Assert.Equal(10.25m, total "t1" es)

[<Fact>]
let ``midpoint rounds away from zero`` () =
    Assert.Equal(0.01m, total "t1" [ entry "a" "t1" 0.005m (d 1 1) true ])
    Assert.Equal(0.03m, total "t1" [ entry "a" "t1" 0.025m (d 1 1) true ])

[<Fact>]
let ``negative midpoint rounds away from zero`` () =
    Assert.Equal(-0.01m, total "t1" [ entry "a" "t1" -0.005m (d 1 1) true ])

[<Fact>]
let ``refunds are included`` () =
    let es = [ entry "a" "t1" 10m (d 1 1) true; { entry "r" "t1" -4m (d 1 2) true with RefundOf = Some "a" } ]
    Assert.Equal(6m, total "t1" es)

[<Fact>]
let ``empty ledger totals zero`` () = Assert.Equal(0m, total "t1" [])
