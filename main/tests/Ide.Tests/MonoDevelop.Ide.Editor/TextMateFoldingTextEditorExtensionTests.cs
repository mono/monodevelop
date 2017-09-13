//
// TextMateFoldingTextEditorExtensionTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Ide.Editor.TextMate;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;
using UnitTests;
using System.Threading;
using System.Linq;
using System;
using System.Text;
using System.Collections.Generic;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public class TextMateFoldingTextEditorExtensionTests : IdeTestBase
	{
		[Test]
		public void TestOneLevel ()
		{
			TestFoldings (@"
{+
	foo
	bar
}-
");
		}


		[Test]
		public void TestSeveralLevels ()
		{
			TestFoldings (@"
{+
	foo
	{+
		foo {+
			foo
			bar	
		}-
		bar
	}-
	bar
}-
");
		}

		[Test]
		public void TestSkipEmptyLines ()
		{
			TestFoldings (@"
{+
	foo
	if (true)
		foo


}-
");
		}


		void TestFoldings (string text)
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			var sb = new StringBuilder ();
			var foldStack = new Stack<int> ();
			var expected = new List<ISegment> ();
			foreach (var ch in text) {
				if (ch == '+') {
					foldStack.Push (sb.Length);
					continue;
				}
				if (ch == '-') {
					expected.Add (TextSegment.FromBounds (foldStack.Pop (), sb.Length));
					continue;
				}
				sb.Append (ch);
			}
			editor.Text = sb.ToString ();
			var ext = new TextMateFoldingTextEditorExtension ();
			var foldings = ext.GetFoldingsAsync (editor, default (CancellationToken)).Result.ToList ();
			Assert.AreEqual (expected.Count, foldings.Count);

			for (int i = 0; i < foldings.Count; i++) {
				Assert.AreEqual (expected [i].Offset, foldings [i].Offset);
				Assert.AreEqual (expected [i].Length, foldings [i].Length);
			}
		}
	}
}
