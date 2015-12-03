namespace MonoDevelopTests
open System.Collections.Generic
open Microsoft.FSharp.Compiler.SourceCodeServices
open System.Text.RegularExpressions
open NUnit.Framework
open FsUnit
open MonoDevelop.FSharp

[<TestFixture>]
type TestGlobalSearch() =
    inherit TestBase()

    let getAllSymbols source =  
      let checker = FSharpChecker.Create()
      let file = "test.fsx"

      async {
        let! projOptions = checker.GetProjectOptionsFromScript(file, source)
        let! pfr, cfa = checker.ParseAndCheckFileInProject(file, 0, source, projOptions)
        match cfa with
        | FSharpCheckFileAnswer.Succeeded cfr ->
          let! symbols = cfr.GetAllUsesOfAllSymbolsInFile() 
          return Some symbols
        | _ -> return None }

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
    [<Test>]
    member x.Operators_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "op" xs
        for tag in tags do
          tag.
        tags |> Seq.length |> shouldEqual 1
      | _ -> Assert.Fail "No operator found"

    [<Test>]
    member x.ActivePatterns_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "ap" xs
        tags |> Seq.length |> shouldEqual 1
      | _ -> Assert.Fail "No ActivePattern found"

    [<Test>]
    member x.Records_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "r" xs
        tags |> Seq.length |> shouldEqual 1
      | _ -> Assert.Fail "No record found"

    [<TestCase("t", 1)>]
    [<TestCase("type", 1)>]
    [<TestCase("c", 1)>]
    member x.Types_Can_Be_Filtered(search, expectedCount) =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "t" xs

        tags |> Seq.length |> shouldEqual expectedCount
      | _ -> Assert.Fail "No type found"

    [<Test>]
    member x.Unions_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "u" xs
        tags |> Seq.length |> shouldEqual 1
      | _ -> Assert.Fail "No Union type found"

    [<Test>]
    member x.Modules_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "mod" xs
        tags |> Seq.length |> shouldEqual 1
      | _ -> Assert.Fail "No module found"

    [<Test>]
    member x.Structs_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "s" xs
        tags |> Seq.length |> shouldEqual 1
      | _ -> Assert.Fail "No struct found"

    [<Test>]
    member x.Interfaces_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "i" xs
        tags |> Seq.length |> shouldEqual 1
      | _ -> Assert.Fail "No interfaces found"

    [<Test>]
    member x.Enums_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "e" xs
        tags |> Seq.length |> shouldEqual 1
      | _ -> Assert.Fail "No Enums found"

    [<Test>]
    member x.Properties_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "p" xs
        tags |> Seq.length |> shouldEqual 1
      | _ -> Assert.Fail "No Properties found"

    [<Test>]
    member x.Members_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let mtags = Search.byTag "m" xs
        let membertags = Search.byTag "m" xs
        mtags |> Seq.length |> shouldEqual 3 //1 in the record, 2 in the normal type, 1 in the delegate
        membertags |> Seq.length |> shouldEqual 3 //1 in the record, 2 in the normal type, 1 in the delegate
      | _ -> Assert.Fail "No Members found"

    [<Test>]
    member x.Fields_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "f" xs
        tags |> Seq.length |> shouldEqual 6//2 in the Enum, 3 in the struct, 1 in the record
      | _ -> Assert.Fail "No Fields found"

    [<Test>]
    member x.Delegates_Can_Be_Filtered() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let tags = Search.byTag "d" xs
        tags |> Seq.length |> shouldEqual 1
      | _ -> Assert.Fail "No Delegates found"

    [<Test>]
    member x.Search_By_Unique_Pattern_Is_Correct() =
      match getAllSymbols input |> Async.RunSynchronously with
      | Some xs ->
        let result =
          Search.byPattern (Dictionary<_,_>()) "++" xs
          |> Seq.map (fun (a, b) -> a.Symbol.DisplayName )
          |> Seq.toList
        result |> shouldEqual ["( ++ )"]
      | _ -> Assert.Fail "Not found"

    [<Test>]
    member x.Search_By_Pattern_Is_Correct() =
      match getAllSymbols input |> Async.RunSynchronously with
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