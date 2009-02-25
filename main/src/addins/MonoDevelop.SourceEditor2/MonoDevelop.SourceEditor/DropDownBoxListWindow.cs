//
// DropDownBoxListWindow.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Gtk;

namespace MonoDevelop.SourceEditor
{
	public class DropDownBoxListWindow : Window
	{
		HBox hBox;
		VScrollbar vScrollbar;
		ListWidget list;
		DropDownBox parent;
		
		public IListDataProvider DataProvider {
			get {
				return parent.DataProvider;
			}
		}
		
		public DropDownBoxListWindow (DropDownBox parent) : base (Gtk.WindowType.Popup)
		{
			this.parent = parent;
			this.TypeHint = Gdk.WindowTypeHint.Menu;
			this.BorderWidth = 1;
			
			hBox = new HBox ();
			list = new ListWidget (this);
			list.SelectItem += delegate {
				parent.SetItem (list.Selection);
				Destroy ();
			};
			
			list.ScrollEvent += delegate(object o, ScrollEventArgs args) {
				if (args.Event.Direction == Gdk.ScrollDirection.Up) {
					vScrollbar.Value--;
				} else if (args.Event.Direction == Gdk.ScrollDirection.Down) {
					vScrollbar.Value++;
				}
			};
			hBox.PackStart (list, true, true, 0);
			
			vScrollbar = new VScrollbar (null);
			vScrollbar.ValueChanged += delegate {
				list.Page = (int)vScrollbar.Value;
			};
			
			hBox.PackStart (vScrollbar, false, false, 0);
			Add (hBox);
			
			ResetSizes ();
		}
		public bool ProcessKey (Gdk.Key key, Gdk.ModifierType modifier)
		{
			switch (key) {
				case Gdk.Key.Up:
					if (list.SelectionDisabled)
						list.SelectionDisabled = false;
					else
						list.Selection --;
					vScrollbar.Value = list.Page;
					return true;
					
				case Gdk.Key.Down:
					if (list.SelectionDisabled)
						list.SelectionDisabled = false;
					else
						list.Selection ++;
					vScrollbar.Value = list.Page;
					return true;
					
				case Gdk.Key.Page_Up:
					list.Selection -= list.VisibleRows - 1;
					vScrollbar.Value = list.Page;
					return true;
					
				case Gdk.Key.Page_Down:
					list.Selection += list.VisibleRows - 1;
					vScrollbar.Value = list.Page;
					return true;
				
				case Gdk.Key.Home:
					vScrollbar.Value = list.Selection = (int)vScrollbar.Adjustment.Lower;
					return true;
				
				case Gdk.Key.End:
					vScrollbar.Value = (int)vScrollbar.Adjustment.Upper;
					list.Selection = DataProvider.IconCount;
					return true;
								
				case Gdk.Key.Return:
				case Gdk.Key.ISO_Enter:
				case Gdk.Key.Key_3270_Enter:
				case Gdk.Key.KP_Enter:
					list.OnSelectItem (EventArgs.Empty);
					return true;
			}
			
			return false;
		}
		
		internal void ResetSizes ()
		{
			vScrollbar.Adjustment.Lower = 0;
			vScrollbar.Adjustment.Upper = Math.Max(0, DataProvider.IconCount - list.VisibleRows);
			vScrollbar.Adjustment.PageIncrement = list.VisibleRows - 1;
			vScrollbar.Adjustment.StepIncrement = 1;
			
			if (list.VisibleRows >= DataProvider.IconCount)
				hBox.Remove (vScrollbar);
				//vScrollbar.Hide ();
			
			this.Resize (this.Allocation.Width, this.list.HeightRequest + 2);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose args)
		{
			bool result = base.OnExposeEvent (args);
			args.Window.DrawRectangle (Style.MidGC (Gtk.StateType.Normal), false, 0, 0, this.Allocation.Width - 1, this.Allocation.Height - 1);
			return result;
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			Destroy ();
			return base.OnFocusOutEvent (evnt);
		}
		
		internal class ListWidget: Gtk.DrawingArea
		{
			int margin = 0;
			int padding = 4;
			int listWidth = 300;
			
			Pango.Layout layout;
			DropDownBoxListWindow win;
			int selection = 0;
			int page = 0;
			int visibleRows = -1;
			int rowHeight;
		//	bool buttonPressed;
			bool disableSelection;
	
			public event EventHandler SelectionChanged;
					
			public ListWidget (DropDownBoxListWindow win)
			{
				this.win = win;
				this.Events = Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask;
				layout = new Pango.Layout (this.PangoContext);
			}
			
			protected override void OnDestroyed ()
			{
				if (layout != null) {
					layout.Dispose ();
					layout = null;
				}
				base.OnDestroyed ();
			}
			
			public void Reset ()
			{
				if (win.DataProvider == null) {
					selection = -1;
					return;
				}
				
				if (win.DataProvider.IconCount == 0)
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
					if (value >= win.DataProvider.IconCount)
						value = win.DataProvider.IconCount - 1;
					
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
			
			protected override bool OnButtonPressEvent (Gdk.EventButton e)
			{
				Selection = GetRowByPosition ((int) e.Y);
				if (e.Type == Gdk.EventType.ButtonPress) {
					OnSelectItem (EventArgs.Empty);
					Destroy ();
				}
				//buttonPressed = true;
				return base.OnButtonPressEvent (e);
			}
			
			protected override bool OnButtonReleaseEvent (Gdk.EventButton e)
			{
				//buttonPressed = false;
				return base.OnButtonReleaseEvent (e);
			}
			
			protected override bool OnMotionNotifyEvent (Gdk.EventMotion e)
			{
				int winWidth, winHeight;
				this.GdkWindow.GetSize (out winWidth, out winHeight);
				
				Selection = GetRowByPosition ((int) e.Y);
				
				return base.OnMotionNotifyEvent (e);
			}
	
			protected override bool OnExposeEvent (Gdk.EventExpose args)
			{
				base.OnExposeEvent (args);
				DrawList ();
		  		return true;
			}
	
			void DrawList ()
			{
				int winWidth, winHeight;
				this.GdkWindow.GetSize (out winWidth, out winHeight);
				
				int ypos = margin;
				int lineWidth = winWidth - margin*2;
				int xpos = margin + padding;
					
				int n = 0;
				while (ypos < winHeight - margin && (page + n) < win.DataProvider.IconCount) {
					layout.SetText (win.DataProvider.GetText (page + n) ?? "<null>");
					
					Gdk.Pixbuf icon = win.DataProvider.GetIcon (page + n);
					int iconHeight = icon != null? icon.Height : 24;
					int iconWidth = icon != null? icon.Width : 24;
					
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
					layout.SetMarkup (string.Empty);
				}
			}
			
			int GetRowByPosition (int ypos)
			{
				if (visibleRows == -1)
					CalcVisibleRows ();
				return page + (ypos-margin) / rowHeight;
			}
			
			public Gdk.Rectangle GetRowArea (int row)
			{
				row -= page;
				int winWidth, winHeight;
				this.GdkWindow.GetSize (out winWidth, out winHeight);
				
				return new Gdk.Rectangle (margin, margin + rowHeight * row, winWidth, rowHeight);
			}
			
			public int VisibleRows {
				get {
					if (visibleRows == -1)
						CalcVisibleRows ();
					return visibleRows;
				}
			}
			
			void CalcVisibleRows ()
			{
				int winHeight = 200;
				int lvWidth, lvHeight;
				int rowWidth;
				
				this.GetSizeRequest (out lvWidth, out lvHeight);
				if (layout == null)
					return;
				layout.GetPixelSize (out rowWidth, out rowHeight);
				rowHeight += padding;
				visibleRows = (winHeight + padding - margin * 2) / rowHeight;
				int newHeight;
	
				if (this.win.DataProvider.IconCount > this.visibleRows)
					newHeight = (rowHeight * visibleRows) + margin * 2;
				else
					newHeight = (rowHeight * this.win.DataProvider.IconCount) + margin * 2;
				
				if (lvWidth != listWidth || lvHeight != newHeight) {
					this.SetSizeRequest (listWidth, newHeight);
				}
				
				visibleRows = (newHeight + padding - margin * 2) / rowHeight;
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
				
				Pango.FontDescription des = this.Style.FontDescription.Copy();
				layout.FontDescription = des;
				CalcVisibleRows ();
			}
			internal virtual void OnSelectItem (EventArgs e)
			{
				if (SelectItem != null)
					SelectItem (this, e);
			}
			public event EventHandler SelectItem;
		}
		
		public interface IListDataProvider
		{
			int IconCount {
				get;
			}
			void Reset ();
			string GetText (int n);
			Gdk.Pixbuf GetIcon (int n);
			object GetTag (int n);
		}
	}
}
