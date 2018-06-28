//
// RegexTests.cs
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
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	class RegexTests : IdeTestBase
	{
		[Test]
		public void TestSimpleMatch ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			editor.Text = "FooBarFoo";

			var r = new Regex ("Bar");
			var match = r.Match (editor.Text);
			Assert.IsTrue (match.Success);
			Assert.AreEqual ("Foo".Length, match.Groups [0].Index);
		}

		[Test]
		public void TestHRefSearching ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			editor.Text = "My favorite web sites include:</P>" +
						"<A HREF=\"http://msdn2.microsoft.com\">" +
						"MSDN Home Page</A></P>" +
						"<A HREF=\"http://www.microsoft.com\">" +
						"Microsoft Corporation Home Page</A></P>" +
						"<A HREF=\"http://blogs.msdn.com/bclteam\">" +
						".NET Base Class Library blog</A></P>"; ;

			var HRefPattern = "href\\s*=\\s*(?:[\"'](?<1>[^\"']*)[\"']|(?<1>\\S+))";
			var m = Regex.Match (editor.Text, HRefPattern,
				RegexOptions.IgnoreCase | RegexOptions.Compiled,
				TimeSpan.FromSeconds (1));
			int matches = 0;
			while (m.Success) {
				matches++;
				m = m.NextMatch ();
			}

			Assert.AreEqual (3, matches);
		}

		[Test]
		public void TestPositionalMatch ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			editor.Text = "FooBarFoo";

			var r = new Regex ("B.r");
			var match = r.Match (editor.Text, "Foo".Length, editor.Length - "Foo".Length);
			Assert.IsTrue (match.Success);
			Assert.AreEqual ("Foo".Length, match.Groups [0].Index);
		}

		[Test]
		public void TestUsingMatch ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			editor.Text = "// FooBar\nusing System;";

			var r = new Regex ("^\\s*(using)\\s+([^ ;]*);", RegexOptions.Multiline);
			var line = editor.GetLine (2);
			var match = r.Match (editor.Text, line.Offset, line.Length);
			Assert.IsTrue (match.Success);
		}

		//	Bug 595108: [Feedback] VS for MAC 7.4 and 7.5 preview: javascript syntax highlighting breaks when passing parameter to a function
		[Test]
		public void TestVSTS595108 ()
		{
			var regex = new MonoDevelop.Ide.Editor.Highlighting.RegexEngine.Regex ("(?=^)");
			var input = "// test";
			var match = regex.Match (input, 2, input.Length - 2);
			Assert.AreEqual (false, match.Success);
		}
	}
}