using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Text.Tagging;
using AppKit;
using CoreGraphics;
using MonoDevelop.Debugger;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Debugger
{
	public class BreakpointGlyphMouseProcessor : CocoaMouseProcessorBase
	{
		private readonly ICocoaTextViewHost textViewHost;
		private ITagAggregator<IGlyphTag> glyphTagAggregator;
		private readonly ICocoaTextViewMargin glyphMargin;
		private readonly IViewPrimitives viewPrimitives;
		private IActiveGlyphDropHandler activeGlyphDropHandler;

		// Tooltip
		//private readonly Popup popup;

		// Hover members
		private DispatcherTimer mouseHoverTimer = null;
		private ITextViewLine lastHoverPosition;
		private ITextViewLine currentlyHoveringLine;

		// Drag/drop members
		private CGPoint clickLocation;
		internal bool LastLeftButtonWasDoubleClick = true;  // internal for unit testing
		private bool dragOccurred = false;

		// Constants for scrolling tolerance (borrowed from \platform\text\dragdrop\DragDropMouseProcessor.cs).
		private const double VerticalScrollTolerance = 15;
		private const double HorizontalScrollTolerance = 15;

		// Delay before tooltips display
		private const int ToolTipDelayMilliseconds = 150;

		public BreakpointGlyphMouseProcessor (
			ICocoaTextViewHost wpfTextViewHost,
			ICocoaTextViewMargin margin,
			ITagAggregator<IGlyphTag> tagAggregator,
			IViewPrimitives viewPrimitives)
		{
			this.textViewHost = wpfTextViewHost;
			this.glyphMargin = margin;
			this.glyphTagAggregator = tagAggregator;
			this.viewPrimitives = viewPrimitives;

			// Setup the UI
			//TODO:MAC
			//popup = new Popup();
			//popup.IsOpen = false;
			//popup.Visibility = Visibility.Hidden;

			textViewHost.Closed += delegate {
				glyphTagAggregator.Dispose ();
				glyphTagAggregator = null;
			};
		}

		public override void PreprocessMouseLeftButtonDown (MouseEvent e)
		{
			// Record click location.
			clickLocation = GetMouseLocationInTextView (e);

			// We don't handle double-click, and set the flag so that the mouse
			// up handler doesn't try to handle it as a click either.
			LastLeftButtonWasDoubleClick = e.Event.ClickCount == 2;

			if (LastLeftButtonWasDoubleClick) {
				return;
			}

			// Starts a drag if applicable.
			HandleDragStart (clickLocation);
		}

		public override void PreprocessMouseLeftButtonUp (MouseEvent e)
		{
			// Did we successfully drag a glyph?
			var mouseUpLocation = GetMouseLocationInTextView (e);
			if (HandleDragEnd (mouseUpLocation)) {
				e.Handled = true;
				return;
			}

			// We don't handle double-click
			if (LastLeftButtonWasDoubleClick) {
				return;
			}

			if (GetTextViewLine (mouseUpLocation.Y) != GetTextViewLine (clickLocation.Y)) {
				// If the user (probably accidentally) click-dragged to a different line, don't treat it as a click.
				e.Handled = false;
				return;
			}

			// Treat this as a marker click
			e.Handled = HandleMarkerClick (e);
		}

		public override void PostprocessMouseRightButtonUp (MouseEvent e)
		{
			var mouseUpLocation = GetMouseLocationInTextView (e);
			var textViewLine = GetTextViewLine (mouseUpLocation.Y);
			if (!textViewLine.Extent.Contains (textViewHost.TextView.Caret.Position.BufferPosition))
				textViewHost.TextView.Caret.MoveTo (textViewLine.Start);
			var view = (ViewContent)textViewHost.TextView.Properties [typeof (ViewContent)];
			var ctx = view.WorkbenchWindow?.ExtensionContext ?? Mono.Addins.AddinManager.AddinEngine;
			var cset = IdeApp.CommandService.CreateCommandEntrySet (ctx, "/MonoDevelop/SourceEditor2/IconContextMenu/Editor");
			var pt = ((NSEvent)e.Event).LocationInWindow;
			pt = textViewHost.TextView.VisualElement.ConvertPointFromView (pt, null);
			IdeApp.CommandService.ShowContextMenu (textViewHost.TextView.VisualElement, (int)pt.X, (int)pt.Y, cset, view);
		}

		public override void PostprocessMouseEnter (MouseEvent e)
		{
			EnableToolTips ();
		}

		public override void PostprocessMouseLeave (MouseEvent e)
		{
			DisableToolTips ();
		}

		public override void PreprocessMouseMove (MouseEvent e)
		{
			// If we're dragging a glyph, show proper cursor, scroll the view, etc.
			var pt = GetMouseLocationInTextView (e);
			if (HandleDragOver (pt)) {
				return;
			}

			ITextViewLine textLine = GetTextViewLine (pt.Y);

			// If we're hovering, make sure we're still hovering over the same line.
			if (mouseHoverTimer != null) {
				if (textLine != currentlyHoveringLine) {
					currentlyHoveringLine = null;

					HideTooltip ();
				}

				mouseHoverTimer.Start ();
			}

			// If there's a glyph with a hover mouse cursor, show it.
			if (textLine != null) {
				foreach (IInteractiveGlyph textMarkerGlyphTag in GetTextMarkerGlyphTagsStartingOnLine (textLine)) {
					if (textMarkerGlyphTag.HoverCursor != null) {
						//TODO:MAC
						//Mouse.SetCursor(textMarkerGlyphTag.HoverCursor);
						break;
					}
				}
			}
		}

		#region Private Drag/Drop Helpers

		// Internal for unit testing.
		internal void HandleDragStart (CGPoint viewPoint)
		{
			Debug.Assert (activeGlyphDropHandler == null);
			activeGlyphDropHandler = null;

			// Get the ITextViewLine corresponding to the click point.
			ITextViewLine textLine = GetTextViewLine (viewPoint.Y);
			if (textLine == null) {
				return;
			}

			// Is there a draggable glyph here?
			IActiveGlyphDropHandler draggableTextMarkerGlyphTag = null;
			foreach (IInteractiveGlyph textMarkerGlyphTag in GetTextMarkerGlyphTagsStartingOnLine (textLine)) {
				if (textMarkerGlyphTag.DropHandler != null) {
					draggableTextMarkerGlyphTag = textMarkerGlyphTag.DropHandler;
					break;
				}
			}

			if (draggableTextMarkerGlyphTag == null) {
				return;
			}

			// We have a draggable glyph.  Start dragging it!
			// Store the handler for the glyph being dragged.
			activeGlyphDropHandler = draggableTextMarkerGlyphTag;

			HideTooltip ();

			dragOccurred = false;

			// Capture mouse events so we catch mouse moves over the entire screen and the mouse up event.
			// Note: This may trigger an immediate OnMouseMove event, so make sure we do it last.
			//glyphMargin.VisualElement.CaptureMouse();
		}

		internal bool HandleDragOver (CGPoint viewPoint)
		{
			if (activeGlyphDropHandler == null) {
				return false; // not dragging
			}

			if (!dragOccurred) {
				// if the mouse moved position is less than the system settings for drag start, don't start the
				// drag operation
				Vector dragDelta = new Vector (clickLocation.X - viewPoint.X, clickLocation.Y - viewPoint.Y);
				if (Math.Abs (dragDelta.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs (dragDelta.Y) < SystemParameters.MinimumVerticalDragDistance) {
					return false;
				}

				// Now that the mouse has moved, this is an actual drag.
				dragOccurred = true;
			}

			Tuple<int, int> lineAndColumn = GetLineNumberAndColumn (viewPoint);

			// If this line isn't in the data (surface) buffer, we can't use it
			if (lineAndColumn.Item1 < 0) {
				return false;
			}

			// Query if we can drop here and set mouse cursor appropriately.
			if (activeGlyphDropHandler.CanDrop (lineAndColumn.Item1, lineAndColumn.Item2)) {
				// We can drop here.  Use "hand" cursor.
				//TODO:MAC
				//Mouse.OverrideCursor = Cursors.Hand;
			} else {
				// Can't drop here; change mouse cursor.
				//TODO:MAC
				//Mouse.OverrideCursor = Cursors.No;
			}

			// if the view does not have any focus, grab it
			if (!textViewHost.TextView.HasAggregateFocus) {
				textViewHost.TextView.VisualElement.BecomeFirstResponder ();
			}

			// Position the drag/drop caret and ensure it's visible.
			ITextViewLine textViewLine = GetTextViewLine (viewPoint.Y);
			viewPrimitives.Caret.AdvancedCaret.MoveTo (textViewLine, viewPoint.X);

			this.EnsureCaretVisibleWithPadding ();

			return true;
		}

		private void EnsureCaretVisibleWithPadding ()
		{
			// ensure caret is visible with a little padding to all sides.
			ITextViewLine caretLine = viewPrimitives.Caret.AdvancedCaret.ContainingTextViewLine;
			double padding = Math.Max (0.0, (textViewHost.TextView.ViewportHeight - caretLine.Height) * 0.5);

			// Ensure vertical padding.
			double topSpace = caretLine.Top - (textViewHost.TextView.ViewportTop + VerticalScrollTolerance);
			double bottomSpace = (textViewHost.TextView.ViewportBottom - VerticalScrollTolerance) - caretLine.Bottom;

			if ((topSpace < 0.0) != (bottomSpace < 0.0)) {
				if (topSpace < 0.0) {
					textViewHost.TextView.DisplayTextLineContainingBufferPosition (caretLine.Start, Math.Min (VerticalScrollTolerance, padding), ViewRelativePosition.Top);
				} else {
					textViewHost.TextView.DisplayTextLineContainingBufferPosition (caretLine.Start, Math.Min (VerticalScrollTolerance, padding), ViewRelativePosition.Bottom);
				}
			}

			// Ensure horizontal padding
			double leftSpace = viewPrimitives.Caret.AdvancedCaret.Left - (textViewHost.TextView.ViewportLeft + HorizontalScrollTolerance);
			double rightSpace = (textViewHost.TextView.ViewportRight - HorizontalScrollTolerance) - viewPrimitives.Caret.AdvancedCaret.Right;

			if ((leftSpace < 0.0) != (rightSpace < 0.0)) {
				if (leftSpace < 0.0) {
					textViewHost.TextView.ViewportLeft = viewPrimitives.Caret.AdvancedCaret.Left - Math.Min (HorizontalScrollTolerance, rightSpace);
				} else {
					textViewHost.TextView.ViewportLeft = (viewPrimitives.Caret.AdvancedCaret.Right + Math.Min (HorizontalScrollTolerance, leftSpace)) - textViewHost.TextView.ViewportWidth;
				}
			}
		}

		internal bool HandleDragEnd (CGPoint viewPoint)
		{
			// Clean up our dragging state.
			//glyphMargin.VisualElement.ReleaseMouseCapture();
			//Mouse.OverrideCursor = null;

			IActiveGlyphDropHandler glyphDropHandler = activeGlyphDropHandler;
			activeGlyphDropHandler = null;

			// Did we actually do a drag?
			if (!dragOccurred) {
				return false;
			}

			dragOccurred = false;

			if (glyphDropHandler != null) {
				Tuple<int, int> lineAndColumn = GetLineNumberAndColumn (viewPoint);

				// If this line isn't in the data (surface) buffer, we can't use it
				if (lineAndColumn.Item1 < 0) {
					return false;
				}

				// Query if we can drop here.
				if (glyphDropHandler.CanDrop (lineAndColumn.Item1, lineAndColumn.Item2)) {
					glyphDropHandler.DropAtLocation (lineAndColumn.Item1, lineAndColumn.Item2);
				}
			}

			// Even if we couldn't drop here, we return true to ensure that we don't handle this as a normal click.
			return true;
		}

		private bool HandleMarkerClick (MouseEvent e)
		{
			// Raise MarkerCommandValues.mcvGlyphSingleClickCommand
			if (ExecuteMarkerCommand (glyphMargin.VisualElement.ConvertPointFromView (e.Event.LocationInWindow, null),
				GlyphCommandType.SingleClick)) {
				return true;
			}

			// Also, make sure that this point in the margin maps to a line/column in the correct buffer.  In
			// certain projection scenarios, this span may not be in the data buffer.
			Tuple<int, int> lineCol = GetLineNumberAndColumn (GetMouseLocationInTextView (e));
			if (lineCol.Item1 < 0) {
				return false;
			}

			return ToggleBreakpoint (lineCol.Item1, lineCol.Item2);
		}

		private bool ToggleBreakpoint (int line, int column)
		{
			var buffer = textViewHost.TextView.TextBuffer;
			var path = buffer.GetFilePathOrNull ();
			if (path == null)
				return false;
			DebuggingService.Breakpoints.Toggle (path, line + 1, column + 1);
			return true;
		}

		#endregion

		#region Private Helpers

		private CGPoint GetMouseLocationInTextView (MouseEvent e)
		{
			ICocoaTextView textView = textViewHost.TextView;
			var pt = textView.VisualElement.ConvertPointFromView (e.Event.LocationInWindow, null);
			pt.Y += (nfloat)textView.ViewportTop;
			pt.X += (nfloat)textView.ViewportLeft;

			return pt;
		}

		private ITextViewLine GetTextViewLine (double y)
		{
			ICocoaTextView textView = textViewHost.TextView;

			// Establish line for point
			ITextViewLine textViewLine = textView.TextViewLines.GetTextViewLineContainingYCoordinate (y);
			if (textViewLine == null) {
				textViewLine = y <= textView.TextViewLines [0].Top ?
					textView.TextViewLines.FirstVisibleLine :
					textView.TextViewLines.LastVisibleLine;
			}

			return textViewLine;
		}

		private static SnapshotPoint GetBufferPosition (ITextViewLine textViewLine, double x)
		{
			SnapshotPoint? bufferPosition = textViewLine.GetBufferPositionFromXCoordinate (x);
			if (!bufferPosition.HasValue) {
				bufferPosition = x < textViewLine.TextLeft ? textViewLine.Start : textViewLine.End;
			}

			return bufferPosition.Value;
		}

		private Tuple<int, int> GetLineNumberAndColumn (CGPoint viewPoint)
		{
			int line = 0;
			int col = 0;

			ITextViewLine textViewLine = GetTextViewLine (viewPoint.Y);
			if (textViewLine != null) {
				// The line number should be the line number in the data buffer,
				// which is the text buffer adapter's surface buffer
				var dataSpans = textViewLine.ExtentAsMappingSpan.GetSpans (textViewHost.TextView.TextViewModel.DataBuffer);

				// If this point doesn't map into the data buffer at all, then return line
				// number of -1 to indicate failure
				if (dataSpans.Count == 0) {
					return Tuple.Create (-1, -1);
				}

				line = dataSpans [0].Start.GetContainingLine ().LineNumber;
			}

			// Establish col for point
			int bufferPosition = GetBufferPosition (textViewLine, viewPoint.X);
			col = bufferPosition - textViewLine.Start;

			return Tuple.Create (line, col);
		}

		// Internal for unit testing
		internal bool ExecuteMarkerCommand (CGPoint pt, GlyphCommandType markerCommand)
		{
			bool commandHandled = false;

			pt.Y += (nfloat)textViewHost.TextView.ViewportTop;

			ITextViewLine textLine = textViewHost.TextView.TextViewLines.GetTextViewLineContainingYCoordinate (pt.Y);
			if (textLine == null) {
				return commandHandled;
			}

			// Tags are sorted from low priority to high priority
			IInteractiveGlyph highestPriorityTag = null;
			foreach (IInteractiveGlyph textMarkerTag in GetTextMarkerGlyphTagsStartingOnLine (textLine)) {
				// only consider visible glyph markers.
				if (textMarkerTag.IsEnabled) {
					highestPriorityTag = textMarkerTag;
				}
			}

			if (highestPriorityTag != null && highestPriorityTag.ExecuteCommand (markerCommand)) {
				// Only handle the highest priority command
				commandHandled = true;
			}

			return commandHandled;
		}

		private void EnableToolTips ()
		{
			if (mouseHoverTimer == null) {
				mouseHoverTimer = new DispatcherTimer (
					TimeSpan.FromMilliseconds (ToolTipDelayMilliseconds),
					DispatcherPriority.Normal,
					OnHoverTimer,
					Dispatcher.CurrentDispatcher);
			}

			mouseHoverTimer.Start ();
		}

		private void DisableToolTips ()
		{
			if (mouseHoverTimer != null) {
				mouseHoverTimer.Stop ();
			}

			HideTooltip ();
			lastHoverPosition = null;
		}

		private void OnHoverTimer (object sender, EventArgs e)
		{
			// It's possible the view was closed before our timer triggered, in which case
			// _glyphMargin is disposed and we can't touch it.
			if (textViewHost.IsClosed) {
				return;
			}

			if ((NSEvent.CurrentPressedMouseButtons & 1) > 0) {
				return; // don't show tooltips when button is pressed (e.g. during a drag)
			}
			//TODO:MAC, convert mouse location
			HoverAtPoint (NSEvent.CurrentMouseLocation);
		}

		// internal for exposure to unit tests
		internal void HoverAtPoint (CGPoint pt)
		{
			// The HoverAtPoint calls get dispatched to the UI thread and you can imagine rare scenarios where
			// either multiple calls get queued up (and, possibly, execute after the view has lost focus and
			// the mouse hover has been disabled). Only execute if we still have a running mouse timer.
			if ((mouseHoverTimer != null) && mouseHoverTimer.IsEnabled && glyphMargin.Enabled && !dragOccurred) {
				ITextViewLine textLine = textViewHost.TextView.TextViewLines.GetTextViewLineContainingYCoordinate (pt.Y + textViewHost.TextView.ViewportTop);
				if (textLine != lastHoverPosition) {
					lastHoverPosition = textLine;

					// Get textmarkers
					if (textLine != null) {
						// Tags are returned in lowest to highest priority order
						// Keep track of highest priority tip text
						string tipText = null;
						foreach (IInteractiveGlyph textMarkerGlyphTag in GetTextMarkerGlyphTagsStartingOnLine (textLine)) {
							tipText = textMarkerGlyphTag.TooltipText;
						}
						//TODO:MAC
						//if (!string.IsNullOrEmpty(tipText))
						//{
						//    popup.Child = null;

						//    TextBlock textBlock = new TextBlock();
						//    textBlock.Text = tipText;
						//    textBlock.Name = "GlyphToolTip";

						//    Border border = new Border();
						//    border.Padding = new Thickness(1);
						//    border.BorderThickness = new Thickness(1);
						//    border.Child = textBlock;

						//    // set colors of the tooltip to shell's defined colors
						//    textBlock.SetResourceReference(TextBlock.ForegroundProperty, "VsBrush.ScreenTipText");
						//    border.SetResourceReference(Border.BorderBrushProperty, "VsBrush.ScreenTipBorder");
						//    border.SetResourceReference(Border.BackgroundProperty, "VsBrush.ScreenTipBackground");

						//    popup.Child = border;
						//    popup.Placement = PlacementMode.Relative;
						//    popup.PlacementTarget = glyphMargin.VisualElement;
						//    popup.HorizontalOffset = 0.0;
						//    popup.VerticalOffset = textLine.Bottom - textViewHost.TextView.ViewportTop;
						//    popup.IsOpen = true;
						//    popup.Visibility = Visibility.Visible;

						//    currentlyHoveringLine = textLine;
						//}
					}
				}
			}
		}

		private void HideTooltip ()
		{
			//TODO:MAC
			//popup.Child = null;
			//popup.IsOpen = false;
			//popup.Visibility = Visibility.Hidden;
		}

		// helper method to get the text marker tags starting on a line.
		private IEnumerable<IInteractiveGlyph> GetTextMarkerGlyphTagsStartingOnLine (ITextViewLine textViewLine)
		{
			var visualBuffer = textViewHost.TextView.TextViewModel.VisualBuffer;
			var editBuffer = textViewHost.TextView.TextBuffer;

			// Tags are sorted from low priority to high priority
			foreach (var mappingTagSpan in glyphTagAggregator.GetTags (textViewLine.ExtentAsMappingSpan)) {
				// Only take tag spans with a visible start point and that map to something
				// in the edit buffer, and the markers that *start* on this line

				IInteractiveGlyph textMarkerGlyphTag = mappingTagSpan.Tag as IInteractiveGlyph;

				if (textMarkerGlyphTag == null) {
					continue;
				}

				SnapshotPoint? pointInVisualBuffer = mappingTagSpan.Span.Start.GetPoint (
					visualBuffer,
					PositionAffinity.Predecessor);
				SnapshotPoint? pointInEditBuffer = mappingTagSpan.Span.Start.GetPoint (
					editBuffer,
					PositionAffinity.Predecessor);

				if (!(pointInVisualBuffer.HasValue && pointInEditBuffer.HasValue)) {
					continue;
				}

				if (pointInEditBuffer.Value >= textViewLine.Start &&
					pointInEditBuffer.Value <= textViewLine.End) {
					yield return textMarkerGlyphTag;
				}
			}
		}

		#endregion
	}
}
