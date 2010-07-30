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
				this.CreateDiff ();
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

		List<TextEditorData> localUpdate = new List<TextEditorData> ();
		List<Conflict> currentConflicts = new List<Conflict> ();
		List<Mono.TextEditor.Utils.Hunk> leftConflicts = new List<Mono.TextEditor.Utils.Hunk> ();
		List<Mono.TextEditor.Utils.Hunk> rightConflicts = new List<Mono.TextEditor.Utils.Hunk> ();
		
		public void CreateDiff ()
		{
			int curOffset = 0;
			
			var conflicts = new List<Conflict> (Conflicts (middleEditor.Document));
			if (!conflicts.Equals (currentConflicts)) {
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
					
					leftConflicts.Add (new Mono.TextEditor.Utils.Hunk (middleA, leftA, middleB - middleA, leftB - leftA));
					rightConflicts.Add (new Mono.TextEditor.Utils.Hunk (middleA, rightA, middleB - middleA, rightB - rightA));
					
					curOffset = conflict.EndOffset;
				}
			}
			
			string lastPart = middleEditor.Document.GetTextBetween (curOffset, middleEditor.Document.Length);
			leftEditor.Insert (leftEditor.Document.Length, lastPart);
			rightEditor.Insert (rightEditor.Document.Length, lastPart);
			
			leftDiff  = new List<Mono.TextEditor.Utils.Hunk> (middleEditor.Document.Diff (leftEditor.Document));
			rightDiff = new List<Mono.TextEditor.Utils.Hunk> (middleEditor.Document.Diff (rightEditor.Document));
			
/*			foreach (var item in leftDiff) {
				Console.WriteLine ("@@ -" + item.StartA + "," + item.DeletedA + " +" + item.StartB + "," + item.InsertedB + " @@");
				for (int i = item.StartA; i < item.StartA + item.DeletedA; i++) {
					Console.Write ("-" + middleEditor.GetTextEditorData ().GetLineText (i));
				}
				for (int i = item.StartB; i < item.StartB + item.InsertedB; i++) {
					Console.Write ("+" + leftEditor.GetTextEditorData ().GetLineText (i));
				}
			}
		*/
			
			LineSegment line;
			leftDiff.RemoveAll (item => null != (line = middleEditor.Document.GetLine (item.StartA)) && 
				currentConflicts.Any (c => c.StartOffset <= line.Offset && line.Offset < c.EndOffset));
			rightDiff.RemoveAll (item => null != (line = middleEditor.Document.GetLine (item.StartA)) && 
				currentConflicts.Any (c => c.StartOffset <= line.Offset && line.Offset < c.EndOffset));
			
			leftDiff.AddRange (leftConflicts);
			rightDiff.AddRange (rightConflicts);
			
			QueueDraw ();
		}

		Dictionary<Mono.TextEditor.Document, TextEditorData> dict = new Dictionary<Mono.TextEditor.Document, TextEditorData> ();
		public void SetLocal (TextEditorData data)
		{
			dict[data.Document] = data;
			data.Document.ReadOnly = false;
			data.Document.TextReplaced += HandleDataDocumentTextReplaced;
			localUpdate.Add (data);
			CreateDiff ();
		}

		void HandleDataDocumentTextReplaced (object sender, ReplaceEventArgs e)
		{
			var data = dict[(Document)sender];
			localUpdate.Remove (data);
			localUpdate.Add (data);
			CreateDiff ();
		}

		public void RemoveLocal (TextEditorData data)
		{
			localUpdate.Remove (data);
			data.Document.ReadOnly = true;
			data.Document.TextReplaced -= HandleDataDocumentTextReplaced;
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
			if (item.DeletedA == 0)
				return new Cairo.Color (0.4, 0.8, 0.4, alpha);
			if (item.InsertedB == 0) 
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
					int y1 = editor.LineToVisualY (takeLeftSide ? hunk.StartA : hunk.StartB) - (int)editor.VAdjustment.Value;
					int y2 = editor.LineToVisualY (takeLeftSide ? hunk.StartA + hunk.DeletedA : hunk.StartB + hunk.InsertedB) - (int)editor.VAdjustment.Value;
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

		/*
		void HandleRightEditorExposeEvent (object o, ExposeEventArgs args)
		{
			using (Cairo.Context cr = Gdk.CairoHelper.Create (args.Event.Window)) {
				if (Diff != null) {
					foreach (Diff.Hunk hunk in Diff) {
						if (!hunk.Same) {
							int y1 = leftEditor.LineToVisualY (hunk.Left.Start) - (int)leftEditor.VAdjustment.Value;
							int y2 = leftEditor.LineToVisualY (hunk.Left.Start + hunk.Left.Count) - (int)leftEditor.VAdjustment.Value;
								
							if (y1 == y2)
								y2 = y1 + 1;
							
							cr.Rectangle (0, y1, leftEditor.Allocation.Width, y2 - y1);
							cr.Color = GetColor (hunk, fillAlpha);
							cr.Fill ();
							
							if (hunk.Right.Count > 0 && hunk.Left.Count > 0) {
								int startOffset = middleEditor.Document.GetLine (hunk.Right.Start).Offset;
								int rStartOffset = leftEditor.Document.GetLine (hunk.Left.Start).Offset;
								var lcs = GetLCS (hunk);
								if (lcs == null) {
									string leftText = middleEditor.Document.GetTextBetween (startOffset, middleEditor.Document.GetLine (hunk.Right.Start + hunk.Right.Count - 1).EndOffset);
									string rightText = leftEditor.Document.GetTextBetween (rStartOffset, leftEditor.Document.GetLine (hunk.Left.Start + hunk.Left.Count - 1).EndOffset);
									llcsCache[hunk] = lcs = GetLCS (leftText, rightText);
								}
								
								int ll = lcs.GetLength (0), rl = lcs.GetLength (1);
								int blockStart = -1;
								Stack<KeyValuePair<int, int>> posStack = new Stack<KeyValuePair<int, int>> ();
								if (ll > 0 && rl > 0)
									posStack.Push (new KeyValuePair<int, int> (ll - 1, rl - 1));
								while (posStack.Count > 0) {
									var pos = posStack.Pop ();
									int i = pos.Key, j = pos.Value;
									if (i > 0 && j > 0 && middleEditor.Document.GetCharAt (startOffset + i) == leftEditor.Document.GetCharAt (rStartOffset + j)) {
										posStack.Push (new KeyValuePair<int, int> (i - 1, j - 1));
										PaintBlock (leftEditor, cr, rStartOffset, j, ref blockStart);
										continue;
									}
									
									if (j > 0 && (i == 0 || lcs[i, j - 1] >= lcs[i - 1, j])) {
										posStack.Push (new KeyValuePair<int, int> (i, j - 1));
										if (blockStart < 0)
											blockStart = j;
									} else if (i > 0 && (j == 0 || lcs[i, j - 1] < lcs[i - 1, j])) {
										posStack.Push (new KeyValuePair<int, int> (i - 1, j));
										PaintBlock (leftEditor, cr, rStartOffset, j, ref blockStart);
									}
								}
								PaintBlock (leftEditor, cr, rStartOffset, 0, ref blockStart);
							}
							
							cr.Color = GetColor (hunk, lineAlpha);
							cr.MoveTo (0, y1);
							cr.LineTo (leftEditor.Allocation.Width, y1);
							cr.Stroke ();
							
							cr.MoveTo (0, y2);
							cr.LineTo (leftEditor.Allocation.Width, y2);
							cr.Stroke ();
						}
					}
				}
			}
		}
		 */
		public static int[,] GetLCS (string left, string right)
		{
			int[,] result = new int[left.Length, right.Length];
			for (int i = 0; i < left.Length; i++) {
				for (int j = 0; j < right.Length; j++) {
					if (left[i] == right[j]) {
						result[i, j] = (i == 0 || j == 0) ? 1 : 1 + result[i - 1, j - 1];
					} else {
						if (i == 0) {
							result[i, j] = j == 0 ? 0 : Math.Max(0, result[i, j - 1]);
						} else {
							result[i, j] = j == 0 ? Math.Max(result[i - 1, j], 0) : Math.Max(result[i - 1, j], result [i, j - 1]);
						}
					}
				}
			}
			return result;
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
			{/*
				bool hideButton = widget.OriginalEditor.Document.ReadOnly || !widget.DiffEditor.Document.ReadOnly;
				Mono.TextEditor.Utils.Hunk selectedHunk = Mono.TextEditor.Utils.Hunk.Empty;
				if (!hideButton) {
					int delta = widget.OriginalEditor.Allocation.Y - Allocation.Y;
					foreach (var hunk in widget.leftDiff) {
						int y1 = delta + toEditor.LineToVisualY (hunk.StartB) - (int)toEditor.VAdjustment.Value;
						int y2 = delta + toEditor.LineToVisualY (hunk.StartB + hunk.insertedB) - (int)toEditor.VAdjustment.Value;
						if (y1 == y2)
							y2 = y1 + 1;
						
						int z1 = delta + fromEditor.LineToVisualY (hunk.StartA) - (int)fromEditor.VAdjustment.Value;
						int z2 = delta + fromEditor.LineToVisualY (hunk.StartA + hunk.deletedA) - (int)fromEditor.VAdjustment.Value;
							
						if (z1 == z2)
							z2 = z1 + 1;
							
						int x, y, r;
						GetButtonPosition (hunk, z1, z2, y1, y2, out x, out y, out r);
							
						if (evnt.X >= x && evnt.X - x < r && evnt.Y >= y && evnt.Y - y < r) {
							selectedHunk = hunk;
							TooltipText = GettextCatalog.GetString ("Revert this change");
							break;
						}
					}
				} else {
					selectedHunk = null;
				}
				if (selectedHunk == null)
					TooltipText = null;
				if (this.selectedHunk != selectedHunk) {
					this.selectedHunk = selectedHunk;
					QueueDraw ();
				}*/
				return base.OnMotionNotifyEvent (evnt);
			}
			
			protected override bool OnButtonPressEvent (EventButton evnt)
			{/*
				bool hideButton = fromEditor.Document.ReadOnly || !toEditor.Document.ReadOnly;
				if (!hideButton && selectedHunk != null) {
					toEditor.Document.BeginAtomicUndo ();
					LineSegment start = toEditor.Document.GetLine (selectedHunk.Right.Start);
					LineSegment end   = toEditor.Document.GetLine (selectedHunk.Right.Start + selectedHunk.Right.Count - 1);
					if (selectedHunk.Right.Count > 0)
						toEditor.Remove (start.Offset, end.EndOffset - start.Offset);
					int offset = start.Offset;
					
					if (selectedHunk.Left.Count > 0) {
						start = fromEditor.Document.GetLine (selectedHunk.Left.Start);
						end   = fromEditor.Document.GetLine (selectedHunk.Left.Start + selectedHunk.Left.Count - 1);
						toEditor.Insert (offset, fromEditor.Document.GetTextBetween (start.Offset, end.EndOffset));
					}
					toEditor.Document.EndAtomicUndo ();
				}*/
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
				if (hunk.DeletedA == 0) {
					x = 0;
					y = Math.Min (z1, z2);
				} else {
					x = Allocation.Width - r;
					y = Math.Min (y1, y2);
				}
			}

			protected override bool OnExposeEvent (EventExpose evnt)
			{
				bool hideButton = widget.OriginalEditor.Document.ReadOnly || !widget.DiffEditor.Document.ReadOnly;
				using (Cairo.Context cr = Gdk.CairoHelper.Create (evnt.Window)) {
					int delta = widget.OriginalEditor.Allocation.Y - Allocation.Y;
					foreach (Mono.TextEditor.Utils.Hunk hunk in (useLeft ? widget.leftDiff : widget.rightDiff)) {
						int y1 = delta + fromEditor.LineToVisualY (useLeft ? hunk.StartA : hunk.StartB) - (int)fromEditor.VAdjustment.Value;
						int y2 = delta + fromEditor.LineToVisualY (useLeft ? hunk.StartA + hunk.DeletedA : hunk.StartB + hunk.InsertedB) - (int)fromEditor.VAdjustment.Value;
						if (y1 == y2)
							y2 = y1 + 1;
						
						int z1 = delta + toEditor.LineToVisualY (useLeft ? hunk.StartB : hunk.StartA) - (int)toEditor.VAdjustment.Value;
						int z2 = delta + toEditor.LineToVisualY (useLeft ? hunk.StartB + hunk.InsertedB : hunk.StartA + hunk.DeletedA) - (int)toEditor.VAdjustment.Value;
						
						if (z1 == z2)
							z2 = z1 + 1;
						cr.MoveTo (Allocation.Width, y1);
						
						cr.CurveTo (Allocation.Width / 2, y1,
							Allocation.Width / 2,  z1,
							0, z1);
						
						cr.LineTo (0, z2);
						cr.CurveTo (Allocation.Width / 2, z2, 
							Allocation.Width / 2, y2,
							Allocation.Width, y2);
						cr.ClosePath ();
						cr.Color = GetColor (hunk, fillAlpha);
						cr.Fill ();
						
						cr.Color = GetColor (hunk, lineAlpha);
						cr.MoveTo (Allocation.Width, y1);
						cr.CurveTo (Allocation.Width / 2, y1,
							Allocation.Width / 2,  z1,
							0, z1);
						cr.Stroke ();
						
						cr.MoveTo (0, z2);
						cr.CurveTo (Allocation.Width / 2, z2, 
							Allocation.Width / 2, y2,
							Allocation.Width, y2);
						cr.Stroke ();
						
		/*				if (!hideButton) {
							bool isButtonSelected = selectedHunk != null && hunk.Left.Start == selectedHunk.Left.Start && hunk.Right.Start == selectedHunk.Right.Start;
							
							cr.Save ();
							int x, y, r;
							GetButtonPosition (hunk, z1, z2, y1, y2, out x, out y, out r);
							
							FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, x, y, r / 2, r, r);
							cr.Color = new Cairo.Color (1, 0, 0);
							cr.Fill ();
							FoldingScreenbackgroundRenderer.DrawRoundRectangle (cr, true, true, x + 1, y + 1, (r - 2) / 2, r - 2, r - 2);

							var shadowGradient = new Cairo.LinearGradient (x, y, x + r, y + r);
							if (isButtonSelected) {
								shadowGradient.AddColorStop (0, new Cairo.Color (1, 1, 1, 0));
								shadowGradient.AddColorStop (1, new Cairo.Color (1, 1, 1, 0.8));
							} else {
								shadowGradient.AddColorStop (0, new Cairo.Color (1, 1, 1, 0.8));
								shadowGradient.AddColorStop (1, new Cairo.Color (1, 1, 1, 0));
							}
							cr.Source = shadowGradient;
							cr.Fill ();
							
							cr.LineWidth = 2;
							cr.LineCap = Cairo.LineCap.Round;
							cr.Color = isButtonSelected ? new Cairo.Color (0.9, 0.9, 0.9) : new Cairo.Color (1, 1, 1);
							
							int a = 4;
							cr.MoveTo (x + a, y + a);
							cr.LineTo (x + r - a, y + r - a);
							cr.MoveTo (x + r - a, y + a);
							cr.LineTo (x + a, y + r - a);
							cr.Stroke ();
							cr.Restore ();
						}*/
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
						if (h.DeletedA == 0) {
							cr.Color = new Cairo.Color (0.4, 0.8, 0.4);
						} else if (h.InsertedB == 0) {
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
				pos += System.Math.Max (h.InsertedB, h.DeletedA);
/*				if (sidebyside)
					pos += h.MaxLines();
				else if (h.Same)
					pos += h.Original().Count;
				else {
					pos += h.Original().Count;
					for (int i = 0; i < h.ChangedLists; i++)
						pos += h.Changes(i).Count;
				}*/
			}
		}
	}
}