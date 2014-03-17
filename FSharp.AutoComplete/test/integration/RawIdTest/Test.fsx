#I "FSharp.Data/lib/net40/"
#r "FSharp.Data.dll"

open FSharp.Data

type TestType = CsvProvider<"data.csv">

let data = TestType.Load("data.csv")

let row = data.Rows |> Seq.head

row.

row.``Column T

row.``Another`C



