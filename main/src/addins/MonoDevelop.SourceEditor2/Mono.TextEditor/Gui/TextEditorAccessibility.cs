// 
// TextEditorAccessibility.cs
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

#if ATK

using System;
using Atk;
using System.Collections.Generic;

namespace Mono.TextEditor
{
	public class TextEditorAccessible : BaseWidgetAccessible, TextImplementor, EditableTextImplementor
	{
		TextEditor editor;
		
		Document Document {
			get {
				return editor.Document;
			}
		}
		
		TextEditorData TextEditorData {
			get {
				return editor.GetTextEditorData ();
			}
		}
		
		public TextEditorAccessible (TextEditor editor) : base(editor)
		{
			this.editor = editor;/*
			Document.TextReplaced += AtkHandleDocTextReplaced;
			TextEditorData.Caret.PositionChanged += delegate {
			
				Atk.TextCaretMovedHandler handler = textCaretMoved;
				if (handler != null) {
					Atk.TextCaretMovedArgs caretMoveArgs = new Atk.TextCaretMovedArgs ();
	//				caretMoveArgs.Location = Caret.Offset;
					handler (this, caretMoveArgs);
				}
			};*/
		}
		
		
		#region TextImplementor implementation
		string TextImplementor.GetText (int start_offset, int end_offset)
		{
			return Document.GetTextAt (start_offset, end_offset - start_offset);
		}

		string TextImplementor.GetTextAfterOffset (int offset, TextBoundary boundary_type, out int start_offset, out int end_offset)
		{
			LineSegment line;
			switch (boundary_type) {
			case Atk.TextBoundary.Char:
				start_offset = offset;
				end_offset = offset + 1;
				break;
				
			case Atk.TextBoundary.SentenceEnd:
			case Atk.TextBoundary.LineEnd:
				line = Document.GetLineByOffset (offset);
				start_offset = offset;
				end_offset = line.Offset + line.EditableLength;
				break;
			case Atk.TextBoundary.SentenceStart:
			case Atk.TextBoundary.LineStart:
				line = Document.GetLineByOffset (offset);
				start_offset = line.Offset;
				end_offset = offset;
				break;
			case Atk.TextBoundary.WordEnd:
				start_offset = offset;
				end_offset = TextEditorData.FindCurrentWordEnd (offset);
				break;
			case Atk.TextBoundary.WordStart:
				start_offset = TextEditorData.FindCurrentWordStart (offset);
				end_offset = offset;
				break;
			default:
				start_offset = end_offset = offset;
				break;
			}
			start_offset = System.Math.Min (start_offset, offset);
			return Document.GetTextBetween (start_offset, end_offset);	
		}

		string TextImplementor.GetTextAtOffset (int offset, TextBoundary boundary_type, out int start_offset, out int end_offset)
		{
			LineSegment line;
			switch (boundary_type) {
			case Atk.TextBoundary.Char:
				start_offset = offset;
				end_offset = offset + 1;
				break;
				
			case Atk.TextBoundary.SentenceEnd:
			case Atk.TextBoundary.LineEnd:
				line = Document.GetLineByOffset (offset);
				start_offset = offset;
				end_offset = line.Offset + line.EditableLength;
				break;
			case Atk.TextBoundary.SentenceStart:
			case Atk.TextBoundary.LineStart:
				line = Document.GetLineByOffset (offset);
				start_offset = line.Offset;
				end_offset = offset;
				break;
			case Atk.TextBoundary.WordEnd:
				start_offset = offset;
				end_offset = TextEditorData.FindCurrentWordEnd (offset);
				break;
			case Atk.TextBoundary.WordStart:
				start_offset = TextEditorData.FindCurrentWordStart (offset);
				end_offset = offset;
				break;
			default:
				start_offset = end_offset = offset;
				break;
			}
			return Document.GetTextBetween (start_offset, end_offset);
		}

		char TextImplementor.GetCharacterAtOffset (int offset)
		{
			return Document.GetCharAt (offset);
		}

		string TextImplementor.GetTextBeforeOffset (int offset, TextBoundary boundary_type, out int start_offset, out int end_offset)
		{
			LineSegment line;
			switch (boundary_type) {
			case Atk.TextBoundary.Char:
				start_offset = offset;
				end_offset = offset + 1;
				break;
				
			case Atk.TextBoundary.SentenceEnd:
			case Atk.TextBoundary.LineEnd:
				line = Document.GetLineByOffset (offset);
				start_offset = offset;
				end_offset = line.Offset + line.EditableLength;
				break;
			case Atk.TextBoundary.SentenceStart:
			case Atk.TextBoundary.LineStart:
				line = Document.GetLineByOffset (offset);
				start_offset = line.Offset;
				end_offset = offset;
				break;
			case Atk.TextBoundary.WordEnd:
				start_offset = offset;
				end_offset = TextEditorData.FindCurrentWordEnd (offset);
				break;
			case Atk.TextBoundary.WordStart:
				start_offset = TextEditorData.FindCurrentWordStart (offset);
				end_offset = offset;
				break;
			default:
				start_offset = end_offset = offset;
				break;
			}
			end_offset = System.Math.Min (end_offset, offset);
			return Document.GetTextBetween (start_offset, end_offset);
		}

		Atk.Attribute[] TextImplementor.GetRunAttributes (int offset, out int start_offset, out int end_offset)
		{
			// TODO
			start_offset = end_offset = offset;
			return null;
		}

		void TextImplementor.GetCharacterExtents (int offset, out int x, out int y, out int width, out int height, CoordType coords)
		{
			var point = editor.LocationToPoint (Document.OffsetToLocation (offset));
			x = point.X + (int)editor.TextViewMargin.XOffset;
			y = point.Y;
			width = (int)editor.TextViewMargin.CharWidth;
			height = (int)editor.LineHeight;
			switch (coords) {
			case Atk.CoordType.Screen:
				int ox, oy;
				editor.GdkWindow.GetOrigin (out ox, out oy);
				x += ox; y += oy;
				break;
			case Atk.CoordType.Window:
				// nothing
				break;
			}
		}

		int TextImplementor.GetOffsetAtPoint (int x, int y, CoordType coords)
		{
			int rx = 0, ry = 0;
			switch (coords) {
			case Atk.CoordType.Screen:
				editor.TranslateCoordinates (editor, x, y, out rx, out ry);
				rx -= (int)editor.TextViewMargin.XOffset;
				break;
			case Atk.CoordType.Window:
				rx = x - (int)editor.TextViewMargin.XOffset;
				ry = y;
				break;
			}
			return Document.LocationToOffset (editor.PointToLocation (rx, ry));
		}

		string TextImplementor.GetSelection (int selection_num, out int start_offset, out int end_offset)
		{
			if (!TextEditorData.IsSomethingSelected) {
				start_offset = end_offset = editor.Caret.Offset;
				return "";
			}
			var selection = TextEditorData.SelectionRange;
			start_offset = selection.Offset;
			end_offset = selection.EndOffset;
			return editor.SelectedText;
		}

		bool TextImplementor.AddSelection (int start_offset, int end_offset)
		{
			editor.GetTextEditorData ().SetSelection (start_offset, end_offset);
			return true;
		}

		bool TextImplementor.RemoveSelection (int selection_num)
		{
			TextEditorData.DeleteSelectedText ();
			return true;
		}

		bool TextImplementor.SetSelection (int selection_num, int start_offset, int end_offset)
		{
			TextEditorData.SetSelection (start_offset, end_offset);
			return true;
		}

		bool TextImplementor.SetCaretOffset (int offset)
		{
			TextEditorData.Caret.Offset = offset;
			return true;
		}

		void TextImplementor.GetRangeExtents (int start_offset, int end_offset, CoordType coord_type, out TextRectangle rect)
		{
			Atk.TextRectangle result = new Atk.TextRectangle ();
			var point1 = editor.LocationToPoint (Document.OffsetToLocation (start_offset));
			var point2 = editor.LocationToPoint (Document.OffsetToLocation (end_offset));

			result.X = System.Math.Min (point2.X, point1.Y);
			result.Y = System.Math.Min (point2.Y, point1.Y);
			result.Width = System.Math.Abs (point2.X - point1.X);
			result.Height = (int)(System.Math.Abs (point2.Y - point1.Y) + editor.LineHeight);
			rect = result;
		}

		TextRange TextImplementor.GetBoundedRanges (TextRectangle rect, CoordType coord_type, TextClipType x_clip_type, TextClipType y_clip_type)
		{
			Atk.TextRange result = new Atk.TextRange ();
			// todo 
			return result;
		}

		int TextImplementor.CaretOffset {
			get {
				return TextEditorData.Caret.Offset;
			}
		}

		Atk.Attribute[] TextImplementor.DefaultAttributes {
			get {
				// TODO
				return null;
			}
		}

		int TextImplementor.CharacterCount {
			get {
				return Document.Length;
			}
		}

		int TextImplementor.NSelections {
			get {
				return TextEditorData.IsSomethingSelected ? 1 : 0;
			}
		}
		#endregion

		#region EditableTextImplementor implementation
		bool EditableTextImplementor.SetRunAttributes (GLib.SList attrib_set, int start_offset, int end_offset)
		{
			// TODO
			return false;
		}

		void EditableTextImplementor.InsertText (string str1ng, ref int position)
		{
			position += editor.Insert (position, str1ng);
		}

		void EditableTextImplementor.CopyText (int start_pos, int end_pos)
		{
			var oldSelection = TextEditorData.MainSelection;
			TextEditorData.SetSelection (start_pos, end_pos);
			ClipboardActions.Copy (TextEditorData);
			TextEditorData.MainSelection = oldSelection;
		}

		void EditableTextImplementor.CutText (int start_pos, int end_pos)
		{
			var oldSelection = TextEditorData.MainSelection;
			TextEditorData.SetSelection (start_pos, end_pos);
			ClipboardActions.Cut (TextEditorData);
			TextEditorData.MainSelection = oldSelection;
		}

		void EditableTextImplementor.DeleteText (int start_pos, int end_pos)
		{
			editor.Remove (start_pos, end_pos - start_pos);
		}

		void EditableTextImplementor.PasteText (int position)
		{
			editor.Caret.Offset = position;
			ClipboardActions.Paste (TextEditorData);
		}
		
		string EditableTextImplementor.TextContents {
			set {
				Document.Text = value;
			}
		}
		#endregion
		internal sealed class Factory : Atk.ObjectFactory
		{
			public static void Init (object editor)
			{
				Atk.Global.DefaultRegistry.SetFactoryType ((GLib.GType)editor.GetType (), (GLib.GType)typeof(Factory));
			}

			protected override Atk.Object OnCreateAccessible (GLib.Object obj)
			{
				// seems to be never get called ?
				return new TextEditorAccessible ((TextEditor) obj);
			}

			protected override GLib.GType OnGetAccessibleType ()
			{
				return TextEditorAccessible.GType;
			}
		}
	}

	public class BaseWidgetAccessible : Gtk.Accessible, Atk.ComponentImplementor
	{
		private Gtk.Widget widget;
		private uint focus_id = 0;
		private Dictionary<uint, Atk.FocusHandler> focus_handlers = new Dictionary<uint, Atk.FocusHandler> ();

		public BaseWidgetAccessible (Gtk.Widget widget)
		{
			this.widget = widget;
			widget.SizeAllocated += OnAllocated;
			widget.Mapped += OnMap;
			widget.Unmapped += OnMap;
			widget.FocusInEvent += OnFocus;
			widget.FocusOutEvent += OnFocus;
			widget.AddNotification ("sensitive", (o, a) => NotifyStateChange (StateType.Sensitive, widget.Sensitive));
			widget.AddNotification ("visible", (o, a) => NotifyStateChange (StateType.Visible, widget.Visible));
		}

		public virtual new Atk.Layer Layer {
			get { return Layer.Widget; }
		}

		protected override Atk.StateSet OnRefStateSet ()
		{
			var s = base.OnRefStateSet ();
			
			AddStateIf (s, widget.CanFocus, StateType.Focusable);
			AddStateIf (s, widget.HasFocus, StateType.Focused);
			AddStateIf (s, widget.Sensitive, StateType.Sensitive);
			AddStateIf (s, widget.Sensitive, StateType.Enabled);
			AddStateIf (s, widget.HasDefault, StateType.Default);
			AddStateIf (s, widget.Visible, StateType.Visible);
			AddStateIf (s, widget.Visible && widget.IsMapped, StateType.Showing);
			
			return s;
		}

		private static void AddStateIf (StateSet s, bool condition, StateType t)
		{
			if (condition) {
				s.AddState (t);
			}
		}

		private void OnFocus (object o, EventArgs args)
		{
			NotifyStateChange (StateType.Focused, widget.HasFocus);
			var handler = FocusChanged;
			if (handler != null) {
				handler (this, widget.HasFocus);
			}
		}

		private void OnMap (object o, EventArgs args)
		{
			NotifyStateChange (StateType.Showing, widget.Visible && widget.IsMapped);
		}

		private void OnAllocated (object o, EventArgs args)
		{
			var a = widget.Allocation;
			var bounds = new Atk.Rectangle { X = a.X, Y = a.Y, Width = a.Width, Height = a.Height };
			GLib.Signal.Emit (this, "bounds_changed", bounds);
		}
			/*var handler = BoundsChanged;
            if (handler != null) {
                handler (this, new BoundsChangedArgs () { Args = new object [] { bounds } });
            }*/			
		
				private event FocusHandler FocusChanged;

		#region Atk.Component

		public uint AddFocusHandler (Atk.FocusHandler handler)
		{
			if (!focus_handlers.ContainsValue (handler)) {
				FocusChanged += handler;
				focus_handlers[++focus_id] = handler;
				return focus_id;
			}
			return 0;
		}

		public bool Contains (int x, int y, Atk.CoordType coordType)
		{
			int x_extents, y_extents, w, h;
			GetExtents (out x_extents, out y_extents, out w, out h, coordType);
			Gdk.Rectangle extents = new Gdk.Rectangle (x_extents, y_extents, w, h);
			return extents.Contains (x, y);
		}

		public virtual Atk.Object RefAccessibleAtPoint (int x, int y, Atk.CoordType coordType)
		{
			return new NoOpObject (widget);
		}

		public void GetExtents (out int x, out int y, out int w, out int h, Atk.CoordType coordType)
		{
			w = widget.Allocation.Width;
			h = widget.Allocation.Height;
			
			GetPosition (out x, out y, coordType);
		}

		public void GetPosition (out int x, out int y, Atk.CoordType coordType)
		{
			Gdk.Window window = null;
			
			if (!widget.IsDrawable) {
				x = y = Int32.MinValue;
				return;
			}
			
			if (widget.Parent != null) {
				x = widget.Allocation.X;
				y = widget.Allocation.Y;
				window = widget.ParentWindow;
			} else {
				x = 0;
				y = 0;
				window = widget.GdkWindow;
			}
			
			int x_window, y_window;
			window.GetOrigin (out x_window, out y_window);
			x += x_window;
			y += y_window;
			
			if (coordType == Atk.CoordType.Window) {
				window = widget.GdkWindow.Toplevel;
				int x_toplevel, y_toplevel;
				window.GetOrigin (out x_toplevel, out y_toplevel);
				
				x -= x_toplevel;
				y -= y_toplevel;
			}
		}

		public void GetSize (out int w, out int h)
		{
			w = widget.Allocation.Width;
			h = widget.Allocation.Height;
		}

		public bool GrabFocus ()
		{
			if (!widget.CanFocus) {
				return false;
			}
			
			widget.GrabFocus ();
			
			var toplevel_window = widget.Toplevel as Gtk.Window;
			if (toplevel_window != null) {
				toplevel_window.Present ();
			}
			
			return true;
		}

		public void RemoveFocusHandler (uint handlerId)
		{
			if (focus_handlers.ContainsKey (handlerId)) {
				FocusChanged -= focus_handlers[handlerId];
				focus_handlers.Remove (handlerId);
			}
		}

		public bool SetExtents (int x, int y, int w, int h, Atk.CoordType coordType)
		{
			return SetSizeAndPosition (x, y, w, h, coordType, true);
		}

		public bool SetPosition (int x, int y, Atk.CoordType coordType)
		{
			return SetSizeAndPosition (x, y, 0, 0, coordType, false);
		}

		private bool SetSizeAndPosition (int x, int y, int w, int h, Atk.CoordType coordType, bool setSize)
		{
			if (!widget.IsTopLevel) {
				return false;
			}
			
			if (coordType == CoordType.Window) {
				int x_off, y_off;
				widget.GdkWindow.GetOrigin (out x_off, out y_off);
				x += x_off;
				y += y_off;
				
				if (x < 0 || y < 0) {
					return false;
				}
			}
			
			#pragma warning disable 0612
			widget.SetUposition (x, y);
			#pragma warning restore 0612
			
			if (setSize) {
				widget.SetSizeRequest (w, h);
			}
			
			return true;
		}

		public bool SetSize (int w, int h)
		{
			if (widget.IsTopLevel) {
				widget.SetSizeRequest (w, h);
				return true;
			} else {
				return false;
			}
		}

		public double Alpha {
			get { return 1.0; }
		}
		
		#endregion Atk.Component
		
	}

}
#endif
