//
// ITextEditor.cs
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
using MonoDevelop.Core.Text;
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor
{
	public interface ITextEditor : IDocument
	{
		ISyntaxMode SyntaxMode { get; set; }
		ITextEditorOptions Options { get; set; }

		TextLocation CaretLocation { get; set; }

		int CaretLine { get; set; }
		int CaretColumn { get; set; }

		int CaretOffset { get; set; }

		void Undo ();
		void Redo ();

		IDisposable OpenUndoGroup();

		bool ReadOnly {
			get;
			set;
		}

		bool IsSomethingSelected { get; }

		SelectionMode SelectionMode { get; }

		ISegment SelectionRange { get; set; }
		DocumentRegion SelectionRegion { get; set; }

		string SelectedText { get; }

		void SetSelection (int anchorOffset, int leadOffset);

		void SetSelection (TextLocation anchor, TextLocation lead);


		event EventHandler SelectionChanged;

		event EventHandler CaretPositionChanged;

		void ClearSelection ();

		void CenterToCaret ();

		void StartCaretPulseAnimation ();

		int EnsureCaretIsNotVirtual ();

		void FixVirtualIndentation ();

		IEditorActionHost Actions { get; }

		bool IsInAtomicUndo {
			get;
		}

		event EventHandler BeginUndo;
		event EventHandler EndUndo;

		Gtk.Widget GetGtkWidget ();

		void RunWhenLoaded (Action action);

		string FormatString (TextLocation insertPosition, string code);

		void StartInsertionMode (string operation, IList<InsertionPoint> insertionPoints, Action<InsertionCursorEventArgs> action);

		void StartTextLinkMode (List<TextLink> links);

		void RequestRedraw ();

		void InsertAtCaret (string text);

		double LineHeight { get; }

		TextLocation PointToLocation (double xp, double yp, bool endAtEol = false);

		Cairo.PointD LocationToPoint (TextLocation currentSmartTagBegin);
	}
}

