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
		
		public TextEditor () : this (new Document ())
		{
		}
		
		Gdk.Pixmap buffer = null, flipBuffer = null;
		void DoFlipBuffer ()
		{
			Gdk.Pixmap tmp = buffer;
			buffer = flipBuffer;
			flipBuffer = tmp;
		}
		void AllocateWindowBuffer (Rectangle allocation)
		{
			if (buffer != null) {
				buffer.Dispose ();
				flipBuffer.Dispose ();
			}
			if (this.IsRealized) {
				buffer = new Gdk.Pixmap (this.GdkWindow, allocation.Width, allocation.Height);
				flipBuffer = new Gdk.Pixmap (this.GdkWindow, allocation.Width, allocation.Height);
			}
		}
		
		void HAdjustmentValueChanged (object sender, EventArgs args)
		{
			this.QueueDrawArea (this.textViewMargin.XOffset, 0, this.Allocation.Width - this.textViewMargin.XOffset, this.Allocation.Height);
		}
		void VAdjustmentValueChanged (object sender, EventArgs args)
		{
//				this.QueueDraw ();
//				return;
				if (this.textEditorData.VAdjustment.Value != System.Math.Ceiling (this.textEditorData.VAdjustment.Value)) {
					this.textEditorData.VAdjustment.Value = System.Math.Ceiling (this.textEditorData.VAdjustment.Value);
					return;
				}
				int delta = (int)(this.textEditorData.VAdjustment.Value - this.oldVadjustment);
				oldVadjustment = this.textEditorData.VAdjustment.Value;
				if (System.Math.Abs (delta) >= Allocation.Height - this.LineHeight * 2 || this.TextViewMargin.inSelectionDrag) {
					this.QueueDraw ();
					return;
				}
				int from, to;
				if (delta > 0) {
					from = delta;
					to   = 0;
				} else {
					from = 0;
					to   = -delta;
				}
				
				DoFlipBuffer ();
				Caret.IsVisible = false;
				this.buffer.DrawDrawable (Style.BackgroundGC (StateType.Normal), 
				                          this.flipBuffer,
				                          0, from, 
				                          0, to, 
				                          Allocation.Width, Allocation.Height - from - to);
				if (delta > 0) {
					RenderMargins (buffer, new Gdk.Rectangle (0, Allocation.Height - delta, Allocation.Width, delta));
				} else {
					RenderMargins (buffer, new Gdk.Rectangle (0, 0, Allocation.Width, -delta));
				}
				Caret.IsVisible = true;
				
				GdkWindow.DrawDrawable (Style.BackgroundGC (StateType.Normal),
				                        buffer,
				                        0, 0, 
				                        0, 0, 
				                        Allocation.Width, Allocation.Height);
		}
		
		protected override void OnSetScrollAdjustments (Adjustment hAdjustement, Adjustment vAdjustement)
		{
			this.textEditorData.HAdjustment = hAdjustement;
			this.textEditorData.VAdjustment = vAdjustement;
			
			if (hAdjustement == null || vAdjustement == null)
				return;
			
			this.textEditorData.HAdjustment.ValueChanged += HAdjustmentValueChanged; 
			this.textEditorData.VAdjustment.ValueChanged += VAdjustmentValueChanged;
		}
		
		public TextEditor (Document doc)
		{
			this.textEditorData.Document = doc;
			this.Events = EventMask.AllEventsMask;
			this.DoubleBuffered = false;
			base.CanFocus = true;
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left), new CaretMoveLeft ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ShiftMask), new SelectionMoveLeft ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ControlMask), new CaretMovePrevWord ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Left, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMovePrevWord ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Left), new CaretMoveLeft ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask), new SelectionMoveLeft ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ControlMask), new CaretMovePrevWord ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Left, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMovePrevWord ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right), new CaretMoveRight ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ShiftMask), new SelectionMoveRight ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ControlMask), new CaretMoveNextWord ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Right, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMoveNextWord ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Right), new CaretMoveRight ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask), new SelectionMoveRight ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ControlMask), new CaretMoveNextWord ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Right, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMoveNextWord ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up), new CaretMoveUp ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ControlMask), new ScrollUpAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Up, Gdk.ModifierType.ShiftMask), new SelectionMoveUp ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Up), new CaretMoveUp ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ControlMask), new ScrollUpAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask), new SelectionMoveUp ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Up, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMoveUp ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down), new CaretMoveDown ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ControlMask), new ScrollDownAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Down, Gdk.ModifierType.ShiftMask), new SelectionMoveDown ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Down), new CaretMoveDown ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ControlMask), new ScrollDownAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask), new SelectionMoveDown ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Down, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMoveDown ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home), new CaretMoveHome ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ShiftMask), new SelectionMoveHome ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ControlMask), new CaretMoveToDocumentStart ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Home, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMoveToDocumentStart ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Home), new CaretMoveHome ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ShiftMask), new SelectionMoveHome ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ControlMask), new CaretMoveToDocumentStart ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Home, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMoveToDocumentStart ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End), new CaretMoveEnd ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ShiftMask), new SelectionMoveEnd ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ControlMask), new CaretMoveToDocumentEnd ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_End, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMoveToDocumentEnd ());
			keyBindings.Add (GetKeyCode (Gdk.Key.End), new CaretMoveEnd ());
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ShiftMask), new SelectionMoveEnd ());
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ControlMask), new CaretMoveToDocumentEnd ());
			keyBindings.Add (GetKeyCode (Gdk.Key.End, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new SelectionMoveToDocumentEnd ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Insert), new SwitchCaretModeAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert), new SwitchCaretModeAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.Tab), new InsertTab ());
			keyBindings.Add (GetKeyCode (Gdk.Key.ISO_Left_Tab, Gdk.ModifierType.ShiftMask), new RemoveTab ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Return), new InsertNewLine ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Enter), new InsertNewLine ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace), new BackspaceAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ControlMask), new DeletePrevWord ());
			keyBindings.Add (GetKeyCode (Gdk.Key.BackSpace, Gdk.ModifierType.ShiftMask), new BackspaceAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete), new DeleteAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete, Gdk.ModifierType.ControlMask), new DeleteNextWord ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete), new DeleteAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete, Gdk.ModifierType.ControlMask), new DeleteNextWord ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Delete, Gdk.ModifierType.ShiftMask), new CutAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Delete, Gdk.ModifierType.ShiftMask), new CutAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Insert, Gdk.ModifierType.ControlMask), new CopyAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Insert, Gdk.ModifierType.ShiftMask), new PasteAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert, Gdk.ModifierType.ControlMask), new CopyAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Insert, Gdk.ModifierType.ShiftMask), new PasteAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.x, Gdk.ModifierType.ControlMask), new CutAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.c, Gdk.ModifierType.ControlMask), new CopyAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.v, Gdk.ModifierType.ControlMask), new PasteAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.a, Gdk.ModifierType.ControlMask), new SelectionSelectAll ());

			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Down), new PageDownAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Down, Gdk.ModifierType.ShiftMask), new SelectionPageDownAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down), new PageDownAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Down, Gdk.ModifierType.ShiftMask), new SelectionPageDownAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Up), new PageUpAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.KP_Page_Up, Gdk.ModifierType.ShiftMask), new SelectionPageUpAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up), new PageUpAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.Page_Up, Gdk.ModifierType.ShiftMask), new SelectionPageUpAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.d, Gdk.ModifierType.ControlMask), new DeleteCaretLine ());
			keyBindings.Add (GetKeyCode (Gdk.Key.D, Gdk.ModifierType.ShiftMask | Gdk.ModifierType.ControlMask), new DeleteCaretLineToEnd ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.z, Gdk.ModifierType.ControlMask), new UndoAction ());
			keyBindings.Add (GetKeyCode (Gdk.Key.z, Gdk.ModifierType.ControlMask | Gdk.ModifierType.ShiftMask), new RedoAction ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.F2), new GotoNextBookmark ());
			keyBindings.Add (GetKeyCode (Gdk.Key.F2, Gdk.ModifierType.ShiftMask), new GotoPrevBookmark ());
			
			keyBindings.Add (GetKeyCode (Gdk.Key.b, Gdk.ModifierType.ControlMask), new GotoMatchingBracket ());
			
			bookmarkMargin = new BookmarkMargin (this);
			gutterMargin = new GutterMargin (this);
			foldMarkerMargin = new FoldMarkerMargin (this);
			textViewMargin = new TextViewMargin (this);
			
			margins.Add (bookmarkMargin);
			margins.Add (gutterMargin);
			margins.Add (foldMarkerMargin);
			margins.Add (textViewMargin);
			ISegment oldSelection = null;
			this.textEditorData.SelectionChanged += delegate {
				if (IsSomethingSelected && SelectionRange.Offset >= 0 && SelectionRange.EndOffset < Document.Length)
					new CopyAction ().CopyToPrimary (this.textEditorData);
				
				// Handle redraw
				ISegment selection = SelectionRange;
				int startLine    = selection != null ? Document.OffsetToLineNumber (selection.Offset) : -1;
				int endLine      = selection != null ? Document.OffsetToLineNumber (selection.EndOffset) : -1;
				int oldStartLine = oldSelection != null ? Document.OffsetToLineNumber (oldSelection.Offset) : -1;
				int oldEndLine   = oldSelection != null ? Document.OffsetToLineNumber (oldSelection.EndOffset) : -1;
				if (endLine < 0 && startLine >=0)
					endLine = Document.LineCount;
				if (oldEndLine < 0 && oldStartLine >=0)
					oldEndLine = Document.LineCount;
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
				oldSelection = selection != null ? new Segment (selection.Offset, selection.Length) : null;
				this.RedrawLines (System.Math.Min (from, to) - 1, System.Math.Max (from, to));
				OnSelectionChanged (EventArgs.Empty);
			};
			
			Document.DocumentUpdated += DocumentUpdatedHandler;
				
			TextEditorOptions.Changed += OptionsChanged;
			
			Gtk.TargetList list = new Gtk.TargetList ();
			list.AddTextTargets (CopyAction.TextType);
			Gtk.Drag.DestSet (this, DestDefaults.All, (TargetEntry[])list, DragAction.Move | DragAction.Copy);
			this.Destroyed += delegate {
				Dispose ();
			};
		}
		
		
		void DocumentUpdatedHandler (object sender, EventArgs args)
		{
			foreach (DocumentUpdateRequest request in Document.UpdateRequests) {
				request.Update (this);
			}
		}
		
		protected virtual void OptionsChanged (object sender, EventArgs args)
		{
			if (!this.IsRealized)
				return;
			this.textEditorData.ColorStyle = TextEditorOptions.Options.GetColorStyle (this);
			
			bookmarkMargin.IsVisible   = TextEditorOptions.Options.ShowIconMargin;
			gutterMargin.IsVisible     = TextEditorOptions.Options.ShowLineNumberMargin;
			foldMarkerMargin.IsVisible = TextEditorOptions.Options.ShowFoldMargin;
			foreach (IMargin margin in this.margins) {
				margin.OptionsChanged ();
			}
			SetAdjustments (Allocation);
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
			Document.DocumentUpdated -= DocumentUpdatedHandler;
			TextEditorOptions.Changed -= OptionsChanged;
			
			if (this.textEditorData.HAdjustment != null) {
				this.textEditorData.HAdjustment.ValueChanged -= HAdjustmentValueChanged; 
				this.textEditorData.HAdjustment = null;
			}
			if (this.textEditorData.VAdjustment!= null) {
				this.textEditorData.VAdjustment.ValueChanged -= VAdjustmentValueChanged;
				this.textEditorData.VAdjustment = null;
			}
			
			if (margins != null) {
				foreach (IMargin margin in this.margins) {
					if (margin is IDisposable)
						((IDisposable)margin).Dispose ();
				}
				this.margins = null;
			}
			
			if (this.textEditorData != null) {
				this.textEditorData.Dispose ();
				this.textEditorData = null;
			}
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
		
		public void RunAction (EditAction action)
		{
			try {
				action.Run (this.textEditorData);
			} catch (Exception e) {
				Console.WriteLine ("Error while executing " + action + " :" + e);
			}
		}
		
		public void SimulateKeyPress (Gdk.Key key, uint unicodeKey, Gdk.ModifierType modifier)
		{
			int keyCode = GetKeyCode (key, modifier);
			if (keyBindings.ContainsKey (keyCode)) {
				try {
					Document.BeginAtomicUndo ();
					keyBindings[keyCode].Run (this.textEditorData);
					Document.EndAtomicUndo ();
				} catch (Exception e) {
					Console.WriteLine ("Error while executing " + keyBindings[keyCode] + " :" + e);
				}
			} else if (unicodeKey != 0) {
				Document.BeginAtomicUndo ();
				this.textEditorData.DeleteSelectedText ();
				char ch = (char)unicodeKey;
				if (!char.IsControl (ch)) {
					LineSegment line = Document.GetLine (Caret.Line);
					if (Caret.IsInInsertMode || Caret.Column >= line.EditableLength) {
						Document.Insert (Caret.Offset, ch.ToString());
					} else {
						Document.Replace (Caret.Offset, 1, ch.ToString());
					}
					bool autoScroll = Caret.AutoScrollToCaret;
					Caret.Column++;
					Caret.AutoScrollToCaret = autoScroll;
					if (autoScroll)
						ScrollToCaret ();
					Document.RequestUpdate (new LineUpdate (Caret.Line));
					Document.CommitDocumentUpdate ();
				}
				Document.EndAtomicUndo ();
				Document.OptimizeTypedUndo ();
			}
			textViewMargin.ResetCaretBlink ();
		}
		
		
		IMMulticontext context = null;
		protected override bool OnKeyPressEvent (Gdk.EventKey evt)
		{
			if (context == null) {
				context = new IMMulticontext ();
				context.ClientWindow = this.GdkWindow;
				context.Commit += delegate (object sender, Gtk.CommitArgs ca) {
					foreach (char ch in ca.Str) {
						SimulateKeyPress (evt.Key, (uint)ch, evt.State);
					}
				};
			}
			if (!context.FilterKeypress (evt)) 
				SimulateKeyPress (evt.Key, Gdk.Keyval.ToUnicode (evt.KeyValue), evt.State);
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
				} else {
					lastTime = 0;
				}
				if (e.Button == 1)
					mousePressed = true;
				int startPos;
				IMargin margin = GetMarginAtX ((int)e.X, out startPos);
				if (margin != null) {
					margin.MousePressed ((int)e.Button, (int)(e.X - startPos), (int)e.Y, e.Type, e.State);
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
			int startPos;
			IMargin margin = GetMarginAtX ((int)e.X, out startPos);
			if (margin != null)
				margin.MouseReleased ((int)e.Button, (int)(e.X - startPos), (int)e.Y, e.State);
			ResetMouseState ();
			return base.OnButtonReleaseEvent (e);
		}
		protected void ResetMouseState ()
		{
			mousePressed = false;
			textViewMargin.inDrag = false;
			textViewMargin.inSelectionDrag = false;
		}
		
		bool dragOver = false;
		CopyAction dragContents = null;
		DocumentLocation defaultCaretPos, dragCaretPos;
		ISegment selection = null;
		
		protected override void OnDragDataDelete (DragContext context)
		{
			int offset = Caret.Offset;
			Document.Remove (selection.Offset, selection.Length);
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
				Caret.Location = dragCaretPos;
				int offset = Caret.Offset;
				if (selection != null && selection.Offset >= offset)
					selection.Offset += selection_data.Text.Length;
				Document.Insert (offset, selection_data.Text);
				Caret.Offset = offset + selection_data.Text.Length;
				SelectionRange = new Segment (offset, selection_data.Text.Length);
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
			if (margin != null)
				GdkWindow.Cursor = margin.MarginCursor;
			
			if (textViewMargin.inSelectionDrag) {
				margin   = textViewMargin;
				startPos = textViewMargin.XOffset;
			}
			if (oldMargin != margin && oldMargin != null)
				oldMargin.MouseLeft ();
			
			if (textViewMargin.inDrag && margin == this.textViewMargin) {
				dragContents = new CopyAction ();
				dragContents.CopyData (textEditorData);
				Gtk.Drag.Begin (this, CopyAction.TargetList, DragAction.Move | DragAction.Copy, 1, e);
				selection = SelectionRange;
				textViewMargin.inDrag = false;
			} else if (margin != null) {
				margin.MouseHover ((int)(e.X - startPos), (int)e.Y, mousePressed);
			}
			oldMargin = margin;
			return base.OnMotionNotifyEvent (e);
		}
		
		string     customText;
		Gtk.Widget customSource;
		public void BeginDrag (string text, Gtk.Widget source, DragContext context)
		{
			customText = text;
			customSource = source;
			source.DragDataGet += CustomDragDataGet;
			source.DragEnd += CustomDragEnd;
		}
		void CustomDragDataGet (object sender, Gtk.DragDataGetArgs args) 
		{
			args.SelectionData.Text = customText;
		}
		void CustomDragEnd (object sender, Gtk.DragEndArgs args) 
		{
			customSource.DragDataGet -= CustomDragDataGet;
			customSource.DragEnd -= CustomDragEnd;
			customSource = null;
			customText = null;
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing e)
		{
			if (e.Mode == CrossingMode.Normal) {
				GdkWindow.Cursor = null;
				if (oldMargin != null)
					oldMargin.MouseLeft ();
			}
			return base.OnLeaveNotifyEvent (e); 
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
		
		public void CenterToCaret ()
		{
			if (Caret.Line < 0 || Caret.Line >= Document.LineCount)
				return;
			int yMargin = 1 * this.LineHeight;
			int xMargin = 10 * this.textViewMargin.CharWidth;
			int caretPosition = Document.LogicalToVisualLine (Caret.Line) * this.LineHeight;
			this.textEditorData.VAdjustment.Value = caretPosition - this.textEditorData.VAdjustment.PageSize / 2;
			int caretX = textViewMargin.ColumnToVisualX (Document.GetLine (Caret.Line), Caret.Column);
			if (this.textEditorData.HAdjustment.Value > caretX) {
				this.textEditorData.HAdjustment.Value = caretX ;
			} else if (this.textEditorData.HAdjustment.Value + this.textEditorData.HAdjustment.PageSize - 60 < caretX + xMargin) {
				this.textEditorData.HAdjustment.Value = caretX - this.textEditorData.HAdjustment.PageSize + 60 + xMargin;
			}
		}
		
		public void ScrollToCaret ()
		{
			if (Caret.Line < 0 || Caret.Line >= Document.LineCount)
				return;
			int yMargin = 1 * this.LineHeight;
			int xMargin = 10 * this.textViewMargin.CharWidth;
			int caretPosition = Document.LogicalToVisualLine (Caret.Line) * this.LineHeight;
			if (this.textEditorData.VAdjustment.Value > caretPosition) {
				this.textEditorData.VAdjustment.Value = caretPosition;
			} else if (this.textEditorData.VAdjustment.Value + this.textEditorData.VAdjustment.PageSize - this.LineHeight < caretPosition + yMargin) {
				this.textEditorData.VAdjustment.Value = caretPosition - this.textEditorData.VAdjustment.PageSize + this.LineHeight + yMargin;
			}
			int caretX = textViewMargin.ColumnToVisualX (Document.GetLine (Caret.Line), Caret.Column);
			if (this.textEditorData.HAdjustment.Value > caretX) {
				this.textEditorData.HAdjustment.Value = caretX ;
			} else if (this.textEditorData.HAdjustment.Value + this.textEditorData.HAdjustment.PageSize - 60 < caretX + xMargin) {
				this.textEditorData.HAdjustment.Value = caretX - this.textEditorData.HAdjustment.PageSize + 60 + xMargin;
			}
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (IsRealized) 
				AllocateWindowBuffer (allocation);
			SetAdjustments (allocation);
			base.OnSizeAllocated (allocation);
		}
		
		protected override bool OnScrollEvent (EventScroll evnt)
		{
			if ((evnt.State & Gdk.ModifierType.ControlMask) == Gdk.ModifierType.ControlMask) {
				if (evnt.Direction == ScrollDirection.Down)
					TextEditorOptions.Options.ZoomIn ();
				else 
					TextEditorOptions.Options.ZoomOut ();
				return true;
			}
			return base.OnScrollEvent (evnt); 
		}
		
		internal void SetAdjustments (Gdk.Rectangle allocation)
		{
			if (this.textEditorData.VAdjustment != null) {
				int maxY = (Document.LogicalToVisualLine (Document.LineCount - 1) + 5) * this.LineHeight;
				this.textEditorData.VAdjustment.SetBounds (0, 
				                                           maxY, 
				                                           LineHeight,
				                                           allocation.Height,
				                                           allocation.Height);
				if (maxY < allocation.Height) 
					this.textEditorData.VAdjustment.Value = 0;
			}
			if (longestLine != null && this.textEditorData.HAdjustment != null)
				this.textEditorData.HAdjustment.SetBounds (0, 
				                       (longestLine.Length + 100) * this.textViewMargin.CharWidth, 
				                       this.textViewMargin.CharWidth,
				                       allocation.Width,
				                       allocation.Width);
		}
		
		public int GetWidth (string text)
		{
			return this.textViewMargin.GetWidth (text);
		}
		
		void RenderMargins (Gdk.Drawable win, Gdk.Rectangle area)
		{
			this.TextViewMargin.rulerX = TextEditorOptions.Options.RulerColumn * this.TextViewMargin.CharWidth - (int)this.textEditorData.HAdjustment.Value;
			int reminder  = (int)this.textEditorData.VAdjustment.Value % LineHeight;
			int firstLine = (int)(this.textEditorData.VAdjustment.Value / LineHeight);
			int startLine = area.Top / this.LineHeight;
			int endLine   = startLine + (area.Height / this.LineHeight);
			if (area.Height % this.LineHeight == 0) {
				startLine = (area.Top + reminder) / this.LineHeight;
				endLine   = startLine + (area.Height / this.LineHeight);
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
		
		double oldVadjustment = 0;
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			int lastVisibleLine = Document.LogicalToVisualLine (Document.LineCount - 1);
			if (oldRequest != lastVisibleLine) {
				SetAdjustments (this.Allocation);
				oldRequest = lastVisibleLine;
			}
			
			//RenderMargins (e.Window, e.Area);
			RenderMargins (this.buffer, e.Area);
			
			e.Window.DrawDrawable (Style.BackgroundGC (StateType.Normal), 
			                     buffer,
			                     e.Area.X, e.Area.Y, e.Area.X, e.Area.Y, 
			                     e.Area.Width, e.Area.Height);
			return true;
		}
		
		#region TextEditorData functions
		public Mono.TextEditor.Highlighting.Style ColorStyle {
			get {
				return this.textEditorData.ColorStyle;
			}
		}
		
		public bool IsSomethingSelected {
			get {
				return this.textEditorData.IsSomethingSelected;
			}
		}
		
		public int SelectionAnchor {
			get {
				return this.textEditorData.SelectionAnchor;
			}
			set {
				this.textEditorData.SelectionAnchor = value;
			}
		}
		
		public DocumentLocation SelectionAnchorLocation {
			get {
				return Document.OffsetToLocation (SelectionAnchor);
			}
			set {
				SelectionAnchor = Document.LocationToOffset (value);
			}
		}
		
		public ISegment SelectionRange {
			get {
				return this.textEditorData.SelectionRange;
			}
			set {
				this.textEditorData.SelectionRange = value;
			}
		}
		
		public string SelectedText {
			get {
				return this.textEditorData.SelectedText;
			}
			set {
				this.textEditorData.SelectedText = value;
			}
		}
		
		public IEnumerable<LineSegment> SelectedLines {
			get {
				return this.textEditorData.SelectedLines;
			}
		}
		
		public Adjustment HAdjustment {
			get {
				return this.textEditorData.HAdjustment;
			}
		}
		
		public Adjustment VAdjustment {
			get {
				return this.textEditorData.VAdjustment;
			}
		}
		
		public void ClearSelection ()
		{
			this.textEditorData.ClearSelection ();
		}
		
		public void DeleteSelectedText ()
		{
			this.textEditorData.DeleteSelectedText ();
		}
		
		public void RunEditAction (EditAction action)
		{
			action.Run (this.textEditorData);
		}
		public void ExtendSelectionTo (DocumentLocation location)
		{
			this.textEditorData.ExtendSelectionTo (location);
		}
		public void ExtendSelectionTo (int offset)
		{
			this.textEditorData.ExtendSelectionTo (offset);
		}
		public void SetSelectLines (int from, int to)
		{
			this.textEditorData.SetSelectLines (from, to);
		}
		
		public void InsertAtCaret (string text)
		{
			textEditorData.InsertAtCaret (text);
		}
		
		/// <summary>
		/// Use with care.
		/// </summary>
		/// <returns>
		/// A <see cref="TextEditorData"/>
		/// </returns>
		public TextEditorData GetTextEditorData ()
		{
			return this.textEditorData;
		}
		
		public event EventHandler SelectionChanged;
		protected virtual void OnSelectionChanged (EventArgs args)
		{
			if (SelectionChanged != null) 
				SelectionChanged (this, args);
		}
		#endregion
		
		#region Search & Replace
		bool highlightSearchPattern = false;
		public string SearchPattern {
			get {
				return this.textEditorData.SearchPattern;
			}
			set {
				this.textEditorData.SearchPattern = value;
				this.QueueDraw ();
			}
		}
		
		public bool HighlightSearchPattern {
			get {
				return highlightSearchPattern;
			}
			set {
				if (highlightSearchPattern != value) {
					this.highlightSearchPattern = value;
					this.QueueDraw ();
				}
			}
		}
		public bool IsCaseSensitive {
			get {
				return this.textEditorData.IsCaseSensitive;
			}
			set {
				this.textEditorData.IsCaseSensitive = value;
			}
		}
		
		public bool IsWholeWordOnly {
			get {
				return this.textEditorData.IsWholeWordOnly;
			}
			
			set {
				this.textEditorData.IsWholeWordOnly = value;
			}
		}
		
		public SearchResult SearchForward (int fromOffset)
		{
			return textEditorData.SearchForward (fromOffset);
		}
		
		public SearchResult SearchBackward (int fromOffset)
		{
			return textEditorData.SearchBackward (fromOffset);
		}
		
		public SearchResult FindNext ()
		{
			return textEditorData.FindNext ();
		}
		
		public SearchResult FindPrevious ()
		{
			return textEditorData.FindPrevious ();
		}
		
		public bool Replace (string withPattern)
		{
			return textEditorData.Replace (withPattern);
		}
		
		public int ReplaceAll (string withPattern)
		{
			return textEditorData.ReplaceAll (withPattern);
		}
		#endregion
		
	}
}
