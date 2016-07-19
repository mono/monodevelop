//
// SyntaxHighlightingTest.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Microsoft
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
using Mono.TextEditor;
using MonoDevelop.Ide.Editor.Util;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Ide.Editor.Highlighting;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Text;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	class SyntaxHighlightingTest : TestBase
	{
		[Test]
		public void TestMatch()
		{
			string highlighting = @"%YAML 1.2
---
name: Test
file_extensions: [t]
scope: source

contexts:
  main:
    - match: \b(foo|bar)\b
      scope: keyword
";
			string test = @"
test foo this bar
^ source
     ^ keyword
         ^ source
              ^ keyword
";
			RunHighlightingTest (highlighting, test);

		}

		[Test]
		public void TestPushPop()
		{
			string highlighting = @"%YAML 1.2
---
name: Test
file_extensions: [t]
scope: source

contexts:
  main:
    - match: \b(foo|bar)\b
      scope: keyword
    - match: '""'
      scope: string.quoted
      push: string

  string:
    - match: '\\.'
      scope: constant.character.escape
    - match: '""'
      pop: true
";
			string test = @"
test foo ""th\tis"" bar
^ source
     ^ keyword
         ^ string.quoted
            ^ constant.character.escape
                  ^ keyword
";
			RunHighlightingTest (highlighting, test);

		}

		static void RunHighlightingTest (string highlightingSrc, string inputText)
		{
			var highlighting = Sublime3Format.ReadHighlighting (new StringReader (highlightingSrc));
			var editor = TextEditorFactory.CreateNewEditor ();
			var sb = new StringBuilder ();
			int lineNumber = 0;
			var expectedSegments = new List<Tuple<DocumentLocation, string>> ();
			using (var sr = new StringReader (inputText)) {
				while (true) {
					var line = sr.ReadLine ();
					if (line == null)
						break;
					var idx = line.IndexOf ('^');
					if (idx >= 0) {
						expectedSegments.Add (Tuple.Create (new DocumentLocation (lineNumber, idx + 1), line.Substring (idx + 1).Trim ()));
					} else {
						lineNumber++;
						sb.AppendLine (line);
					}
				}
			}
			editor.Text = sb.ToString ();

			editor.SyntaxHighlighting = new SyntaxHighlighting (highlighting, editor);
			foreach (var line in editor.GetLines ()) {
				var coloredSegments = editor.SyntaxHighlighting.GetColoredSegments (line, line.Offset, line.Length).ToList ();
				for (int i = 0; i < expectedSegments.Count; i++) {
					var seg = expectedSegments [i];
					if (seg.Item1.Line == line.LineNumber) {
						var matchedSegment = coloredSegments.FirstOrDefault (s => s.Contains (seg.Item1.Column + line.Offset - 1));
						Assert.NotNull (matchedSegment, "No segment found at : " + seg.Item1);
						Assert.AreEqual (seg.Item2, matchedSegment.ColorStyleKey, "Wrong color at " + seg.Item1);
						expectedSegments.RemoveAt (i);
						i--;
					}
				}
			}
			Assert.AreEqual (0, expectedSegments.Count, "Not all segments matched.");
		}
	}
}
