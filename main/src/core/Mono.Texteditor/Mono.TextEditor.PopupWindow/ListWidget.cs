// 
// ListWidget.cs
//  
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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

using Gtk;
using Gdk;
using Pango;
using System;
using System.Text;

namespace Mono.TextEditor.PopupWindow
{
	class ListWidget<T> : Gtk.DrawingArea
	{
		int margin = 0;
		int padding = 4;
		int listWidth = 300;
		
		Pango.Layout layout;
		ListWindow<T> win;
		int selection = 0;
		int page = 0;
		int visibleRows = -1;
		int rowHeight;
		bool buttonPressed;
		bool disableSelection;

		public event EventHandler SelectionChanged;
				
		public ListWidget (ListWindow<T> win)
		{
			this.win = win;
			this.Events = EventMask.ButtonPressMask | EventMask.ButtonReleaseMask | EventMask.PointerMotionMask; 
		}
		
		public void Reset ()
		{
			if (win.DataProvider == null) {
				selection = -1;
				return;
			}
			
			if (win.DataProvider.Count == 0)
				selection = -1;
			else
				selection = 0;

			page = 0;
			disableSelection = false;
			if (IsRealized) {
				UpdateStyle ();
				QueueDraw ();
			}
			if (SelectionChanged != null) SelectionChanged (this, EventArgs.Empty);
		}
		
		public int Selection
		{
			get {
				return selection;
			}
			
			set {
				if (value < 0)
					value = 0;
				if (value >= win.DataProvider.Count)
					value = win.DataProvider.Count - 1;
				
				if (value != selection) 
				{
					selection = value;
					UpdatePage ();
					
					if (SelectionChanged != null)
						SelectionChanged (this, EventArgs.Empty);
				}
				
				if (disableSelection)
					disableSelection = false;

				this.QueueDraw ();
			}
		}
		
		void UpdatePage ()
		{
			if (!IsRealized) {
				page = 0;
				return;
			}
			
			if (selection < page || selection >= page + VisibleRows) {
				page = selection - (VisibleRows / 2);
				if (page < 0) page = 0;
			}
		}
		
		public bool SelectionDisabled
		{
			get { return disableSelection; }
			
			set {
				disableSelection = value; 
				this.QueueDraw ();
			}
		}
		
		public int Page
		{
			get { 
				return page; 
			}
			
			set {
				page = value;
				this.QueueDraw ();
			}
		}
		
		protected override bool OnButtonPressEvent (EventButton e)
		{
			Selection = GetRowByPosition ((int) e.Y);
			buttonPressed = true;
			return base.OnButtonPressEvent (e);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton e)
		{
			buttonPressed = false;
			return base.OnButtonReleaseEvent (e);
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion e)
		{
			if (!buttonPressed)
				return base.OnMotionNotifyEvent (e);
			
			int winWidth, winHeight;
			this.GdkWindow.GetSize (out winWidth, out winHeight);
			
	/*		int ypos = (int) e.Y;
			if (ypos < 0) {
			}
			else if (ypos >= winHeight) {
			}
			else
	*/			Selection = GetRowByPosition ((int) e.Y);
			
			return true;
		}

		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			base.OnExposeEvent (args);
			DrawList ();
	  		return true;
		}
		
		public int TextOffset {
			get {
				int iconWidth, iconHeight;
				if (!Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out iconWidth, out iconHeight)) {
					iconHeight = iconWidth = 24;
				}
				return iconWidth + margin + padding + 2;
			}
		}

		void DrawList ()
		{
			int winWidth, winHeight;
			this.GdkWindow.GetSize (out winWidth, out winHeight);
			
			int ypos = margin;
			int lineWidth = winWidth - margin*2;
			int xpos = margin + padding;
				
			int n = 0;
			while (ypos < winHeight - margin && (page + n) < win.DataProvider.Count)
			{
				bool hasMarkup = false;
				IMarkupListDataProvider<T> markupListDataProvider = win.DataProvider as IMarkupListDataProvider<T>;
				if (markupListDataProvider != null) {
					if (markupListDataProvider.HasMarkup (page + n)) {
						layout.SetMarkup (markupListDataProvider.GetMarkup (page + n) ?? "&lt;null&gt;");
						hasMarkup = true;
					}
				}
				
				if (!hasMarkup)
					layout.SetText (win.DataProvider.GetText (page + n) ?? "<null>");
				
				Gdk.Pixbuf icon = win.DataProvider.GetIcon (page + n);
				int iconHeight, iconWidth;
				
				if (icon != null) {
					iconWidth = icon.Width;
					iconHeight = icon.Height;
				} else if (!Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out iconWidth, out iconHeight)) {
					iconHeight = iconWidth = 24;
				}
				
				int wi, he, typos, iypos;
				layout.GetPixelSize (out wi, out he);
				typos = he < rowHeight ? ypos + (rowHeight - he) / 2 : ypos;
				iypos = iconHeight < rowHeight ? ypos + (rowHeight - iconHeight) / 2 : ypos;
				
				if (page + n == selection) {
					if (!disableSelection) {
						this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Selected),
						                              true, margin, ypos, lineWidth, he + padding);
						this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Selected),
							                           xpos + iconWidth + 2, typos, layout);
					}
					else {
						this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Selected),
						                              false, margin, ypos, lineWidth, he + padding);
						this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), 
						                           xpos + iconWidth + 2, typos, layout);
					}
				}
				else
					this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal),
					                           xpos + iconWidth + 2, typos, layout);
				
				if (icon != null)
					this.GdkWindow.DrawPixbuf (this.Style.ForegroundGC (StateType.Normal), icon, 0, 0,
					                           xpos, iypos, iconWidth, iconHeight, Gdk.RgbDither.None, 0, 0);
				
				ypos += rowHeight;
				n++;
				
				//reset the markup or it carries over to the next SetText
				if (hasMarkup)
					layout.SetMarkup (string.Empty);
			}
		}
		
		int GetRowByPosition (int ypos)
		{
			if (visibleRows == -1) CalcVisibleRows ();
			return page + (ypos-margin) / rowHeight;
		}
		
		public Gdk.Rectangle GetRowArea (int row)
		{
			row -= page;
			int winWidth, winHeight;
			this.GdkWindow.GetSize (out winWidth, out winHeight);
			
			return new Gdk.Rectangle (margin, margin + rowHeight * row, winWidth, rowHeight);
		}
		
		public int VisibleRows
		{
			get {
				if (visibleRows == -1) CalcVisibleRows ();
				return visibleRows;
			}
		}
		
		void CalcVisibleRows ()
		{
			int winHeight = 200;
			int lvWidth, lvHeight;
			int rowWidth;
			
			this.GetSizeRequest (out lvWidth, out lvHeight);

			layout.GetPixelSize (out rowWidth, out rowHeight);
			rowHeight += padding;
			visibleRows = (winHeight + padding - margin * 2) / rowHeight;
			
			int newHeight;

			if (this.win.DataProvider.Count > this.visibleRows)
				newHeight = (rowHeight * visibleRows) + margin * 2;
			else
				newHeight = (rowHeight * this.win.DataProvider.Count) + margin * 2;
			
			if (lvWidth != listWidth || lvHeight != newHeight)
				this.SetSizeRequest (listWidth, newHeight);
		} 

		protected override void OnRealized ()
		{
			base.OnRealized ();
			UpdateStyle ();
			UpdatePage ();
		}
		
		void UpdateStyle ()
		{
			this.GdkWindow.Background = this.Style.Base (StateType.Normal);
			layout = new Pango.Layout (this.PangoContext);
			layout.Wrap = Pango.WrapMode.Char;
			
			FontDescription des = this.Style.FontDescription.Copy();
			layout.FontDescription = des;
			CalcVisibleRows ();
		}
	}
}
