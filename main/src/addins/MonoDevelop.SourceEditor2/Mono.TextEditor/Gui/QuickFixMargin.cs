//
// QuickFixMargin.cs
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
using Gtk;
using Gdk;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

namespace Mono.TextEditor
{
	class QuickFixMargin : Margin
	{
		MonoTextEditor editor;
		Cairo.Color backgroundColor;
		const int marginWidth = 22;

		public QuickFixMargin (MonoTextEditor editor)
		{
			this.editor = editor;

			editor.Document.MarkerAdded += OnMarkerAdded;
			editor.Document.MarkerRemoved += OnMarkerRemoved;
			this.editor.Caret.PositionChanged += HandlePositionChanged; 
		}

		void HandlePositionChanged (object sender, MonoDevelop.Ide.Editor.DocumentLocationEventArgs e)
		{
			if (e.Location.Line == editor.Caret.Line)
				return;
			editor.RedrawMarginLine (this, e.Location.Line);
			editor.RedrawMarginLine (this, editor.Caret.Line);
		}

		public override void Dispose ()
		{
			editor.Document.MarkerAdded -= OnMarkerAdded;
			editor.Document.MarkerRemoved -= OnMarkerRemoved;
			editor.Caret.PositionChanged -= HandlePositionChanged; ;

			if (markerToAccessible != null) {
				foreach (var proxy in markerToAccessible.Values) {
					proxy.Dispose ();
				}
				markerToAccessible = null;
			}

			base.Dispose ();
		}

		public override double Width {
			get {
				return marginWidth;
			}
		}

		internal protected override void OptionsChanged ()
		{
			backgroundColor = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.LineNumbersBackground);
		}

		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);

			var lineSegment = args.LineSegment;
			if (lineSegment != null) {
				foreach (var marker in editor.Document.GetMarkers (lineSegment)) {
					if (marker is MarginMarker marginMarker)
						marginMarker.InformMousePress (editor, this, args);
				}
			}
		}

		internal protected override void MouseReleased (MarginMouseEventArgs args)
		{
			base.MouseReleased (args);

			var lineSegment = args.LineSegment;
			if (lineSegment != null) {
				foreach (var marker in editor.Document.GetMarkers (lineSegment)) {
					if (marker is MarginMarker marginMarker)
						marginMarker.InformMouseRelease (editor, this, args);
				}
			}
		}

		readonly static Cursor textLinkCursor = new Cursor (CursorType.Hand1);
		public MarginMarker HoveredSmartTagMarker {
			get => hoveredSmartTagMarker; 
			set {
				hoveredSmartTagMarker = value;
				cursor = value != null ? textLinkCursor : null;

				editor.TextArea.RedrawMargin (this);
			}
		}

		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);
			args.Editor.TooltipText = null;
			var lineSegment = args.LineSegment;
			if (lineSegment != null) {
				foreach (var marker in editor.Document.GetMarkers (lineSegment)) {
					if (marker is MarginMarker marginMarker)
						marginMarker.InformMouseHover (editor, this, args);
				}
			}
		}

		internal protected override void MouseLeft ()
		{
			HoveredSmartTagMarker = null;
			base.MouseLeft ();
		}

		internal protected override void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine line, int lineNumber, double x, double y, double lineHeight)
		{
			bool backgroundIsDrawn = false;
			bool lineMarkerIsSelected = false;
			if (line != null) {
				foreach (var marker in editor.Document.GetMarkersOrderedByInsertion (line)) {
					if (marker is MarginMarker marginMarker) {
						if (marginMarker.CanDrawBackground (this)) {
							backgroundIsDrawn = marginMarker.DrawBackground (editor, cr, new MarginDrawMetrics (this, area, line, lineNumber, x, y, lineHeight));
						}

						if (focusMarkers != null) {
							SanitizeFocusedIndex ();

							lineMarkerIsSelected = (HasFocus && (marginMarker == focusMarkers [focusedIndex]));
						}
					}
				}
			}

			if (!backgroundIsDrawn) {
				if (editor.Caret.Line == lineNumber) {
					editor.TextViewMargin.DrawCaretLineMarker (cr, x, y, Width, lineHeight);
				} else {
					cr.Rectangle (x, y, Width, lineHeight);
					cr.SetSourceColor (backgroundColor);
					cr.Fill ();
				}
			}

			if (line != null && lineNumber <= editor.Document.LineCount) {
				foreach (var marker in editor.Document.GetMarkersOrderedByInsertion (line)) {
					if (marker is MarginMarker marginMarker && marginMarker.CanDrawForeground (this)) {
						var metrics = new MarginDrawMetrics (this, area, line, lineNumber, x, y, lineHeight);
						marginMarker.DrawForeground (editor, cr, metrics);

						if (markerToAccessible != null) {
							var accessible = markerToAccessible [marker];
							if (accessible != null) {
								accessible.Metrics = metrics;
								accessible.UpdateAccessibilityDetails ();
							}
						}
					}
				}
				DrawEvent?.Invoke (this, new BookmarkMarginDrawEventArgs (editor, cr, line, lineNumber, x, y));
			}
		}

		Dictionary<TextLineMarker, AccessibilityMarkerProxy> markerToAccessible;

		void OnMarkerAdded (object sender, TextMarkerEvent e)
		{
			if (!IdeTheme.AccessibilityEnabled) {
				return;
			}

			if (markerToAccessible == null) {
				markerToAccessible = new Dictionary<TextLineMarker, AccessibilityMarkerProxy> ();
			}

			var proxy = new AccessibilityMarkerProxy (e.TextMarker, editor, this);
			Accessible.AddAccessibleChild (proxy.Accessible);

			markerToAccessible [e.TextMarker] = proxy;

			if (focusMarkers != null) {
				UpdateMarkers ();
			}
		}

		void OnMarkerRemoved (object sender, TextMarkerEvent e)
		{
			if (!IdeTheme.AccessibilityEnabled) {
				return;
			}

			if (markerToAccessible == null) {
				return;
			}

			var proxy = markerToAccessible [e.TextMarker];
			if (proxy == null) {
				throw new Exception ("No accessible found for marker");
			}

			Accessible.RemoveAccessibleChild (proxy.Accessible);
			markerToAccessible.Remove (e.TextMarker);

			if (focusMarkers != null) {
				UpdateMarkers ();
			}
		}

		List<TextLineMarker> focusMarkers;
		int focusedIndex;
		protected internal override bool SupportsItemCommands => true;

		void SanitizeFocusedIndex ()
		{
			if (focusMarkers == null) {
				focusedIndex = 0;
				return;
			}

			var count = focusMarkers.Count;
			focusedIndex = focusedIndex < 0 ? 0 : focusedIndex >= count ? count - 1 : focusedIndex;
		}

		void UpdateMarkers ()
		{
			focusMarkers = new List<TextLineMarker> ();

			for (int lineNumber = 0; lineNumber <= editor.Document.LineCount; lineNumber++) {
				foreach (var marker in editor.Document.GetMarkersOrderedByInsertion (editor.GetLine (lineNumber))) {
					focusMarkers.Add (marker);
				}
			}
		}

		protected internal override void FocusIn ()
		{
			base.FocusIn ();

			UpdateMarkers ();
			focusedIndex = 0;

			if (focusMarkers.Count == 0) {
				return;
			}

			var marker = focusMarkers [0];
			editor.CenterTo (marker.LineSegment.LineNumber, 1);
		}

		protected internal override void FocusOut ()
		{
			base.FocusOut ();

			focusMarkers = null;
			focusedIndex = 0;

			editor.RedrawMargin (this);
		}

		protected internal override bool HandleItemCommand (ItemCommand command)
		{
			if (focusMarkers.Count == 0) {
				return false;
			}


			if (!(focusMarkers [focusedIndex] is MarginMarker marker)) {
				return false;
			}

			switch (command) {
			case ItemCommand.ActivateCurrentItem:
				var lineNumber = marker.LineSegment.LineNumber;
				var y = editor.LineToY (lineNumber);
				MousePressed (new MarginMouseEventArgs (editor, EventType.ButtonPress, 1, 0, y, ModifierType.None));

				if (focusedIndex >= focusMarkers.Count) {
					focusedIndex = focusMarkers.Count - 1;
				}

				editor.RedrawMargin (this);
				break;

			case ItemCommand.FocusNextItem:
				focusedIndex++;

				SanitizeFocusedIndex ();

				marker = focusMarkers [focusedIndex] as MarginMarker;
				editor.CenterTo (marker.LineSegment.LineNumber, 1);
				break;

			case ItemCommand.FocusPreviousItem:
				focusedIndex--;

				SanitizeFocusedIndex ();

				marker = focusMarkers [focusedIndex] as MarginMarker;
				editor.CenterTo (marker.LineSegment.LineNumber, 1);
				break;
			}

			if (marker != null && markerToAccessible != null) {
				var accessible = markerToAccessible [marker];
				if (accessible != null) {
					AtkCocoaExtensions.SetCurrentFocus (accessible.Accessible);
				}
			}
			return true;
		}

		public EventHandler<BookmarkMarginDrawEventArgs> DrawEvent;
		MarginMarker hoveredSmartTagMarker;
	}
}
