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
	public class TextEditor : Gtk.DrawingArea, IMargin
	{
		TextEditorData textEditorData = new TextEditorData ();
		Mono.TextEditor.Highlighting.Style style = new Mono.TextEditor.Highlighting.Style ();
		protected Dictionary <int, EditAction> keyBindings = new Dictionary<int,EditAction> ();
		BookmarkMargin bookmarkMargin;
		GutterMargin   gutterMargin;
		FoldMarkerMargin foldMarkerMargin;
		
		Gdk.Cursor textCursor;
		int caretBlinkStatus;
		uint caretBlinkTimeoutId = 0;
		const int CaretBlinkTime = 800;
			
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
		
		bool isVisible = true;
		bool IMargin.IsVisible {
			get {
				return isVisible;
			}
			set {
				isVisible = value;
			}
		}
		int xOffset = 0;
		public int XOffset {
			get {
				return xOffset;
			}
			set {
				 xOffset = value;
			}
		}
		
		public TextEditor () : this (new Document ())
		{
		}
		
		public TextEditor(Document doc)
		{
			this.textEditorData.Document = doc;
			this.Events =  EventMask.ExposureMask | 
				           EventMask.EnterNotifyMask |
				           EventMask.LeaveNotifyMask |
				           EventMask.ButtonPressMask | 
				           EventMask.ButtonReleaseMask | 
				           EventMask.KeyPressMask | 
				           EventMask.KeyReleaseMask | 
				           EventMask.ScrollMask | 
					       EventMask.PointerMotionMask
			;
			base.CanFocus =true;
			this.ParentSet += delegate {
//				if (Parent is Viewport) {
//					Viewport vp = (Viewport)Parent;
//					this.textEditorData.HAdjustment = vp.Hadjustment;
//					this.textEditorData.HAdjustment.ValueChanged += delegate {
//						this.QueueDraw ();
//					};
//					
//					this.textEditorData.VAdjustment = vp.Vadjustment;
//					this.textEditorData.VAdjustment.ValueChanged += delegate {
//						this.QueueDraw ();
//					};
//				}
				if (Parent is ScrolledWindow) {
					ScrolledWindow sw = (ScrolledWindow)Parent;
					this.textEditorData.HAdjustment = sw.Hadjustment;
					this.textEditorData.HAdjustment.ValueChanged += delegate {
						this.QueueDraw ();
					};
					
					this.textEditorData.VAdjustment = sw.Vadjustment;
					this.textEditorData.VAdjustment.ValueChanged += delegate {
						this.QueueDraw ();
					};
				}
			};
			this.SizeAllocated += delegate {
				SetAdjustments ();
			};

			ResetCaretBlink ();
			
			layout = new Pango.Layout (this.PangoContext);
			layout.Alignment = Pango.Alignment.Left;
			layout.FontDescription = TextEditorOptions.Options.Font;
			
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
			
			margins.Add (bookmarkMargin);
			margins.Add (gutterMargin);
			margins.Add (foldMarkerMargin);
			margins.Add (this);
			Caret.PositionChanged += delegate (object sender, DocumentLocationEventArgs args) {
				if (Caret.AutoScrollToCaret) {
					ScrollToCaret ();
					caretBlink = true;
					if (args.Location.Line != Caret.Line) 
						RedrawLine (args.Location.Line);
					RedrawLine (Caret.Line);
				}
			};
			Document.Splitter.LinesInserted  += delegate (object sender, LineEventArgs e) {
				int lineNumber = Document.Splitter.GetLineNumberForOffset (e.Line.Offset);
				RedrawFromLine (lineNumber);
			};
			Document.Splitter.LinesRemoved  += delegate (object sender, LineEventArgs e) {
				int lineNumber = Document.Splitter.GetLineNumberForOffset (e.Line.Offset);
				RedrawFromLine (lineNumber);
			};
//			Document.Splitter.LineLenghtChanged += delegate (object sender, LineEventArgs e) {
//				int lineNumber = Document.Splitter.GetLineNumberForOffset (e.Line.Offset);
//				this.RedrawLine (lineNumber);
//				if (longestLine == null || longestLine.Length < e.Line.Length || longestLine == e.Line) {
//					longestLine = e.Line;
//					this.SetAdjustments ();
//				}
//			};
			Document.DocumentUpdated += delegate {
				foreach (DocumentUpdateRequest request in Document.UpdateRequests) {
					request.Update (this);
				}
			};
			this.textEditorData.SelectionStartChanged += delegate {
				this.QueueDraw ();
			};
			this.textEditorData.SelectionEndChanged += delegate {
				this.QueueDraw ();
			};
			this.ColorStyle = new DefaultStyle (this);
			
			bookmarkMargin.IsVisible = TextEditorOptions.Options.ShowIconMargin;
			gutterMargin.IsVisible = TextEditorOptions.Options.ShowLineNumberMargin;
			foldMarkerMargin.IsVisible = TextEditorOptions.Options.ShowFoldMargin;
			
			textCursor = new Gdk.Cursor (Gdk.CursorType.Xterm);
		}
		
		protected override void OnRealized ()
		{
			base.OnRealized ();
			this.GdkWindow.Cursor = textCursor;
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
		
		public override void Destroy ()
		{
			if (caretBlinkTimeoutId != 0)
				GLib.Source.Remove (caretBlinkTimeoutId);
			base.Destroy ();
		}
		
		
		public void RedrawLine (int logicalLine)
		{
//			Console.WriteLine ("redraw:" + logicalLine);
			this.QueueDrawArea (0, (int)-this.textEditorData.VAdjustment.Value + Document.LogicalToVisualLine (logicalLine) * LineHeight, this.Allocation.Width, LineHeight);
		}
		
		public void RedrawPosition (int logicalLine, int logicalColumn)
		{
//			Console.WriteLine ("redraw:" + logicalLine);
			this.QueueDrawArea (0, (int)-this.textEditorData.VAdjustment.Value + Document.LogicalToVisualLine (logicalLine) * LineHeight, this.Allocation.Width, LineHeight);
		}
		
		public void RedrawLines (int start, int end)
		{
//			Console.WriteLine ("redraw:" + logicalLine);
			this.QueueDrawArea (0, (int)-this.textEditorData.VAdjustment.Value + Document.LogicalToVisualLine (start) * LineHeight, 
			                    this.Allocation.Width, 
			                    (int)-this.textEditorData.VAdjustment.Value + Document.LogicalToVisualLine (end) * LineHeight + LineHeight);
		}
		public void RedrawFromLine (int logicalLine)
		{
//			Console.WriteLine ("redraw from :" + logicalLine);
			this.QueueDrawArea (0, (int)-this.textEditorData.VAdjustment.Value + Document.LogicalToVisualLine (logicalLine) * LineHeight, this.Allocation.Width, this.Allocation.Height);
		}
		
		void ResetCaretBlink ()
		{
			if (caretBlinkTimeoutId != 0)
				GLib.Source.Remove (caretBlinkTimeoutId);
			caretBlinkStatus = 0;
			caretBlinkTimeoutId = GLib.Timeout.Add (CaretBlinkTime / 2, new GLib.TimeoutHandler (CaretThread));
		}
		
		bool CaretThread ()
		{
			bool newCaretBlink = caretBlink;
			if (caretBlinkStatus < 4)
				newCaretBlink = true;
			else
				newCaretBlink = (caretBlinkStatus - 4) % 3 != 0;
			
			if (layout != null && newCaretBlink != caretBlink) {
				caretBlink = newCaretBlink;
				RedrawLine (Caret.Line);
			}
			
			caretBlinkStatus++;
			return true;
		}
		bool caretBlink = true;
		
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
					Buffer.Insert (Caret.Offset, new StringBuilder (ch.ToString()));
					Caret.Column++;
				}
			}
			ResetCaretBlink ();
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
				lastTime = e.Time;
				mousePressed = true;
				int startPos;
				IMargin margin = GetMarginAtX ((int)e.X, out startPos);
				if (margin != null) {
					margin.MousePressed ((int)e.Button, (int)(e.X - startPos), (int)e.Y);
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
			mousePressed = false;
			return base.OnButtonReleaseEvent (e);
		}
		
		IMargin oldMargin = null;
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
		{
			int startPos;
			IMargin margin = GetMarginAtX ((int)e.X, out startPos);
			if (oldMargin != margin && oldMargin != null)
				oldMargin.MouseLeft ();
			if (margin != null) {
				margin.MouseDragged ((int)(e.X - startPos), (int)e.Y, mousePressed);
			}
			oldMargin = margin;
			return base.OnMotionNotifyEvent (e);
		}
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing e)
		{ 
			if (oldMargin != null)
				oldMargin.MouseLeft ();
			return base.OnLeaveNotifyEvent (e); 
		}

		
		public int Width {
			get {
				return -1;
			}
		}
		
		int lineHeight = 16;
		public int LineHeight {
			get {
				return lineHeight;
			}
		}

		public Mono.TextEditor.Highlighting.Style ColorStyle {
			get {
				return this.textEditorData.ColorStyle;
			}
			set {
				this.textEditorData.ColorStyle = value;
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

		int ColumnToVisualX (LineSegment line, int column)
		{
			string text = this.Document.Buffer.GetTextAt (line.Offset, System.Math.Min (column, line.EditableLength));
			text = text.Replace ("\t", new string (' ', TextEditorOptions.Options.TabSize));
			layout.SetText (text);
			int width, height;
			layout.GetPixelSize (out width, out height);
			return width;
		}
		int rulerX = 0;

		static Color DimColor (Color color)
		{
			return new Color ((byte)(((byte)color.Red * 19) / 20),
			                  (byte)(((byte)color.Green * 19) / 20),
			                  (byte)(((byte)color.Blue * 19) / 20));			
		}

		void DrawRectangleWithRuler (Gdk.Window win, Gdk.GC gc, int x, Gdk.Rectangle area, Gdk.Color color)
		{
			gc.RgbFgColor = color;
			if (TextEditorOptions.Options.ShowRuler) {
				int divider = System.Math.Max (area.Left, System.Math.Min (x + rulerX, area.Right));
				win.DrawRectangle (gc, true, new Rectangle (area.X, area.Y, divider - area.X, area.Height));
				gc.RgbFgColor = DimColor (color);
				win.DrawRectangle (gc, true, new Rectangle (divider, area.Y, area.Right - divider, area.Height));
			} else {
				win.DrawRectangle (gc, true, area);
			}
		}
		
		public void Draw (Gdk.Window win, Gdk.Rectangle area, int lineNr, int x, int y)
		{
			layout.Alignment       = Pango.Alignment.Left;
			LineSegment line = lineNr < Splitter.LineCount ? Splitter.Get (lineNr) : null;
			
			Gdk.GC gc = new Gdk.GC (win);
			gc.ClipRectangle = new Gdk.Rectangle (x, y, area.Width, LineHeight);
		
			Gdk.Rectangle lineArea = new Gdk.Rectangle (x, y, area.Width - x, LineHeight);
			bool isSelected = false;
			bool drawDefaultBg = true;
			Gdk.Color defaultStateType = lineNr == Caret.Line && TextEditorOptions.Options.HighlightCaretLine ? this.ColorStyle.LineMarker : this.ColorStyle.Background;
			
			if (line != null && this.textEditorData.SelectionStart != null && this.textEditorData.SelectionEnd != null) {
				SelectionMarker start;
				SelectionMarker end;
				
				if (this.textEditorData.SelectionStart.Segment.Offset < this.textEditorData.SelectionEnd.Segment.EndOffset) {
					start = this.textEditorData.SelectionStart;
					end   = this.textEditorData.SelectionEnd;
				} else {
					start = this.textEditorData.SelectionEnd;
					end   = this.textEditorData.SelectionStart;
				}
				isSelected = start.Segment.Offset < line.Offset && line.Offset + line.EditableLength < end.Segment.EndOffset;
				int selectionColumnStart = -1;
				int selectionColumnEnd   = -1;
				if (line == end.Segment) {
					selectionColumnStart = 0;
					selectionColumnEnd   = end.Column; 
				} 
				if (line == start.Segment) {
					selectionColumnStart = start.Column; 
				} 
				if (selectionColumnStart >= 0) {
					if (selectionColumnStart >= 0 && selectionColumnEnd >= 0 && selectionColumnEnd < selectionColumnStart) {
						int tmp = selectionColumnStart;
						selectionColumnStart = selectionColumnEnd;
						selectionColumnEnd = tmp;
					}
					
					// draw space before selection
					int visualXStart = ColumnToVisualX (line, selectionColumnStart) - (int)this.textEditorData.HAdjustment.Value;
					lineArea = new Gdk.Rectangle (x, y, visualXStart, LineHeight);
					DrawRectangleWithRuler (win, gc, x, lineArea, defaultStateType);
					
					// draw selection (if selection is in the middle)
					if (selectionColumnEnd >= 0) {
						int visualXEnd = ColumnToVisualX (line, selectionColumnEnd) - (int)this.textEditorData.HAdjustment.Value;
						int reminder =  System.Math.Max (0, -visualXStart); 
						if (visualXEnd - visualXStart > reminder) {
							lineArea = new Gdk.Rectangle (x + visualXStart + reminder, y, visualXEnd - visualXStart - reminder, LineHeight);
							DrawRectangleWithRuler (win, gc, x, lineArea, ColorStyle.SelectedBg);
						}
					}
					
					// draw remaining line (unselected, if in middle, otherwise rest of line is selected)
					lineArea = new Gdk.Rectangle (System.Math.Max (x, lineArea.Right), y, area.Width - System.Math.Max (x, lineArea.Right), LineHeight);
					DrawRectangleWithRuler (win, gc, x, lineArea, selectionColumnEnd >= 0 ? defaultStateType : this.ColorStyle.SelectedBg);
					drawDefaultBg = false;
				}
			}
			int width, height;
			
			if (drawDefaultBg) {
				DrawRectangleWithRuler (win, gc, x, lineArea, isSelected ? this.ColorStyle.SelectedBg : defaultStateType);
				
/*				Color color = isSelected ? this.ColorStyle.SelectedBg : defaultStateType;
				gc.RgbFgColor = color;
				if (TextEditorOptions.Options.ShowRuler) {
					win.DrawRectangle (gc, true, new Rectangle (lineArea.X, lineArea.Y,
					                                            rulerX, lineArea.Height));
					gc.RgbFgColor = DimColor (color);
					win.DrawRectangle (gc, true, new Rectangle (lineArea.X + rulerX, lineArea.Y,
					                                            lineArea.Width - rulerX, lineArea.Height));
				} else {
					win.DrawRectangle (gc, true, lineArea);
				}*/
			}
			
			
			if (TextEditorOptions.Options.ShowRuler) {
				gc.RgbFgColor = ColorStyle.Ruler;
				win.DrawLine (gc, x + rulerX, y, x + rulerX, y + LineHeight); 
			}
			
			if (line == null) {
				if (TextEditorOptions.Options.ShowInvalidLines) {
					DrawInvalidLineMarker (win, gc, x, y);
				}
				return;
			}
			
			List<FoldSegment> foldings = Document.GetStartFoldings (line);
			int offset = line.Offset;
			int xPos   = (int)(x - this.textEditorData.HAdjustment.Value);
			for (int i = 0; i < foldings.Count; ++i) {
				FoldSegment folding = foldings[i];
				int foldOffset = folding.StartLine.Offset + folding.Column;
				if (foldOffset < offset)
					continue;
				
				if (folding.IsFolded) {
					layout.SetText (Document.Buffer.GetTextAt (offset, foldOffset - offset));
					gc.RgbFgColor = ColorStyle.FoldLine;
					win.DrawLayout (gc, xPos, y, layout);
					layout.GetPixelSize (out width, out height);
					
					xPos += width;
					offset = folding.EndLine.Offset + folding.EndColumn;
					if (folding.EndLine != line) {
						line   = folding.EndLine;
						foldings = Document.GetStartFoldings (line);
						i = -1;
					}
					layout.SetText (folding.Description);
					layout.GetPixelSize (out width, out height);
					gc.RgbBgColor = ColorStyle.Background;
					gc.RgbFgColor = ColorStyle.FoldLine;
					win.DrawRectangle (gc, false, new Rectangle (xPos, y, width, this.LineHeight - 1));
					
					gc.RgbFgColor = ColorStyle.FoldLine;
					win.DrawLayout (gc, xPos, y, layout);
					xPos += width;
				}
			}
			if (line.EndOffset - offset > 0) {
				if (Document.SyntaxMode != null) {
					Chunk[] chunks = Document.SyntaxMode.GetChunks (Document, style, line, offset, line.Offset + line.EditableLength - offset);
					int start  = offset;
					int xStart = xPos;
					offset = line.Offset + line.EditableLength - offset;
					Gdk.Color selectedTextColor = ColorStyle.SelectedFg;
					int selectionStart = textEditorData.SelectionStart != null ? textEditorData.SelectionStart.Segment.Offset + textEditorData.SelectionStart.Column : -1;
					int selectionEnd = textEditorData.SelectionEnd != null ? textEditorData.SelectionEnd.Segment.Offset + textEditorData.SelectionEnd.Column : -1;
					if (selectionStart > selectionEnd) {
						int tmp = selectionEnd;
						selectionEnd = selectionStart;
						selectionStart = tmp;
					}
//					Console.WriteLine ("#" + chunks.Length);
					foreach (Chunk chunk in chunks) {
//						Console.WriteLine (chunk + " style:" + chunk.Style);
						layout.FontDescription.Weight = chunk.Style.Bold ? Pango.Weight.Bold : Pango.Weight.Normal;
						layout.FontDescription.Style = chunk.Style.Italic ? Pango.Style.Italic : Pango.Style.Normal;
						if (chunk.Offset >= selectionStart && chunk.EndOffset <= selectionEnd) {
							DrawTextWithHighlightedWs (win, gc, selectedTextColor, ref xPos, y, Document.Buffer.GetTextAt (chunk));
						} else if (chunk.Offset >= selectionStart && chunk.Offset < selectionEnd && chunk.EndOffset > selectionEnd) {
							DrawTextWithHighlightedWs (win, gc, selectedTextColor, ref xPos, y, Document.Buffer.GetTextAt (chunk.Offset, selectionEnd - chunk.Offset));
							DrawTextWithHighlightedWs (win, gc, chunk.Style.Color, ref xPos, y, Document.Buffer.GetTextAt (selectionEnd, chunk.EndOffset - selectionEnd));
						} else if (chunk.Offset < selectionStart && chunk.EndOffset > selectionStart && chunk.EndOffset <= selectionEnd) {
							DrawTextWithHighlightedWs (win, gc, chunk.Style.Color, ref xPos, y, Document.Buffer.GetTextAt (chunk.Offset, selectionStart - chunk.Offset));
							DrawTextWithHighlightedWs (win, gc, selectedTextColor, ref xPos, y, Document.Buffer.GetTextAt (selectionStart, chunk.EndOffset - selectionStart));
						} else if (chunk.Offset < selectionStart && chunk.EndOffset > selectionEnd) {
							DrawTextWithHighlightedWs (win, gc, chunk.Style.Color, ref xPos, y, Document.Buffer.GetTextAt (chunk.Offset, selectionStart - chunk.Offset));
							DrawTextWithHighlightedWs (win, gc, selectedTextColor, ref xPos, y, Document.Buffer.GetTextAt (selectionStart, selectionEnd - selectionStart));
							DrawTextWithHighlightedWs (win, gc, chunk.Style.Color, ref xPos, y, Document.Buffer.GetTextAt (selectionEnd, chunk.EndOffset - selectionEnd));
						} else 
							DrawTextWithHighlightedWs (win, gc, chunk.Style.Color, ref xPos, y, Document.Buffer.GetTextAt (chunk));
					}
					if (line.Markers != null) {
						foreach (TextMarker marker in line.Markers) {
							marker.Draw (this, win, start, line.Offset + line.EditableLength, y, xStart, xPos);
						}
					}
				} else {
					layout.FontDescription.Weight = Pango.Weight.Normal;
					Gdk.Color textColor = isSelected ? this.Style.Foreground (Gtk.StateType.Selected) : ColorStyle.Default;
					DrawTextWithHighlightedWs (win, gc, textColor, ref xPos, y, Document.Buffer.GetTextAt (offset, line.Offset + line.EditableLength - offset));
				}
				if (TextEditorOptions.Options.ShowEolMarkers) 
					DrawEolMarker (win, gc, ref xPos, y);
			}
			
			if (lineNr == Caret.Line) {
				int caretX = ColumnToVisualX (line, Caret.Column);
				Gdk.Rectangle cursor = new Gdk.Rectangle ((int)(x + caretX - this.textEditorData.HAdjustment.Value), y, 0, LineHeight);
				
				if (caretBlink && this.IsFocus && cursor.X >= x) 
					Gtk.Draw.InsertionCursor (this, win, area, cursor, true, Gtk.TextDirection.Ltr, Caret.IsInInsertMode);
			}
		}
		
		void DrawTextWithHighlightedWs (Gdk.Window win, Gdk.GC gc, Gdk.Color defaultColor, ref int xPos, int y, string text)
		{
			string[] spaces = text.Split (' ');
			Pango.Weight weight = layout.FontDescription.Weight;
			Pango.Style style = layout.FontDescription.Style;

			for (int i = 0; i < spaces.Length; i++) {
				string[] tabs = spaces[i].Split ('\t');
				
				for (int j = 0; j < tabs.Length; j++) {
					gc.RgbFgColor = defaultColor;		

					layout.FontDescription.Weight = weight; 
					layout.FontDescription.Style  = style; 
					DrawText (win, gc, ref xPos, y, tabs[j]);
					if (j + 1 < tabs.Length) {
						if (TextEditorOptions.Options.ShowTabs) { 
							DrawTabMarker (win, gc, ref xPos, y);
						} else {
							DrawText (win, gc, ref xPos, y, new string (' ', TextEditorOptions.Options.TabSize));
						}
					}
				}
				
				if (i + 1 < spaces.Length) {
					if (TextEditorOptions.Options.ShowSpaces) { 
						DrawSpaceMarker (win, gc, ref xPos, y);
					} else {
						DrawText (win, gc, ref xPos, y, " ");
					}
				}
			}
		}
		
		void DrawText (Gdk.Window win, Gdk.GC gc, ref int xPos, int y, string text)
		{
			layout.SetText (text);
			win.DrawLayout (gc, xPos, y, layout);
			int width, height;
			layout.GetPixelSize (out width, out height);
			xPos += width;
		}
		
		void DrawEolMarker (Gdk.Window win, Gdk.GC gc, ref int xPos, int y)
		{
			gc.RgbFgColor = ColorStyle.WhitespaceMarker;
			layout.FontDescription.Weight = Pango.Weight.Normal;
			layout.FontDescription.Style = Pango.Style.Normal;
			DrawText (win, gc, ref xPos, y, "\u00B6");
		}
		void DrawSpaceMarker (Gdk.Window win, Gdk.GC gc, ref int xPos, int y)
		{
			gc.RgbFgColor = ColorStyle.WhitespaceMarker;
			layout.FontDescription.Weight = Pango.Weight.Normal;
			layout.FontDescription.Style = Pango.Style.Normal;
			DrawText (win, gc, ref xPos, y, "\u00B7");
		}
		void DrawTabMarker (Gdk.Window win, Gdk.GC gc, ref int xPos, int y)
		{
			gc.RgbFgColor = ColorStyle.WhitespaceMarker;
			layout.FontDescription.Weight = Pango.Weight.Normal;
			layout.FontDescription.Style = Pango.Style.Normal;
			DrawText (win, gc, ref xPos, y, "\u00BB" + new string (' ', TextEditorOptions.Options.TabSize - 1));
		}
		
		void DrawInvalidLineMarker (Gdk.Window win, Gdk.GC gc, int x, int y)
		{
			gc.RgbFgColor = ColorStyle.InvalidLineMarker;
			layout.FontDescription.Weight = Pango.Weight.Normal;
			layout.FontDescription.Style = Pango.Style.Normal;
			DrawText (win, gc, ref x, y, "~");
		}
		
		public Gdk.Point DocumentToVisualLocation (DocumentLocation loc)
		{
			Gdk.Point result = new Point ();
			result.X = this.ColumnToVisualX (Document.GetLine (loc.Line), loc.Column);
			result.Y = this.Document.LogicalToVisualLine (loc.Line) * this.LineHeight;
			return result;
		}
		
		public DocumentLocation VisualToDocumentLocation (int x, int y)
		{
			int lineNumber = Document.VisualToLogicalLine (System.Math.Min ((int)(y + this.textEditorData.VAdjustment.Value) / LineHeight, Document.Splitter.LineCount - 1));
			LineSegment line = Document.Splitter.Get (lineNumber);
			int lineXPos = 0;
			int column;
			for (column = 0; column < line.EditableLength; column++) {
				if (this.Document.Buffer.GetCharAt (line.Offset + column) == '\t') {
					lineXPos += TextEditorOptions.Options.TabSize * this.charWidth;
				} else {
					lineXPos += this.charWidth;
				}
				if (lineXPos >= x + this.textEditorData.HAdjustment.Value) {
					break;
				}
			}
			return new DocumentLocation (lineNumber, column);
		}
		
		public void MousePressed (int button, int x, int y)
		{
			this.Caret.Location = VisualToDocumentLocation (x, y);
			this.caretBlink = false;
			this.QueueDraw ();
		}
		
		public void MouseDragged (int x, int y, bool buttonPressed)
		{
			if (!buttonPressed)
				return;
			if (!this.textEditorData.IsSomethingSelected) {
				SelectionMoveLeft.StartSelection (this.textEditorData);
			}
			Caret.PreserveSelection = true;
			Caret.Location = VisualToDocumentLocation (x, y);
			Caret.PreserveSelection = false;
			SelectionMoveLeft.EndSelection (this.textEditorData);
			this.caretBlink = false;
			this.QueueDraw ();
		}
		
		public void MouseLeft ()
		{
		}
		
		public void ScrollToCaret ()
		{
			int caretPosition = Document.LogicalToVisualLine (Caret.Line) * this.LineHeight;
			if (this.textEditorData.VAdjustment.Value > caretPosition) {
				this.textEditorData.VAdjustment.Value = caretPosition;
			} else if (this.textEditorData.VAdjustment.Value + this.textEditorData.VAdjustment.PageSize - this.LineHeight < caretPosition) {
				this.textEditorData.VAdjustment.Value = caretPosition - this.textEditorData.VAdjustment.PageSize + this.LineHeight;
			}
			int caretX = ColumnToVisualX (Document.Splitter.Get (Caret.Line), Caret.Column);
			if (this.textEditorData.HAdjustment.Value > caretX) {
				this.textEditorData.HAdjustment.Value = caretX;
			} else if (this.textEditorData.HAdjustment.Value + this.textEditorData.HAdjustment.PageSize - 60 < caretX) {
				this.textEditorData.HAdjustment.Value = caretX - this.textEditorData.HAdjustment.PageSize + 60;
			}
			
		}
		
		LineSegment longestLine;
		void SetAdjustments ()
		{
			this.textEditorData.VAdjustment.SetBounds (0, 
			                       Splitter.LineCount * this.LineHeight, 
			                       LineHeight,
			                       this.Allocation.Height,
			                       this.Allocation.Height);
			if (longestLine != null)
				this.textEditorData.HAdjustment.SetBounds (0, 
				                       longestLine.Length * 8 + 100, 
				                       8,
				                       this.Allocation.Width,
				                       this.Allocation.Width);
		}
				
		List<IMargin> margins = new List<IMargin> ();
		Pango.Layout layout;
		int oldRequest = -1;
		int charWidth;
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			Gdk.Window    win = e.Window;
			Gdk.Rectangle area = e.Area;
			layout.SetText ("H");
			layout.GetPixelSize (out this.charWidth, out this.lineHeight);
			
			int width;
			if (TextEditorOptions.Options.ShowRuler) {
				layout.SetText (new string (' ', TextEditorOptions.Options.RulerColumn));
				layout.GetPixelSize (out this.rulerX, out width);
			}
			
			if (oldRequest !=Splitter.LineCount * this.LineHeight) {
				SetAdjustments ();
				oldRequest = Splitter.LineCount * this.LineHeight;
			}
			int reminder  = (int)this.textEditorData.VAdjustment.Value % LineHeight;
			int firstLine = (int)(this.textEditorData.VAdjustment.Value / LineHeight);
			
			for (int visualLineNumber = area.Top / this.LineHeight; visualLineNumber <= area.Bottom / this.LineHeight; visualLineNumber++) {
				int curX = 0;
				int logicalLineNumber = Document.VisualToLogicalLine (visualLineNumber + firstLine);
				foreach (IMargin margin in this.margins) {
					if (margin.IsVisible) {
						margin.XOffset = curX;
						margin.Draw (win, area, logicalLineNumber, curX, visualLineNumber * LineHeight - reminder);
						curX += margin.Width;
					}
				}
			}
			return true;
		}
	}
}
