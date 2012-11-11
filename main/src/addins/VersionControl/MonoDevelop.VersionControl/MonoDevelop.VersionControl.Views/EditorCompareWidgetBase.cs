
//
// EditorCompareWidgetBase.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using Gtk;
using Gdk;
using System.Collections.Generic;
using Mono.TextEditor;
using Mono.TextEditor.Utils;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.ComponentModel;
using MonoDevelop.Core;
using System.Globalization;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.VersionControl.Views
{
	public abstract class EditorCompareWidgetBase : Gtk.Bin
	{
		internal protected VersionControlDocumentInfo info;

		Adjustment vAdjustment;
		Adjustment[] attachedVAdjustments;

		Adjustment hAdjustment;
		Adjustment[] attachedHAdjustments;

		Gtk.HScrollbar[] hScrollBars;

		DiffScrollbar rightDiffScrollBar, leftDiffScrollBar;
		MiddleArea[] middleAreas;

		protected TextEditor[] editors;
		protected Widget[] headerWidgets;

		
		List<Hunk> leftDiff;
		protected List<Hunk> LeftDiff {
			get { return leftDiff; }
			set {
				leftDiff = value;
				OnDiffChanged (EventArgs.Empty);
			}
		}
		
		List<Hunk> rightDiff;
		protected List<Hunk> RightDiff {
			get { return rightDiff; }
			set {
				rightDiff = value;
				OnDiffChanged (EventArgs.Empty);
			}
		}

		static readonly Cairo.Color lightRed = new Cairo.Color (255 / 255.0, 200 / 255.0, 200 / 255.0);
		static readonly Cairo.Color darkRed = new Cairo.Color (178 / 255.0, 140 / 255.0, 140 / 255.0);
		
		static readonly Cairo.Color lightGreen = new Cairo.Color (190 / 255.0, 240 / 255.0, 190 / 255.0);
		static readonly Cairo.Color darkGreen = new Cairo.Color (133 / 255.0, 168 / 255.0, 133 / 255.0);
		
		static readonly Cairo.Color lightBlue = new Cairo.Color (190 / 255.0, 190 / 255.0, 240 / 255.0);
		static readonly Cairo.Color darkBlue = new Cairo.Color (133 / 255.0, 133 / 255.0, 168 / 255.0);
		
		protected abstract TextEditor MainEditor {
			get;
		}
		
		public TextEditor FocusedEditor {
			get {
				foreach (TextEditor editor in editors) {
					
					if (editor.HasFocus)
						return editor;
				}
				return null;
			}
		}
		
		protected bool viewOnly;

		protected EditorCompareWidgetBase (bool viewOnly)
		{
			this.viewOnly = viewOnly;
		}

		public EditorCompareWidgetBase ()
		{
			GtkWorkarounds.FixContainerLeak (this);
			Intialize ();
		}

		protected void Intialize ()
		{
			CreateComponents ();

			vAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			attachedVAdjustments = new Adjustment[editors.Length];
			attachedHAdjustments = new Adjustment[editors.Length];
			for (int i = 0; i < editors.Length; i++) {
				attachedVAdjustments[i] = new Adjustment (0, 0, 0, 0, 0, 0);
				attachedHAdjustments[i] = new Adjustment (0, 0, 0, 0, 0, 0);
			}

			foreach (var attachedAdjustment in attachedVAdjustments) {
				Connect (attachedAdjustment, vAdjustment);
			}

			hAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			foreach (var attachedAdjustment in attachedHAdjustments) {
				Connect (attachedAdjustment, hAdjustment);
			}

			hScrollBars = new Gtk.HScrollbar[attachedHAdjustments.Length];
			for (int i = 0; i < hScrollBars.Length; i++) {
				hScrollBars[i] = new HScrollbar (hAdjustment);
				Add (hScrollBars[i]);
			}

			for (int i = 0; i < editors.Length; i++) {
				var editor = editors[i];
				Add (editor);
				editor.DoPopupMenu += (e) => ShowPopup (editor, e);
				editor.Caret.PositionChanged += CaretPositionChanged;
				editor.FocusInEvent += EditorFocusIn;
				editor.SetScrollAdjustments (attachedHAdjustments[i], attachedVAdjustments[i]);
			}

			if (editors.Length == 2) {
				editors[0].Painted +=  delegate (object sender, PaintEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, LeftDiff, true);
				};

				editors[1].Painted +=  delegate (object sender, PaintEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, LeftDiff, false);
				};

				rightDiffScrollBar = new DiffScrollbar (this, editors[1], true, true);
				Add (rightDiffScrollBar);
			} else {
				editors[0].Painted +=  delegate (object sender, PaintEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, LeftDiff, true);
				};
				editors[1].Painted +=  delegate (object sender, PaintEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, LeftDiff, false);
					PaintEditorOverlay (myEditor, args, RightDiff, false);
				};
				editors[2].Painted +=  delegate (object sender, PaintEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, RightDiff, true);
				};
				rightDiffScrollBar = new DiffScrollbar (this, editors[2], false, false);
				Add (rightDiffScrollBar);
			}
			
			leftDiffScrollBar = new DiffScrollbar (this, editors[0], true, false);
			Add (leftDiffScrollBar);
			if (headerWidgets != null) {
				foreach (var widget in headerWidgets) {
					Add (widget);
				}
			}

			middleAreas = new MiddleArea [editors.Length - 1];
			if (middleAreas.Length <= 0 || middleAreas.Length > 2)
				throw new NotSupportedException ();

			middleAreas[0] = new MiddleArea (this, editors[0], MainEditor, true);
			Add (middleAreas[0]);

			if (middleAreas.Length == 2) {
				middleAreas[1] = new MiddleArea (this, editors[2], MainEditor, false);
				Add (middleAreas[1]);
			}
			this.MainEditor.EditorOptionsChanged += HandleMainEditorhandleEditorOptionsChanged;
		}
		
		void ShowPopup (TextEditor editor, EventButton evt)
		{
			CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/VersionControl/DiffView/ContextMenu");
			Gtk.Menu menu = IdeApp.CommandService.CreateMenu (cset);
			menu.Destroyed += delegate {
				this.QueueDraw ();
			};
			
			if (evt != null) {
				GtkWorkarounds.ShowContextMenu (menu, this, evt);
			} else {
				var pt = editor.LocationToPoint (editor.Caret.Location);
				GtkWorkarounds.ShowContextMenu (menu, editor, new Gdk.Rectangle (pt.X, pt.Y, 1, (int)editor.LineHeight));
			}
		}
		
		void HandleMainEditorhandleEditorOptionsChanged (object sender, EventArgs e)
		{
			ClearDiffCache ();
		}
		
		public string MimeType {
			get {
				return editors[0].Document.MimeType;
			}
			set {
				foreach (var editor in editors) {
					editor.Document.MimeType = value;
				}
			}
		}
		
		public void SetVersionControlInfo (VersionControlDocumentInfo info)
		{
			this.info = info;

			var mimeType = DesktopService.GetMimeTypeForUri (info.Item.Path);
			foreach (var editor in editors) {
				editor.Document.IgnoreFoldings = true;
				editor.Document.MimeType = mimeType;
				editor.Document.ReadOnly = true;

				editor.Options.ShowFoldMargin = false;
				editor.Options.ShowIconMargin = false;
			}
		}
		
		protected abstract void CreateComponents ();
		
		public static ICollection<Cairo.Rectangle> GetDiffRectangles (TextEditor editor, int startOffset, int endOffset)
		{
			ICollection<Cairo.Rectangle> rectangles = new List<Cairo.Rectangle> ();
			var startLine = editor.GetLineByOffset (startOffset);
			var endLine = editor.GetLineByOffset (endOffset);
			int lineCount = endLine.LineNumber - startLine.LineNumber;
			var line = startLine;
			for (int i = 0; i <= lineCount; i++) {
				Cairo.Point point = editor.LocationToPoint (editor.Document.OffsetToLocation (Math.Max (startOffset, line.Offset)), true);
				Cairo.Point point2 = editor.LocationToPoint (editor.Document.OffsetToLocation (Math.Min (line.EndOffset, endOffset)), true);
				rectangles.Add (new Cairo.Rectangle (point.X - editor.TextViewMargin.XOffset, point.Y, point2.X - point.X, editor.LineHeight));
				line = line.NextLine;
			}
			return rectangles;
		}
		
		Dictionary<List<Mono.TextEditor.Utils.Hunk>, Dictionary<Hunk, Tuple<List<Cairo.Rectangle>, List<Cairo.Rectangle>>>> diffCache = new Dictionary<List<Mono.TextEditor.Utils.Hunk>, Dictionary<Hunk, Tuple<List<Cairo.Rectangle>, List<Cairo.Rectangle>>>> ();
		
		protected void ClearDiffCache ()
		{
			diffCache.Clear ();
		}
		
		static List<TextSegment> BreakTextInWords (TextEditor editor, int start, int count)
		{
			return TextBreaker.BreakLinesIntoWords(editor, start, count);
		}
		
		List<Cairo.Rectangle> CalculateChunkPath (TextEditor editor, List<Hunk> diff, List<TextSegment> words, bool useRemove)
		{
			List<Cairo.Rectangle> result = new List<Cairo.Rectangle> ();
			int startOffset = -1;
			int endOffset = -1;
			foreach (var hunk in diff) {
				int start = useRemove ? hunk.RemoveStart : hunk.InsertStart;
				int count = useRemove ? hunk.Removed : hunk.Inserted;
				for (int i = 0; i < count; i++) {
					var word = words[start + i - 1];
					if (endOffset != word.Offset) {
						if (startOffset >= 0)
							result.AddRange (GetDiffRectangles (editor, startOffset, endOffset));
						startOffset = word.Offset;
					}
					endOffset = word.EndOffset;
				}
			}
			if (startOffset >= 0)
				result.AddRange (GetDiffRectangles (editor, startOffset, endOffset));
			return result;
		}
		
		Tuple<List<Cairo.Rectangle>, List<Cairo.Rectangle>> GetDiffPaths (List<Mono.TextEditor.Utils.Hunk> diff, TextEditor editor, Hunk hunk)
		{
			if (!diffCache.ContainsKey (diff))
				diffCache[diff] = new Dictionary<Hunk, Tuple<List<Cairo.Rectangle>, List<Cairo.Rectangle>>> ();
			var pathCache = diffCache[diff];
			
			Tuple<List<Cairo.Rectangle>, List<Cairo.Rectangle>> result;
			if (pathCache.TryGetValue (hunk, out result))
				return result;
			
			var words = BreakTextInWords (editor, hunk.RemoveStart, hunk.Removed);
			var cmpWords = BreakTextInWords (MainEditor, hunk.InsertStart, hunk.Inserted);
			
			var wordDiff = new List<Hunk> (Diff.GetDiff (words.Select (w => editor.GetTextAt (w)).ToArray (),
				cmpWords.Select (w => MainEditor.GetTextAt (w)).ToArray ()));
			
			result = Tuple.Create (CalculateChunkPath (editor, wordDiff, words, true), 
				CalculateChunkPath (MainEditor, wordDiff, cmpWords, false));
			
			pathCache[hunk] = result;
			return result;
		}
		
		public virtual void UpdateDiff ()
		{
			ClearDiffCache ();
		}
		
		public abstract void CreateDiff ();

		void RedrawMiddleAreas ()
		{
			foreach (var middleArea in middleAreas) {
				middleArea.QueueDraw ();
			}
		}
		
		void Connect (Adjustment fromAdj, Adjustment toAdj)
		{
			fromAdj.Changed += AdjustmentChanged;
			fromAdj.ValueChanged += delegate {
				double fromValue = fromAdj.Value / (fromAdj.Upper - fromAdj.Lower);
				if (toAdj.Value != fromValue)
					toAdj.Value = fromValue;
				RedrawMiddleAreas ();
			};

			toAdj.ValueChanged += delegate {
				double toValue = System.Math.Round (toAdj.Value * (fromAdj.Upper - fromAdj.Lower)); 
				if (fromAdj.Value != toValue)
					fromAdj.Value = toValue;
				RedrawMiddleAreas ();
			};
		}

		void AdjustmentChanged (object sender, EventArgs e)
		{
			vAdjustment.SetBounds (0, 1.0,
				attachedVAdjustments.Select (adj => adj.StepIncrement / (adj.Upper - adj.Lower)).Min (),
				attachedVAdjustments.Select (adj => adj.PageIncrement / (adj.Upper - adj.Lower)).Min (),
				attachedVAdjustments.Select (adj => adj.PageSize / (adj.Upper - adj.Lower)).Min ());
			
			hAdjustment.SetBounds (0, 1.0,
				attachedHAdjustments.Select (adj => adj.StepIncrement / (adj.Upper - adj.Lower)).Min (),
				attachedHAdjustments.Select (adj => adj.PageIncrement / (adj.Upper - adj.Lower)).Min (),
				attachedHAdjustments.Select (adj => adj.PageSize / (adj.Upper - adj.Lower)).Min ());
			
		}

		internal static void EditorFocusIn (object sender, FocusInEventArgs args)
		{
			TextEditor editor = (TextEditor)sender;
			UpdateCaretPosition (editor.Caret);
		}

		internal static void CaretPositionChanged (object sender, DocumentLocationEventArgs e)
		{
			Caret caret = (Caret)sender;
			UpdateCaretPosition (caret);
		}

		static void UpdateCaretPosition (Caret caret)
		{
			int offset = caret.Offset;
			if (offset < 0 || offset > caret.TextEditorData.Document.TextLength)
				return;
			DocumentLocation location = caret.TextEditorData.LogicalToVisualLocation (caret.Location);
			IdeApp.Workbench.StatusBar.ShowCaretState (caret.Line,
			                                           location.Column,
			                                           caret.TextEditorData.IsSomethingSelected ? caret.TextEditorData.SelectionRange.Length : 0,
			                                           caret.IsInInsertMode);
		}

		#region Container implementation
		List<ContainerChild> children = new List<ContainerChild> ();
		public override ContainerChild this [Widget w] {
			get {
				return children.FirstOrDefault (c => c.Child == w);
			}
		}

		public override GLib.GType ChildType ()
		{
			return Gtk.Widget.GType;
		}

		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			if (include_internals)
				children.ForEach (child => callback (child.Child));
		}

		protected override void OnAdded (Widget widget)
		{
			widget.Parent = this;
			children.Add (new ContainerChild (this, widget));
			widget.Show ();
		}

		protected override void OnRemoved (Widget widget)
		{
			widget.Unparent ();
			children.RemoveAll (c => c.Child == widget);
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			
			if (vAdjustment != null) {
				vAdjustment.Destroy ();
				hAdjustment.Destroy ();
				foreach (var adj in attachedVAdjustments)
					adj.Destroy ();
				foreach (var adj in attachedHAdjustments)
					adj.Destroy ();
				vAdjustment = null;
			}
			
			children.ForEach (child => child.Child.Destroy ());
		}

		#endregion

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			const int overviewWidth = 16;
			int vwidth = 1;

			bool hScrollBarVisible = hScrollBars[0].Visible;

			int hheight = hScrollBarVisible ? hScrollBars[0].Requisition.Height : 0;
			int headerSize = 0;

			if (headerWidgets != null)
				headerSize = System.Math.Max (headerWidgets[0].SizeRequest ().Height, 16);

			Rectangle childRectangle = new Rectangle (allocation.X + overviewWidth + 1, allocation.Y + headerSize + 1, allocation.Width - vwidth - overviewWidth * 2, allocation.Height - hheight - headerSize - 1);
			
			
			leftDiffScrollBar.SizeAllocate (new Rectangle (allocation.Left, childRectangle.Y, overviewWidth - 1, childRectangle.Height));
			rightDiffScrollBar.SizeAllocate (new Rectangle (allocation.Right - overviewWidth + 1, childRectangle.Y, overviewWidth - 1, childRectangle.Height ));

			const int middleAreaWidth = 42;
			int editorWidth = (childRectangle.Width - middleAreaWidth * (editors.Length - 1)) / editors.Length;

			for (int i = 0; i < editors.Length; i++) {
				Rectangle editorRectangle = new Rectangle (childRectangle.X + (editorWidth + middleAreaWidth) * i  , childRectangle.Top, editorWidth, childRectangle.Height);
				editors[i].SizeAllocate (editorRectangle);

				if (hScrollBarVisible) {
					hScrollBars[i].SizeAllocate (new Rectangle (editorRectangle.X, editorRectangle.Y + editorRectangle.Height, editorRectangle.Width, hheight));
				}

				if (headerWidgets != null)
					headerWidgets[i].SizeAllocate (new Rectangle (editorRectangle.X, allocation.Y + 1, editorRectangle.Width, headerSize));
			}

			for (int i = 0; i < middleAreas.Length; i++) {
				middleAreas[i].SizeAllocate (new Rectangle (childRectangle.X + editorWidth * (i + 1) + middleAreaWidth * i, childRectangle.Top, middleAreaWidth + 1, childRectangle.Height));
			}
			base.OnSizeAllocated (allocation);
		}
		
		// FIXME: if the editors have different adjustment ranges, the pixel deltas
		// don't really feel quite right since they're applied after scaling via the
		// linked adjustment
		protected override bool OnScrollEvent (EventScroll evnt)
		{
			//using the size of an editor for the calculations means pixel deltas apply better
			var alloc = editors[0].Allocation;
			
			double dx, dy;
			evnt.GetPageScrollPixelDeltas (alloc.Width, alloc.Height, out dx, out dy);
			
			if (dx != 0.0 && hAdjustment.PageSize < (hAdjustment.Upper - hAdjustment.Lower))
				hAdjustment.AddValueClamped (dx / (alloc.Width / hAdjustment.PageSize));
			
			if (dy != 0.0 && vAdjustment.PageSize < (vAdjustment.Upper - vAdjustment.Lower))
				vAdjustment.AddValueClamped (dy / (alloc.Height / vAdjustment.PageSize));
			
			return (dx != 0.0 || dy != 0.0) || base.OnScrollEvent (evnt);
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			children.ForEach (child => child.Child.SizeRequest ());
		}

		public static Cairo.Color GetColor (Mono.TextEditor.Utils.Hunk hunk, bool removeSide, bool dark, double alpha)
		{
			Cairo.Color result;
			if (hunk.Removed > 0 && hunk.Inserted > 0) {
				result = dark ? darkBlue : lightBlue;
			} else if (removeSide) {
				if (hunk.Removed > 0) {
					result = dark ? darkRed : lightRed;
				} else {
					result = dark ? darkGreen : lightGreen;
				}
			} else {
				if (hunk.Inserted > 0) {
					result = dark ? darkGreen : lightGreen;
				} else {
					result = dark ? darkRed : lightRed;
				}
			}
			result.A = alpha;
			return result;
		}
		
		void PaintEditorOverlay (TextEditor editor, PaintEventArgs args, List<Mono.TextEditor.Utils.Hunk> diff, bool paintRemoveSide)
		{
			if (diff == null)
				return;
			var cr = args.Context;
			foreach (var hunk in diff) {
				double y1 = editor.LineToY (paintRemoveSide ? hunk.RemoveStart : hunk.InsertStart) - editor.VAdjustment.Value;
				double y2 = editor.LineToY (paintRemoveSide ? hunk.RemoveStart + hunk.Removed : hunk.InsertStart + hunk.Inserted) - editor.VAdjustment.Value;
				if (y1 == y2)
					y2 = y1 + 1;
				cr.Rectangle (0, y1, editor.Allocation.Width, y2 - y1);
				cr.Color = GetColor (hunk, paintRemoveSide, false, 0.15);
				cr.Fill ();
				
				var paths = GetDiffPaths (diff, editors[0], hunk);
				
				cr.Save ();
				cr.Translate (-editor.HAdjustment.Value + editor.TextViewMargin.XOffset, -editor.VAdjustment.Value);
				foreach (var rect in (paintRemoveSide ? paths.Item1 : paths.Item2)) {
					cr.Rectangle (rect);
				}
				
				cr.Color = GetColor (hunk, paintRemoveSide, false, 0.3);
				cr.Fill ();
				cr.Restore ();
				
				cr.Color = GetColor (hunk, paintRemoveSide, true, 0.15);
				cr.MoveTo (0, y1);
				cr.LineTo (editor.Allocation.Width, y1);
				cr.Stroke ();

				cr.MoveTo (0, y2);
				cr.LineTo (editor.Allocation.Width, y2);
				cr.Stroke ();
			}
		}

		Dictionary<Mono.TextEditor.TextDocument, TextEditorData> dict = new Dictionary<Mono.TextEditor.TextDocument, TextEditorData> ();

		List<TextEditorData> localUpdate = new List<TextEditorData> ();

		void HandleInfoDocumentTextEditorDataDocumentTextReplaced (object sender, DocumentChangeEventArgs e)
		{
			foreach (var data in localUpdate.ToArray ()) {
				data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
				data.Replace (e.Offset, e.RemovalLength, e.InsertedText.Text);
				data.Document.TextReplaced += HandleDataDocumentTextReplaced;
				data.Document.CommitUpdateAll ();
			}
		}
		
		public void UpdateLocalText ()
		{
			var text = info.Document.GetContent<ITextFile> ();
			foreach (var data in dict.Values) {
				data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
				data.Document.Text = text.Text;
				data.Document.TextReplaced += HandleDataDocumentTextReplaced;
			}
			CreateDiff ();
		}

		public void SetLocal (TextEditorData data)
		{
			if (info == null)
				throw new InvalidOperationException ("Version control info must be set before attaching the merge view to an editor.");
			dict[data.Document] = data;
			
			var editor = info.Document.GetContent <ITextFile> ();
			if (editor != null)
				data.Document.Text = editor.Text;
			data.Document.ReadOnly = info.Document.GetContent<IEditableTextFile> () == null;
			
			CreateDiff ();
			data.Document.TextReplaced += HandleDataDocumentTextReplaced;
		}

		void HandleDataDocumentTextReplaced (object sender, DocumentChangeEventArgs e)
		{
			var data = dict [(TextDocument)sender];
			localUpdate.Remove (data);
			var editor = info.Document.GetContent<IEditableTextFile> ();
			editor.DeleteText (e.Offset, e.RemovalLength);
			editor.InsertText (e.Offset, e.InsertedText.Text);
			localUpdate.Add (data);
			UpdateDiff ();
		}

		public void RemoveLocal (TextEditorData data)
		{
			localUpdate.Remove (data);
			data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
		}

		protected virtual void UndoChange (TextEditor fromEditor, TextEditor toEditor, Hunk hunk)
		{
			using (var undo = toEditor.OpenUndoGroup ()) {
				var start = toEditor.Document.GetLine (hunk.InsertStart);
				int toOffset = start != null ? start.Offset : toEditor.Document.TextLength;
				if (start != null && hunk.Inserted > 0) {
					int line = Math.Min (hunk.InsertStart + hunk.Inserted - 1, toEditor.Document.LineCount);
					var end = toEditor.Document.GetLine (line);
					toEditor.Remove (start.Offset, end.EndOffsetIncludingDelimiter - start.Offset);
				}
	
				if (hunk.Removed > 0) {
					start = fromEditor.Document.GetLine (Math.Min (hunk.RemoveStart, fromEditor.Document.LineCount));
					int line = Math.Min (hunk.RemoveStart + hunk.Removed - 1, fromEditor.Document.LineCount);
					var end = fromEditor.Document.GetLine (line);
					toEditor.Insert (toOffset, start.Offset == end.EndOffsetIncludingDelimiter ? toEditor.EolMarker : fromEditor.Document.GetTextBetween (start.Offset, end.EndOffsetIncludingDelimiter));
				}
			}
		}

		class MiddleArea : DrawingArea
		{
			EditorCompareWidgetBase widget;
			TextEditor fromEditor, toEditor;
			bool useLeft;

			IEnumerable<Mono.TextEditor.Utils.Hunk> Diff {
				get {
					return useLeft ? widget.LeftDiff : widget.RightDiff;
				}
			}

			public MiddleArea (EditorCompareWidgetBase widget, TextEditor fromEditor, TextEditor toEditor, bool useLeft)
			{
				this.widget = widget;
				this.Events |= EventMask.PointerMotionMask | EventMask.ButtonPressMask;
				this.fromEditor = fromEditor;
				this.toEditor = toEditor;
				this.useLeft = useLeft;
				this.toEditor.EditorOptionsChanged += HandleToEditorhandleEditorOptionsChanged;
			}
			
			protected override void OnDestroyed ()
			{
				this.toEditor.EditorOptionsChanged -= HandleToEditorhandleEditorOptionsChanged;
				base.OnDestroyed ();
			}
			
			void HandleToEditorhandleEditorOptionsChanged (object sender, EventArgs e)
			{
				QueueDraw ();
			}

			Mono.TextEditor.Utils.Hunk selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				bool hideButton = widget.MainEditor.Document.ReadOnly;
				Mono.TextEditor.Utils.Hunk selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
				if (!hideButton) {
					int delta = widget.MainEditor.Allocation.Y - Allocation.Y;
					if (Diff != null) {
						foreach (var hunk in Diff) {
							double z1 = delta + fromEditor.LineToY (hunk.RemoveStart) - fromEditor.VAdjustment.Value;
							double z2 = delta + fromEditor.LineToY (hunk.RemoveStart + hunk.Removed) - fromEditor.VAdjustment.Value;
							if (z1 == z2)
								z2 = z1 + 1;
	
							double y1 = delta + toEditor.LineToY (hunk.InsertStart) - toEditor.VAdjustment.Value;
							double y2 = delta + toEditor.LineToY (hunk.InsertStart + hunk.Inserted) - toEditor.VAdjustment.Value;
	
							if (y1 == y2)
								y2 = y1 + 1;
							double x, y, w, h;
							GetButtonPosition (hunk, y1, y2, z1, z2, out x, out y, out w, out h);
	
							if (evnt.X >= x && evnt.X < x + w && evnt.Y >= y && evnt.Y < y + h) {
								selectedHunk = hunk;
								TooltipText = GettextCatalog.GetString ("Revert this change");
								QueueDrawArea ((int)x, (int)y, (int)w, (int)h);
								break;
							}
						}
					}
				} else {
					selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
				}

				if (selectedHunk.IsEmpty)
					TooltipText = null;

				if (this.selectedHunk != selectedHunk) {
					this.selectedHunk = selectedHunk;
					QueueDraw ();
				}
				return base.OnMotionNotifyEvent (evnt);
			}

			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				if (!evnt.TriggersContextMenu () && evnt.Button == 1 && !selectedHunk.IsEmpty) {
					widget.UndoChange (fromEditor, toEditor, selectedHunk);
					return true;
				}
				return base.OnButtonPressEvent (evnt);
			}

			protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
			{
				selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
				TooltipText = null;
				QueueDraw ();
				return base.OnLeaveNotifyEvent (evnt);
			}

			const int buttonSize = 16;

			public bool GetButtonPosition (Mono.TextEditor.Utils.Hunk hunk, double y1, double y2, double z1, double z2, out double x, out double y, out double w, out double h)
			{
				if (hunk.Removed > 0) {
					var b1 = z1;
					var b2 = z2;
					x = useLeft ? 0 : Allocation.Width - buttonSize;
					y = b1;
					w = buttonSize;
					h = b2 - b1;
					return hunk.Inserted > 0;
				} else {
					var b1 = y1;
					var b2 = y2;

					x = useLeft ? Allocation.Width - buttonSize : 0;
					y = b1;
					w = buttonSize;
					h = b2 - b1;
					return  hunk.Removed > 0;
				}
			}

			void DrawArrow (Cairo.Context cr, double x, double y)
			{
				if (useLeft) {
					cr.MoveTo (x - 2, y - 3);
					cr.LineTo (x + 2, y);
					cr.LineTo (x - 2, y + 3);
				} else {
					cr.MoveTo (x + 2, y - 3);
					cr.LineTo (x - 2, y);
					cr.LineTo (x + 2, y + 3);
				}
			}
			static void DrawCross (Cairo.Context cr, double x, double y)
			{
				cr.MoveTo (x - 2, y - 3);
				cr.LineTo (x + 2, y + 3);
				cr.MoveTo (x + 2, y - 3);
				cr.LineTo (x - 2, y + 3);
			}

			protected override bool OnExposeEvent (EventExpose evnt)
			{
				bool hideButton = widget.MainEditor.Document.ReadOnly;
				using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
					cr.Rectangle (evnt.Region.Clipbox.X, evnt.Region.Clipbox.Y, evnt.Region.Clipbox.Width, evnt.Region.Clipbox.Height);
					cr.Clip ();
					int delta = widget.MainEditor.Allocation.Y - Allocation.Y;
					if (Diff != null) {
						foreach (Mono.TextEditor.Utils.Hunk hunk in Diff) {
							double z1 = delta + fromEditor.LineToY (hunk.RemoveStart) - fromEditor.VAdjustment.Value;
							double z2 = delta + fromEditor.LineToY (hunk.RemoveStart + hunk.Removed) - fromEditor.VAdjustment.Value;
							if (z1 == z2)
								z2 = z1 + 1;
	
							double y1 = delta + toEditor.LineToY (hunk.InsertStart) - toEditor.VAdjustment.Value;
							double y2 = delta + toEditor.LineToY (hunk.InsertStart + hunk.Inserted) - toEditor.VAdjustment.Value;
	
							if (y1 == y2)
								y2 = y1 + 1;
	
							if (!useLeft) {
								var tmp = z1;
								z1 = y1;
								y1 = tmp;
	
								tmp = z2;
								z2 = y2;
								y2 = tmp;
							}
	
							int x1 = 0;
							int x2 = Allocation.Width;
	
							if (!hideButton) {
								if (useLeft && hunk.Removed > 0 || !useLeft && hunk.Removed == 0) {
									x1 += 16;
								} else {
									x2 -= 16;
								}
							}
	
							if (z1 == z2)
								z2 = z1 + 1;
	
							cr.MoveTo (x1, z1);
							
							cr.CurveTo (x1 + (x2 - x1) / 4, z1,
								x1 + (x2 - x1) * 3 / 4, y1,
								x2, y1);
	
							cr.LineTo (x2, y2);
							cr.CurveTo (x1 + (x2 - x1) * 3 / 4, y2,
								x1 + (x2 - x1) / 4, z2,
								x1, z2);
							cr.ClosePath ();
							cr.Color = GetColor (hunk, this.useLeft, false, 1.0);
							cr.Fill ();
	
							cr.Color = GetColor (hunk, this.useLeft, true, 1.0);
							cr.MoveTo (x1, z1);
							cr.CurveTo (x1 + (x2 - x1) / 4, z1,
								x1 + (x2 - x1) * 3 / 4, y1,
								x2, y1);
							cr.Stroke ();
							
							cr.MoveTo (x2, y2);
							cr.CurveTo (x1 + (x2 - x1) * 3 / 4, y2,
								x1 + (x2 - x1) / 4, z2,
								x1, z2);
							cr.Stroke ();
	
							if (!hideButton) {
								bool isButtonSelected = hunk == selectedHunk;
	
								double x, y, w, h;
								bool drawArrow = useLeft ? GetButtonPosition (hunk, y1, y2, z1, z2, out x, out y, out w, out h) :
									GetButtonPosition (hunk, z1, z2, y1, y2, out x, out y, out w, out h);
	
								cr.Rectangle (x, y, w, h);
								if (isButtonSelected) {
									int mx, my;
									GetPointer (out mx, out my);
								//	mx -= (int)x;
								//	my -= (int)y;
									Cairo.RadialGradient gradient = new Cairo.RadialGradient (mx, my, h, 
										mx, my, 2);
									var color = (Mono.TextEditor.HslColor)Style.Mid (StateType.Normal);
									color.L *= 1.05;
									gradient.AddColorStop (0, color);
									color.L *= 1.07;
									gradient.AddColorStop (1, color);
									cr.Pattern = gradient;
								} else {
									cr.Color = (Mono.TextEditor.HslColor)Style.Mid (StateType.Normal);
								}
								cr.FillPreserve ();
								
								cr.Color = (Mono.TextEditor.HslColor)Style.Dark (StateType.Normal);
								cr.Stroke ();
								cr.LineWidth = 1;
								cr.Color = new Cairo.Color (0, 0, 0);
								if (drawArrow) {
									DrawArrow (cr, x + w / 1.5, y + h / 2);
									DrawArrow (cr, x + w / 2.5, y + h / 2);
								} else {
									DrawCross (cr, x + w / 2 , y + (h) / 2);
								}
								cr.Stroke ();
							}
						}
					}
				}
//				var result = base.OnExposeEvent (evnt);
//
//				Gdk.GC gc = Style.DarkGC (State);
//				evnt.Window.DrawLine (gc, Allocation.X, Allocation.Top, Allocation.X, Allocation.Bottom);
//				evnt.Window.DrawLine (gc, Allocation.Right, Allocation.Top, Allocation.Right, Allocation.Bottom);
//
//				evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Y, Allocation.Right, Allocation.Y);
//				evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Bottom, Allocation.Right, Allocation.Bottom);

				return true;
			}
		}

		class DiffScrollbar : DrawingArea
		{
			TextEditor editor;
			EditorCompareWidgetBase widget;
			bool useLeftDiff;
			bool paintInsert;
			
			public DiffScrollbar (EditorCompareWidgetBase widget, TextEditor editor, bool useLeftDiff, bool paintInsert)
			{
				this.editor = editor;
				this.useLeftDiff = useLeftDiff;
				this.paintInsert = paintInsert;
				this.widget = widget;
				widget.vAdjustment.ValueChanged += delegate {
					QueueDraw ();
				};
				WidthRequest = 50;

				Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.ButtonMotionMask;

				Show ();
			}

			public void MouseMove (double y)
			{
				var adj = widget.vAdjustment;
				double position = (y / Allocation.Height) * adj.Upper - (double)adj.PageSize / 2;
				position = Math.Max (0, Math.Min (position, adj.Upper - adj.PageSize));
				widget.vAdjustment.Value = position;
			}

			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				if (button != 0)
					MouseMove (evnt.Y);
				return base.OnMotionNotifyEvent (evnt);
			}

			uint button;

			protected override bool OnButtonPressEvent (EventButton evnt)
			{
				button |= evnt.Button;
				MouseMove (evnt.Y);
				return base.OnButtonPressEvent (evnt);
			}

			protected override bool OnButtonReleaseEvent (EventButton evnt)
			{
				button &= ~evnt.Button;
				return base.OnButtonReleaseEvent (evnt);
			}

			protected override bool OnExposeEvent (Gdk.EventExpose e)
			{
				if (widget.LeftDiff == null)
					return true;
				var adj = widget.vAdjustment;
				
				var diff = useLeftDiff ? widget.LeftDiff : widget.RightDiff;
				
				using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
					cr.LineWidth = 1;
					double curY = 0;
					
					foreach (var hunk in diff) {
						double y, count;
						if (paintInsert) {
							y = hunk.InsertStart / (double)editor.LineCount;
							count = hunk.Inserted / (double)editor.LineCount;
						} else {
							y = hunk.RemoveStart / (double)editor.LineCount;
							count = hunk.Removed / (double)editor.LineCount;
						}
						
						double start  = y *  Allocation.Height;
						FillGradient (cr, 0.5 + curY, start - curY);
						
						curY = start;
						double height = Math.Max (cr.LineWidth, count * Allocation.Height);
						cr.Rectangle (0.5, 0.5 + curY, Allocation.Width, height);
						cr.Color = GetColor (hunk, !paintInsert, false, 1.0);
						cr.Fill ();
						curY += height;
					}
					
					FillGradient (cr, 0.5 + curY, Allocation.Height - curY);
					
					int barPadding = 3;
					var allocH = Allocation.Height;
					var adjUpper = adj.Upper;
					var barY = allocH * adj.Value / adjUpper + barPadding;
					var barH = allocH * (adj.PageSize / adjUpper) - barPadding - barPadding;
					DrawBar (cr, barY, barH);
					
					cr.Rectangle (0.5, 0.5, Allocation.Width - 1, Allocation.Height - 1);
					cr.Color = (Mono.TextEditor.HslColor)Style.Dark (StateType.Normal);
					cr.Stroke ();
				}
				return true;
			}
			
			void FillGradient (Cairo.Context cr, double y, double h)
			{
				cr.Rectangle (0.5, y, Allocation.Width, h);
				var grad = new Cairo.LinearGradient (0, y, Allocation.Width, y);
				var col = (Mono.TextEditor.HslColor)Style.Base (StateType.Normal);
				col.L *= 0.95;
				grad.AddColorStop (0, col);
				grad.AddColorStop (0.7, (Mono.TextEditor.HslColor)Style.Base (StateType.Normal));
				grad.AddColorStop (1, col);
				cr.Pattern = grad;
				
				cr.Fill ();
			}
			
			void DrawBar (Cairo.Context cr, double y, double h)
			{
				int barPadding = 3;
				int barWidth = Allocation.Width - barPadding - barPadding;
				
				MonoDevelop.Components.CairoExtensions.RoundedRectangle (cr, 
					barPadding,
					y,
					barWidth,
					h,
					barWidth / 2);
				
				var color = (Mono.TextEditor.HslColor)Style.Mid (StateType.Normal);
				color.L = 0.5;
				var c = (Cairo.Color)color;
				c.A = 0.6;
				cr.Color = c;
				cr.Fill ();
			}
	
			void IncPos(Mono.TextEditor.Utils.Hunk h, ref int pos)
			{
				pos += System.Math.Max (h.Inserted, h.Removed);
			}
		}
		
		protected virtual void OnDiffChanged (EventArgs e)
		{
			EventHandler handler = this.DiffChanged;
			if (handler != null)
				handler (this, e);
		}
		
		public event EventHandler DiffChanged;
	}

}

