//// 
//// DebugValueMarker.cs
////  
//// Author:
////       Mike Krüger <mkrueger@novell.com>
//// 
//// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
//// 
//// Permission is hereby granted, free of charge, to any person obtaining a copy
//// of this software and associated documentation files (the "Software"), to deal
//// in the Software without restriction, including without limitation the rights
//// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//// copies of the Software, and to permit persons to whom the Software is
//// furnished to do so, subject to the following conditions:
//// 
//// The above copyright notice and this permission notice shall be included in
//// all copies or substantial portions of the Software.
//// 
//// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//// THE SOFTWARE.
//
//using System;
//using System.Linq;
//using Mono.TextEditor;
//using MonoDevelop.Debugger;
//using MonoDevelop.Ide.Tasks;
//using System.Collections.Generic;
//using Mono.Debugging.Client;
// 
//using MonoDevelop.Core;
//
//namespace MonoDevelop.SourceEditor
//{
//	public class DebugValueMarker : TextMarker, IActionTextMarker, IDisposable
//	{
//		TextEditor editor;
//		LineSegment lineSegment;
//		List<ObjectValue> objectValues = new List<ObjectValue> ();
//		PinnedWatch watch;
//		
//		public bool HasValue (ObjectValue val)
//		{
//			return objectValues.Any (v => v.Name == val.Name);
//		}
//		
//		public void AddValue (ObjectValue val)
//		{
//			objectValues.Add (val);
//		}
//		
//		public DebugValueMarker (TextEditor editor, LineSegment lineSegment, PinnedWatch watch)
//		{
//			this.watch = watch;
//			this.editor = editor;
//			this.lineSegment = lineSegment;
//			editor.TextViewMargin.HoveredLineChanged += HandleEditorTextViewMarginHoveredLineChanged;
//		}
//		
//		public void Clear ()
//		{
//			objectValues.Clear ();
//		}
//
//		void HandleEditorTextViewMarginHoveredLineChanged (object sender, LineEventArgs e)
//		{
//			if (e.Line == lineSegment || editor.TextViewMargin.HoveredLine == lineSegment) {
//				editor.Document.CommitLineUpdate (lineSegment);
//			}
//		}
//		
//		
//		public override void Draw (TextEditor editor, Gdk.Drawable win, Pango.Layout layout, bool selected, int startOffset, int endOffset, int y, int startXPos, int endXPos)
//		{
//			LineSegment line = editor.Document.GetLineByOffset (startOffset);
//			int lineHeight = editor.GetLineHeight (line) - 1;
//			
//			int width, height;
//			layout.GetPixelSize (out width, out height);
//			startXPos += width + 4;
//			
//			foreach (ObjectValue val in objectValues) {
//				startXPos = DrawObjectValue (y, lineHeight, startXPos, win, editor, val) + 2;
//			}
//		}
//		
//		static string GetString (ObjectValue val)
//		{
//			if (val.IsUnknown) 
//				return GettextCatalog.GetString ("The name '{0}' does not exist in the current context.", val.Name);
//			if (val.IsError) 
//				return val.Value;
//			if (val.IsNotSupported) 
//				return val.Value;
//			if (val.IsError) 
//				return val.Value;
//			if (val.IsEvaluating) 
//				return GettextCatalog.GetString ("Evaluating...");
//			return val.DisplayValue ?? "(null)";
//		}
//		
//		int MeasureObjectValue (int y, int lineHeight, Pango.Layout layout, int startXPos, TextEditor editor, ObjectValue val)
//		{
//			int width, height;
//			int xPos = startXPos;
//			
//			Pango.Layout nameLayout = new Pango.Layout (editor.PangoContext);
//			nameLayout.FontDescription = editor.Options.Font;
//			nameLayout.SetText (val.Name);
//			
//			Pango.Layout valueLayout = new Pango.Layout (editor.PangoContext);
//			valueLayout.FontDescription = editor.Options.Font;
//			valueLayout.SetText (GetString (val));
//			
//			Gdk.Pixbuf pixbuf = ImageService.GetPixbuf (ObjectValueTreeView.GetIcon (val.Flags), Gtk.IconSize.Menu);
//			int pW = pixbuf.Width;
//			xPos += 2;
//
//			xPos += pixbuf.Width + 2;
//			nameLayout.GetPixelSize (out width, out height);
//			
//			xPos += width;
//			
//			xPos += 4;
//			
//			valueLayout.GetPixelSize (out width, out height);
//			xPos += width;
//			/*
//			if (editor.TextViewMargin.HoveredLine == lineSegment) {
//				xPos += 4;
//				
//				pixbuf = ImageService.GetPixbuf (Stock.CloseIcon, Gtk.IconSize.Menu);
//				pW = pixbuf.Width;
//				xPos += pW + 2;
//			}*/
//			
//			nameLayout.Dispose ();
//			valueLayout.Dispose ();
//			return xPos;
//		}
//		
//		int DrawObjectValue (int y, int lineHeight, int startXPos, Gdk.Drawable win, TextEditor editor, ObjectValue val)
//		{
//			int y2 = y + lineHeight;
//			
//			int width, height;
//			int xPos = startXPos;
//			int startX = xPos;
//			Gdk.GC lineGc = new Gdk.GC (win);
//			
//			Gdk.Rectangle clipRectangle = new Gdk.Rectangle (editor.TextViewMargin.XOffset, 0, editor.Allocation.Width - editor.TextViewMargin.XOffset, editor.Allocation.Height);
//			lineGc.ClipRectangle = clipRectangle;
//			lineGc.RgbFgColor = editor.ColorStyle.FoldLine.Color;
//			
//			Gdk.GC textGc = new Gdk.GC (win);
//			textGc.ClipRectangle = clipRectangle;
//			textGc.RgbFgColor = editor.ColorStyle.Default.Color;
//			
//			Pango.Layout nameLayout = new Pango.Layout (editor.PangoContext);
//			nameLayout.FontDescription = editor.Options.Font;
//			nameLayout.SetText (val.Name);
//			
//			Pango.Layout valueLayout = new Pango.Layout (editor.PangoContext);
//			valueLayout.FontDescription = editor.Options.Font;
//			valueLayout.SetText (GetString (val));
//			
//			Gdk.Pixbuf pixbuf = ImageService.GetPixbuf (ObjectValueTreeView.GetIcon (val.Flags), Gtk.IconSize.Menu);
//			int pW = pixbuf.Width;
//			int pH = pixbuf.Height;
//			xPos += 2;
//			win.DrawPixbuf (lineGc, pixbuf, 0, 0, xPos, y + 1 + (lineHeight - pH) / 2, pW, pH, Gdk.RgbDither.None, 0, 0 );
//			xPos += pixbuf.Width + 2;
//			nameLayout.GetPixelSize (out width, out height);
//			win.DrawLayout (textGc, xPos, y + (lineHeight - height) / 2, nameLayout);
//			
//			xPos += width;
//			
//			win.DrawLine (lineGc, xPos + 2, y, xPos + 2, y2);
//			xPos += 4;
//			
//			valueLayout.GetPixelSize (out width, out height);
//			win.DrawLayout (textGc, xPos, y  + (lineHeight - height) / 2, valueLayout);
//			xPos += width;
//			
//			xPos += 2;
//			/*
//			if (editor.TextViewMargin.HoveredLine == lineSegment) {
//				win.DrawLine (lineGc, xPos, y, xPos, y2);
//				xPos += 2;
//				pixbuf = ImageService.GetPixbuf ("md-pin-down", Gtk.IconSize.Menu);
//				pW = pixbuf.Width;
//				pH = pixbuf.Height;
//				win.DrawPixbuf (lineGc, pixbuf, 0, 0, xPos, y, pW, pH, Gdk.RgbDither.None, 0, 0 );
//				xPos += pW + 2;
//			}*/
//			
//			win.DrawRectangle (lineGc, false, startX, y, xPos - startX, lineHeight);
//			
//			textGc.Dispose ();
//			lineGc.Dispose ();
//			nameLayout.Dispose ();
//			valueLayout.Dispose ();
//			return xPos;
//		}
//		
//		#region IActionTextMarker implementation
//		int MouseIsOverMarker (TextEditor editor, MarginMouseEventArgs args, out int x, out int y, out int w, out int h)
//		{
//			x = y = w = h = -1;
//			y = editor.LineToVisualY (args.LineNumber) - (int)editor.VAdjustment.Value;
//			h = editor.GetLineHeight (lineSegment);
//			if (args.Y > y + editor.LineHeight)
//				return -1;
//			TextViewMargin.LayoutWrapper layoutWrapper = editor.TextViewMargin.GetLayout (lineSegment);
//			int width, height;
//			layoutWrapper.Layout.GetPixelSize (out width, out height);
//			
//			if (layoutWrapper.IsUncached)
//				layoutWrapper.Dispose ();
//			
//			int startXPos = width;
//			x = (int)(args.X + editor.HAdjustment.Value);
//			
//			for (int i = 0; i < objectValues.Count; i++) {
//				ObjectValue curValue = objectValues[i];
//				int oldX = startXPos;
//				startXPos = MeasureObjectValue (y, 0, layoutWrapper.Layout, startXPos, editor, curValue) + 2;
//				if (x < startXPos && x >= startXPos - 16) {
//					w = startXPos - oldX;
//					return i;
//				}
//			}
//			
//			return -1;
//		}
//		
//		int MouseIsOverBox (TextEditor editor, MarginMouseEventArgs args, out int x, out int y, out int w, out int h)
//		{
//			x = y = w = h = -1;
//			y = editor.LineToVisualY (args.LineNumber) - (int)editor.VAdjustment.Value;
//			h = editor.GetLineHeight (lineSegment);
//			if (args.Y > y + editor.LineHeight)
//				return -1;
//			TextViewMargin.LayoutWrapper layoutWrapper = editor.TextViewMargin.GetLayout (lineSegment);
//			int width, height;
//			layoutWrapper.Layout.GetPixelSize (out width, out height);
//			
//			if (layoutWrapper.IsUncached)
//				layoutWrapper.Dispose ();
//			
//			int startXPos = width;
//			int mouseX = (int)(args.X + editor.HAdjustment.Value);
//			
//			for (int i = 0; i < objectValues.Count; i++) {
//				ObjectValue curValue = objectValues[i];
//				int oldX = startXPos;
//				startXPos = MeasureObjectValue (y, 0, layoutWrapper.Layout, startXPos, editor, curValue) + 2;
//				if (oldX < mouseX && mouseX < startXPos) {
//					x = oldX;
//					w = startXPos - oldX;
//					return i;
//				}
//			}
//			
//			return -1;
//		}
//		
//		public bool MousePressed (TextEditor editor, MarginMouseEventArgs args)
//		{
//			return false;
//		}
//		
////		static Gdk.Cursor arrowCursor = new Gdk.Cursor (Gdk.CursorType.Arrow);
//		public bool MouseHover (TextEditor editor, MarginMouseEventArgs args, ref Gdk.Cursor cursor)
//		{
//			int x, y, w, h;
//			int value = MouseIsOverBox (editor, args, out x, out y, out w, out h);
//			if (value >= 0) {
//				DebugValueWindow window = new DebugValueWindow (editor, lineSegment.Offset, DebuggingService.CurrentFrame, objectValues[value], watch);
//				int ox = 0, oy = 0;
//				editor.GdkWindow.GetOrigin (out ox, out oy);
//				window.Events |= Gdk.EventMask.LeaveNotifyMask; 
//				window.Move (ox + editor.TextViewMargin.XOffset + x, oy + y);
//				window.Resize (w, h);
//				window.LeaveNotifyEvent += delegate {
//					window.Destroy ();
//				};
//				window.ShowAll ();
//				return true;
//			}
//			return false;
//		}
//		
//		#endregion
//		
//		#region IDisposable implementation
//		public void Dispose ()
//		{
//			editor.TextViewMargin.HoveredLineChanged -= HandleEditorTextViewMarginHoveredLineChanged;
//		}
//		#endregion
//	}
//}
//*/
