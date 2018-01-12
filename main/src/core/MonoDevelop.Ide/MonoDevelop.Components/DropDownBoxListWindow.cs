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
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.Components
{
	public class DropDownBoxListWindow : Gtk.Window
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
				SetSizeRequest (list.WidthRequest + WidthModifier, list.HeightRequest);
			}
		}

		const int padding = 5;

		int WidthModifier {
			get {
				return padding + 2 * (int)BorderWidth;
			}
		}

		public DropDownBoxListWindow (IListDataProvider provider) : this (provider, WindowType.Toplevel)
		{
		}

		public DropDownBoxListWindow (IListDataProvider provider, WindowType windowType) : base (windowType)
		{
			Accessible.Name = "DropDownBoxListWindow";

			this.DataProvider = provider;
			this.TransientFor = IdeApp.Workbench.RootWindow;
			this.TypeHint = Gdk.WindowTypeHint.DropdownMenu;
			this.Decorated = false;
			this.BorderWidth = 1;
			list = new ListWidget (this);
			list.Accessible.Name = "DropDownBoxListWindow.List";

			list.SelectItem += delegate {
				var sel = list.Selection;
				if (sel >= 0 && sel < DataProvider.IconCount) {
					DataProvider.ActivateItem (sel);
					Destroy ();
				}
			};
			SetSizeRequest (list.WidthRequest + WidthModifier, list.HeightRequest);
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
			int count = DataProvider.IconCount;
			for (int i = 0; i < count; i++) {
				if (DataProvider.GetTag (i) == item) {
					list.Selection = i;
					vScrollbar.Vadjustment.Value = Math.Max (0, i * list.RowHeight - vScrollbar.Vadjustment.PageSize / 2);
					break;
				}
			}
		}

		void SwitchToSeletedWord ()
		{
			string selection = list.WordSelection.ToString ();
			int count = DataProvider.IconCount;
			for (int i = 0; i < count; i++) {
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

		internal class ListWidget: DrawingArea
		{
			const int leftXAlignment = 1;
			const int padding = 4;
			const int iconTextDistance = 4;
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
				Accessible.Role = Atk.Role.List;
				this.win = win;
				this.Events = Gdk.EventMask.ButtonPressMask | Gdk.EventMask.ButtonReleaseMask | Gdk.EventMask.PointerMotionMask | Gdk.EventMask.LeaveNotifyMask;
				this.CanFocus = true;
				UpdateStyle ();

				CalcAccessibility ();
			}

			class TextElement : AtkCocoaHelper.AccessibilityElementProxy
			{
				public int RowIndex { get; set; }
				internal TextElement ()
				{
				}
			}

			TextElement[] textElements;
			void CalcAccessibility ()
			{
				if (!AtkCocoaHelper.AccessibilityElementProxy.Enabled) {
					return;
				}

				var columnElement = new AtkCocoaHelper.AccessibilityElementProxy ();
				columnElement.GtkParent = this;
				columnElement.SetRole (AtkCocoa.Roles.AXColumn);
				Accessible.AddAccessibleElement (columnElement);

				int count = win.DataProvider.IconCount;
				textElements = new TextElement[count];
				for (int i = 0; i < count; i++) {
					var rowElement = new AtkCocoaHelper.AccessibilityElementProxy ();
					rowElement.GtkParent = this;
					rowElement.SetRole (AtkCocoa.Roles.AXRow);
					Accessible.AddAccessibleElement (rowElement);

					var cellElement = new AtkCocoaHelper.AccessibilityElementProxy ();
					cellElement.GtkParent = this;
					cellElement.SetRole (AtkCocoa.Roles.AXCell);
					columnElement.AddAccessibleChild (cellElement);
					rowElement.AddAccessibleChild (cellElement);

					var textElement = textElements[i] = new TextElement ();
					textElement.RowIndex = i;
					textElement.PerformPress += PerformPress;
					textElement.GtkParent = this;
					textElement.Value = win.DataProvider.GetMarkup (i);
					cellElement.AddAccessibleChild (textElement);
				}
			}

			void PerformPress (object sender, EventArgs args)
			{
				var element = (TextElement)sender;
				win.DataProvider.ActivateItem (element.RowIndex);
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
				if (textElements != null) {
					foreach (var textElement in textElements) {
						textElement.PerformPress -= PerformPress;
					}
					textElements = null;
				}

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

			protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
			{
				return win.ProcessKey (evnt.Key, evnt.State);
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

				int lineWidth = winWidth - leftXAlignment * 2;
				const int xpos = leftXAlignment + padding;

				int n = (int)(vAdjustment.Value / rowHeight);
				int ypos = (int)(leftXAlignment + n * rowHeight - vAdjustment.Value);
				while (ypos < winHeight - leftXAlignment && n < win.DataProvider.IconCount) {
					string text = win.DataProvider.GetMarkup (n) ?? "&lt;null&gt;";

					var icon = win.DataProvider.GetIcon (n);
					int iconHeight = icon != null ? (int)icon.Height : 24;
					int iconWidth = icon != null ? (int)icon.Width : 0;

					layout.Ellipsize = Pango.EllipsizeMode.End;
					layout.Width = (Allocation.Width - xpos - padding - iconWidth - iconTextDistance) * (int)Pango.Scale.PangoScale;
					layout.SetMarkup (PathBar.GetFirstLineFromMarkup (text));

					int wi, he, typos, iypos;
					layout.GetPixelSize (out wi, out he);
					typos = he < rowHeight ? ypos + (rowHeight - he) / 2 : ypos;
					iypos = iconHeight < rowHeight ? ypos + (rowHeight - iconHeight) / 2 : ypos;
					
					if (n == selection) {
						if (!disableSelection) {
							GdkWindow.DrawRectangle (Style.BaseGC (StateType.Selected), 
								true, leftXAlignment, ypos, lineWidth, rowHeight);
							GdkWindow.DrawLayout (Style.TextGC (StateType.Selected), 
							                      xpos + iconWidth + iconTextDistance, typos, layout);
							if (icon != null)
								icon = icon.WithStyles ("sel");
						} else {
							GdkWindow.DrawRectangle (Style.BaseGC (StateType.Selected), 
								false, leftXAlignment, ypos, lineWidth, rowHeight);
							GdkWindow.DrawLayout (Style.TextGC (StateType.Normal), 
							                      xpos + iconWidth + iconTextDistance, typos, layout);
						}
					} else
						GdkWindow.DrawLayout (Style.TextGC (StateType.Normal), 
						                      xpos + iconWidth + iconTextDistance, typos, layout);
					
					if (icon != null) {
						using (var ctx = Gdk.CairoHelper.Create (this.GdkWindow))
							ctx.DrawImage (this, icon, xpos, iypos);
					}
					
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
								(rowHeight * MaxVisibleRows) + leftXAlignment * 2 :
								(rowHeight * win.DataProvider.IconCount) + leftXAlignment * 2;
				newHeight += 2;
				listWidth = Math.Min (450, CalcWidth ());
				SetSizeRequest (listWidth, newHeight);
			}

			internal int CalcWidth ()
			{
				if (win.DataProvider.IconCount == 0)
					return 0;
				int longest = 0;
				string longestText = win.DataProvider.GetMarkup (0) ?? "";

				int count = win.DataProvider.IconCount;
				for (int i = 1; i < count; i++) {
					string curText = win.DataProvider.GetMarkup (i) ?? "";
					if (curText.Length > longestText.Length) {
						longestText = curText;
						longest = i;
					}
				}
				layout.Width = -1;
				layout.SetMarkup (longestText ?? "&lt;null&gt;");
				int w, h;
				layout.GetPixelSize(out w, out h);
				var icon = win.DataProvider.GetIcon (longest);
				int iconWidth = icon != null ? (int) icon.Width : 24;
				return iconWidth + iconTextDistance + (padding * 2) + leftXAlignment + w;
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
				if (IsRealized)
					GdkWindow.Background = Style.Base (StateType.Normal);
				if (layout != null)
					layout.Dispose ();
				layout = new Pango.Layout (PangoContext);
				layout.Wrap = Pango.WrapMode.Char;
				layout.FontDescription = FontService.SansFont.CopyModified (Ide.Gui.Styles.FontScale11);
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

			Xwt.Drawing.Image GetIcon (int n);

			object GetTag (int n);

			void ActivateItem (int n);
		}
	}
}

