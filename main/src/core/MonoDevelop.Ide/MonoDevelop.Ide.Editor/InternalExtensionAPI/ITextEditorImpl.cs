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
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Components;
using Xwt;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Editor
{
	public enum EditMode
	{
		Edit,
		TextLink,
		CursorInsertion
	}

	interface ITextEditorImpl : IDisposable
	{
		ViewContent ViewContent { get; }

		string ContentName { get; set; }

		string ContextMenuPath { get; set; }

		EditMode EditMode { get; }

		ITextEditorOptions Options { get; set; }

		IReadonlyTextDocument Document { get; }

		SemanticHighlighting SemanticHighlighting { get; set; }

		ISyntaxHighlighting SyntaxHighlighting { get; set; }
	
		int CaretOffset { get; set; }

		bool IsSomethingSelected { get; }

		IEnumerable<Selection> Selections { get; }

		SelectionMode SelectionMode { get; }

		ISegment SelectionRange { get; set; }
		int SelectionAnchorOffset { get; set; }
		int SelectionLeadOffset { get; set; }

		DocumentRegion SelectionRegion { get; set; }

		void SetSelection (int anchorOffset, int leadOffset);

		event EventHandler SelectionChanged;

		event EventHandler CaretPositionChanged;

		event EventHandler<MouseMovedEventArgs> MouseMoved;

		event EventHandler VAdjustmentChanged;

		event EventHandler HAdjustmentChanged;

		void ClearSelection ();

		void CenterToCaret ();

		void StartCaretPulseAnimation ();

		int EnsureCaretIsNotVirtual ();

		void FixVirtualIndentation ();

		IEditorActionHost Actions { get; }

		ITextMarkerFactory TextMarkerFactory { get; }

		event EventHandler BeginAtomicUndoOperation;

		event EventHandler EndAtomicUndoOperation;

		object CreateNativeControl ();

		void RunWhenLoaded (Action action);

		void RunWhenRealized (Action action);

		string FormatString (int offset, string code);

		void StartInsertionMode (InsertionModeOptions insertionModeOptions);

		void StartTextLinkMode (TextLinkModeOptions textLinkModeOptions);

		double LineHeight { get; }

		DocumentLocation PointToLocation (double xp, double yp, bool endAtEol = false);

		Xwt.Point LocationToPoint (int line, int column);

		void AddMarker (IDocumentLine line, ITextLineMarker lineMarker);

		void RemoveMarker (ITextLineMarker lineMarker);

		void ScrollTo (int offset);

		void CenterTo (int offset);

		EditSession CurrentSession {
			get;
		}

		void StartSession (EditSession session);

		void EndSession ();

		string GetVirtualIndentationString (int lineNumber);

		IEnumerable<ITextLineMarker> GetLineMarkers (IDocumentLine line);

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

		IFoldSegment CreateFoldSegment (int offset, int length, bool isFolded = false);

		void SetFoldings (IEnumerable<IFoldSegment> foldings);

		IEnumerable<IFoldSegment> GetFoldingsContaining (int offset);

		IEnumerable<IFoldSegment> GetFoldingsIn (int offset, int length);

		string GetPangoMarkup (int offset, int length, bool fitIdeStyle = false);

		string GetMarkup (int offset, int length, MarkupOptions options);

        IndentationTracker IndentationTracker { get; set; }
        void SetSelectionSurroundingProvider (SelectionSurroundingProvider surroundingProvider);
		void SetTextPasteHandler (TextPasteHandler textPasteHandler);

		#region Internal use only API (do not mirror in TextEditor)

		TextEditorExtension EditorExtension {
			get;
			set;
		}

		IEnumerable<TooltipProvider> TooltipProvider {
			get;
		}

		void ClearTooltipProviders ();

		void AddTooltipProvider (TooltipProvider provider);

		void RemoveTooltipProvider (TooltipProvider provider);

		Xwt.Point GetEditorWindowOrigin ();

		Xwt.Rectangle GetEditorAllocation ();

		void InformLoadComplete ();

		void SetUsageTaskProviders (IEnumerable<UsageProviderEditorExtension> providers);

		void SetQuickTaskProviders (IEnumerable<IQuickTaskProvider> providers);
		#endregion

		double ZoomLevel { get; set; }
		bool SuppressTooltips { get; set; }

		event EventHandler ZoomLevelChanged;

		void AddOverlay (Control messageOverlayContent, Func<int> sizeFunc);
		void RemoveOverlay (Control messageOverlayContent);
		void UpdateBraceMatchingResult (BraceMatchingResult? result);

		IEnumerable<IDocumentLine> VisibleLines { get; }
		IReadOnlyList<Caret> Carets { get; }

		void GrabFocus ();
		bool HasFocus { get; }

		event EventHandler<LineEventArgs> LineShown;
		event EventHandler FocusLost;

		void ShowTooltipWindow (Control window, TooltipWindowOptions options);
		Task<ScopeStack> GetScopeStackAsync (int offset, CancellationToken cancellationToken);

		double GetLineHeight (int line);
	}
}
