// IconMargin.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using Gtk;
using Gdk;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

namespace Mono.TextEditor
{
	class IconMargin : Margin
	{
		MonoTextEditor editor;
		Cairo.Color backgroundColor, separatorColor, focusedBackgroundColor, focusedMarkerColor;
		const int marginWidth = 22;

		public IconMargin (MonoTextEditor editor)
		{
			this.editor = editor;

			if (IdeTheme.AccessibilityEnabled) {
				editor.Document.MarkerAdded += OnMarkerAdded;
				editor.Document.MarkerRemoved += OnMarkerRemoved;
				markerToAccessible = new Dictionary<TextLineMarker, AccessibilityMarkerProxy> ();
			}
		}

		public override void Dispose ()
		{
			if (IdeTheme.AccessibilityEnabled) {
				editor.Document.MarkerAdded -= OnMarkerAdded;
				editor.Document.MarkerRemoved -= OnMarkerRemoved;
			}

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
			backgroundColor = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.IndicatorMargin);
			focusedBackgroundColor = backgroundColor.AddLight (-0.02);
			focusedMarkerColor = backgroundColor.AddLight (-0.3);
			separatorColor = SyntaxHighlightingService.GetColor (editor.EditorTheme, EditorThemeColors.IndicatorMarginSeparator);
		}

		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);

			DocumentLine lineSegment = args.LineSegment;
			if (lineSegment != null) {
				foreach (TextLineMarker marker in editor.Document.GetMarkers (lineSegment)) {
					var marginMarker = marker as MarginMarker;
					if (marginMarker != null)
						marginMarker.InformMousePress (editor, this, args);
				}
			}
		}

		internal protected override void MouseReleased (MarginMouseEventArgs args)
		{
			base.MouseReleased (args);

			DocumentLine lineSegment = args.LineSegment;
			if (lineSegment != null) {
				foreach (TextLineMarker marker in editor.Document.GetMarkers (lineSegment)) {
					var marginMarker = marker as MarginMarker;
					if (marginMarker != null)
						marginMarker.InformMouseRelease (editor, this, args);
				}
			}
		}

		Gdk.Cursor textLinkCursor = new Gdk.Cursor (Gdk.CursorType.Hand1);

		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);
			cursor = textLinkCursor;
			args.Editor.TooltipText = null;
			DocumentLine lineSegment = args.LineSegment;
			if (lineSegment != null) {
				foreach (TextLineMarker marker in editor.Document.GetMarkers (lineSegment)) {
					var marginMarker = marker as MarginMarker;
					if (marginMarker != null)
						marginMarker.InformMouseHover (editor, this, args);
				}
			}
		}

		internal protected override void MouseLeft ()
		{
			if (!string.IsNullOrEmpty (editor.TooltipText))
				editor.TooltipText = null;
			base.MouseLeft ();
		}

		internal protected override void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine line, int lineNumber, double x, double y, double lineHeight)
		{
			bool backgroundIsDrawn = false;
			bool lineMarkerIsSelected = false;
			if (line != null) {
				foreach (var marker in editor.Document.GetMarkersOrderedByInsertion (line)) {
					var marginMarker = marker as MarginMarker;
					if (marginMarker != null) {
						if (marginMarker.CanDrawBackground (this)) {
							backgroundIsDrawn = marginMarker.DrawBackground (editor, cr, new MarginDrawMetrics (this, area, line, lineNumber, x, y, lineHeight));
						}

						if (focusMarkers != null) {
							SanitizeFocusedIndex ();

							lineMarkerIsSelected = (HasFocus && (marginMarker == focusMarkers[focusedIndex]));
						}
					}
				}
			}

			if (!backgroundIsDrawn) {
				cr.Rectangle (x, y, Width, lineHeight);
				if (HasFocus && lineMarkerIsSelected) {
					cr.SetSourceColor (focusedMarkerColor);
				} else {
					cr.SetSourceColor (HasFocus ? focusedBackgroundColor : backgroundColor);
				}
				cr.Fill ();

				cr.MoveTo (x + Width - 0.5, y);
				cr.LineTo (x + Width - 0.5, y + lineHeight);
				cr.SetSourceColor (separatorColor);
				cr.Stroke ();
			}

			if (line != null && lineNumber <= editor.Document.LineCount) {
				foreach (var marker in editor.Document.GetMarkersOrderedByInsertion (line)) {
					var marginMarker = marker as MarginMarker;
					if (marginMarker != null && marginMarker.CanDrawForeground (this)) {
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
				if (DrawEvent != null)
					DrawEvent (this, new BookmarkMarginDrawEventArgs (editor, cr, line, lineNumber, x, y));
			}
		}

		Dictionary<TextLineMarker, AccessibilityMarkerProxy> markerToAccessible;

		void OnMarkerAdded (object sender, TextMarkerEvent e)
		{
			lock (markerToAccessible) {
				var proxy = new AccessibilityMarkerProxy (e.TextMarker, editor, this);
				Accessible.AddAccessibleChild (proxy.Accessible);
				markerToAccessible [e.TextMarker] = proxy;
			}

			if (focusMarkers != null) {
				UpdateMarkers ();
			}
		}

		void OnMarkerRemoved (object sender, TextMarkerEvent e)
		{
			lock (markerToAccessible) {
				if (!markerToAccessible.TryGetValue (e.TextMarker, out var proxy)) {
					return;
				}
				Accessible.RemoveAccessibleChild (proxy.Accessible);
				markerToAccessible.Remove (e.TextMarker);
			}

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

		protected internal override void FocusIn()
		{
			base.FocusIn();

			UpdateMarkers ();
			focusedIndex = 0;

			if (focusMarkers.Count == 0) {
				return;
			}

			var marker = focusMarkers[0];
			editor.CenterTo (marker.LineSegment.LineNumber, 1);
		}

		protected internal override void FocusOut()
		{
			base.FocusOut();

			focusMarkers = null;
			focusedIndex = 0;

			editor.RedrawMargin (this);
		}

		protected internal override bool HandleItemCommand(ItemCommand command)
		{
			if (focusMarkers.Count == 0) {
				return false;
			}

			var marker = focusMarkers[focusedIndex] as MarginMarker;

			if (marker == null) {
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

				marker = focusMarkers[focusedIndex] as MarginMarker;
				editor.CenterTo (marker.LineSegment.LineNumber, 1);
				break;

			case ItemCommand.FocusPreviousItem:
				focusedIndex--;

				SanitizeFocusedIndex ();

				marker = focusMarkers[focusedIndex] as MarginMarker;
				editor.CenterTo (marker.LineSegment.LineNumber, 1);
				break;
			}

			if (marker != null && markerToAccessible != null) {
				var accessible = markerToAccessible[marker];
				if (accessible != null) {
					AtkCocoaExtensions.SetCurrentFocus (accessible.Accessible);
				}
			}
			return true;
		}

		public EventHandler<BookmarkMarginDrawEventArgs> DrawEvent;
	}

	class AccessibilityMarkerProxy : IDisposable
	{
		public AccessibilityElementProxy Accessible { get; private set; }

		TextLineMarker marker;
		MonoTextEditor editor;
		Margin margin;

		MarginDrawMetrics metrics;
		public MarginDrawMetrics Metrics {
			get {
				return metrics;
			}

			set {
				metrics = value;

				Accessible.FrameInGtkParent = new Rectangle ((int)metrics.X, (int)metrics.Y, (int)metrics.Width, (int)metrics.Height);

				var halfParentHeight = margin.RectInParent.Height / 2.0f;
				var dy = metrics.Y - halfParentHeight;
				var cocoaY = halfParentHeight - dy;

				Accessible.FrameInParent = new Rectangle ((int)metrics.X, (int)cocoaY - (int)metrics.Height, (int)metrics.Width, (int)metrics.Height);
			}
		}

		public void Dispose ()
		{
			marker = null;
			editor = null;
			margin = null;

			Accessible.PerformPress -= PerformPress;
			Accessible = null;
		}

		public void UpdateAccessibilityDetails ()
		{
			string label, help;
			var marginMarker = marker as MarginMarker;

			marginMarker.UpdateAccessibilityDetails (out label, out help);
			Accessible.Label = label;
			Accessible.Help = help;
		}

		public AccessibilityMarkerProxy (TextLineMarker marker, MonoTextEditor editor, Margin margin)
		{
			Accessible = AccessibilityElementProxy.ButtonElementProxy ();
			Accessible.PerformPress += PerformPress;
			Accessible.GtkParent = margin.Accessible.GtkParent;

			this.marker = marker;
			this.editor = editor;
			this.margin = margin;
		}

		void PerformPress (object sender, EventArgs args)
		{
			var marginMarker = marker as MarginMarker;

			if (marginMarker != null) {
				var fakeArgs = new MarginMouseEventArgs (editor, EventType.ButtonRelease, 1, metrics.X, metrics.Y, ModifierType.None);
				margin.MousePressed (fakeArgs);
			}
		}
	}

	class BookmarkMarginDrawEventArgs : EventArgs
	{
		public MonoTextEditor Editor {
			get;
			private set;
		}

		public Cairo.Context Context {
			get;
			private set;
		}

		public int Line {
			get;
			private set;
		}

		public double X {
			get;
			private set;
		}

		public double Y {
			get;
			private set;
		}

		public DocumentLine LineSegment {
			get;
			private set;
		}

		public BookmarkMarginDrawEventArgs (MonoTextEditor editor, Cairo.Context context, DocumentLine line, int lineNumber, double xPos, double yPos)
		{
			this.Editor = editor;
			this.Context = context;
			this.LineSegment = line;
			this.Line = lineNumber;
			this.X = xPos;
			this.Y = yPos;
		}
	}

}
