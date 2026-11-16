module Hidden.T4

open Xunit
open Hidden.Common
open FxLedger.Ledger

[<Fact>]
let ``entry on the cut-off date is included`` () =
    Assert.Equal(10m, balance "t1" (d 2 1) [ entry "a" "t1" 10m (d 2 1) true ])

[<Fact>]
let ``later unapproved and other tenant excluded`` () =
    let es = [ entry "a" "t1" 10m (d 2 1) true; entry "b" "t1" 3m (d 2 2) true; entry "c" "t1" 4m (d 1 1) false; entry "e" "t2" 5m (d 1 1) true ]
    Assert.Equal(10m, balance "t1" (d 2 1) es)
