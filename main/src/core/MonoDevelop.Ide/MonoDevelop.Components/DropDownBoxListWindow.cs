// 
// DropDownBoxListWindow.cs
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
using MonoDevelop.Ide;
using Gtk;
using System.Text;

namespace MonoDevelop.Components
{
	public class DropDownBoxListWindow : Window
	{
		readonly ScrolledWindow vScrollbar;
		internal ListWidget list;

		public IListDataProvider DataProvider {
			get;
			private set;
		}

		public int FixedRowHeight {
			get {
				return list.FixedRowHeight;
			}
			set {
				list.FixedRowHeight = value;
				list.CalcRowHeight ();
			}
		}

		public int MaxVisibleRows {
			get {
				return list.MaxVisibleRows;
			}
			set {
				list.MaxVisibleRows = value;
				list.CalcVisibleRows ();
				SetSizeRequest (list.WidthRequest, list.HeightRequest);
			}
		}

		public DropDownBoxListWindow (IListDataProvider provider) : base (WindowType.Popup)
		{
			this.DataProvider = provider;
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.TypeHint = Gdk.WindowTypeHint.Menu;
			this.BorderWidth = 1;
			this.Events |= Gdk.EventMask.KeyPressMask;
			list = new ListWidget (this);
			list.SelectItem += delegate {
				var sel = list.Selection;
				if (sel >= 0 && sel < DataProvider.IconCount) {
					DataProvider.ActivateItem (sel);
					Destroy ();
				}
			};
			SetSizeRequest (list.WidthRequest, list.HeightRequest);
			vScrollbar = new ScrolledWindow ();
			vScrollbar.VScrollbar.SizeAllocated += (o, args) => {
				var minWidth = list.WidthRequest + args.Allocation.Width;
				if (this.Allocation.Width < minWidth)
					SetSizeRequest (minWidth, list.HeightRequest);
			};
			vScrollbar.Child = list;
			var vbox = new VBox ();
			vbox.PackStart (vScrollbar, true, true, 0);
			Add (vbox);
		}

		public void SelectItem (object item)
		{
			for (int i = 0; i < DataProvider.IconCount; i++) {
				if (DataProvider.GetTag (i) == item) {
					list.Selection = i;
					vScrollbar.Vadjustment.Value = Math.Max (0, i * list.RowHeight - vScrollbar.Vadjustment.PageSize / 2);
					break;
				}
			}
		}

		protected override void OnMapped ()
		{
			base.OnMapped ();
			Gdk.Pointer.Grab (GdkWindow, true, Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask, null, null, Global.CurrentEventTime);
			Grab.Add (this);
			GrabBrokenEvent += delegate {
				Destroy ();
			};
		}

		protected override void OnUnmapped ()
		{
			Grab.Remove (this);
			Gdk.Pointer.Ungrab (Global.CurrentEventTime);
			base.OnUnmapped ();
		}

		void SwitchToSeletedWord ()
		{
			string selection = list.WordSelection.ToString ();
			for (int i = 0; i < DataProvider.IconCount; i++) {
				if (DataProvider.GetMarkup (i).StartsWith (selection, StringComparison.OrdinalIgnoreCase)) {
					list.Selection = i;
					list.WordSelection.Append (selection);
				}
			} 
		}

		public bool ProcessKey (Gdk.Key key, Gdk.ModifierType modifier)
		{
			switch (key) {
			case Gdk.Key.Up:
				if (list.SelectionDisabled)
					list.SelectionDisabled = false;
				else
					list.Selection--;
				return true;
				
			case Gdk.Key.Down:
				if (list.SelectionDisabled)
					list.SelectionDisabled = false;
				else
					list.Selection++;
				return true;

			case Gdk.Key.Page_Up:
				list.Selection -= list.VisibleRows - 1;
				return true;
				
			case Gdk.Key.Page_Down:
				list.Selection += list.VisibleRows - 1;
				return true;

			case Gdk.Key.Home:
				list.Selection = 0;
				return true;
			
			case Gdk.Key.End:
				list.Selection = DataProvider.IconCount;
				return true;
							
			case Gdk.Key.Escape:
				Destroy ();
				return true;
				
			case Gdk.Key.Return:
			case Gdk.Key.ISO_Enter:
			case Gdk.Key.Key_3270_Enter:
			case Gdk.Key.KP_Enter:
				list.OnSelectItem (EventArgs.Empty);
				return true;
			default:
				char ch = (char)key;
				if (char.IsLetterOrDigit (ch)) {
					list.WordSelection.Append (ch);
					SwitchToSeletedWord ();
				}
				break;
			}
			
			return false;
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			Destroy ();
			return base.OnFocusOutEvent (evnt);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			if (left)
				Destroy ();
			return base.OnButtonPressEvent (evnt);
		}

		bool left = true;

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			left = true;
			return base.OnLeaveNotifyEvent (evnt);
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			left &= evnt.Window != GdkWindow;
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			ProcessKey (evnt.Key, evnt.State);
			return base.OnKeyPressEvent (evnt);
		}

		internal class ListWidget: DrawingArea
		{
			const int margin = 0;
			const int padding = 4;
			int listWidth = 300;
			Pango.Layout layout;
			readonly DropDownBoxListWindow win;
			int selection;
			int rowHeight;

			public int RowHeight {
				get {
					return rowHeight;
				}
			}

			public int FixedRowHeight {
				get;
				set;
			}
			//	bool buttonPressed;
			bool disableSelection;

			public event EventHandler SelectionChanged;

			public ListWidget (DropDownBoxListWindow win)
			{
				this.win = win;
				this.Events = Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.LeaveNotifyMask;
				layout = new Pango.Layout (this.PangoContext);
				CalcRowHeight ();
				CalcVisibleRows ();
			}

			internal void CalcRowHeight ()
			{
				if (FixedRowHeight > 0) {
					rowHeight = FixedRowHeight;
				} else {
					layout.SetText ("|");
					int rowWidth;
					layout.GetPixelSize (out rowWidth, out rowHeight);
					rowHeight += padding;
				}
				SetBounds ();
			}

			protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
			{
				selection = -1;
				QueueDraw ();
				return base.OnLeaveNotifyEvent (evnt);
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
				
				selection = win.DataProvider.IconCount == 0 ? -1 : 0;
				CalcVisibleRows ();
				disableSelection = false;
				if (IsRealized) {
					UpdateStyle ();
					QueueDraw ();
				}
				if (SelectionChanged != null)
					SelectionChanged (this, EventArgs.Empty);
			}

			readonly StringBuilder wordSelection = new StringBuilder ();

			public StringBuilder WordSelection {
				get {
					return wordSelection;
				}
			}

			public int Selection {
				get {
					return selection;
				}
				
				set {
					wordSelection.Length = 0;
					var newValue = Math.Max (0, Math.Min (value, win.DataProvider.IconCount - 1));
					
					if (newValue != selection) {
						selection = newValue;
						UpdatePage ();
						
						if (SelectionChanged != null)
							SelectionChanged (this, EventArgs.Empty);
					}
					
					disableSelection &= !disableSelection;
	
					QueueDraw ();
				}
			}

			void UpdatePage ()
			{
				var area = GetRowArea (selection);
				var value = vAdjustment.Value;
				if (area.Y < value) {
					value = area.Y;
				} else if (value + Allocation.Height < area.Bottom) {
					value = Math.Max (0, area.Bottom - vAdjustment.PageSize + 1);
				}
				vAdjustment.Value = Math.Max (vAdjustment.Lower, Math.Min (value, vAdjustment.Upper - vAdjustment.PageSize));

			}

			public bool SelectionDisabled {
				get { return disableSelection; }
				set {
					disableSelection = value; 
					QueueDraw ();
				}
			}

			protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
			{
				Selection = GetRowByPosition ((int)evnt.Y);
				if (evnt.Type == Gdk.EventType.ButtonPress) {
					OnSelectItem (EventArgs.Empty);
					Destroy ();
				}
				//buttonPressed = true;
				return base.OnButtonPressEvent (evnt);
			}
			int curMouseY;
			protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
			{
				int winWidth, winHeight;
				GdkWindow.GetSize (out winWidth, out winHeight);
				curMouseY = (int)evnt.Y;
				Selection = GetRowByPosition (curMouseY);
				
				return base.OnMotionNotifyEvent (evnt);
			}

			protected override bool OnScrollEvent (Gdk.EventScroll evnt)
			{
				var s = GetRowByPosition ((int)evnt.Y);
				Selection = s;
				
				return base.OnScrollEvent (evnt);
			}

			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				base.OnExposeEvent (evnt);
				DrawList ();
				return false;
			}

			void DrawList ()
			{
				int winWidth, winHeight;
				GdkWindow.GetSize (out winWidth, out winHeight);

				int lineWidth = winWidth - margin * 2;
				const int xpos = margin + padding;

				int n = (int)(vAdjustment.Value / rowHeight);
				int ypos = (int)(margin + n * rowHeight - vAdjustment.Value);
				while (ypos < winHeight - margin && n < win.DataProvider.IconCount) {
					string text = win.DataProvider.GetMarkup (n) ?? "&lt;null&gt;";

					Gdk.Pixbuf icon = win.DataProvider.GetIcon (n);
					int iconHeight = icon != null ? icon.Height : 24;
					int iconWidth = icon != null ? icon.Width : 0;

					layout.Ellipsize = Pango.EllipsizeMode.End;
					layout.Width = (Allocation.Width - xpos - iconWidth - 2) * (int)Pango.Scale.PangoScale;
					layout.SetMarkup (text);

					int wi, he, typos, iypos;
					layout.GetPixelSize (out wi, out he);
					typos = he < rowHeight ? ypos + (rowHeight - he) / 2 : ypos;
					iypos = iconHeight < rowHeight ? ypos + (rowHeight - iconHeight) / 2 : ypos;
					
					if (n == selection) {
						if (!disableSelection) {
							GdkWindow.DrawRectangle (Style.BaseGC (StateType.Selected), 
								true, margin, ypos, lineWidth, rowHeight);
							GdkWindow.DrawLayout (Style.TextGC (StateType.Selected), 
							                      xpos + iconWidth + 2, typos, layout);
						} else {
							GdkWindow.DrawRectangle (Style.BaseGC (StateType.Selected), 
								false, margin, ypos, lineWidth, rowHeight);
							GdkWindow.DrawLayout (Style.TextGC (StateType.Normal), 
							                      xpos + iconWidth + 2, typos, layout);
						}
					} else
						GdkWindow.DrawLayout (Style.TextGC (StateType.Normal), 
						                      xpos + iconWidth + 2, typos, layout);
					
					if (icon != null)
						GdkWindow.DrawPixbuf (Style.ForegroundGC (StateType.Normal), icon, 0, 0, 
						                      xpos, iypos, iconWidth, iconHeight, Gdk.RgbDither.None, 0, 0);
					
					ypos += rowHeight;
					n++;
					
//reset the markup or it carries over to the next SetText
					layout.SetMarkup (string.Empty);
				}
			}

			int GetRowByPosition (int ypos)
			{
				return (int)(vAdjustment.Value + ypos) / rowHeight;
			}

			public Gdk.Rectangle GetRowArea (int row)
			{
				return new Gdk.Rectangle (0, row * rowHeight, Allocation.Width, rowHeight - 1);
			}

			public int VisibleRows {
				get {
					return Allocation.Height / rowHeight;
				}
			}

			internal int MaxVisibleRows = 8;

			internal void CalcVisibleRows ()
			{
				int lvWidth, lvHeight;
				GetSizeRequest (out lvWidth, out lvHeight);
				if (layout == null)
					return;
				int newHeight;
				newHeight = win.DataProvider.IconCount > MaxVisibleRows ? 
								(rowHeight * MaxVisibleRows) + margin * 2 :
								(rowHeight * win.DataProvider.IconCount) + margin * 2;
				listWidth = Math.Min (450, CalcWidth ());
				SetSizeRequest (listWidth, newHeight);
			}

			internal int CalcWidth ()
			{
				if (win.DataProvider.IconCount == 0)
					return 0;
				int longest = 0;
				string longestText = win.DataProvider.GetMarkup (0) ?? "";
				
				for (int i = 1; i < win.DataProvider.IconCount; i++) {
					string curText = win.DataProvider.GetMarkup (i) ?? "";
					if (curText.Length > longestText.Length) {
						longestText = curText;
						longest = i;
					}
				}
				layout.SetMarkup (win.DataProvider.GetMarkup (longest) ?? "&lt;null&gt;");
				int w, h;
				layout.GetPixelSize (out w, out h);
				Gdk.Pixbuf icon = win.DataProvider.GetIcon (longest);
				int iconWidth = icon != null ? icon.Width : 24;
				w += iconWidth + 2 + padding * 2 + margin;
				return w;
			}

			void SetBounds ()
			{
				if (vAdjustment == null)
					return;
				var h = Allocation.Height;
				var height = Math.Max (h, rowHeight * win.DataProvider.IconCount);
				if (win.DataProvider.IconCount < MaxVisibleRows) {
					vAdjustment.SetBounds (0, h, 0, 0, h);
				} else {
					vAdjustment.SetBounds (0, height, RowHeight, h, h);
				}
				UpdatePage ();
			}

			protected override void OnSizeAllocated (Gdk.Rectangle allocation)
			{
				base.OnSizeAllocated (allocation);

				hAdjustment.SetBounds (0, allocation.Width, 0, 0, allocation.Width);

				SetBounds ();

				UpdatePage ();
			}

			protected override void OnRealized ()
			{
				base.OnRealized ();
				UpdateStyle ();
			}

			void UpdateStyle ()
			{
				GdkWindow.Background = Style.Base (StateType.Normal);
				if (layout != null)
					layout.Dispose ();
				layout = new Pango.Layout (PangoContext);
				layout.Wrap = Pango.WrapMode.Char;
				layout.FontDescription = Style.FontDescription.Copy ();
				CalcRowHeight ();
				CalcVisibleRows ();
			}

			Adjustment hAdjustment;
			Adjustment vAdjustment;

			protected override void OnSetScrollAdjustments (Adjustment hadj, Adjustment vadj)
			{
				hAdjustment = hadj;
				vAdjustment = vadj;
				if (vAdjustment != null)
					vAdjustment.ValueChanged += delegate {
						if (selection > -1)
							Selection = GetRowByPosition (curMouseY);
						QueueDraw ();
					};
				base.OnSetScrollAdjustments (hadj, vadj);
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

			string GetMarkup (int n);

			Gdk.Pixbuf GetIcon (int n);

			object GetTag (int n);

			void ActivateItem (int n);
		}
	}
}

