namespace MonoDevelop.FSharp.Tests
open System
open NUnit.Framework
open MonoDevelop.FSharp

[<TestFixture>]
type DebuggerExpressionResolver() = 

    [<Test>]
    member x.WhatToTest() =
        Assert.IsTrue(true)

