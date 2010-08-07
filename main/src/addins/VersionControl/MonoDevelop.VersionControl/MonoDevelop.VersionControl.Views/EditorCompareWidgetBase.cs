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

namespace MonoDevelop.VersionControl.Views
{
	public abstract class EditorCompareWidgetBase : Gtk.Bin
	{
		protected VersionControlDocumentInfo info;

		Adjustment vAdjustment;
		Adjustment[] attachedVAdjustments;

		Adjustment hAdjustment;
		Adjustment[] attachedHAdjustments;

		Gtk.HScrollbar[] hScrollBars;

		OverviewRenderer overview;
		MiddleArea[] middleAreas;

		protected TextEditor[] editors;
		protected Widget[] headerWidgets;

		protected List<Mono.TextEditor.Utils.Hunk> leftDiff, rightDiff;

		protected abstract TextEditor MainEditor {
			get;
		}

		public EditorCompareWidgetBase (VersionControlDocumentInfo info)
		{
			this.info = info;
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
				editor.Caret.PositionChanged += CaretPositionChanged;
				editor.FocusInEvent += EditorFocusIn;
				editor.SetScrollAdjustments (attachedHAdjustments[i], attachedVAdjustments[i]);
			}

			if (editors.Length == 2) {
				editors[0].ExposeEvent +=  delegate (object sender, ExposeEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, leftDiff, true);
				};

				editors[1].ExposeEvent +=  delegate (object sender, ExposeEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, leftDiff, false);
				};
			} else {
				editors[0].ExposeEvent +=  delegate (object sender, ExposeEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, leftDiff, true);
				};
				editors[1].ExposeEvent +=  delegate (object sender, ExposeEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, leftDiff, false);
					PaintEditorOverlay (myEditor, args, rightDiff, false);
				};
				editors[2].ExposeEvent +=  delegate (object sender, ExposeEventArgs args) {
					var myEditor = (TextEditor)sender;
					PaintEditorOverlay (myEditor, args, rightDiff, true);
				};
			}

			foreach (var widget in headerWidgets) {
				Add (widget);
			}

			overview = new OverviewRenderer (this);
			Add (overview);

			middleAreas = new MiddleArea [editors.Length - 1];
			if (middleAreas.Length <= 0 || middleAreas.Length > 2)
				throw new NotSupportedException ();

			middleAreas[0] = new MiddleArea (this, editors[0], MainEditor, true);
			Add (middleAreas[0]);

			if (middleAreas.Length == 2) {
				middleAreas[1] = new MiddleArea (this, editors[2], MainEditor, false);
				Add (middleAreas[1]);
			}
		}

		protected abstract void CreateComponents ();

		public abstract void UpdateDiff ();
		public abstract void CreateDiff ();

		void Connect (Adjustment fromAdj, Adjustment toAdj)
		{
			fromAdj.Changed += AdjustmentChanged;
			fromAdj.ValueChanged += delegate {
				if (toAdj.Value != fromAdj.Value)
					toAdj.Value = fromAdj.Value;
			};

			toAdj.ValueChanged += delegate {
				if (toAdj.Value != fromAdj.Value)
					fromAdj.Value = toAdj.Value;
			};
		}

		void AdjustmentChanged (object sender, EventArgs e)
		{
			vAdjustment.SetBounds (attachedVAdjustments.Select (adj => adj.Lower).Min (),
				attachedVAdjustments.Select (adj => adj.Upper).Max (),
				attachedVAdjustments[0].StepIncrement,
				attachedVAdjustments[0].PageIncrement,
				attachedVAdjustments[0].PageSize);

			hAdjustment.SetBounds (attachedHAdjustments.Select (adj => adj.Lower).Min (),
				attachedHAdjustments.Select (adj => adj.Upper).Max (),
				attachedHAdjustments[0].StepIncrement,
				attachedHAdjustments[0].PageIncrement,
				attachedHAdjustments[0].PageSize);
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
			if (offset < 0 || offset > caret.TextEditorData.Document.Length)
				return;
			DocumentLocation location = caret.TextEditorData.LogicalToVisualLocation (caret.Location);
			IdeApp.Workbench.StatusBar.ShowCaretState (caret.Line + 1,
			                                           location.Column + 1,
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

		protected EditorCompareWidgetBase (IntPtr ptr) : base (ptr)
		{
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
			children.ForEach (child => child.Child.Destroy ());
		}

		#endregion

		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			const int overviewWidth = 16;
			int vwidth = 1;

			bool hScrollBarVisible = hScrollBars[0].Visible;

			int hheight = hScrollBarVisible ? hScrollBars[0].Requisition.Height : 1;
			int headerSize = 0;

			if (headerWidgets != null)
				headerSize = System.Math.Max (headerWidgets[0].SizeRequest ().Height, 16);

			Rectangle childRectangle = new Rectangle (allocation.X + 1, allocation.Y + headerSize + 1, allocation.Width - vwidth - overviewWidth, allocation.Height - hheight - headerSize);

			overview.SizeAllocate (new Rectangle (allocation.Right - overviewWidth + 1, childRectangle.Y, overviewWidth - 1, childRectangle.Height ));

			const int middleAreaWidth = 42;
			int editorWidth = (childRectangle.Width - middleAreaWidth * (editors.Length - 1)) / editors.Length;

			for (int i = 0; i < editors.Length; i++) {
				Rectangle editorRectangle = new Rectangle (childRectangle.X + (editorWidth + middleAreaWidth) * i  , childRectangle.Top, editorWidth, childRectangle.Height);
				editors[i].SizeAllocate (editorRectangle);

				if (hScrollBarVisible)
					hScrollBars[i].SizeAllocate (new Rectangle (editorRectangle.X, editorRectangle.Bottom, editorRectangle.Width, hheight));

				if (headerWidgets != null)
					headerWidgets[i].SizeAllocate (new Rectangle (editorRectangle.X, allocation.Y + 1, editorRectangle.Width, headerSize));
			}

			for (int i = 0; i < middleAreas.Length; i++) {
				middleAreas[i].SizeAllocate (new Rectangle (childRectangle.X + editorWidth * (i + 1) + middleAreaWidth * i, childRectangle.Top, middleAreaWidth + 1, Allocation.Height - hheight));
			}
		}

		static double GetWheelDelta (Adjustment adjustment, ScrollDirection direction)
		{
			double delta = System.Math.Pow (adjustment.PageSize, 2.0 / 3.0);
			if (direction == ScrollDirection.Up || direction == ScrollDirection.Left)
				delta = -delta;

//			if (scrollbar.Inverted)
//				delta = -delta;
			return delta;
		}

		protected override bool OnScrollEvent (EventScroll evnt)
		{
			var adjustment = (evnt.Direction == ScrollDirection.Up || evnt.Direction == ScrollDirection.Down) ? vAdjustment : hAdjustment;

			if (adjustment.PageSize < adjustment.Upper) {
				double newValue = adjustment.Value + GetWheelDelta (adjustment, evnt.Direction);
				newValue = System.Math.Max (System.Math.Min (adjustment.Upper  - adjustment.PageSize, newValue), adjustment.Lower);
				adjustment.Value = newValue;
			}
			foreach (var middleWidget in middleAreas) {
				middleWidget.QueueDraw ();
			}
			return base.OnScrollEvent (evnt);
		}

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			children.ForEach (child => child.Child.SizeRequest ());
		}

		public static Cairo.Color GetColor (Mono.TextEditor.Utils.Hunk hunk, bool removeSide, double alpha)
		{
			if (removeSide)
				return hunk.Removed > 0 ? new Cairo.Color (0.8, 0.4, 0.4, alpha) : new Cairo.Color (0.4, 0.8, 0.4, alpha);
			return hunk.Inserted == 0 ? new Cairo.Color (0.8, 0.4, 0.4, alpha) : new Cairo.Color (0.4, 0.8, 0.4, alpha);
		}

		const double fillAlpha = 0.1;
		const double lineAlpha = 0.6;

		void PaintEditorOverlay (TextEditor editor, ExposeEventArgs args, List<Mono.TextEditor.Utils.Hunk> diff, bool paintRemoveSide)
		{
			if (diff == null)
				return;
			using (Cairo.Context cr = Gdk.CairoHelper.Create (args.Event.Window)) {
				foreach (var hunk in diff) {
					int y1 = editor.LineToY (paintRemoveSide ? hunk.RemoveStart : hunk.InsertStart) - (int)editor.VAdjustment.Value;
					int y2 = editor.LineToY (paintRemoveSide ? hunk.RemoveStart + hunk.Removed : hunk.InsertStart + hunk.Inserted) - (int)editor.VAdjustment.Value;
					if (y1 == y2)
						y2 = y1 + 1;
					cr.Rectangle (0, y1, editor.Allocation.Width, y2 - y1);
					cr.Color = GetColor (hunk, paintRemoveSide, fillAlpha);
					cr.Fill ();
//					if (hunk.Right.Count > 0 && hunk.Left.Count > 0) {
//						int startOffset = editor.Document.GetLine (hunk.Right.Start).Offset;
//						int rStartOffset = leftEditor.Document.GetLine (hunk.Left.Start).Offset;
//						var lcs = GetLCS (hunk);
//						if (lcs == null) {
//							string leftText = middleEditor.Document.GetTextBetween (startOffset, middleEditor.Document.GetLine (hunk.Right.Start + hunk.Right.Count - 1).EndOffset);
//							string rightText = leftEditor.Document.GetTextBetween (rStartOffset, leftEditor.Document.GetLine (hunk.Left.Start + hunk.Left.Count - 1).EndOffset);
//							llcsCache[hunk] = lcs = GetLCS (leftText, rightText);
//						}
//
//						int ll = lcs.GetLength (0), rl = lcs.GetLength (1);
//						int blockStart = -1;
//						Stack<KeyValuePair<int, int>> p-osStack = new Stack<KeyValuePair<int, int>> ();
//						if (ll > 0 && rl > 0)
//							posStack.Push (new KeyValuePair<int, int> (ll - 1, rl - 1));
//						while (posStack.Count > 0) {
//							var pos = posStack.Pop ();
//							int i = pos.Key, j = pos.Value;
//							if (i > 0 && j > 0 && middleEditor.Document.GetCharAt (startOffset + i) == leftEditor.Document.GetCharAt (rStartOffset + j)) {
//								posStack.Push (new KeyValuePair<int, int> (i - 1, j - 1));
//								PaintBlock (middleEditor, cr, startOffset, i, ref blockStart);
//								continue;
//							}
//
//							if (j > 0 && (i == 0 || lcs[i, j - 1] >= lcs[i - 1, j])) {
//								posStack.Push (new KeyValuePair<int, int> (i, j - 1));
//								PaintBlock (middleEditor, cr, startOffset, i, ref blockStart);
//							} else if (i > 0 && (j == 0 || lcs[i, j - 1] < lcs[i - 1, j])) {
//								posStack.Push (new KeyValuePair<int, int> (i - 1, j));
//								if (blockStart < 0)
//									blockStart = i;
//							}
//						}
//						PaintBlock (editor, cr, startOffset, 0, ref blockStart);
//					}

					cr.Color =  GetColor (hunk, paintRemoveSide, lineAlpha);
					cr.MoveTo (0, y1);
					cr.LineTo (editor.Allocation.Width, y1);
					cr.Stroke ();

					cr.MoveTo (0, y2);
					cr.LineTo (editor.Allocation.Width, y2);
					cr.Stroke ();
				}
			}
		}

		void PaintBlock (TextEditor editor, Cairo.Context cr, int startOffset, int i, ref int blockStart)
		{
			if (blockStart < 0)
				return;
			var point = editor.LocationToPoint (editor.Document.OffsetToLocation (startOffset + i + 1));
			point.X += (int)(editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition - hAdjustment.Value);
			point.Y -= (int)editor.VAdjustment.Value;

			var point2 = editor.LocationToPoint (editor.Document.OffsetToLocation (startOffset + blockStart + 1));
			point2.X += (int)(editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition - hAdjustment.Value);
			point2.Y -= (int)editor.VAdjustment.Value;

			cr.Rectangle (point.X, point.Y, point2.X - point.X, editor.LineHeight);
			cr.Color = editor == MainEditor ? new Cairo.Color (0, 1, 0, 0.2) : new Cairo.Color (1, 0, 0, 0.2);
			cr.Fill ();
			blockStart = -1;
		}

		Dictionary<Mono.TextEditor.Document, TextEditorData> dict = new Dictionary<Mono.TextEditor.Document, TextEditorData> ();

		List<TextEditorData> localUpdate = new List<TextEditorData> ();

		void HandleInfoDocumentTextEditorDataDocumentTextReplaced (object sender, ReplaceEventArgs e)
		{
			foreach (var data in localUpdate.ToArray ()) {
				data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
				data.Replace (e.Offset, e.Count, e.Value);
				data.Document.TextReplaced += HandleDataDocumentTextReplaced;
				data.Document.CommitUpdateAll ();
			}
		}

		public void SetLocal (TextEditorData data)
		{
			if (info == null)
				throw new InvalidOperationException ("Version control info must be set before attaching the merge view to an editor.");
			dict[data.Document] = data;
			data.Document.Text = info.Document.Editor.Document.Text;
			data.Document.ReadOnly = false;
			CreateDiff ();
			data.Document.TextReplaced += HandleDataDocumentTextReplaced;
		}

		void HandleDataDocumentTextReplaced (object sender, ReplaceEventArgs e)
		{
			var data = dict[(Document)sender];
			localUpdate.Remove (data);
			info.Document.Editor.Replace (e.Offset, e.Count, e.Value);
			localUpdate.Add (data);
			UpdateDiff ();
		}

		public void RemoveLocal (TextEditorData data)
		{
			localUpdate.Remove (data);
			data.Document.ReadOnly = true;
			data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
		}

		protected virtual void UndoChange (TextEditor fromEditor, TextEditor toEditor, Hunk hunk)
		{
			toEditor.Document.BeginAtomicUndo ();
			var start = toEditor.Document.GetLine (hunk.InsertStart);
			int toOffset = start != null ? start.Offset : toEditor.Document.Length;
			if (start != null && hunk.Inserted > 0) {
				int line = Math.Min (hunk.InsertStart + hunk.Inserted - 1, toEditor.Document.LineCount - 1);
				var end = toEditor.Document.GetLine (line);
				toEditor.Remove (start.Offset, end.EndOffset - start.Offset);
			}

			if (hunk.Removed > 0) {
				start = fromEditor.Document.GetLine (Math.Min (hunk.RemoveStart, fromEditor.Document.LineCount - 1));
				int line = Math.Min (hunk.RemoveStart + hunk.Removed - 1, fromEditor.Document.LineCount - 1);
				var end = fromEditor.Document.GetLine (line);
				toEditor.Insert (toOffset, start.Offset == end.EndOffset ? toEditor.EolMarker : fromEditor.Document.GetTextBetween (start.Offset, end.EndOffset));
			}

			toEditor.Document.EndAtomicUndo ();
		}

		class MiddleArea : DrawingArea
		{
			EditorCompareWidgetBase widget;
			TextEditor fromEditor, toEditor;
			bool useLeft;

			IEnumerable<Mono.TextEditor.Utils.Hunk> Diff {
				get {
					return useLeft ? widget.leftDiff : widget.rightDiff;
				}
			}

			public MiddleArea (EditorCompareWidgetBase widget, TextEditor fromEditor, TextEditor toEditor, bool useLeft)
			{
				this.widget = widget;
				this.Events |= EventMask.PointerMotionMask | EventMask.ButtonPressMask;
				this.fromEditor = fromEditor;
				this.toEditor = toEditor;
				this.useLeft = useLeft;
			}

			Mono.TextEditor.Utils.Hunk selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
			protected override bool OnMotionNotifyEvent (EventMotion evnt)
			{
				bool hideButton = widget.MainEditor.Document.ReadOnly;
				Mono.TextEditor.Utils.Hunk selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
				if (!hideButton) {
					int delta = widget.MainEditor.Allocation.Y - Allocation.Y;
					foreach (var hunk in Diff) {
						int z1 = delta + fromEditor.LineToY (hunk.RemoveStart) - (int)fromEditor.VAdjustment.Value;
						int z2 = delta + fromEditor.LineToY (hunk.RemoveStart + hunk.Removed) - (int)fromEditor.VAdjustment.Value;
						if (z1 == z2)
							z2 = z1 + 1;

						int y1 = delta + toEditor.LineToY (hunk.InsertStart) - (int)toEditor.VAdjustment.Value;
						int y2 = delta + toEditor.LineToY (hunk.InsertStart + hunk.Inserted) - (int)toEditor.VAdjustment.Value;

						if (y1 == y2)
							y2 = y1 + 1;
						double x, y, w, h;
						GetButtonPosition (hunk, y1, y2, z1, z2, out x, out y, out w, out h);

						if (evnt.X >= x && evnt.X < x + w && evnt.Y >= y && evnt.Y < y + h) {
							selectedHunk = hunk;
							TooltipText = GettextCatalog.GetString ("Revert this change");
							break;
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
				if (!selectedHunk.IsEmpty)
					widget.UndoChange (fromEditor, toEditor, selectedHunk);
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
			double lineWidth;

			public bool GetButtonPosition (Mono.TextEditor.Utils.Hunk hunk, int y1, int y2, int z1, int z2, out double x, out double y, out double w, out double h)
			{
				if (hunk.Removed > 0) {
					int b1 = z1;
					int b2 = z2;
					x = useLeft ? lineWidth : Allocation.Width - buttonSize;
					y = b1;
					w = buttonSize;
					h = b2 - b1;
					return hunk.Inserted > 0;
				} else {
					int b1 = y1;
					int b2 = y2;

					x = useLeft ? Allocation.Width - buttonSize : lineWidth;
					y = b1;
					w = buttonSize - lineWidth;
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
					lineWidth = cr.LineWidth;
					int delta = widget.MainEditor.Allocation.Y - Allocation.Y;
					foreach (Mono.TextEditor.Utils.Hunk hunk in Diff) {
						int z1 = delta + fromEditor.LineToY (hunk.RemoveStart) - (int)fromEditor.VAdjustment.Value;
						int z2 = delta + fromEditor.LineToY (hunk.RemoveStart + hunk.Removed) - (int)fromEditor.VAdjustment.Value;
						if (z1 == z2)
							z2 = z1 + 1;

						int y1 = delta + toEditor.LineToY (hunk.InsertStart) - (int)toEditor.VAdjustment.Value;
						int y2 = delta + toEditor.LineToY (hunk.InsertStart + hunk.Inserted) - (int)toEditor.VAdjustment.Value;

						if (y1 == y2)
							y2 = y1 + 1;

						if (!useLeft) {
							int tmp = z1;
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
						cr.CurveTo ((x2 - x1) / 2, z1,
							(x2 - x1) / 2,  y1,
							x2, y1);

						cr.LineTo (x2, y2);
						cr.CurveTo ((x2 - x1) / 2, y2,
							(x2 - x1) / 2, z2,
							x1, z2);
						cr.ClosePath ();
						cr.Color = GetColor (hunk, this.useLeft, fillAlpha);
						cr.Fill ();

						cr.Color = GetColor (hunk, this.useLeft, lineAlpha);
						cr.MoveTo (x2, y1);
						cr.CurveTo ((x2 - x1) / 2, y1,
							(x2 - x1) / 2,  z1,
							x1, z1);
						cr.Stroke ();

						cr.MoveTo (x1, z2);
						cr.CurveTo ((x2 - x1) / 2, z2,
							(x2 - x1) / 2, y2,
							x2, y2);
						cr.Stroke ();

						if (!hideButton) {
							bool isButtonSelected = hunk == selectedHunk;

							double x, y, w, h;
							bool drawArrow = useLeft ? GetButtonPosition (hunk, y1, y2, z1, z2, out x, out y, out w, out h) :
								GetButtonPosition (hunk, z1, z2, y1, y2, out x, out y, out w, out h);

							cr.Rectangle (x, y, w, h);
							if (isButtonSelected) {
								cr.Color = new Cairo.Color (0.7, 0.7, 0.7, 0.3);
								cr.FillPreserve ();
							}
							cr.Color = new Cairo.Color (0.7, 0.7, 0.7);
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
				var result = base.OnExposeEvent (evnt);

				Gdk.GC gc = Style.DarkGC (State);
				evnt.Window.DrawLine (gc, Allocation.X, Allocation.Top, Allocation.X, Allocation.Bottom);
				evnt.Window.DrawLine (gc, Allocation.Right, Allocation.Top, Allocation.Right, Allocation.Bottom);

				evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Y, Allocation.Right, Allocation.Y);
				evnt.Window.DrawLine (gc, Allocation.Left, Allocation.Bottom, Allocation.Right, Allocation.Bottom);

				return result;
			}
		}

		class OverviewRenderer : DrawingArea
		{
			EditorCompareWidgetBase widget;

			public OverviewRenderer (EditorCompareWidgetBase widget)
			{
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
				if (widget.leftDiff == null)
					return true;
				var adj = widget.vAdjustment;

				using (Cairo.Context cr = Gdk.CairoHelper.Create (e.Window)) {
					cr.LineWidth = 1;

					int count = 0;
					foreach (var h in widget.leftDiff) {
						IncPos(h, ref count);
					}

					int start = 0;
					foreach (var h in widget.leftDiff) {
						int size = 0;
						IncPos(h, ref size);

						cr.Rectangle (0.5, 0.5 + Allocation.Height * start / count, Allocation.Width, Math.Max (1, Allocation.Height * size / count));
						if (h.Removed == 0) {
							cr.Color = new Cairo.Color (0.4, 0.8, 0.4);
						} else if (h.Inserted == 0) {
							cr.Color = new Cairo.Color (0.8, 0.4, 0.4);
						} else {
							cr.Color = new Cairo.Color (0.4, 0.8, 0.8);
						}
						cr.Fill ();
						start += size;
					}

					cr.Rectangle (1,
					              (int)(Allocation.Height * adj.Value / adj.Upper),
					              Allocation.Width - 2,
					              (int)(Allocation.Height * ((double)adj.PageSize / adj.Upper)));
					cr.Color = new Cairo.Color (0, 0, 0, 0.5);
					cr.StrokePreserve ();

					cr.Color = new Cairo.Color (0, 0, 0, 0.03);
					cr.Fill ();
					cr.Rectangle (0.5, 0.5, Allocation.Width - 1, Allocation.Height - 1);
					cr.Color = (Mono.TextEditor.HslColor)Style.Dark (StateType.Normal);
					cr.Stroke ();
				}
				return true;
			}

			void IncPos(Mono.TextEditor.Utils.Hunk h, ref int pos)
			{
				pos += System.Math.Max (h.Inserted, h.Removed);
			}
		}
	}

}

