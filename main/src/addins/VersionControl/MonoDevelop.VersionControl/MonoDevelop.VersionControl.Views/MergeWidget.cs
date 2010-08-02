// 
// MergeWidget.cs
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
using MonoDevelop.Components.Diff;
using MonoDevelop.Ide;
using MonoDevelop.Components;
using System.ComponentModel;
using MonoDevelop.Core;

namespace MonoDevelop.VersionControl.Views
{
	public class MergeWidget : Bin
	{
		Adjustment vAdjustment, leftVAdjustment, middleVAdjustment, rightVAdjustment;
		Gtk.VScrollbar vScrollBar;
		
		Adjustment hAdjustment, leftHAdjustment, middleHAdjustment, rightHAdjustment;
		Gtk.HScrollbar leftHScrollBar, middleHScrollBar, rightHScrollBar;
		
		OverviewRenderer overview;
		MiddleArea leftMiddleArea, rightMiddleArea;
		
		TextEditor middleEditor, leftEditor, rightEditor;
		
		List<ContainerChild> children = new List<ContainerChild> ();
		
		public Adjustment Vadjustment {
			get { return this.vAdjustment; }
		}
		
		public Adjustment Hadjustment {
			get { return this.hAdjustment; }
		}
		
		public override ContainerChild this [Widget w] {
			get {
				foreach (ContainerChild info in children.ToArray ()) {
					if (info.Child == w)
						return info;
				}
				return null;
			}
		}
		
		public TextEditor OriginalEditor {
			get {
				return this.middleEditor;
			}
		}

		public TextEditor DiffEditor {
			get {
				return this.leftEditor;
			}
		}
		
		List<Mono.TextEditor.Utils.Hunk> leftDiff, rightDiff;
		
		protected MergeWidget (IntPtr ptr) : base (ptr)
		{
		}
		
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
			vAdjustment.SetBounds (Math.Min (leftVAdjustment.Lower, rightVAdjustment.Lower), 
				Math.Max (leftVAdjustment.Upper, rightVAdjustment.Upper),
				leftVAdjustment.StepIncrement,
				leftVAdjustment.PageIncrement,
				leftVAdjustment.PageSize);
			hAdjustment.SetBounds (Math.Min (leftHAdjustment.Lower, rightHAdjustment.Lower), 
				Math.Max (leftHAdjustment.Upper, rightHAdjustment.Upper),
				leftHAdjustment.StepIncrement,
				leftHAdjustment.PageIncrement,
				leftHAdjustment.PageSize);
		}

		public MergeWidget (string content)
		{
			vAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			vAdjustment.Changed += HandleAdjustmentChanged;
			leftVAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			Connect (leftVAdjustment, vAdjustment);
			
			rightVAdjustment =  new Adjustment (0, 0, 0, 0, 0, 0);
			Connect (rightVAdjustment, vAdjustment);
			
			middleVAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			Connect (middleVAdjustment, vAdjustment);
			
			vScrollBar = new VScrollbar (vAdjustment);
			AddChild (vScrollBar);
			vScrollBar.Hide ();
			
			hAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			hAdjustment.Changed += HandleAdjustmentChanged;
			leftHAdjustment = new Adjustment (0, 0, 0, 0, 0, 0);
			Connect (leftHAdjustment, hAdjustment);
			
			rightHAdjustment =  new Adjustment (0, 0, 0, 0, 0, 0);
			Connect (rightHAdjustment, hAdjustment);
			
			middleHAdjustment =  new Adjustment (0, 0, 0, 0, 0, 0);
			Connect (middleHAdjustment, hAdjustment);
			
			leftHScrollBar = new HScrollbar (hAdjustment);
			AddChild (leftHScrollBar);
			
			rightHScrollBar = new HScrollbar (hAdjustment);
			AddChild (rightHScrollBar);
			
			middleHScrollBar = new HScrollbar (hAdjustment);
			AddChild (middleHScrollBar);
			
			middleEditor = new TextEditor ();
			middleEditor.Caret.PositionChanged += CaretPositionChanged;
			middleEditor.FocusInEvent += EditorFocusIn;
			AddChild (middleEditor);
			middleEditor.SetScrollAdjustments (middleHAdjustment, middleVAdjustment);
			
			leftEditor = new TextEditor ();
			leftEditor.Caret.PositionChanged += CaretPositionChanged;
			leftEditor.FocusInEvent += EditorFocusIn;
			AddChild (leftEditor);
			leftEditor.Document.ReadOnly = true;
			leftEditor.SetScrollAdjustments (leftHAdjustment, leftVAdjustment);
			this.vAdjustment.ValueChanged += delegate {
				leftMiddleArea.QueueDraw ();
			};

			rightEditor = new TextEditor ();
			rightEditor.Caret.PositionChanged += CaretPositionChanged;
			rightEditor.FocusInEvent += EditorFocusIn;
			AddChild (rightEditor);
			rightEditor.Document.ReadOnly = true;
			rightEditor.SetScrollAdjustments (rightHAdjustment, rightVAdjustment);
			this.vAdjustment.ValueChanged += delegate {
				rightMiddleArea.QueueDraw ();
			};
			
			overview = new OverviewRenderer (this);
			AddChild (overview);
			
			leftMiddleArea = new MiddleArea (this, leftEditor, middleEditor, true);
			AddChild (leftMiddleArea);
			
			rightMiddleArea = new MiddleArea (this, rightEditor, middleEditor, false);
			AddChild (rightMiddleArea);
		
			this.DoubleBuffered = true;
			middleEditor.ExposeEvent += HandleMiddleEditorExposeEvent;
			leftEditor.ExposeEvent += HandleLeftEditorExposeEvent;
			rightEditor.ExposeEvent += HandleRightEditorExposeEvent;
			
			this.middleEditor.Document.Text = content;
			
			this.CreateDiff ();
			this.middleEditor.Document.TextReplaced += delegate {
				this.UpdateDiff ();
			};
		}
		
		// todo: move to version control backend
		IEnumerable<Conflict> Conflicts (Mono.TextEditor.Document doc)
		{
			foreach (int mergeStart in doc.SearchForward ("<<<<<<<", 0)) {
				LineSegment start = doc.GetLineByOffset (mergeStart);
				if (start.Offset != mergeStart)
					continue;
				int dividerOffset = doc.SearchForward ("=======", mergeStart).First ();
				LineSegment divider = doc.GetLineByOffset (dividerOffset);
				
				int endOffset      = doc.SearchForward (">>>>>>>", dividerOffset).First (); 
				LineSegment end = doc.GetLineByOffset (endOffset);
				
				yield return new Conflict (new Mono.TextEditor.Segment (start.EndOffset, divider.Offset - start.EndOffset),
					new Mono.TextEditor.Segment (divider.EndOffset, end.Offset - divider.EndOffset), start.Offset, end.EndOffset);
			}
		}

		class Conflict
		{
			public readonly Mono.TextEditor.Segment MySegment;
			public readonly Mono.TextEditor.Segment TheirSegment;
			public readonly int StartOffset;
			public readonly int EndOffset;
			
			public Conflict (Mono.TextEditor.Segment mySegment, Mono.TextEditor.Segment theirSegment, int startOffset, int endOffset)
			{
				this.MySegment = mySegment;
				this.TheirSegment = theirSegment;
				this.StartOffset = startOffset;
				this.EndOffset = endOffset;
			}
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

		List<Conflict> currentConflicts = new List<Conflict> ();
		Dictionary<Conflict, Mono.TextEditor.Utils.Hunk> leftConflicts = new Dictionary<Conflict, Mono.TextEditor.Utils.Hunk> ();
		Dictionary<Conflict, Mono.TextEditor.Utils.Hunk> rightConflicts = new Dictionary<Conflict, Mono.TextEditor.Utils.Hunk> ();

		public void UpdateDiff ()
		{
			var conflicts = new List<Conflict> (Conflicts (middleEditor.Document));
			
			leftDiff  = new List<Mono.TextEditor.Utils.Hunk> (middleEditor.Document.Diff (leftEditor.Document));
			rightDiff = new List<Mono.TextEditor.Utils.Hunk> (middleEditor.Document.Diff (rightEditor.Document));
			
			LineSegment line;
			leftDiff.RemoveAll (item => null != (line = middleEditor.Document.GetLine (item.InsertStart)) && 
				conflicts.Any (c => c.StartOffset <= line.Offset && line.Offset < c.EndOffset));
			rightDiff.RemoveAll (item => null != (line = middleEditor.Document.GetLine (item.InsertStart)) && 
				conflicts.Any (c => c.StartOffset <= line.Offset && line.Offset < c.EndOffset));
			
			int j = 0;
			for (int i = 0; i < currentConflicts.Count && j < conflicts.Count;) {
				var curConflict = currentConflicts[i];
				var newConflict = conflicts[j];
				
				if (curConflict.EndOffset - curConflict.StartOffset == newConflict.EndOffset - newConflict.StartOffset) {
					Console.WriteLine ("Found conflict !!");
					var left = leftConflicts[curConflict];
					var right = rightConflicts[curConflict];
					
					int middleA = middleEditor.Document.OffsetToLineNumber (newConflict.StartOffset);
					int middleB = middleEditor.Document.OffsetToLineNumber (newConflict.EndOffset);
				
					leftDiff.Add (new Mono.TextEditor.Utils.Hunk (middleA, left.RemoveStart, middleB - middleA, left.Inserted));
					rightDiff.Add (new Mono.TextEditor.Utils.Hunk (middleA, right.RemoveStart, middleB - middleA, right.Inserted));
					i++;j++;
				} else {
					j++;
				}
			}
			QueueDraw ();
		}
		
		public void CreateDiff ()
		{
			int curOffset = 0;
			
			var conflicts = new List<Conflict> (Conflicts (middleEditor.Document));
			currentConflicts = conflicts;
			leftConflicts.Clear ();
			rightConflicts.Clear ();
			
			leftEditor.Document.Text = "";
			rightEditor.Document.Text = "";
			foreach (Conflict conflict in currentConflicts) {
				string above = middleEditor.Document.GetTextBetween (curOffset, conflict.StartOffset);
				leftEditor.Insert (leftEditor.Document.Length, above);
				int leftA = leftEditor.Document.LineCount - 1;
				leftEditor.Insert (leftEditor.Document.Length, middleEditor.Document.GetTextAt (conflict.MySegment));
				int leftB = leftEditor.Document.LineCount - 1;
				
				rightEditor.Insert (rightEditor.Document.Length, above);
				int rightA = rightEditor.Document.LineCount - 1;
				rightEditor.Insert (rightEditor.Document.Length, middleEditor.Document.GetTextAt (conflict.TheirSegment));
				int rightB = rightEditor.Document.LineCount - 1;
				
				int middleA = middleEditor.Document.OffsetToLineNumber (conflict.StartOffset);
				int middleB = middleEditor.Document.OffsetToLineNumber (conflict.EndOffset);
				
				leftConflicts[conflict] = new Mono.TextEditor.Utils.Hunk (middleA, leftA, middleB - middleA, leftB - leftA);
				rightConflicts[conflict] = new Mono.TextEditor.Utils.Hunk (middleA, rightA, middleB - middleA, rightB - rightA);
				
				curOffset = conflict.EndOffset;
			}
			
			string lastPart = middleEditor.Document.GetTextBetween (curOffset, middleEditor.Document.Length);
			leftEditor.Insert (leftEditor.Document.Length, lastPart);
			rightEditor.Insert (rightEditor.Document.Length, lastPart);

			leftDiff  = new List<Mono.TextEditor.Utils.Hunk> (middleEditor.Document.Diff (leftEditor.Document));
			rightDiff = new List<Mono.TextEditor.Utils.Hunk> (middleEditor.Document.Diff (rightEditor.Document));

			/*
			foreach (var item in leftDiff) {
				Console.WriteLine ("@@ -" + item.RemoveStart + "," + item.Removed + " +" + item.InsertStart + "," + item.Inserted + " @@");
				for (int i = item.RemoveStart; i < item.RemoveStart + item.Removed; i++) {
					Console.Write ("-" + middleEditor.GetTextEditorData ().GetLineText (i));
				}
				for (int i = item.InsertStart; i < item.InsertStart + item.Inserted; i++) {
					Console.Write ("+" + leftEditor.GetTextEditorData ().GetLineText (i));
				}
			}*/
			
			LineSegment line;
			leftDiff.RemoveAll (item => null != (line = middleEditor.Document.GetLine (item.InsertStart)) && 
				currentConflicts.Any (c => c.StartOffset <= line.Offset && line.Offset < c.EndOffset));
			rightDiff.RemoveAll (item => null != (line = middleEditor.Document.GetLine (item.InsertStart)) && 
				currentConflicts.Any (c => c.StartOffset <= line.Offset && line.Offset < c.EndOffset));
			
			leftDiff.AddRange (leftConflicts.Values);
			rightDiff.AddRange (rightConflicts.Values);
		}
		
		void HandleAdjustmentChanged (object sender, EventArgs e)
		{
			Adjustment adjustment = (Adjustment)sender;
			Scrollbar scrollbar = adjustment == vAdjustment ? (Scrollbar)vScrollBar : leftHScrollBar;
			bool newVisible = adjustment.Upper - adjustment.Lower > adjustment.PageSize;
			if (scrollbar.Visible != newVisible) {
				scrollbar.Visible = newVisible;
				QueueResize ();
			}
		}
		
		public override GLib.GType ChildType ()
		{
			return Gtk.Widget.GType;
		}
		
		protected override void ForAll (bool include_internals, Gtk.Callback callback)
		{
			base.ForAll (include_internals, callback);
			
			if (include_internals)
				children.ForEach (child => callback (child.Child));
		}
		
		public void AddChild (Gtk.Widget child)
		{
			child.Parent = this;
			children.Add (new ContainerChild (this, child));
			child.Show ();
		}
		
		protected override void OnAdded (Widget widget)
		{
			base.OnAdded (widget);
			if (widget == Child)
				widget.SetScrollAdjustments (hAdjustment, vAdjustment);
		}
		
		protected override void OnRemoved (Widget widget)
		{
			widget.Unparent ();
			foreach (var info in children.ToArray ()) {
				if (info.Child == widget) {
					children.Remove (info);
					break;
				}
			}
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			children.ForEach (child => child.Child.Destroy ());
			children.Clear ();
		}
		 
		protected override void OnSizeAllocated (Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			
			int overviewWidth = 16;
			int vwidth = 1; 
			int hheight = leftHScrollBar.Visible ? leftHScrollBar.Requisition.Height : 1; 
			Rectangle childRectangle = new Rectangle (allocation.X + 1, allocation.Y + 1, allocation.Width - vwidth - overviewWidth, allocation.Height - hheight);
			overview.SizeAllocate (new Rectangle (allocation.Right - overviewWidth + 1, childRectangle.Y, overviewWidth - 1, childRectangle.Height ));
			
			int spacerWidth = 34;
			int editorWidth = (childRectangle.Width - spacerWidth * 2) / 3;
			
			Rectangle leftEditorRect = new Rectangle (childRectangle.X, childRectangle.Top, editorWidth, Allocation.Height - hheight);
			leftEditor.SizeAllocate (leftEditorRect);
			
			Rectangle middleEditorRect = new Rectangle (leftEditorRect.Right + spacerWidth, childRectangle.Top, editorWidth, Allocation.Height - hheight);
			middleEditor.SizeAllocate (middleEditorRect);
			
			Rectangle rightEditorRect = new Rectangle (middleEditorRect.Right + spacerWidth, childRectangle.Top, editorWidth, Allocation.Height - hheight);
			rightEditor.SizeAllocate (rightEditorRect);
			
			leftMiddleArea.SizeAllocate (new Rectangle (leftEditorRect.Right, childRectangle.Top, spacerWidth + 1, childRectangle.Height));
			
			rightMiddleArea.SizeAllocate (new Rectangle (middleEditorRect.Right, childRectangle.Top, spacerWidth + 1, childRectangle.Height));
			
			if (leftHScrollBar.Visible) {
				leftHScrollBar.SizeAllocate (new Rectangle (leftEditorRect.X, childRectangle.Bottom, editorWidth, hheight));
				middleHScrollBar.SizeAllocate (new Rectangle (middleEditorRect.X, childRectangle.Bottom, editorWidth, hheight));
				rightHScrollBar.SizeAllocate (new Rectangle (rightEditorRect.X, childRectangle.Bottom, editorWidth, hheight));
				leftHScrollBar.Value = middleHAdjustment.Value = rightHScrollBar.Value = System.Math.Max (System.Math.Min (hAdjustment.Upper - hAdjustment.PageSize, leftHScrollBar.Value), hAdjustment.Lower);
			}
		}
		
		static double GetWheelDelta (Scrollbar scrollbar, ScrollDirection direction)
		{
			double delta = System.Math.Pow (scrollbar.Adjustment.PageSize, 2.0 / 3.0);
			if (direction == ScrollDirection.Up || direction == ScrollDirection.Left)
				delta = -delta;
			if (scrollbar.Inverted)
				delta = -delta;
			return delta;
		}
		
		protected override bool OnScrollEvent (EventScroll evnt)
		{
			Scrollbar scrollWidget = (evnt.Direction == ScrollDirection.Up || evnt.Direction == ScrollDirection.Down) ? (Scrollbar)vScrollBar : leftHScrollBar;
			
			if (scrollWidget.Visible) {
				double newValue = scrollWidget.Adjustment.Value + GetWheelDelta (scrollWidget, evnt.Direction);
				newValue = System.Math.Max (System.Math.Min (scrollWidget.Adjustment.Upper  - scrollWidget.Adjustment.PageSize, newValue), scrollWidget.Adjustment.Lower);
				scrollWidget.Adjustment.Value = newValue;
			}
			return base.OnScrollEvent (evnt);
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			children.ForEach (child => child.Child.SizeRequest ());
		}

		public static Cairo.Color GetColor (Mono.TextEditor.Utils.Hunk item, double alpha)
		{
			if (item.Inserted == 0)
				return new Cairo.Color (0.4, 0.8, 0.4, alpha);
			if (item.Removed == 0) 
				return new Cairo.Color (0.8, 0.4, 0.4, alpha);
			return new Cairo.Color (0.4, 0.8, 0.8, alpha);
		}

		const double fillAlpha = 0.1;
		const double lineAlpha = 0.6;
		
		void PaintEditorOverlay (TextEditor editor, ExposeEventArgs args, List<Mono.TextEditor.Utils.Hunk> diff, bool takeLeftSide)
		{
			if (diff == null)
				return;
			
			using (Cairo.Context cr = Gdk.CairoHelper.Create (args.Event.Window)) {
				foreach (var hunk in diff) {
					int y1 = editor.LineToVisualY (takeLeftSide ? hunk.InsertStart : hunk.RemoveStart) - (int)editor.VAdjustment.Value;
					int y2 = editor.LineToVisualY (takeLeftSide ? hunk.InsertStart + hunk.Removed : hunk.RemoveStart + hunk.Inserted) - (int)editor.VAdjustment.Value;
					if (y1 == y2)
						y2 = y1 + 1;
					cr.Rectangle (0, y1, editor.Allocation.Width, y2 - y1);
					cr.Color = GetColor (hunk, fillAlpha);
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
					
					cr.Color = GetColor (hunk, lineAlpha);
					cr.MoveTo (0, y1);
					cr.LineTo (editor.Allocation.Width, y1);
					cr.Stroke ();
					
					cr.MoveTo (0, y2);
					cr.LineTo (editor.Allocation.Width, y2);
					cr.Stroke ();
				}
			}
		}

		void HandleLeftEditorExposeEvent (object o, ExposeEventArgs args)
		{
			PaintEditorOverlay (leftEditor, args, leftDiff, false);
		}

		void HandleMiddleEditorExposeEvent (object o, ExposeEventArgs args)
		{
			PaintEditorOverlay (middleEditor, args, leftDiff, true);
		}

		void HandleRightEditorExposeEvent (object o, ExposeEventArgs args)
		{
			PaintEditorOverlay (rightEditor, args, rightDiff, false);
		}

		void PaintBlock (TextEditor editor, Cairo.Context cr, int startOffset, int i, ref int blockStart)
		{
			if (blockStart < 0)
				return;
			var point = editor.DocumentToVisualLocation (editor.Document.OffsetToLocation (startOffset + i + 1));
			point.X += editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition - (int)middleEditor.HAdjustment.Value;
			point.Y -= (int)editor.VAdjustment.Value;
			
			var point2 = editor.DocumentToVisualLocation (editor.Document.OffsetToLocation (startOffset + blockStart + 1));
			point2.X += editor.TextViewMargin.XOffset + editor.TextViewMargin.TextStartPosition - (int)middleEditor.HAdjustment.Value;
			point2.Y -= (int)editor.VAdjustment.Value;
			
			cr.Rectangle (point.X, point.Y, point2.X - point.X, editor.LineHeight);
			cr.Color = editor == middleEditor ? new Cairo.Color (0, 1, 0, 0.2) : new Cairo.Color (1, 0, 0, 0.2);
			cr.Fill ();
			blockStart = -1;
		}

		class MiddleArea : DrawingArea 
		{
			MergeWidget widget;
			TextEditor fromEditor, toEditor;
			bool useLeft;
			
			public MiddleArea (MergeWidget widget, TextEditor fromEditor, TextEditor toEditor, bool useLeft)
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
				bool hideButton = widget.OriginalEditor.Document.ReadOnly || !widget.DiffEditor.Document.ReadOnly;
				Mono.TextEditor.Utils.Hunk selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
				if (!hideButton) {
					int delta = widget.OriginalEditor.Allocation.Y - Allocation.Y;
					foreach (var hunk in widget.leftDiff) {
						int y1;
						int y2;
						int x1;
						int x2;
						
						if (hunk.Removed > 0) {
							x1 = 0; 
							x2 = 16;
							y1 = delta + toEditor.LineToVisualY (useLeft ? hunk.RemoveStart : hunk.InsertStart) - (int)toEditor.VAdjustment.Value;
							y2 = delta + toEditor.LineToVisualY (useLeft ? hunk.RemoveStart + hunk.Inserted : hunk.InsertStart + hunk.Removed) - (int)toEditor.VAdjustment.Value;
						} else {
							x1 = Allocation.Width - 16; 
							x2 = Allocation.Width;
							y1 = delta + fromEditor.LineToVisualY (useLeft ? hunk.InsertStart : hunk.RemoveStart) - (int)fromEditor.VAdjustment.Value;
							y2 = delta + fromEditor.LineToVisualY (useLeft ? hunk.InsertStart + hunk.Removed : hunk.RemoveStart + hunk.Inserted) - (int)fromEditor.VAdjustment.Value;
						}
						if (evnt.X >= x1 && evnt.X < x2 && evnt.Y >= y1 && evnt.Y < y2) {
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
				if (!selectedHunk.IsEmpty) {
					Console.WriteLine (selectedHunk);
					toEditor.Document.BeginAtomicUndo ();
					
					var start = toEditor.Document.GetLine (selectedHunk.RemoveStart);
					int toOffset = start.Offset;
					if (selectedHunk.Removed > 0) {
						var end = toEditor.Document.GetLine (selectedHunk.RemoveStart + selectedHunk.Removed - 1);
						toEditor.Remove (start.Offset, end.EndOffset - start.Offset);
					}
				
					if (selectedHunk.Inserted > 0) {
						start = fromEditor.Document.GetLine (selectedHunk.InsertStart);
						var end = fromEditor.Document.GetLine (selectedHunk.InsertStart + selectedHunk.Inserted - 1);
						toEditor.Insert (toOffset, fromEditor.Document.GetTextBetween (start.Offset, end.EndOffset));
					}
					
					toEditor.Document.EndAtomicUndo ();
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
			
			public void GetButtonPosition (Mono.TextEditor.Utils.Hunk hunk, int z1, int z2, int y1, int y2, out int x, out int y, out int r)
			{
				r = 14;
				if (hunk.Removed == 0) {
					x = 0;
					y = Math.Min (z1, z2);
				} else {
					x = Allocation.Width - r;
					y = Math.Min (y1, y2);
				}
			}

			static void DrawArrow (Cairo.Context cr, double x, double y)
			{
				cr.MoveTo (x - 2, y - 3);
				cr.LineTo (x + 2, y);
				cr.LineTo (x - 2, y + 3);
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
				bool hideButton = widget.OriginalEditor.Document.ReadOnly || !widget.DiffEditor.Document.ReadOnly;
				using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
					int delta = widget.OriginalEditor.Allocation.Y - Allocation.Y;
					foreach (Mono.TextEditor.Utils.Hunk hunk in (useLeft ? widget.leftDiff : widget.rightDiff)) {
						int y1 = delta + fromEditor.LineToVisualY (useLeft ? hunk.InsertStart : hunk.RemoveStart) - (int)fromEditor.VAdjustment.Value;
						int y2 = delta + fromEditor.LineToVisualY (useLeft ? hunk.InsertStart + hunk.Removed : hunk.RemoveStart + hunk.Inserted) - (int)fromEditor.VAdjustment.Value;
						if (y1 == y2)
							y2 = y1 + 1;
						
						int z1 = delta + toEditor.LineToVisualY (useLeft ? hunk.RemoveStart : hunk.InsertStart) - (int)toEditor.VAdjustment.Value;
						int z2 = delta + toEditor.LineToVisualY (useLeft ? hunk.RemoveStart + hunk.Inserted : hunk.InsertStart + hunk.Removed) - (int)toEditor.VAdjustment.Value;
						
						int x1 = 0;
						int x2 = Allocation.Width;
						
						if (!hideButton) {
							if (hunk.Removed > 0) {
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
						cr.Color = GetColor (hunk, fillAlpha);
						cr.Fill ();
						
						cr.Color = GetColor (hunk, lineAlpha);
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
							
							if (hunk.Removed > 0) {
								cr.Rectangle (0, z1, x1, z2 - z1);
								if (isButtonSelected) {
									cr.Color = new Cairo.Color (0.7, 0.7, 0.7, 0.3);
									cr.FillPreserve ();
								}
								cr.Color = new Cairo.Color (0.7, 0.7, 0.7);
								cr.Stroke ();
								cr.LineWidth = 1;
								cr.Color = new Cairo.Color (0, 0, 0);
								DrawArrow (cr, x1 / 1.8, z1 +(z2 - z1) / 2);
								DrawArrow (cr, x1 / 2.5, z1 +(z2 - z1) / 2);
								cr.Stroke ();
							} else {
								cr.Rectangle (y1, y1, Allocation.Width - x2, y2 - y1);
								if (isButtonSelected) {
									cr.Color = new Cairo.Color (0.7, 0.7, 0.7, 0.3);
									cr.FillPreserve ();
								}
								cr.Color = new Cairo.Color (0.7, 0.7, 0.7);
								cr.Stroke ();
								cr.LineWidth = 1;
								cr.Color = new Cairo.Color (0, 0, 0);
								DrawCross (cr, x2 + (Allocation.Width - x2) / 2 , z1 +(z2 - z1) / 2);
								cr.Stroke ();
							}
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
			MergeWidget widget;
				
			public OverviewRenderer (MergeWidget widget)
			{
				this.widget = widget;
				widget.Vadjustment.ValueChanged += delegate {
					QueueDraw ();
				};
				WidthRequest = 50;
				
				Events |= EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.ButtonMotionMask;
				
				Show ();
			}
			
			public void MouseMove (double y)
			{
				var adj = widget.Vadjustment;
				double position = (y / Allocation.Height) * adj.Upper - (double)adj.PageSize / 2;
				position = Math.Max (0, Math.Min (position, adj.Upper - adj.PageSize));
				widget.vScrollBar.Adjustment.Value = position;
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
				var adj = widget.Vadjustment;
				
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