// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.Completion;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion.Roslyn
{
	[TestFixture]
	public class ExplicitInterfaceContextHandlerTests : CompletionTestBase
	{
		[Test]
		public void ExplicitInterfaceMember()
		{
			var markup = @"
interface IFoo
{
    void Foo();
    void Foo(int x);
    int Prop { get; }
}

class Bar : IFoo
{
     void IFoo.$
}";

			VerifyItemExists(markup, "Foo");
			VerifyItemExists(markup, "Prop");
		}

//		[Test]
//		public void CommitOnNotParen()
//		{
//			var markup = @"
//interface IFoo
//{
//    void Foo();
//}
//
//class Bar : IFoo
//{
//     void IFoo.$$
//}";
//
//			var expected = @"
//interface IFoo
//{
//    void Foo();
//}
//
//class Bar : IFoo
//{
//     void IFoo.Foo()
//}";
//
//			VerifyProviderCommit(markup, "Foo()", expected, null, "");
//		}

//		[WorkItem(709988)]
//		[Fact, Trait(Traits.Feature, Traits.Features.Completion)]
//		public void CommitOnParen()
//		{
//			var markup = @"
//interface IFoo
//{
//    void Foo();
//}
//
//class Bar : IFoo
//{
//     void IFoo.$$
//}";
//
//			var expected = @"
//interface IFoo
//{
//    void Foo();
//}
//
//class Bar : IFoo
//{
//     void IFoo.Foo
//}";
//
//			VerifyProviderCommit(markup, "Foo()", expected, '(', "");
//		}
	}
}
