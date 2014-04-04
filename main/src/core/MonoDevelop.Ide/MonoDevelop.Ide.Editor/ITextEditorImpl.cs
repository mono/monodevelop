//
// ITextEditorImpl.cs
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
	public interface ITextEditorImpl : ITextSource
	{
		ISyntaxMode SyntaxMode { get; set; }
		ITextEditorOptions Options { get; set; }

		TextLocation CaretLocation { get; set; }
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

		void SetSelection (int anchorOffset, int leadOffset);

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

		double LineHeight { get; }

		TextLocation PointToLocation (double xp, double yp, bool endAtEol = false);

		Cairo.PointD LocationToPoint (TextLocation currentSmartTagBegin);

		/// <summary>
		/// Gets/Sets the text of the whole document..
		/// </summary>
		new string Text { get; set; } // hides ITextSource.Text to add the setter

		string EolMarker { get; }

		/// <summary>
		/// This event is called directly before a change is applied to the document.
		/// </summary>
		/// <remarks>
		/// It is invalid to modify the document within this event handler.
		/// Aborting the change (by throwing an exception) is likely to cause corruption of data structures
		/// that listen to the Changing and Changed events.
		/// </remarks>
		event EventHandler<TextChangeEventArgs> TextChanging;

		/// <summary>
		/// This event is called directly after a change is applied to the document.
		/// </summary>
		/// <remarks>
		/// It is invalid to modify the document within this event handler.
		/// Aborting the event handler (by throwing an exception) is likely to cause corruption of data structures
		/// that listen to the Changing and Changed events.
		/// </remarks>
		event EventHandler<TextChangeEventArgs> TextChanged;

		/// <summary>
		/// Gets the number of lines in the document.
		/// </summary>
		int LineCount { get; }

		int LocationToOffset (TextLocation location);

		TextLocation OffsetToLocation (int offset);

		int Insert (int offset, string text);

		void Remove (ISegment segment);

		int Replace (int offset, int count, string value);

		IDocumentLine GetLine (int lineNumber);

		IDocumentLine GetLineByOffset (int offset);

		int OffsetToLineNumber (int offset);

		void AddMarker (IDocumentLine line, ITextLineMarker lineMarker);
		void RemoveMarker (ITextLineMarker lineMarker);

		IEnumerable<ITextLineMarker> GetLineMarker (IDocumentLine line);

		#region Text segment markers

		IEnumerable<ITextSegmentMarker> GetTextSegmentMarkersAt (ISegment segment);
		IEnumerable<ITextSegmentMarker> GetTextSegmentMarkersAt (int offset);

		/// <summary>
		/// Adds a marker to the document.
		/// </summary>
		void AddMarker (ITextSegmentMarker marker);

		/// <summary>
		/// Removes a marker from the document.
		/// </summary>
		/// <returns><c>true</c>, if marker was removed, <c>false</c> otherwise.</returns>
		/// <param name="marker">Marker.</param>
		bool RemoveMarker (ITextSegmentMarker marker);

		#endregion

		IEnumerable<IFoldSegment> GetFoldingsFromOffset (int offset);
		IEnumerable<IFoldSegment> GetFoldingContaining (IDocumentLine line);
		IEnumerable<IFoldSegment> GetStartFoldings (IDocumentLine line);
		IEnumerable<IFoldSegment> GetEndFoldings (IDocumentLine line);
	}
}

