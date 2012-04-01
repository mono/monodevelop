// 
// TextEditorTestBase.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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

namespace Mono.TextEditor.Tests
{
	public class TextEditorTestBase
	{
		static bool firstRun = true;
		
		[TestFixtureSetUp]
		public virtual void Setup ()
		{
			if (firstRun) {
				Gtk.Application.Init ();
				firstRun = false;
			}
		}

		[TestFixtureTearDown]
		public virtual void TearDown ()
		{
		}


		public static TextEditorData Create (string content)
		{
			int caretIndex = content.IndexOf ("$");
			if (caretIndex >= 0)
				content = content.Substring (0, caretIndex) + content.Substring (caretIndex + 1);

			int selection1 = content.IndexOf ("<-");
			int selection2 = content.IndexOf ("->");
			
			int selectionStart = 0;
			int selectionEnd = 0;
			if (0 <= selection1 && selection1 < selection2) {
				content = content.Substring (0, selection2) + content.Substring (selection2 + 2);
				content = content.Substring (0, selection1) + content.Substring (selection1 + 2);
				selectionStart = selection1;
				selectionEnd = selection2 - 2;
				caretIndex = selectionEnd;
			}

			var data = new TextEditorData ();
			data.Text = content;
			if (caretIndex >= 0)
				data.Caret.Offset = caretIndex;
			if (selection1 >= 0) {
				if (caretIndex == selectionStart) {
					data.SetSelection (selectionEnd, selectionStart);
				} else {
					data.SetSelection (selectionStart, selectionEnd);
				}
			}
			return data;
		}

		public static void Check (TextEditorData data, string content)
		{
			int caretIndex = content.IndexOf ("$");
			if (caretIndex >= 0)
				content = content.Substring (0, caretIndex) + content.Substring (caretIndex + 1);

			int selection1 = content.IndexOf ("<-");
			int selection2 = content.IndexOf ("->");
			
			int selectionStart = 0;
			int selectionEnd = 0;
			if (0 <= selection1 && selection1 < selection2) {
				content = content.Substring (0, selection2) + content.Substring (selection2 + 2);
				content = content.Substring (0, selection1) + content.Substring (selection1 + 2);
				selectionStart = selection1;
				selectionEnd = selection2 - 2;
				caretIndex = selectionEnd;
			}

			if (caretIndex >= 0)
				Assert.AreEqual (caretIndex, data.Caret.Offset, "Caret offset mismatch.");
			if (selection1 >= 0) {
				Assert.AreEqual (new TextSegment (selection1, selection2 - selection1), data.SelectionRange, "Selection mismatch.");
			}
			Assert.AreEqual (content, data.Text);
		}
	}
}
