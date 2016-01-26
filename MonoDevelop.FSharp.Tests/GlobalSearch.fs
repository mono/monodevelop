namespace MonoDevelopTests
open System.Collections.Generic
open Microsoft.FSharp.Compiler.SourceCodeServices
open NUnit.Framework
open FsUnit
open MonoDevelop.FSharp

[<TestFixture>]
type TestGlobalSearch() =


    let input = """
module Test
let (++) a b = a + b
let (|Full|Empty|) x = if x = "" then Empty else Full
type MyRecord = {Test : int}
type MyType() =
  member x.Foo = 42
  member x.Bar() = 43
type MyUnion = First of int

[<Struct>]
type MyPoint3D =
  val x: float
  val y: float
  val z: float

type IMyInterface =
  abstract member Test : int -> int

type MyEnum = First = 1 | Second = 2

type MyDelegate = delegate of (int * int) -> int
"""
    let searchByTag tag =
        match TestHelpers.getAllSymbols input with
        | Some xs ->
            let tags = Search.byTag tag xs
            tags
            |> Seq.map(fun s -> s.Symbol.DisplayName)
            |> Seq.toList
        | _ -> []

    [<Test>]
    member x.Operators_Can_Be_Filtered() =
        searchByTag "op" |> shouldEqual ["( + )"; "( ++ )"; "( = )"] // ( + ) and ( = ) aren't user defined operators

    [<Test>]
    member x.ActivePatterns_Can_Be_Filtered() =
        searchByTag "ap" |> shouldEqual ["( |Full|Empty| )"]

    [<Test>]
    member x.Records_Can_Be_Filtered() =
        searchByTag "r" |> shouldEqual ["MyRecord"]

    [<TestCase("t")>]
    [<TestCase("type")>]
    [<TestCase("c")>]
    member x.Types_Can_Be_Filtered(search) =
        searchByTag search |> shouldEqual ["MyType"; "StructAttribute"; "StructAttribute"] // needs fixing

    [<Test>]
    member x.Unions_Can_Be_Filtered() =
        searchByTag "u" |> shouldEqual ["MyUnion"]

    [<Test>]
    member x.Modules_Can_Be_Filtered() =
        searchByTag "mod" |> shouldEqual ["Test"]

    [<Test>]
    member x.Structs_Can_Be_Filtered() =
        searchByTag "s" |> shouldEqual ["MyPoint3D"]

    [<Test>]
    member x.Interfaces_Can_Be_Filtered() =
        searchByTag "i" |> shouldEqual ["IMyInterface"]

    [<Test>]
    member x.Enums_Can_Be_Filtered() =
        searchByTag "e" |> shouldEqual ["MyEnum"]

    [<Test>]
    member x.Properties_Can_Be_Filtered() =
        searchByTag "p" |> shouldEqual ["Foo"]

    [<Test>]
    member x.Members_Can_Be_Filtered() =
        searchByTag "m" |> shouldEqual ["Bar"; "Test"; "Invoke"] //Invoke?

    [<Test>]
    member x.Fields_Can_Be_Filtered() =
        searchByTag "f" |> shouldEqual ["Test"; "x"; "y"; "z"; "First"; "Second"]  //Test?

    [<Test>]
    member x.Delegates_Can_Be_Filtered() =
        searchByTag "d" |> shouldEqual ["MyDelegate"]

    [<Test>]
    member x.Search_By_Unique_Pattern_Is_Correct() =
      match TestHelpers.getAllSymbols input with
      | Some xs ->
          let result =
              Search.byPattern (Dictionary<_,_>()) "++" xs
              |> Seq.map (fun (a, b) -> a.Symbol.DisplayName )
              |> Seq.toList
          result |> shouldEqual ["( ++ )"]
      | _ -> Assert.Fail "Not found"

    [<Test>]
    member x.Search_By_Pattern_Is_Correct() =
      match TestHelpers.getAllSymbols input with
      | Some xs ->
          let result = Search.byPattern (Dictionary<_,_>()) "My" xs

          result
          |> Seq.map (fun (a, b) -> a.Symbol.DisplayName, a.Symbol.GetType() )
          |> Seq.toList
          |> shouldEqual
            [ "MyRecord",     typeof<FSharpEntity>
              "MyType",       typeof<FSharpEntity>
              "( .ctor )",    typeof<FSharpMemberOrFunctionOrValue>
              "MyUnion",      typeof<FSharpEntity>
              "MyPoint3D",    typeof<FSharpEntity>
              "IMyInterface", typeof<FSharpEntity>
              "MyEnum",       typeof<FSharpEntity>
              "MyDelegate",   typeof<FSharpEntity> ]
      | _ -> Assert.Fail "Not found"
