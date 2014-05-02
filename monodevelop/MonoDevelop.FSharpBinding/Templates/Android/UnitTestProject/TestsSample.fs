namespace ${Namespace}

open System
open NUnit.Framework

[<TestFixture>]
type TestsSample =

    [<SetUp>]
    member x.Setup () =
        ()// write your test fixture setup

    [<TearDown>]
    member x.Tear() =
        ()// write your test fixture teardown 
        
    [<Test>]
    member x.Pass () =
        Console.WriteLine ("test1");
        Assert.True (true);
        
    [<Test>]
    member x.Fail () =
        Assert.False (true);
        
    [<Test>]
    [<Ignore ("another time")>]
    member x.Ignore () =
        Assert.True (false);
        
    [<Test>]
    member x.Inconclusive () =
        Assert.Inconclusive ("Inconclusive");

