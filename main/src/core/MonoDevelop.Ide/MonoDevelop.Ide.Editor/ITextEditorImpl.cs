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
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Editor
{
	public interface ITextEditorImpl : IViewContent
	{
		ISyntaxMode SyntaxMode { get; set; }
		ITextEditorOptions Options { get; set; }
		IReadonlyTextDocument Document { get; set; }
	
		TextLocation CaretLocation { get; set; }
		int CaretOffset { get; set; }

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

		event EventHandler BeginUndo;
		event EventHandler EndUndo;

		Gtk.Widget GetGtkWidget ();

		void RunWhenLoaded (Action action);

		string FormatString (int offset, string code);

		void StartInsertionMode (string operation, IList<InsertionPoint> insertionPoints, Action<InsertionCursorEventArgs> action);

		void StartTextLinkMode (List<TextLink> links);

		void RequestRedraw ();

		double LineHeight { get; }

		TextLocation PointToLocation (double xp, double yp, bool endAtEol = false);

		Cairo.Point LocationToPoint (TextLocation currentSmartTagBegin);

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