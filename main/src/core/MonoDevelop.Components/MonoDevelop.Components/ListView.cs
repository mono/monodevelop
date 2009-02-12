// ListView.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using Gtk;
using Gdk;
using Pango;
using System.ComponentModel;

namespace MonoDevelop.Components
{
	[ToolboxItem (true)]
	public class ListView: Gtk.DrawingArea
	{
		int margin = 0;
		int padding = 4;
//		int listWidth = 300;
		
		Pango.Layout layout;
		int selection = 0;
		int page = 0;
		int visibleRows = -1;
		int rowHeight;
		bool buttonPressed;
		bool disableSelection;
		int viewWidth;

		const int IconWidth = 24;
		const int ColumnGap = 2;

		IListViewDataSource dataProvider;

		public event EventHandler SelectionChanged;
		public event EventHandler ItemActivated;
				
		public ListView ()
		{
			base.CanFocus = true;
			
			this.Events = EventMask.ButtonPressMask | 
				EventMask.ButtonReleaseMask | 
				EventMask.PointerMotionMask |
				EventMask.ScrollMask;
			
			layout = new Pango.Layout (this.PangoContext);
			layout.Wrap = Pango.WrapMode.Char;
			
			FontDescription des = this.Style.FontDescription.Copy();
			layout.FontDescription = des;
		}
		
		public void Reset ()
		{
			if (dataProvider == null) {
				selection = -1;
				return;
			}
			
			if (dataProvider.ItemCount == 0)
				selection = -1;
			else
				selection = 0;

			page = 0;
			disableSelection = false;
			if (IsRealized) {
				UpdateStyle ();
				QueueDraw ();
			}
			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
		}

		public void Refresh ()
		{
			if (Selection > RowCount)
				Selection = RowCount - 1;
			UpdatePage (false, true);
			QueueDraw ();
		}
		
		public int Selection
		{
			get {
				return selection;
			}
			
			set {
				if (value < 0)
					value = 0;
				if (value >= RowCount)
					value = RowCount - 1;
				
				if (value != selection) 
				{
					selection = value;
					UpdatePage (false, true);
					
					if (SelectionChanged != null)
						SelectionChanged (this, EventArgs.Empty);
				}
				
				if (disableSelection)
					disableSelection = false;

				this.QueueDraw ();
			}
		}

		public void CenterViewToSelection ()
		{
			if (vAdjustement != null) {
				int val = Selection - VisibleRows / 2;
				if (val + VisibleRows >= RowCount)
					val = RowCount - VisibleRows;
				vAdjustement.Value = val > 0 ? val : 0;
			}
		}

		int RowCount {
			get { return dataProvider != null ? dataProvider.ItemCount : 0; }
		}
		
		void UpdatePage (bool centerRow, bool keepSelectionInView)
		{
			if (!IsRealized) {
				page = 0;
				return;
			}

			CalcVisibleRows ();
			
			if (selection < page || selection >= page + VisibleRows) {
				if (centerRow) {
					page = selection - (VisibleRows / 2);
				} else if (keepSelectionInView) {
					if (selection < page)
						page = selection;
					else
						page = selection - VisibleRows + 1;
				}
				if (page < 0) page = 0;
			}
			
			UpdateAdjustments ();
			
			if (vAdjustement != null)
				vAdjustement.Value = page;
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

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			UpdatePage (false, true);
		}


		Gtk.Adjustment hAdjustement;
		Gtk.Adjustment vAdjustement;
		
		protected override void OnSetScrollAdjustments (Gtk.Adjustment hadj, Gtk.Adjustment vadj)
		{
			if (hAdjustement != null)
				hAdjustement.ValueChanged -= HandleHorValueChanged; 
			if (vAdjustement!= null)
				vAdjustement.ValueChanged -= HandleVerValueChanged;
			
			hAdjustement = hadj;
			vAdjustement = vadj;
			
			if (hAdjustement == null || vAdjustement == null)
				return;

			UpdateAdjustments ();
			
			hAdjustement.ValueChanged += HandleHorValueChanged;
			vAdjustement.ValueChanged += HandleVerValueChanged;
		}

		void HandleVerValueChanged (object sender, EventArgs e)
		{
			page = (int) vAdjustement.Value;
			UpdatePage (false, false);
			QueueDraw ();
		}

		void HandleHorValueChanged(object sender, EventArgs e)
		{
			QueueDraw ();
		}

		void UpdateAdjustments ()
		{
			if (hAdjustement == null)
				return;

			vAdjustement.SetBounds (0, dataProvider.ItemCount, 1, VisibleRows, VisibleRows);
			hAdjustement.SetBounds (0, viewWidth, 1, Allocation.Width, Allocation.Width);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (ProcessKey (evnt.Key, evnt.State))
				return true;
			else
				return base.OnKeyPressEvent (evnt);
		}


		public bool ProcessKey (Gdk.Key key, Gdk.ModifierType modifier)
		{
			switch (key)
			{
				case Gdk.Key.Up:
					if (SelectionDisabled)
						SelectionDisabled = false;
					else
						Selection --;
					return true;
					
				case Gdk.Key.Down:
					if (SelectionDisabled)
						SelectionDisabled = false;
					else
						Selection ++;
					return true;
					
				case Gdk.Key.Page_Up:
					Selection -= VisibleRows - 1;
					return true;
					
				case Gdk.Key.Page_Down:
					Selection += VisibleRows - 1;
					return true;
					
				case Gdk.Key.Left:
					//if (curPos == 0) return KeyAction.CloseWindow | KeyAction.Process;
					//curPos--;
					return true;
					
				case Gdk.Key.Right:
					//if (curPos == word.Length) return KeyAction.CloseWindow | KeyAction.Process;
					//curPos++;
					return true;
				
				case Gdk.Key.Home:
					Selection = 0;
					return true;
					
				case Gdk.Key.End:
					if (dataProvider != null)
						Selection = RowCount - 1;
					return true;
			}
			return false;
		}		
		
		protected override bool OnButtonPressEvent (EventButton e)
		{
			GrabFocus ();
			Selection = GetRowByPosition ((int) e.Y);
			buttonPressed = true;
			if (e.Type == EventType.TwoButtonPress) {
				if (ItemActivated != null)
					ItemActivated (this, EventArgs.Empty);
			}
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
			
			Selection = GetRowByPosition ((int) e.Y);
			
			return true;
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
			int xpos = margin + padding - (int) hAdjustement.Value;

			int n = 0;
			while (ypos < winHeight - margin && (page + n) < RowCount)
			{
				bool hasMarkup = SetLayoutText (page + n);
				
				Gdk.Pixbuf icon = dataProvider.GetIcon (page + n);
				int iconHeight = icon != null? icon.Height : IconWidth;
				int iconWidth = icon != null? icon.Width : IconWidth;
				
				int wi, he, typos, iypos;
				layout.GetPixelSize (out wi, out he);
				typos = he < rowHeight ? ypos + (rowHeight - he) / 2 : ypos;
				iypos = iconHeight < rowHeight ? ypos + (rowHeight - iconHeight) / 2 : ypos;
				
				if (page + n == selection) {
					if (!disableSelection) {
						this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Selected),
						                              true, margin, ypos, lineWidth, he + padding);
						this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Selected),
							                           xpos + iconWidth + ColumnGap, typos, layout);
					}
					else {
						this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Selected),
						                              false, margin, ypos, lineWidth, he + padding);
						this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), 
						                           xpos + iconWidth + ColumnGap, typos, layout);
					}
				}
				else
					this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal),
					                           xpos + iconWidth + ColumnGap, typos, layout);
				
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

		bool SetLayoutText (int n)
		{
			bool hasMarkup = dataProvider.UseMarkup (n);

			string text = dataProvider.GetText (n);
			if (hasMarkup)
				layout.SetMarkup (text ?? "&lt;null&gt;");
			else
				layout.SetText (text ?? "<null>");
			return hasMarkup;
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

		public IListViewDataSource DataSource {
			get {
				return dataProvider;
			}
			set {
				dataProvider = value;
				Refresh ();
			}
		}
		
		void CalcVisibleRows ()
		{
//			int winHeight = 200;
			int rowWidth;

//			int lvWidth = Allocation.Width;
			int lvHeight = Allocation.Height;

			layout.GetPixelSize (out rowWidth, out rowHeight);
			rowHeight += padding;
			visibleRows = (lvHeight + padding - margin * 2) / rowHeight;
			
//			int newHeight;
//			int count = RowCount;

//			if (count > this.visibleRows)
//				newHeight = (rowHeight * visibleRows) + margin * 2;
//			else
//				newHeight = (rowHeight * count) + margin * 2;

			if (dataProvider != null) {
				int maxr = Math.Min (page + visibleRows, dataProvider.ItemCount);
				for (int n=page; n<maxr; n++) {
					SetLayoutText (n);
					int rh, rw;
					layout.GetPixelSize (out rw, out rh);
					rw += (margin + padding) * 2 + IconWidth + ColumnGap;
					if (rw > viewWidth)
						viewWidth = rw;
				}
			}
		} 

		protected override void OnRealized ()
		{
			base.OnRealized ();
			UpdateStyle ();
			UpdatePage (false, true);
		}
		
		void UpdateStyle ()
		{
			this.GdkWindow.Background = this.Style.Base (StateType.Normal);
			CalcVisibleRows ();
		}
	}

	public interface IListViewDataSource
	{
		int ItemCount { get; }
		string GetText (int n);
		bool UseMarkup (int n);
		Gdk.Pixbuf GetIcon (int n);
	}
}