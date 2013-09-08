//
// CompletionTests.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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

using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.AspNet.Mvc.Completion
{
	[TestFixture]
	public class CompletionTests : TestBase
	{
		[Test]
		public void HtmlTagsCompletion ()
		{
			var provider = RazorCompletionTesting.CreateProvider ("<$", false);
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("p"));
			Assert.IsNotNull (provider.Find ("div"));
		}

		[Test]
		public void NestedHtmlTagsCompletion ()
		{
			var provider = RazorCompletionTesting.CreateProvider ("<div><ul><$ </ul></div>", false);
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("li"));
		}

		[Test]
		public void RazorDirectivesAndStatementsCompletion ()
		{
			var provider = RazorCompletionTesting.CreateProvider ("@m$", true);
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("model"));
			Assert.IsNotNull (provider.Find ("sessionstate"));
			Assert.IsNotNull (provider.Find ("using"));
			Assert.IsNotNull (provider.Find ("layout"));
			Assert.IsNotNull (provider.Find ("section"));
			Assert.IsNotNull (provider.Find ("functions"));
			Assert.IsNotNull (provider.Find ("helper"));
			Assert.IsNotNull (provider.Find ("inherits"));
			Assert.IsNotNull (provider.Find ("for"));
			Assert.IsNotNull (provider.Find ("foreach"));
			Assert.IsNotNull (provider.Find ("while"));
			Assert.IsNotNull (provider.Find ("do"));
			Assert.IsNotNull (provider.Find ("lock"));
			Assert.IsNotNull (provider.Find ("switch"));
			Assert.IsNotNull (provider.Find ("if"));
			Assert.IsNotNull (provider.Find ("try"));
		}

		[Test]
		public void CSharpIdentifiersCompletion ()
		{
			var provider = RazorCompletionTesting.CreateProvider ("@{ i$ }", true);
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("int"));
			Assert.IsNotNull (provider.Find ("var"));
		}

		[Test]
		public void CSharpIdentifiersCtrlSpaceCompletion ()
		{
			var provider = RazorCompletionTesting.CreateRazorCtrlSpaceProvider ("@{ $ }", true);
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("int"));
			Assert.IsNotNull (provider.Find ("var"));
		}

		[Test]
		public void CSharpMembersCompletion ()
		{
			var provider = RazorCompletionTesting.CreateProvider ("@{ Char.$ }", true);
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("IsLetter"));
		}

		[Test]
		public void CSharpMembersCtrlSpaceCompletion ()
		{
			var provider = RazorCompletionTesting.CreateRazorCtrlSpaceProvider ("@{ Char.Is$ }", true);
			Assert.IsNotNull (provider);
			Assert.IsNotNull (provider.Find ("IsLetter"));
		}

		[Test]
		public void CSharpParametersCompletion ()
		{
			var provider = RazorCompletionTesting.CreateProvider ("@{ Char.IsLetter($ }");
			Assert.IsNotNull (provider);
			Assert.AreEqual (2, provider.Count);
		}
	}
}
