//
// TextEditor.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Mono.TextEditor.Highlighting;

using Gdk;
using Gtk;

namespace Mono.TextEditor
{
	public class TextEditor : Gtk.DrawingArea
	{
		TextEditorData textEditorData = new TextEditorData ();
		protected Dictionary <int, EditAction> keyBindings = new Dictionary<int,EditAction> ();
		protected BookmarkMargin   bookmarkMargin;
		protected GutterMargin     gutterMargin;
		protected FoldMarkerMargin foldMarkerMargin;
		protected TextViewMargin   textViewMargin;
		
		internal LineSegment longestLine;
		List<IMargin> margins = new List<IMargin> ();
		int oldRequest = -1;
		
		bool isDisposed = false;
		
		public Document Document {
			get {
				return textEditorData.Document;
			}
			set {
				textEditorData.Document = value;
			}
		}

		public Mono.TextEditor.Caret Caret {
			get {
				return textEditorData.Caret;
			}
		}

		public IBuffer Buffer {
			get {
				return Document.Buffer;
			}
		}
		
		public Mono.TextEditor.LineSplitter Splitter {
			get {
				return Document.Splitter;
			}
		}
		
		public TextEditor () : this (new Document ())
		{
		}
		
//		Gdk.Pixmap buffer = null, flipBuffer = null;
//		void DoFlipBuffer ()
//		{
//			Gdk.Pixmap tmp = buffer;
//			buffer = flipBuffer;
//			flipBuffer = tmp;
//		}
//		void AllocateWindowBuffer (Rectangle allocation)
//		{
//			if (buffer != null) {
//				buffer.Dispose ();
//				flipBuffer.Dispose ();
//			}
//			if (this.IsRealized) {
//				buffer = new Gdk.Pixmap (this.GdkWindow, allocation.Width, allocation.Height);
//				flipBuffer = new Gdk.Pixmap (this.GdkWindow, allocation.Width, allocation.Height);
//			}
//		}
		
		protected override void OnSetScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
		{
			this.textEditorData.HAdjustment = hAdjustement;
			this.textEditorData.VAdjustment = vAdjustement;
			
			if (hAdjustement == null || vAdjustement == null)
				return;
			
			this.textEditorData.HAdjustment.ValueChanged += delegate {
				this.QueueDrawArea (this.textViewMargin.XOffset, 0, this.Allocation.Width - this.textViewMargin.XOffset, this.Allocation.Height);
			};
			this.textEditorData.VAdjustment.ValueChanged += delegate {
				this.QueueDraw ();
				return;
//				if (this.textEditorData.VAdjustment.Value != System.Math.Ceiling (this.textEditorData.VAdjustment.Value)) {
//					this.textEditorData.VAdjustment.Value = System.Math.Ceiling (this.textEditorData.VAdjustment.Value);
//					return;
//				}
//				int delta = (int)(this.textEditorData.VAdjustment.Value - this.oldVadjustment);
//				oldVadjustment = this.textEditorData.VAdjustment.Value;
//				if (System.Math.Abs (delta) >= Allocation.Height - this.LineHeight * 2 || this.TextViewMargin.inSelectionDrag) {
//					this.QueueDraw ();
//					return;
//				}
//				int from, to;
//				if (delta > 0) {
//					from = delta;
//					to   = 0;
//				} else {
//					from = 0;
//					to   = -delta;
//				}
//				
//				DoFlipBuffer ();
//				Caret.IsHidden = true;
//				this.buffer.DrawDrawable (Style.BackgroundGC (StateType.Normal), 
//				                          this.flipBuffer,
//				                          0, from, 
//				                          0, to, 
//				                          Allocation.Width, Allocation.Height - from - to);
//				if (delta > 0) {
//					RenderMargins (buffer, new Gdk.Rectangle (0, Allocation.Height - delta, Allocation.Width, delta + this.LineHeight));
//				} else {
//					RenderMargins (buffer, new Gdk.Rectangle (0, 0, Allocation.Width, -delta + this.LineHeight));
//				}
//				Caret.IsHidden = false;
//				
//				GdkWindow.DrawDrawable (Style.BackgroundGC (StateType.Normal),
//				                        buffer,
//				                        0, 0, 
//				                        0, 0, 
//				                        Allocation.Width, Allocation.Height);
			};
		}
		
		public TextEditor (Document doc)
		{
			this.textEditorData.Document = doc;
			this.Events = EventMask.AllEventsMask;
			this.DoubleBuffered = true;
			base.CanFocus = true;
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Left), new CaretMoveLeft ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask), new SelectionMoveLeft ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ControlMask), new CaretMovePrevWord ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMovePrevWord ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Right), new CaretMoveRight ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask), new SelectionMoveRight ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ControlMask), new CaretMoveNextWord ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMoveNextWord ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Up), new CaretMoveUp ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ControlMask), new ScrollUpAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask), new SelectionMoveUp ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Down), new CaretMoveDown ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ControlMask), new ScrollDownAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask), new SelectionMoveDown ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Home), new CaretMoveHome ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ShiftMask), new SelectionMoveHome ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ControlMask), new CaretMoveToDocumentStart ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMoveToDocumentStart ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.End), new CaretMoveEnd ());
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ShiftMask), new SelectionMoveEnd ());
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ControlMask), new CaretMoveToDocumentEnd ());
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMoveToDocumentEnd ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert), new SwitchCaretModeAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Tab), new InsertTab ());
			keyBindings.Add (GetKeyCode (Gdk.Key.ISO_Left_Tab, Gdk.ModifierType.ShiftMask), new RemoveTab ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Return), new InsertNewLine ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Enter), new InsertNewLine ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace), new BackspaceAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ControlMask), new DeletePrevWord ());
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ShiftMask), new BackspaceAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete), new DeleteAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete, Gdk.ModifierType.ControlMask), new DeleteNextWord ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete, Gdk.ModifierType.ShiftMask), new CutAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert, Gdk.ModifierType.ControlMask), new CopyAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert, Gdk.ModifierType.ShiftMask), new PasteAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.x, Gdk.ModifierType.ControlMask), new CutAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.c, Gdk.ModifierType.ControlMask), new CopyAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.v, Gdk.ModifierType.ControlMask), new PasteAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.a, Gdk.ModifierType.ControlMask), new SelectionSelectAll ());

			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down), new PageDownAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down, Gdk.ModifierType.ShiftMask), new SelectionPageDownAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up), new PageUpAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up, Gdk.ModifierType.ShiftMask), new SelectionPageUpAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.d, Gdk.ModifierType.ControlMask), new DeleteCaretLine ());
			keyBindings.Add (GetKeyCode (Gdk.Key.D, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new DeleteCaretLineToEnd ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.z, Gdk.ModifierType.ControlMask), new UndoAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.y, Gdk.ModifierType.ControlMask), new RedoAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.F2), new GotoNextBookmark ());
			keyBindings.Add (GetKeyCode (Gdk.Key.F2, Gdk.ModifierType.ShiftMask), new GotoPrevBookmark ());
			bookmarkMargin = new BookmarkMargin (this);
			gutterMargin = new GutterMargin (this);
			foldMarkerMargin = new FoldMarkerMargin (this);
			textViewMargin = new TextViewMargin (this);
			
			margins.Add (bookmarkMargin);
			margins.Add (gutterMargin);
			margins.Add (foldMarkerMargin);
			margins.Add (textViewMargin);
			ISegment oldSelection = null;
			this.TextEditorData.SelectionChanged += delegate {
				if (this.TextEditorData.IsSomethingSelected && this.TextEditorData.SelectionRange.Offset > 0)
					new CopyAction ().CopyToPrimary (this.TextEditorData);
				
				// Handle redraw
				ISegment selection = this.TextEditorData.SelectionRange;
				int startLine    = selection != null ? Document.Splitter.GetLineNumberForOffset (selection.Offset) : -1;
				int endLine      = selection != null ? Document.Splitter.GetLineNumberForOffset (selection.EndOffset) : -1;
				int oldStartLine = oldSelection != null ? Document.Splitter.GetLineNumberForOffset (oldSelection.Offset) : -1;
				int oldEndLine   = oldSelection != null ? Document.Splitter.GetLineNumberForOffset (oldSelection.EndOffset) : -1;
				if (endLine < 0 && startLine >=0)
					endLine = Splitter.LineCount;
				if (oldEndLine < 0 && oldStartLine >=0)
					oldEndLine = Splitter.LineCount;
				int from = oldEndLine, to = endLine;
				if (selection != null && oldSelection != null) {
					if (startLine != oldStartLine && endLine != oldEndLine) {
						from = System.Math.Min (startLine, oldStartLine);
						to   = System.Math.Max (endLine, oldEndLine);
					} else if (startLine != oldStartLine) {
						from = startLine;
						to   = oldStartLine;
					} else if (endLine != oldEndLine) {
						from = endLine;
						to   = oldEndLine;
					}
				} else {
					if (selection == null) {
						from = oldStartLine;
						to = oldEndLine;
					} else if (oldSelection == null) {
						from = startLine;
						to = endLine;
					} 
				}
				oldSelection = selection;
				this.RedrawLines (System.Math.Min (from, to), System.Math.Max (from, to));
			};
			
			Document.DocumentUpdated += delegate {
				try {
					foreach (DocumentUpdateRequest request in Document.UpdateRequests) {
						request.Update (this);
					}
				} catch (Exception e) {
					System.Console.WriteLine (e);
				}
			};
				
			TextEditorOptions.Changed += OptionsChanged;
			
			Gtk.TargetList list = new Gtk.TargetList ();
			list.AddTextTargets (CopyAction.TextType);
			Gtk.Drag.DestSet (this, DestDefaults.All, (TargetEntry[])list, DragAction.Move | DragAction.Copy);
			this.Destroyed += delegate {
				Dispose ();
			};
		}
		
		protected virtual void OptionsChanged (object sender, EventArgs args)
		{
			this.TextEditorData.ColorStyle = TextEditorOptions.Options.GetColorStyle (this);
			
			bookmarkMargin.IsVisible   = TextEditorOptions.Options.ShowIconMargin;
			gutterMargin.IsVisible     = TextEditorOptions.Options.ShowLineNumberMargin;
			foldMarkerMargin.IsVisible = TextEditorOptions.Options.ShowFoldMargin;
			foreach (IMargin margin in this.margins) {
				margin.OptionsChanged ();
			}
			this.QueueDraw ();
		}
		
		protected static int GetKeyCode (Gdk.Key key)
		{
			return (int)key;
		}
		
		protected static int GetKeyCode (Gdk.Key key, Gdk.ModifierType modifier)
		{
			int m = ((int)modifier) & ((int)Gdk.ModifierType.ControlMask | (int)Gdk.ModifierType.ShiftMask);
			return (int)key | (int)m << 16;
		}
		
		public override void Dispose ()
		{
			if (isDisposed)
				return;
			this.isDisposed = true;
			TextEditorOptions.Changed -= OptionsChanged;
			
			foreach (IMargin margin in this.margins) {
				if (margin is IDisposable)
					((IDisposable)margin).Dispose ();
			}
//			if (buffer != null) {
//				buffer.Dispose ();
//				buffer = null;
//			}
//			if (flipBuffer != null) {
//				flipBuffer.Dispose ();
//				flipBuffer = null;
//			}
//			
			base.Dispose ();
		}
		
		
		internal void RedrawLine (int logicalLine)
		{
			if (isDisposed)
				return;
			this.QueueDrawArea (0, Document.LogicalToVisualLine (logicalLine) * LineHeight - (int)this.textEditorData.VAdjustment.Value,  this.Allocation.Width,  LineHeight);
		}
		
		internal void RedrawPosition (int logicalLine, int logicalColumn)
		{
			if (isDisposed)
				return;
			RedrawLine (logicalLine);
//			this.QueueDrawArea (0, (int)-this.textEditorData.VAdjustment.Value + Document.LogicalToVisualLine (logicalLine) * LineHeight, this.Allocation.Width, LineHeight);
		}
		
		internal void RedrawLines (int start, int end)
		{
			if (isDisposed)
				return;
			int visualStart = (int)-this.textEditorData.VAdjustment.Value + Document.LogicalToVisualLine (start) * LineHeight;
			int visualEnd   = (int)-this.textEditorData.VAdjustment.Value + Document.LogicalToVisualLine (end) * LineHeight + LineHeight;
			this.QueueDrawArea (0, visualStart, this.Allocation.Width, visualEnd - visualStart );
		}
		
		internal void RedrawFromLine (int logicalLine)
		{
			if (isDisposed)
				return;
			this.QueueDrawArea (0, (int)-this.textEditorData.VAdjustment.Value + Document.LogicalToVisualLine (logicalLine) * LineHeight, this.Allocation.Width, this.Allocation.Height);
		}
	
		public void SimulateKeyPress (Gdk.Key key, Gdk.ModifierType modifier)
		{
			int keyCode = GetKeyCode (key, modifier);
			if (keyBindings.ContainsKey (keyCode)) {
				try {
					keyBindings[keyCode].Run (this.textEditorData);
				} catch (Exception e) {
					Console.WriteLine ("Error while executing " + keyBindings[keyCode] + " :" + e);
				}
				
			} else if (((ulong)key) < 65000) {
				this.textEditorData.DeleteSelectedText ();
				char ch = (char)key;
				if (!char.IsControl (ch)) {
					LineSegment line = Document.GetLine (Caret.Line);
					if (Caret.IsInInsertMode || Caret.Column >= line.EditableLength) {
						Buffer.Insert (Caret.Offset, new StringBuilder (ch.ToString()));
					} else {
						Buffer.Replace (Caret.Offset, 1, new StringBuilder (ch.ToString()));
					}
					bool autoScroll = Caret.AutoScrollToCaret;
					Caret.Column++;
					Caret.AutoScrollToCaret = autoScroll;
					if (autoScroll)
						ScrollToCaret ();
					Document.RequestUpdate (new LineUpdate (Caret.Line));
					Document.CommitDocumentUpdate ();
				}
			}
			textViewMargin.ResetCaretBlink ();
		}
		
		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			SimulateKeyPress (evnt.Key, evnt.State);
			return true;
		}
		
		bool mousePressed = false;
		uint lastTime;
		protected override bool OnButtonPressEvent (Gdk.EventButton e)
		{
			base.IsFocus = true;
			
			if (lastTime != e.Time) {// filter double clicks
				if (e.Type == EventType.TwoButtonPress) {
				    lastTime = e.Time;
					mousePressed = false;
				} else {
					lastTime = 0;
					mousePressed = true;
				}
				int startPos;
				IMargin margin = GetMarginAtX ((int)e.X, out startPos);
				if (margin != null) {
					margin.MousePressed ((int)e.Button, (int)(e.X - startPos), (int)e.Y, e.Type == EventType.TwoButtonPress, e.State);
				}
			}
			return base.OnButtonPressEvent (e);
		}
		
		IMargin GetMarginAtX (int x, out int startingPos)
		{
			int curX = 0;
			foreach (IMargin margin in this.margins) {
				if (!margin.IsVisible)
					continue;
				if (curX <= x && (x <= curX + margin.Width || margin.Width < 0)) {
					startingPos = curX;
					return margin;
				}
				curX += margin.Width;
			}
			startingPos = -1;
			return null;
		}

		protected override bool OnButtonReleaseEvent (EventButton e)
		{
			if (textViewMargin.inDrag) 
				Caret.Location = textViewMargin.clickLocation;
			mousePressed = false;
			textViewMargin.inDrag = false;
			textViewMargin.inSelectionDrag = false;
			return base.OnButtonReleaseEvent (e);
		}
		
		bool dragOver = false;
		CopyAction dragContents = null;
		DocumentLocation defaultCaretPos, dragCaretPos;
		ISegment selection = null;
		
		protected override void OnDragDataDelete (DragContext context)
		{
			int offset = Caret.Offset;
			Document.Buffer.Remove (selection.Offset, selection.Length);
			if (offset >= selection.Offset) {
				Caret.PreserveSelection = true;
				Caret.Offset = offset - selection.Length;
				Caret.PreserveSelection = false;
			}
			selection = null;
			base.OnDragDataDelete (context); 
		}

		protected override void OnDragLeave (DragContext context, uint time_)
		{
			if (dragOver) {
				Caret.PreserveSelection = true;
				Caret.Location = defaultCaretPos;
				Caret.PreserveSelection = false;
				dragOver = false;
			}
			base.OnDragLeave (context, time_);
		}
		
		protected override void OnDragDataGet (DragContext context, SelectionData selection_data, uint info, uint time_)
		{
			if (this.dragContents != null) {
				this.dragContents.SetData (selection_data, info);
				this.dragContents = null;
			}
			base.OnDragDataGet (context, selection_data, info, time_);
		}
				
		protected override void OnDragDataReceived (DragContext context, int x, int y, SelectionData selection_data, uint info, uint time_)
		{
			if (selection_data.Length > 0 && selection_data.Format == 8) {
				StringBuilder builder = new StringBuilder (selection_data.Text);
				Caret.Location = dragCaretPos;
				int offset = Caret.Offset;
				if (selection != null && selection.Offset >= offset)
					selection.Offset += builder.Length;
				this.Buffer.Insert (offset, builder);
				Caret.Offset = offset + builder.Length;
				this.TextEditorData.SelectionRange = new Segment (offset, builder.Length);
				dragOver  = false;
				context   = null;
			}
			base.OnDragDataReceived (context, x, y, selection_data, info, time_);
		}

		protected override bool OnDragMotion (DragContext context, int x, int y, uint time_)
		{
			if (!this.HasFocus)
				this.GrabFocus ();
			if (!dragOver) {
				defaultCaretPos = Caret.Location; 
			}
			dragOver = true;
			Caret.PreserveSelection = true;
			dragCaretPos = VisualToDocumentLocation (x - textViewMargin.XOffset, y);
			int offset = Document.LocationToOffset (dragCaretPos);
			if (selection != null && offset >= this.selection.Offset && offset < this.selection.EndOffset) {
				Gdk.Drag.Status (context, DragAction.Default, time_);
				Caret.Location = defaultCaretPos;
			} else {
				Gdk.Drag.Status (context, context.SuggestedAction, time_);
				Caret.Location = dragCaretPos; 
			}
			Caret.PreserveSelection = false;
			return true;
		}

		IMargin oldMargin = null;
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
		{
			int startPos;
			IMargin margin = GetMarginAtX ((int)e.X, out startPos);
			if (textViewMargin.inSelectionDrag) {
				margin   = textViewMargin;
				startPos = textViewMargin.XOffset;
			}
			if (oldMargin != margin && oldMargin != null)
				oldMargin.MouseLeft ();
			
			if (textViewMargin.inDrag && margin == this.textViewMargin) {
				dragContents = new CopyAction ();
				dragContents.CopyData (this.TextEditorData);
				Gtk.Drag.Begin (this, CopyAction.TargetList, DragAction.Move | DragAction.Copy, 1, e);
				selection = this.TextEditorData.SelectionRange;
				textViewMargin.inDrag = false;
			} else if (margin != null) {
				margin.MouseHover ((int)(e.X - startPos), (int)e.Y, mousePressed);
			}
			oldMargin = margin;
			return base.OnMotionNotifyEvent (e);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing e)
		{
			if (e.Mode == CrossingMode.Normal) {
				if (oldMargin != null)
					oldMargin.MouseLeft ();
			}
			return base.OnLeaveNotifyEvent (e); 
		}

		public Mono.TextEditor.Highlighting.Style ColorStyle {
			get {
				return this.textEditorData.ColorStyle;
			}
		}

		public TextEditorData TextEditorData {
			get {
				return textEditorData;
			}
			set {
				textEditorData = value;
			}
		}
		
		public int LineHeight {
			get {
				return this.textViewMargin.LineHeight;
			}
		}
		
		public TextViewMargin TextViewMargin {
			get {
				return textViewMargin;
			}
		}
		
		public Gdk.Point DocumentToVisualLocation (DocumentLocation loc)
		{
			Gdk.Point result = new Point ();
			result.X = textViewMargin.ColumnToVisualX (Document.GetLine (loc.Line), loc.Column);
			result.Y = this.Document.LogicalToVisualLine (loc.Line) * this.LineHeight;
			return result;
		}
		
		public DocumentLocation VisualToDocumentLocation (int x, int y)
		{
			return this.textViewMargin.VisualToDocumentLocation (x, y);
		}
		
		public void ScrollToCaret ()
		{
			if (Caret.Line < 0 || Caret.Line >= Document.Splitter.LineCount)
				return;
			int yMargin = 2 * this.LineHeight;
			int xMargin = 10 * this.textViewMargin.charWidth;
			int caretPosition = Document.LogicalToVisualLine (Caret.Line) * this.LineHeight;
			if (this.textEditorData.VAdjustment.Value > caretPosition - yMargin) {
				this.textEditorData.VAdjustment.Value = caretPosition - yMargin;
			} else if (this.textEditorData.VAdjustment.Value + this.textEditorData.VAdjustment.PageSize - this.LineHeight < caretPosition + yMargin) {
				this.textEditorData.VAdjustment.Value = caretPosition - this.textEditorData.VAdjustment.PageSize + this.LineHeight + yMargin;
			}
			int caretX = textViewMargin.ColumnToVisualX (Document.Splitter.Get (Caret.Line), Caret.Column);
			if (this.textEditorData.HAdjustment.Value > caretX) {
				this.textEditorData.HAdjustment.Value = caretX ;
			} else if (this.textEditorData.HAdjustment.Value + this.textEditorData.HAdjustment.PageSize - 60 < caretX + xMargin) {
				this.textEditorData.HAdjustment.Value = caretX - this.textEditorData.HAdjustment.PageSize + 60 + xMargin;
			}
			
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
//			if (IsRealized) 
//				AllocateWindowBuffer (allocation);
			SetAdjustments (allocation);
			base.OnSizeAllocated (allocation);
		}
		
		internal void SetAdjustments (Gdk.Rectangle allocation)
		{
			if (this.textEditorData.VAdjustment != null)
				this.textEditorData.VAdjustment.SetBounds (0, 
				                                           (Splitter.LineCount + 10) * this.LineHeight, 
				                                           LineHeight,
				                                           allocation.Height,
				                                           allocation.Height);
			if (longestLine != null && this.textEditorData.HAdjustment != null)
				this.textEditorData.HAdjustment.SetBounds (0, 
				                       (longestLine.Length + 100) * this.textViewMargin.charWidth, 
				                       this.textViewMargin.charWidth,
				                       allocation.Width,
				                       allocation.Width);
		}
		
		public int GetWidth (string text)
		{
			return this.textViewMargin.GetWidth (text);
		}
		
		void RenderMargins (Gdk.Drawable win, Gdk.Rectangle area)
		{
			int reminder  = (int)this.textEditorData.VAdjustment.Value % LineHeight;
			int firstLine = (int)(this.textEditorData.VAdjustment.Value / LineHeight);
			int startLine = area.Top / this.LineHeight;
			int endLine   = startLine + (area.Height / this.LineHeight);
			if (area.Height % this.LineHeight == 0) {
				startLine = (area.Top + reminder) / this.LineHeight;
				endLine   = startLine + (area.Height / this.LineHeight) - 1;
			} else {
				endLine++;
			}
			int startY = startLine * this.LineHeight - reminder;
			int curY = startY;
			for (int visualLineNumber = startLine; visualLineNumber <= endLine; visualLineNumber++) {
				int curX = 0;
				int logicalLineNumber = Document.VisualToLogicalLine (visualLineNumber + firstLine);
				foreach (IMargin margin in this.margins) {
					if (margin.IsVisible) {
						margin.XOffset = curX;
						curX += margin.Width;
						if (curX > area.X || margin.Width < 0) {
							margin.Draw (win, area, logicalLineNumber, margin.XOffset, curY);
						}
					}
				}
				curY += LineHeight;
				if (curY > area.Bottom)
					break;
			}
		}
		
		//double oldVadjustment = 0;
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			if (oldRequest != Splitter.LineCount * this.LineHeight) {
				SetAdjustments (this.Allocation);
				oldRequest = Splitter.LineCount * this.LineHeight;
			}
			
			RenderMargins (e.Window, e.Area);
			
//			e.Window.DrawDrawable (Style.BackgroundGC (StateType.Normal), 
//			                     buffer,
//			                     e.Area.X, e.Area.Y, e.Area.X, e.Area.Y, 
//			                     e.Area.Width, e.Area.Height);
			return true;
		}
	}
}
