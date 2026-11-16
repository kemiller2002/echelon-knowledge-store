module Hidden.T6

open Xunit
open Hidden.Common
open FxLedger.Ledger

[<Fact>]
let ``header only when nothing approved`` () =
    Assert.Equal("id,date,amount,description\n", exportCsv "t1" [ entry "a" "t1" 1m (d 1 1) false ])

[<Fact>]
let ``format order quoting and tenant`` () =
    let es =
        [ { entry "b" "t1" 1234.5m (d 3 2) true with Description = "a,\"b\"" }
          { entry "a" "t1" -5m (d 3 2) true with Description = "plain" }
          { entry "c" "t1" 7m (d 1 15) true with Description = "line\nbreak" }
          entry "z" "t2" 9m (d 1 1) true ]
    let expected =
        "id,date,amount,description\n"
        + "c,2026-01-15,7.00,\"line\nbreak\"\n"
        + "a,2026-03-02,-5.00,plain\n"
        + "b,2026-03-02,1234.50,\"a,\"\"b\"\"\"\n"
    Assert.Equal(expected, exportCsv "t1" es)

[<Fact>]
let ``culture independent`` () =
    let previous = System.Globalization.CultureInfo.CurrentCulture
    try
        System.Globalization.CultureInfo.CurrentCulture <- System.Globalization.CultureInfo "de-DE"
        Assert.Equal("id,date,amount,description\na,2026-01-01,1.50,x\n", exportCsv "t1" [ entry "a" "t1" 1.5m (d 1 1) true ])
    finally
        System.Globalization.CultureInfo.CurrentCulture <- previous
