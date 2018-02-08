//
// TextEditor.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Ide.Commands;
using UserInterfaceTests;

namespace MonoDevelop.StressTest
{
	public static class TextEditor
	{
		static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		public static void MoveCaretToDocumentStart ()
		{
			Session.ExecuteCommand (TextEditorCommands.DocumentStart);
		}

		public static void EnterText (IEnumerable<string> items)
		{
			foreach (string text in items) {
				Session.EnterText (IdeQuery.TextArea, text);
			}
		}

		public static void EnterText (string text)
		{
			Session.EnterText (IdeQuery.TextArea, text);
		}

		/// <summary>
		/// Seems to timeout sending first character and character never appears
		/// in text editor.
		/// </summary>
		public static void TypeText (string text)
		{
			foreach (char key in text) {
				Session.TypeKey (IdeQuery.TextArea, key, null);
			}
		}

		public static void DeleteToLineStart ()
		{
			Session.ExecuteCommand (TextEditorCommands.DeleteToLineStart);
		}
	}
}
