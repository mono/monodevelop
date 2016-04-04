//
// ProtocolMemberContextHandlerTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System;
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.Completion;
using ICSharpCode.NRefactory6.CSharp.CodeCompletion.Roslyn;
using MonoDevelop.CSharp.Completion;
using System.Runtime.Remoting.Messaging;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion.NR6
{

	[TestFixture]
	class ProtocolMemberContextHandlerTests : CompletionTestBase
	{
		static readonly string Header = @"
using System;
using Foundation;

namespace Foundation
{
	public class ExportAttribute : Attribute
	{
		public ExportAttribute(string id) { }
	}

	public class ProtocolAttribute : Attribute
	{
		public string Name { get; set; }
		public ProtocolAttribute() { }
	}
}";

		internal override CompletionContextHandler CreateContextHandler ()
		{
			return new ProtocolMemberContextHandler ();
		}

		[Test]
		public void TestSimple ()
		{
			VerifyItemsExist (Header + @"

class MyProtocol
{
	[Export("":FooBar"")]
	public virtual void FooBar()
	{

	}
}


[Protocol(Name = ""MyProtocol"")]
class ProtocolClass
{

}


class FooBar : ProtocolClass
{
	override $$
}

", "FooBar");
		}

		/// <summary>
		/// Bug 39428 - [iOS] Override of protocol method shows 2 completions
		/// </summary>
		[Test]
		public void TestBug39428 ()
		{
			VerifyItemIsAbsent (Header + @"

class MyProtocol
{
	[Export("":FooBar"")]
	public virtual void FooBar()
	{

	}
}


[Protocol(Name = ""MyProtocol"")]
class ProtocolClass
{
	public virtual void FooBar()
	{
	}
}

class FooBar : ProtocolClass
{
	override $$
}

", "FooBar");
		}
	}
}