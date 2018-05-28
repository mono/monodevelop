// FoldMarkerMargin.cs
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
using System.Linq;
using Gtk;
using System.Timers;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core;
using Mono.TextEditor.Theatrics;
using MonoDevelop.Ide.Desktop;

namespace Mono.TextEditor
{
	partial class FoldMarkerMargin : Margin
	{
		const int HoverTimer = 100;
		const int FadeInTimer = 200;
		const int FadeOutTimer = 300;

		FoldMarkerMarginDrawer drawer;
		MonoTextEditor editor;
		DocumentLine lineHover;
		Pango.Layout layout;
		Stage<FoldMarkerMargin> animationStage = new Stage<FoldMarkerMargin> (300);

		double marginWidth;
		public override double Width {
			get {
				return marginWidth;
			}
		}
		
		bool isInCodeFocusMode;
		public bool IsInCodeFocusMode {
			get { 
				return isInCodeFocusMode; 
			}
			set {
				isInCodeFocusMode = value; 
				if (!isInCodeFocusMode) {
					RemoveBackgroundRenderer ();
				} else {
					foldings = null;
					HandleEditorCaretPositionChanged (null, null);
				}
			}
		}
		
		public FoldMarkerMargin (MonoTextEditor editor)
		{
			this.editor = editor;
			layout = PangoUtil.CreateLayout (editor);
			editor.Caret.PositionChanged += HandleEditorCaretPositionChanged;
			editor.Document.FoldTreeUpdated += HandleEditorDocumentFoldTreeUpdated;
			editor.Caret.PositionChanged += EditorCarethandlePositionChanged;
			editor.TextArea.MouseHover += TextArea_MouseHover;
			editor.TextArea.MouseLeft += TextArea_MouseLeft;
			if (PlatformService.AccessibilityInUse) {
				drawer = new VSNetFoldMarkerMarginDrawer (this);
			} else {
				drawer = new VSCodeFoldMarkerMarginDrawer (this);
			}
			UpdateAccessibility ();
			animationStage.ActorStep += AnimationStage_ActorStep;
		}

		void TextArea_MouseLeft (object sender, EventArgs e)
		{
			StartFadeOutAnimation ();
		}

		void TextArea_MouseHover (object sender, MarginEventArgs e)
		{
			if (e.Margin == editor.TextViewMargin) {
				StartFadeOutAnimation ();
			} else {
				StartFadeInAnimation ();
			}
		}

		void StartFadeInAnimation ()
		{
			if (!drawer.AutoHide)
				return;
			if (drawer.FoldMarkerOcapitiy >= 1)
				return;
			if (animationStage.Contains (this)) {
				if (fadeIn)
					return;
				animationStage.Exeunt ();
			}
			fadeIn = true;
			animationStage.Add (this, FadeInTimer, drawer.FoldMarkerOcapitiy);
			animationStage.Play ();
		}

		void StartFadeOutAnimation ()
		{
			if (!drawer.AutoHide)
				return;
			if (animationStage.Contains (this)) {
				if (!fadeIn)
					return;
				animationStage.Exeunt ();
			}
			if (drawer.FoldMarkerOcapitiy > 0) {
				fadeIn = false;
				animationStage.Add (this, FadeOutTimer, 1 - drawer.FoldMarkerOcapitiy);
				animationStage.Play ();
			}
		}

		bool fadeIn;
		bool AnimationStage_ActorStep (Actor<FoldMarkerMargin> actor)
		{
			drawer.FoldMarkerOcapitiy = fadeIn ? actor.Percent : 1.0 - actor.Percent;
			editor.RedrawMargin (this);
			return true;
		}


		void EditorCarethandlePositionChanged (object sender, DocumentLocationEventArgs e)
		{
			if (!editor.GetTextEditorData ().HighlightCaretLine || e.Location.Line == editor.Caret.Line)
				return;
			editor.RedrawMarginLine (this, e.Location.Line);
			editor.RedrawMarginLine (this, editor.Caret.Line);
		}

		void HandleEditorDocumentFoldTreeUpdated (object sender, EventArgs e)
		{
			UpdateAccessibility ();
			editor.RedrawMargin (this);
		}

		Dictionary<FoldSegment, FoldingAccessible> accessibles = null;
		void UpdateAccessibility ()
		{
			if (!IdeTheme.AccessibilityEnabled) {
				return;
			}

			if (accessibles == null) {
				accessibles = new Dictionary<FoldSegment, FoldingAccessible> ();
			}
			foreach (var a in accessibles.Values) {
				Accessible.RemoveAccessibleChild (a.Accessible);
				a.Dispose ();
			}
			accessibles.Clear ();

			// Add any folds
			var segments = editor.Document.FoldSegments;
			foreach (var f in segments) {
				var accessible = new FoldingAccessible (f, this, editor);
				accessibles [f] = accessible;

				Accessible.AddAccessibleChild (accessible.Accessible);
			}
		}

		void HandleEditorCaretPositionChanged (object sender, DocumentLocationEventArgs e)
		{
			if (!IsInCodeFocusMode) 
				return;
			DocumentLine lineSegment = editor.Document.GetLine (editor.Caret.Line);
			if (lineSegment == null) {
				RemoveBackgroundRenderer ();
				return;
			}
			
			IEnumerable<FoldSegment> newFoldings = editor.Document.GetFoldingContaining (lineSegment);
			if (newFoldings == null) {
				RemoveBackgroundRenderer ();
				return;
			}
			
			bool areEqual = foldings != null;
			
			if (areEqual && foldings.Count () != newFoldings.Count ())
				areEqual = false;
			if (areEqual) {
				List<FoldSegment> list1 = new List<FoldSegment> (foldings);
				List<FoldSegment> list2 = new List<FoldSegment> (newFoldings);
				for (int i = 0; i < list1.Count; i++) {
					if (list1[i] != list2[i]) {
						areEqual = false;
						break;
					}
				}
			}
			
			if (!areEqual) {
				foldings = newFoldings;
				StopTimer ();
			}
		}
		
		internal protected override void MousePressed (MarginMouseEventArgs args)
		{
			base.MousePressed (args);
			if (args.LineSegment == null || !editor.Options.ShowFoldMargin)
				return;

			foreach (FoldSegment segment in editor.Document.GetStartFoldings (args.LineSegment)) {
				segment.IsCollapsed = !segment.IsCollapsed;
                editor.Document.InformFoldChanged (new FoldSegmentEventArgs (segment));
            }
            editor.SetAdjustments ();
			editor.Caret.MoveCaretBeforeFoldings ();
		}
		
		internal protected override void MouseHover (MarginMouseEventArgs args)
		{
			base.MouseHover (args);
			if (!editor.Options.ShowFoldMargin)
				return;

			DocumentLine lineSegment = null;
			if (args.LineSegment != null) {
				lineSegment = args.LineSegment;
				if (lineHover != lineSegment) {
					lineHover = lineSegment;
					editor.RedrawMargin (this);
				}
			} 
			lineHover = lineSegment;
			bool found = false;
			foreach (FoldSegment segment in editor.Document.GetFoldingContaining (lineSegment)) {
				if (segment.GetStartLine (editor.Document).Offset == lineSegment.Offset) {
					found = true;
					break;
				}
			}
			StopTimer ();
			if (found) {
				var list = new List<FoldSegment>(editor.Document.GetFoldingContaining (lineSegment));
				list.Sort ((x, y) => x.Offset.CompareTo (y.Offset));
				foldings = list;
				if (editor.TextViewMargin.BackgroundRenderer == null) {
					timerId = GLib.Timeout.Add (150, SetBackgroundRenderer);
				} else {
					SetBackgroundRenderer ();
				}
			} else {
				RemoveBackgroundRenderer ();
			}
		}
		List<FoldSegment> oldFolds;
		bool SetBackgroundRenderer ()
		{
			List<FoldSegment> curFolds = new List<FoldSegment> (foldings);
			if (oldFolds != null && oldFolds.Count == curFolds.Count) {
				bool same = true;
				for (int i = 0; i < curFolds.Count; i++) {
					if (oldFolds[i] != curFolds [i]) {
						same = false;
						break;
					}
				}

				if (same) {
					timerId = 0;
					return false;
				}
			}

			oldFolds = curFolds;
			editor.TextViewMargin.DisposeLayoutDict ();
			editor.TextViewMargin.BackgroundRenderer = new FoldingScreenbackgroundRenderer (editor, foldings);
			editor.QueueDraw ();
			timerId = 0;
			return false;
		}
		
		void StopTimer ()
		{
			if (timerId != 0) {
				GLib.Source.Remove (timerId);
				timerId = 0;
			}
		}
		
		uint timerId;
		IEnumerable<FoldSegment> foldings;
		void RemoveBackgroundRenderer ()
		{
			oldFolds = null;
			if (editor.TextViewMargin.BackgroundRenderer != null) {
				editor.TextViewMargin.BackgroundRenderer = null;
				editor.QueueDraw ();
			}
		}
		
		internal protected override void MouseLeft ()
		{
			base.MouseLeft ();
			
			if (lineHover != null) {
				lineHover = null;
				editor.RedrawMargin (this);
			}
			StopTimer ();
			RemoveBackgroundRenderer ();
		}
		
		internal protected override void OptionsChanged ()
		{
			drawer.OptionsChanged ();

			marginWidth = editor.LineHeight  * 3 / 4;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			StopTimer ();
			animationStage.ActorStep -= AnimationStage_ActorStep;
			animationStage.Exeunt ();
			editor.TextArea.MouseHover -= TextArea_MouseHover;
			editor.TextArea.MouseLeft -= TextArea_MouseLeft; 
			editor.Caret.PositionChanged -= HandleEditorCaretPositionChanged;
			editor.Caret.PositionChanged -= EditorCarethandlePositionChanged;
			editor.Document.FoldTreeUpdated -= HandleEditorDocumentFoldTreeUpdated;
			layout = layout.Kill ();
			foldings = null;
			drawer = null;

			if (accessibles != null) {
				foreach (var a in accessibles.Values) {
					Accessible.RemoveAccessibleChild (a.Accessible);
					a.Dispose ();
				}
				accessibles.Clear ();
			}
		}
		

		internal protected override void Draw (Cairo.Context cr, Cairo.Rectangle area, DocumentLine line, int lineNumber, double x, double y, double lineHeight)
		{
			drawer.Draw (cr, area, line, lineNumber, x, y, lineHeight);
		}

		FoldSegment[] focusSegments;
		int focusedIndex;
		protected internal override bool SupportsItemCommands => true;
		protected internal override bool HandleItemCommand(ItemCommand command)
		{
			bool highlightFold = false;

			switch (command) {
			case ItemCommand.ActivateCurrentItem:
				ActivateFold (focusSegments[focusedIndex]);
				break;

			case ItemCommand.FocusNextItem:
				focusedIndex++;
				if (focusedIndex >= focusSegments.Length) {
					focusedIndex = focusSegments.Length - 1;
				}

				highlightFold = true;
				break;

			case ItemCommand.FocusPreviousItem:
				focusedIndex--;
				if (focusedIndex < 0) {
					focusedIndex = 0;
				}

				highlightFold = true;
				break;
			}

			if (highlightFold && focusSegments.Length > 0) {
				var segment = focusSegments[focusedIndex];

				HighlightFold (segment);
			}

			return base.HandleItemCommand(command);
		}

		protected internal override void FocusIn()
		{
			base.FocusIn();

			focusSegments = editor.Document.FoldSegments.ToArray ();
			focusedIndex = 0;

			if (focusSegments.Length > 0) {
				HighlightFold (focusSegments[0]);
			}
			StartFadeInAnimation ();
		}

		protected internal override void FocusOut()
		{
			focusSegments = null;
			focusedIndex = 0;

			editor.TextViewMargin.BackgroundRenderer = null;
			editor.QueueDraw ();
			base.FocusOut();

			editor.RedrawMargin (this);
			StartFadeOutAnimation ();
		}

		void ActivateFold (FoldSegment segment)
		{
			segment.IsCollapsed = !segment.IsCollapsed;
			editor.Document.InformFoldChanged (new FoldSegmentEventArgs (segment));

			editor.SetAdjustments ();
			editor.Caret.MoveCaretBeforeFoldings ();

			// Collapsing the fold causes highlighting to disappear
			HighlightFold (segment);
		}

		void HighlightFold (FoldSegment segment)
		{
			var line = segment.GetStartLine (editor.Document);
			var list = new List<FoldSegment>(editor.Document.GetFoldingContaining (line));
			list.Sort ((x, y) => x.Offset.CompareTo (y.Offset));

			editor.TextViewMargin.DisposeLayoutDict ();
			editor.TextViewMargin.BackgroundRenderer = new FoldingScreenbackgroundRenderer (editor, list);
			editor.ScrollTo (line.LineNumber, 0);

			if (accessibles != null) {
				AtkCocoaExtensions.SetCurrentFocus (accessibles[segment].Accessible);
			}
		}
	}

	class FoldingAccessible : IDisposable
	{
		public AccessibilityElementProxy Accessible { get; private set; }

		FoldSegment segment;
		FoldMarkerMargin margin;
		MonoTextEditor editor;

		double startY;

		public FoldingAccessible (FoldSegment segment, FoldMarkerMargin margin, MonoTextEditor editor)
		{
			Accessible = AccessibilityElementProxy.ButtonElementProxy ();
			Accessible.PerformPress += PerformPress;
			Accessible.GtkParent = margin.Accessible.GtkParent;

			this.segment = segment;
			this.margin = margin;
			this.editor = editor;

			UpdateAccessibility ();
		}

		void UpdateAccessibility ()
		{
			var startLine = segment.GetStartLine (editor.Document).LineNumber;
			var endLine = segment.GetEndLine (editor.Document).LineNumber;

			Accessible.Label = GettextCatalog.GetString ("Fold Region: Line {0} to line {1} - {2}", startLine, endLine,
														 segment.isFolded ? GettextCatalog.GetString ("Folded") : GettextCatalog.GetString ("Expanded"));
			if (segment.isFolded) {
				Accessible.Help = GettextCatalog.GetString ("Activate to expand the region");
			} else {
				Accessible.Help = GettextCatalog.GetString ("Activate to fold the region");
			}

			startY = editor.LineToY (startLine);
			double endY;

			if (segment.isFolded) {
				endY = startY;
			} else {
				endY = editor.LineToY (endLine);
			}

			var rect = new Gdk.Rectangle (0, (int)startY, (int)margin.Width, (int)(endY - startY));
			Accessible.FrameInGtkParent = rect;

			var halfParentHeight = margin.RectInParent.Height / 2;
			var dEndY = endY - halfParentHeight;
			var cocoaEndY = halfParentHeight - dEndY;

			var dStartY = startY - halfParentHeight;
			var cocoaStartY = halfParentHeight - dStartY;

			var minY = Math.Min (cocoaStartY, cocoaEndY);
			var maxY = Math.Max (cocoaStartY, cocoaEndY);

			Accessible.FrameInParent = new Gdk.Rectangle (0, (int)(minY - editor.LineHeight), (int)margin.Width, (int)((maxY - minY) + editor.LineHeight));
		}

		public void Dispose ()
		{
			margin = null;
			editor = null;
			segment = null;

			Accessible.PerformPress -= PerformPress;
			Accessible = null;
		}

		void PerformPress (object sender, EventArgs args)
		{
			var fakeEvent = new MarginMouseEventArgs (editor, Gdk.EventType.ButtonPress, 0, 0, startY, Gdk.ModifierType.None);
			margin.MousePressed (fakeEvent);

			UpdateAccessibility ();
		}
	}
}
