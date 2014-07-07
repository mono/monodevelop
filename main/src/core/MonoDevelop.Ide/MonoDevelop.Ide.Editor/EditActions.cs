//
// EditActions.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.Ide.Editor
{
	/// <summary>
	/// This class contains some common actions for the text editor.
	/// </summary>
	public static class EditActions
	{
		public static void MoveCaretDown (TextEditor editor)
		{
			editor.EditorActionHost.MoveCaretDown ();
		}

		public static void MoveCaretUp (TextEditor editor)
		{
			editor.EditorActionHost.MoveCaretUp ();
		}

		public static void MoveCaretRight (TextEditor editor)
		{
			editor.EditorActionHost.MoveCaretRight ();
		}

		public static void MoveCaretLeft (TextEditor editor)
		{
			editor.EditorActionHost.MoveCaretLeft ();
		}

		public static void MoveCaretToLineEnd (TextEditor editor)
		{
			editor.EditorActionHost.MoveCaretToLineEnd ();
		}

		public static void MoveCaretToLineStart (TextEditor editor)
		{
			editor.EditorActionHost.MoveCaretToLineStart ();
		}

		public static void MoveCaretToDocumentStart (TextEditor editor)
		{
			editor.EditorActionHost.MoveCaretToDocumentStart ();
		}

		public static void MoveCaretToDocumentEnd (TextEditor editor)
		{
			editor.EditorActionHost.MoveCaretToDocumentEnd ();
		}

		public static void Backspace (TextEditor editor)
		{
			editor.EditorActionHost.Backspace ();
		}

		public static void Delete (TextEditor editor)
		{
			editor.EditorActionHost.Delete ();
		}

		public static void ClipboardCopy (TextEditor editor)
		{
			editor.EditorActionHost.ClipboardCopy ();
		}

		public static void ClipboardCut (TextEditor editor)
		{
			editor.EditorActionHost.ClipboardCut ();
		}

		public static void ClipboardPaste (TextEditor editor)
		{
			editor.EditorActionHost.ClipboardPaste ();
		}

		public static void SelectAll (TextEditor editor)
		{
			editor.EditorActionHost.SelectAll ();
		}

		public static void NewLine (TextEditor editor)
		{
			editor.EditorActionHost.NewLine ();
		}

		public static void Undo (TextEditor editor)
		{
			editor.EditorActionHost.Undo ();
		}

		public static void Redo (TextEditor editor)
		{
			editor.EditorActionHost.Redo ();
		}
	}
}
