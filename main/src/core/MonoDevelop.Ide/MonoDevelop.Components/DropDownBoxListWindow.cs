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
using System.Collections.Generic;
using MonoDevelop.Ide;
using Gtk;
using Mono.TextEditor;
using MonoDevelop.Ide.TypeSystem;
using System.Text;

namespace MonoDevelop.Components
{
	public class DropDownBoxListWindow : Window
	{
		ScrolledWindow vScrollbar;
		internal ListWidget list;
		
		public IListDataProvider DataProvider {
			get;
			private set;
		}
		
		public DropDownBoxListWindow (IListDataProvider provider) : base(Gtk.WindowType.Popup)
		{
			this.DataProvider = provider;
			this.TransientFor = MonoDevelop.Ide.IdeApp.Workbench.RootWindow;
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
			vScrollbar.VScrollbar.SizeAllocated += (object o, SizeAllocatedArgs args) => {
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
			Gdk.Pointer.Grab (this.GdkWindow, true, Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.EnterNotifyMask | Gdk.EventMask.LeaveNotifyMask, null, null, Gtk.Global.CurrentEventTime);
			Gtk.Grab.Add (this);
		}
		
		protected override void OnUnmapped ()
		{
			Gtk.Grab.Remove (this);
			Gdk.Pointer.Ungrab (Gtk.Global.CurrentEventTime);
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
					list.Selection --;
				return true;
				
			case Gdk.Key.Down:
				if (list.SelectionDisabled)
					list.SelectionDisabled = false;
				else
					list.Selection ++;
				return true;
				
			case Gdk.Key.Page_Up:
				list.Selection -= list.VisibleRows - 1;
				return true;
				
			case Gdk.Key.Page_Down:
				list.Selection += list.VisibleRows - 1;
				return true;

			case Gdk.Key.Home:
				list.Selection = (int)0;
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
			Destroy ();
			return base.OnButtonPressEvent (evnt);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			ProcessKey (evnt.Key, evnt.State);
			return base.OnKeyPressEvent (evnt);
		}
		
		internal class ListWidget: Gtk.DrawingArea
		{
			int margin = 0;
			int padding = 4;
			int listWidth = 300;
			
			Pango.Layout layout;
			DropDownBoxListWindow win;
			int selection = 0;

			int rowHeight;

			public int RowHeight {
				get {
					return rowHeight;
				}
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
			
			void CalcRowHeight ()
			{
				layout.SetText ("|");
				int rowWidth;
				layout.GetPixelSize (out rowWidth, out rowHeight);
				rowHeight += padding;
				SetBounds (Allocation);
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
				
				if (win.DataProvider.IconCount == 0)
					selection = -1;
				else
					selection = 0;
				CalcVisibleRows ();
				disableSelection = false;
				if (IsRealized) {
					UpdateStyle ();
					QueueDraw ();
				}
				if (SelectionChanged != null) SelectionChanged (this, EventArgs.Empty);
			}
			StringBuilder wordSelection = new StringBuilder ();

			public StringBuilder WordSelection {
				get {
					return wordSelection;
				}
			}

			public int Selection
			{
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
					
					if (disableSelection)
						disableSelection = false;
	
					this.QueueDraw ();
				}
			}
			
			void UpdatePage ()
			{
				var area = GetRowArea (selection);
				if (area.Y < vadj.Value) {
					vadj.Value = area.Y;
					return;
				}
				if (vadj.Value + Allocation.Height < area.Bottom) {
					vadj.Value = System.Math.Max (0, area.Bottom - vadj.PageSize + 1);
				}
			}
			
			public bool SelectionDisabled {
				get { return disableSelection; }
				set {
					disableSelection = value; 
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
			
			protected override bool OnScrollEvent (Gdk.EventScroll evnt)
			{
				var s = GetRowByPosition ((int) evnt.Y);
				if (Selection != s)
					Selection = s;
				
				return base.OnScrollEvent (evnt);
			}
	
			protected override bool OnExposeEvent (Gdk.EventExpose args)
			{
				base.OnExposeEvent (args);
				DrawList ();
				return false;
			}
	
			void DrawList ()
			{
				int winWidth, winHeight;
				this.GdkWindow.GetSize (out winWidth, out winHeight);

				int ypos = margin;
				int lineWidth = winWidth - margin * 2;
				int xpos = margin + padding;

				int n = (int)(vadj.Value / rowHeight);
				while (ypos < winHeight - margin && n < win.DataProvider.IconCount) {
					string text = win.DataProvider.GetMarkup (n) ?? "&lt;null&gt;";
					layout.SetMarkup (text);

					Gdk.Pixbuf icon = win.DataProvider.GetIcon (n);
					int iconHeight = icon != null ? icon.Height : 24;
					int iconWidth = icon != null ? icon.Width : 0;

					int wi, he, typos, iypos;
					layout.GetPixelSize (out wi, out he);
					if (wi > Allocation.Width) {
						int idx, trail;
						if (layout.XyToIndex (
							(int)((Allocation.Width - xpos - iconWidth - 2) * Pango.Scale.PangoScale),
							0,
							out idx,
							out trail
						) && idx > 3) {
							text = AmbienceService.UnescapeText (text);
							text = text.Substring (0, idx - 3) + "...";
							text = AmbienceService.EscapeText (text);
							layout.SetMarkup (text);
							layout.GetPixelSize (out wi, out he);
						}
					}
					typos = he < rowHeight ? ypos + (rowHeight - he) / 2 : ypos;
					iypos = iconHeight < rowHeight ? ypos + (rowHeight - iconHeight) / 2 : ypos;
					
					if (n == selection) {
						if (!disableSelection) {
							this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Selected), 
							                              true, margin, ypos, lineWidth, he + padding);
							this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Selected), 
								                           xpos + iconWidth + 2, typos, layout);
						} else {
							this.GdkWindow.DrawRectangle (this.Style.BaseGC (StateType.Selected), 
							                              false, margin, ypos, lineWidth, he + padding);
							this.GdkWindow.DrawLayout (this.Style.TextGC (StateType.Normal), 
							                           xpos + iconWidth + 2, typos, layout);
						}
					} else
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
				return (int)(vadj.Value + ypos) / rowHeight;
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

			const int maxVisibleRows = 8;
			void CalcVisibleRows ()
			{
				int lvWidth, lvHeight;
				this.GetSizeRequest (out lvWidth, out lvHeight);
				if (layout == null)
					return;

				int newHeight;
				if (this.win.DataProvider.IconCount > maxVisibleRows)
					newHeight = (rowHeight * maxVisibleRows) + margin * 2;
				else
					newHeight = (rowHeight * this.win.DataProvider.IconCount) + margin * 2;
				listWidth = Math.Min (450, CalcWidth ());
				this.SetSizeRequest (listWidth, newHeight);
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

			void SetBounds (Gdk.Rectangle allocation)
			{
				if (vadj == null)
					return;
				var h = allocation.Height;
				var height = Math.Max (h, rowHeight * win.DataProvider.IconCount);
				if (this.win.DataProvider.IconCount < maxVisibleRows) {
					vadj.SetBounds (0, h, 0, 0, h);
				} else {
					vadj.SetBounds (0, height, RowHeight, h, h);
				}
			}

			protected override void OnSizeAllocated (Gdk.Rectangle allocation)
			{
				base.OnSizeAllocated (allocation);

				hadj.SetBounds (0, allocation.Width, 0, 0, allocation.Width);

				SetBounds (allocation);

				UpdatePage ();
			}
			
			protected override void OnRealized ()
			{
				base.OnRealized ();
				UpdateStyle ();
			}
			
			void UpdateStyle ()
			{
				this.GdkWindow.Background = this.Style.Base (StateType.Normal);
				if (layout != null)
					layout.Dispose ();
				layout = new Pango.Layout (this.PangoContext);
				layout.Wrap = Pango.WrapMode.Char;
				layout.FontDescription = this.Style.FontDescription.Copy();
				CalcRowHeight ();
				CalcVisibleRows ();
			}

			Adjustment hadj;
			Adjustment vadj;

			protected override void OnSetScrollAdjustments (Adjustment hadj, Adjustment vadj)
			{
				this.hadj = hadj;
				this.vadj = vadj;
				if (this.vadj != null)
					this.vadj.ValueChanged += (sender, e) => QueueDraw ();
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

