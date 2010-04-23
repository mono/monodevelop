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
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
		bool allowMultipleSelection;
		List<int> selectedRows = new List<int> ();
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
				selectedRows.Clear ();
				return;
			}
			
			if (dataProvider.ItemCount == 0) {
				selectedRows.Clear ();
			}
			else {
				selectedRows.Clear ();
				selectedRows.Add (0);
			}

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
			// Remove any selections that no longer exist.
			// We need to build a list of 'items to remove', because
			// collections are annoying to remove from.
			List<int> toRemove = new List<int> ();
			foreach (int i in selectedRows) {
				if (i >= RowCount)
					toRemove.Add (i);
			}
			
			foreach (int i in toRemove) {
				selectedRows.Remove (i);
			}
			
			// If there are no selections left, select the last item.
			if (selectedRows.Count == 0)
				SelectedRow = RowCount - 1;
			
			UpdatePage (false, true);
			QueueDraw ();
		}
		
		public bool AllowMultipleSelection
		{
			get {
				return allowMultipleSelection;
			}
			
			set {
				allowMultipleSelection = value;
			}
		}
		
		public int SelectedRow
		{
			get {
				// Just return the most recent selection.
				if (selectedRows.Count > 0)
					return selectedRows[selectedRows.Count - 1];
				else
					return -1;
			}
			
			set {
				// Just set the selection to a specific item.
				if (value < 0)
					value = 0;
				if (value >= RowCount)
					value = RowCount - 1;
				
				if (value != this.SelectedRow || selectedRows.Count > 1) 
				{
					selectedRows.Clear ();
					selectedRows.Add (value);
					
					UpdatePage (false, true);
					
					if (SelectionChanged != null)
						SelectionChanged (this, EventArgs.Empty);
				}
				
				if (disableSelection)
					disableSelection = false;

				this.QueueDraw ();
			}
		}
		
		public ReadOnlyCollection<int> SelectedRows
		{
			get {
				return new ReadOnlyCollection<int> (selectedRows);
			}
		}
		
		public void SelectRow (int index)
		{
			if (!allowMultipleSelection) {
				this.SelectedRow = index;
				return;
			}
			
			if (index < 0 || index >= this.RowCount)
				return;
			
			// Remove it if it already exists.
			selectedRows.Remove (index);
			selectedRows.Add (index);
			
			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
			
			this.QueueDraw ();
		}
		
		public void UnselectRow (int index)
		{
			if (!allowMultipleSelection) {
				if (this.SelectedRow == index) {
					this.SelectedRow = -1;
					return;
				}
			}
			
			if (selectedRows.Remove (index)) {
				if (SelectionChanged != null)
					SelectionChanged (this, EventArgs.Empty);
				this.QueueDraw ();
			}
		}
		
		public void ToggleRowSelection (int index)
		{
			if (selectedRows.Contains (index))
				UnselectRow (index);
			else
				SelectRow (index);
		}
		
		public void ClearSelection()
		{
			selectedRows.Clear();
			this.QueueDraw();
		}

		public void ModifySelection(bool up, bool page, bool addTo)
		{
			// Note that we don't really handle "Shift+PageUp" type selection,
			// because that just gets confusing for the user when there are
			// multiple selection ranges.  Therefore, 'page' only works for
			// non-additive selections.
			
			if (up)
			{
				// See if the most recent selection was one down from the previous.
				// If so, we actually want to turn it off.
				if (addTo && !page) {
					if (selectedRows.Count > 1 && this.SelectedRow == (selectedRows[selectedRows.Count-2] + 1)) {
						UnselectRow (this.SelectedRow);
					}
					else {
						SelectRow (this.SelectedRow - 1);
					}
				}
				else {
					if (page)
						this.SelectedRow -= VisibleRows - 1;
					else
						this.SelectedRow --;
				}
			}
			else
			{
				if (addTo && !page) {
					if (selectedRows.Count > 1 && this.SelectedRow == (selectedRows[selectedRows.Count-2] - 1)) {
						UnselectRow (this.SelectedRow);
					}
					else {
						SelectRow (this.SelectedRow + 1);
					}
				}
				else {
					if (page)
						this.SelectedRow += VisibleRows - 1;
					else
						this.SelectedRow ++;
				}
			}
		}
		
		public void CenterViewToSelection ()
		{
			if (vAdjustement != null) {
				int val = SelectedRow - VisibleRows / 2;
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
			
			if (SelectedRow < page || SelectedRow >= page + VisibleRows) {
				if (centerRow) {
					page = SelectedRow - (VisibleRows / 2);
				} else if (keepSelectionInView) {
					if (SelectedRow < page)
						page = SelectedRow;
					else
						page = SelectedRow - VisibleRows + 1;
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
						ModifySelection (true, false, (modifier & ModifierType.ShiftMask) == ModifierType.ShiftMask);
					return true;
					
				case Gdk.Key.Down:
					if (SelectionDisabled)
						SelectionDisabled = false;
					else
						ModifySelection (false, false, (modifier & ModifierType.ShiftMask) == ModifierType.ShiftMask);
					return true;
					
				case Gdk.Key.Page_Up:
					ModifySelection (true, true, (modifier & ModifierType.ShiftMask) == ModifierType.ShiftMask);
					
					return true;
					
				case Gdk.Key.Page_Down:
					ModifySelection (false, true, (modifier & ModifierType.ShiftMask) == ModifierType.ShiftMask);
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
					SelectedRow = 0;
					return true;
					
				case Gdk.Key.End:
					if (dataProvider != null)
						SelectedRow = RowCount - 1;
					return true;
				
				case Gdk.Key.Return:
					if (ItemActivated != null)
						ItemActivated (this, EventArgs.Empty);
					return true;
			}
			return false;
		}		
		
		protected override bool OnButtonPressEvent (EventButton e)
		{
			GrabFocus ();
			int row = GetRowByPosition ((int) e.Y);
			if ((e.State & ModifierType.ControlMask) == ModifierType.ControlMask)
				ToggleRowSelection (row);
			else
				this.SelectedRow = row;
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
			
			int row = GetRowByPosition ((int) e.Y);
			
			if ((e.State & ModifierType.ControlMask) == ModifierType.ControlMask)
				SelectRow (row);
			else
				this.SelectedRow = row;
			
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
				bool isSelected = selectedRows.Contains (page + n);
				bool hasMarkup = SetLayoutText (page + n, isSelected);
				
				Gdk.Pixbuf icon = dataProvider.GetIcon (page + n);
				int iconHeight = icon != null? icon.Height : IconWidth;
				int iconWidth = icon != null? icon.Width : IconWidth;
				
				int wi, he, typos, iypos;
				layout.GetPixelSize (out wi, out he);
				typos = he < rowHeight ? ypos + (rowHeight - he) / 2 : ypos;
				iypos = iconHeight < rowHeight ? ypos + (rowHeight - iconHeight) / 2 : ypos;
				
				if (isSelected) {
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
					
					// Draw a 'most recent selection' rectangle.
					if (this.SelectedRow == page + n) {
						this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Active),
						                              false, margin, ypos, lineWidth, he + padding - 1);
						this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Active),
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

		bool SetLayoutText (int n, bool isSelected)
		{
			bool hasMarkup = dataProvider.UseMarkup (n);

			string text = isSelected ? dataProvider.GetSelectedText (n) : dataProvider.GetText (n);
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
					SetLayoutText (n, false);
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
		string GetSelectedText (int n);
		bool UseMarkup (int n);
		Gdk.Pixbuf GetIcon (int n);
	}
}