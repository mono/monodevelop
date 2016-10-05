namespace MonoDevelopTests
open System
open System.Reflection
open System.IO
open NUnit.Framework

module FsUnit =

    open System.Diagnostics
    open NUnit.Framework
    open NUnit.Framework.Constraints

    [<DebuggerNonUserCode>]
    let should (f : 'a -> #Constraint) x (y : obj) =
        let c = f x
        let y =
            match y with
            | :? (unit -> unit) -> box (new TestDelegate(y :?> unit -> unit))
            | _                 -> y
        Assert.That(y, c)

    let shouldnot (f : 'a -> #Constraint) x (y : obj) =
        let c = f x
        let y =
            match y with
            | :? (unit -> unit) -> box (new TestDelegate(y :?> unit -> unit))
            | _                 -> y
        Assert.That(y, new NotConstraint(c))

    let equal x = new EqualConstraint(x)

    // like "should equal", but validates same-type
    let shouldEqual (x: 'a) (y: 'a) = Assert.AreEqual(x, y, sprintf "Expected: %A\nActual: %A" x y)
    let replaceLineEnding (s:string) =
      s.Replace("\r\n", "\n")

    let shouldEqualIgnoringLineEndings (x: string) (y: string) =
      Assert.AreEqual((replaceLineEnding x), (replaceLineEnding y), sprintf "Expected: %A\nActual: %A" x y)

    let notEqual x = new NotConstraint(new EqualConstraint(x))

    let NOT c = new NotConstraint(c)

    let contain x = new ContainsConstraint(x)

    let haveLength n = Has.Length.EqualTo(n)

    let haveCount n = Has.Count.EqualTo(n)

    let NotEmpty = Has.Length.GreaterThan(0)

    let endWith (s:string) = new EndsWithConstraint(s)

    let startWith (s:string) = new StartsWithConstraint(s)

    let be = id

    let Null = new NullConstraint()

    let Empty = new EmptyConstraint()

    let EmptyString = new EmptyStringConstraint()

    let NullOrEmptyString = new NullOrEmptyStringConstraint()

    let True = new TrueConstraint()

    let False = new FalseConstraint()

    let sameAs x = new SameAsConstraint(x)

    let throw = Throws.TypeOf


[<AutoOpen>]
module Path =
    let (/) (a:string) (b:string) = Path.Combine(a,b)
    ///Cleans up a path removing trailing double quotes and also consistant forward slash handling
    let neutralise (path:string) =
        Path.GetFullPath( path.TrimStart('\"').TrimEnd('\"'))


[<AutoOpen>]
module AssemblyLocation =
    let fromFqn (fqn:string) =
        let assembly = Assembly.ReflectionOnlyLoad fqn
        assembly.Location

    let (|Fqn|File|) (input:string) =
        if input.Contains "," then Fqn(input)
        else File(input)
