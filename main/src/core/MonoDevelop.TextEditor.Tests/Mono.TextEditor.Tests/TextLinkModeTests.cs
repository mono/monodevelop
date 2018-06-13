//
// TextLinkModeTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Text;
using NUnit.Framework;
using MonoDevelop.Ide.Editor;
using System.Text;

namespace Mono.TextEditor.Tests
{
	[TestFixture]
	class TextLinkModeTests : TextEditorTestBase
	{
		MonoTextEditor CreateEditorWithLinks (string input, out List<TextLink> links)
		{
			links = new List<TextLink> ();
			var segments = new List<List<ISegment>> ();
			int caretOffset = 0;
			var text = new StringBuilder ();
			int curLink = -1, linkStart = 0;
			for (int i = 0; i < input.Length; i++) {
				char ch = input [i];
				switch (ch) {
				case '[':
					i++; // skip number
					curLink = (int)(input[i] - '0');
					if (segments.Count - 1 < curLink) // links need to be defined in order
						segments.Add (new List<ISegment> ());
					linkStart = text.Length;
					break;
				case ']':
					if (curLink < 0)
						goto default;
					segments [curLink].Add (TextSegment.FromBounds (linkStart, text.Length));
					curLink = -1;
					break;
				case '$':
					caretOffset = text.Length;
					break;
				default:
					text.Append (ch);
					break;
				}
			}

			for (int i = 0; i < segments.Count; i++) {
				links.Add (new TextLink (i.ToString ()) {
					Links = segments [i]
				});
			}

			var result = new MonoTextEditor ();
			result.Text = text.ToString ();
			result.Caret.Offset = caretOffset;
			return result;
		}

		[Test]
		public void TestLinkRemoval ()
		{
			var editor = CreateEditorWithLinks (@"[0foo] [0foo] [0foo]", out var links);
			editor.Options.IndentStyle = IndentStyle.Auto;
			var oldLocation = editor.Caret.Location; 

			var tle = new TextLinkEditMode (editor, 0, links);
			tle.StartMode ();
			DeleteActions.Backspace (editor.GetTextEditorData ());
			tle.ExitTextLinkMode ();
			Assert.AreEqual ("  ", editor.Text);
		}

		[Test]
		public void TestLinkReplace ()
		{
			var editor = CreateEditorWithLinks (@"[0foo] [0foo] [0foo]", out var links);
			editor.Options.IndentStyle = IndentStyle.Auto;
			var oldLocation = editor.Caret.Location; 

			var tle = new TextLinkEditMode (editor, 0, links);
			tle.StartMode ();
			editor.InsertAtCaret ("bar");
			tle.ExitTextLinkMode ();
			Assert.AreEqual ("bar bar bar", editor.Text);
		}


		/// <summary>
		/// Bug 627497: [Feedback] code snippet cursor in wrong location after setting condition
		/// </summary>
		[Test]
		public void TestVSTS627497 ()
		{
			var editor = CreateEditorWithLinks (@"[0class] Test
{
	void TestMethod ()
	{
		$
	}
}", out var links);
			var oldLocation = editor.Caret.Location;

			var tle = new TextLinkEditMode (editor, 0, links);
			tle.StartMode ();
			DeleteActions.Backspace (editor.GetTextEditorData ());
			editor.InsertAtCaret ("struct");
			tle.ExitTextLinkMode ();

			Assert.AreEqual (oldLocation, editor.Caret.Location);
		}
	}
}
