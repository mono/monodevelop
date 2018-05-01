// 
// DockItemToolbar.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
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
using System.Linq;
using Gtk;

using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Components.Docking
{
	public class DockItemToolbar
	{
		IDockItemToolbarControl toolbar;
		DockItem parentItem;
		DockPositionType position;
		bool empty = true;
		
		internal DockItemToolbar (DockItem parentItem, DockPositionType position)
		{
			this.parentItem = parentItem;
			this.position = position;

			toolbar = new DockItemToolbarControl ();
			toolbar.Initialize (this);
		}

		internal IDockItemToolbarControl Toolbar {
			get => toolbar;
		}

		internal void SetStyle (DockVisualStyle style)
		{
			toolbar.BackgroundColor = style.PadBackgroundColor.Value;
		}

		internal void SetAccessibilityDetails (string name, string label, string description)
		{
			toolbar.SetAccessibilityDetails (name, label, description);
		}

		public DockItem DockItem {
			get { return parentItem; }
		}

		public DockPositionType Position {
			get { return this.position; }
		}

		public void Add (Control widget)
		{
			Add (widget, false);
		}

		public void Add (Control widget, bool fill)
		{
			Add (widget, fill, -1);
		}

		public void Add (Control widget, bool fill, int padding)
		{
			toolbar.Add (widget, fill, padding, -1);
		}

		public void Insert (Control w, int index)
		{
			toolbar.Add (w, false, 0, index);
		}

		public void Remove (Control widget)
		{
			toolbar.Remove (widget);
		}

		public void RemoveAllChildren ()
		{
			foreach (var c in toolbar.ToolbarItems) {
				toolbar.Remove (c);
			}
		}

		public bool Visible {
			get {
				return empty || toolbar.Visible;
			}
			set {
				toolbar.Visible = value;
			}
		}

		public bool Sensitive {
			get { return toolbar.Sensitive; }
			set { toolbar.Sensitive = value; }
		}

		public void ShowAll ()
		{
			toolbar.ShowAll ();
		}

		internal void UpdateAccessibilityLabel ()
		{
			toolbar.UpdateAccessibilityLabel (this);
		}
	}

	interface IDockItemToolbarControl
	{
		void Initialize (DockItemToolbar toolbar);
		bool Visible { get; set; }
		bool Sensitive { get; set; }
		Control[] ToolbarItems { get; }
		Xwt.Drawing.Color BackgroundColor { get; set; }

		void Add (Control widget, bool fill, int padding, int index);
		void Remove (Control widget);
		void ShowAll ();
		void SetAccessibilityDetails (string name, string label, string description);
		void UpdateAccessibilityLabel (DockItemToolbar toolbar);
	}

	class DockItemToolbarControl : CustomFrame, IDockItemToolbarControl
	{
		Box box;
		bool empty = true;

		public void Initialize (DockItemToolbar toolbar)
		{
			Accessible.SetRole ("AXToolbar", "Pad toolbar");

			SetPadding (3,3,3,3);

			/*			switch (position) {
				case PositionType.Top:
					frame.SetMargins (0, 0, 1, 1); 
					frame.SetPadding (0, 2, 2, 0); 
					break;
				case PositionType.Bottom:
					frame.SetMargins (0, 1, 1, 1);
					frame.SetPadding (2, 2, 2, 0); 
					break;
				case PositionType.Left:
					frame.SetMargins (0, 1, 1, 0);
					frame.SetPadding (0, 0, 2, 2); 
					break;
				case PositionType.Right:
					frame.SetMargins (0, 1, 0, 1);
					frame.SetPadding (0, 0, 2, 2); 
					break;
			}*/

			if (toolbar.Position == DockPositionType.Top || toolbar.Position == DockPositionType.Bottom)
				box = new HBox (false, 3);
			else
				box = new VBox (false, 3);
			box.Show ();

			box.Accessible.SetShouldIgnore (false);
			box.Accessible.Role = Atk.Role.ToolBar;

			UpdateAccessibilityLabel (toolbar);

			Add (box);
		}

		public void UpdateAccessibilityLabel (DockItemToolbar toolbar)
		{
			var position = toolbar.Position;
			var parentItem = toolbar.DockItem;

			string name = "";
			switch (position) {
			case DockPositionType.Bottom:
				name = Core.GettextCatalog.GetString ("Bottom {0} pad toolbar", parentItem.Label);
				break;

			case DockPositionType.Left:
				name = Core.GettextCatalog.GetString ("Left {0} pad toolbar", parentItem.Label);
				break;

			case DockPositionType.Right:
				name = Core.GettextCatalog.GetString ("Right {0} pad toolbar", parentItem.Label);
				break;

			case DockPositionType.Top:
				name = Core.GettextCatalog.GetString ("Top {0} pad toolbar", parentItem.Label);
				break;
			}

			box.Accessible.SetCommonAttributes ("padtoolbar", name, "");
		}

		Xwt.Drawing.Color backgroundColor;
		new public Xwt.Drawing.Color BackgroundColor {
			get {
				return backgroundColor;
			}

			set {
				backgroundColor = value;
				base.BackgroundColor = value.ToGdkColor ();
			}
		}

		public void Add (Control control, bool fill, int padding, int index)
		{
			int defaultPadding = 3;

			Widget widget = control;
			if (widget is Button) {
				((Button)widget).Relief = ReliefStyle.None;
				((Button)widget).FocusOnClick = false;
				defaultPadding = 0;
			}
			else if (widget is Entry) {
				((Entry)widget).HasFrame = false;
			}
			else if (widget is ComboBox) {
				((ComboBox)widget).HasFrame = false;
			}
			else if (widget is VSeparator)
				((VSeparator)widget).HeightRequest = 10;

			if (padding == -1)
				padding = defaultPadding;

			box.PackStart (widget, fill, fill, (uint)padding);
			if (empty) {
				empty = false;
				Show ();
			}
			if (index != -1) {
				Box.BoxChild bc = (Box.BoxChild) box [widget];
				bc.Position = index;
			}
		}

		public void Remove (Control widget)
		{
			box.Remove (widget);
		}

		public Control[] ToolbarItems {
			get { return box.Children.Cast<Control> ().ToArray (); }
		}

		public void SetAccessibilityDetails (string name, string label, string description)
		{
			Accessible.SetCommonAttributes (name, label, description);
		}
	}

	public class DockToolButton : Control
	{
		public Control Image {
			get { return buttonControl.Image; }
			set { buttonControl.Image = value; }
		}

		public string TooltipText {
			get { return buttonControl.TooltipText; }
			set { buttonControl.TooltipText = value; }
		}

		public string Label {
			get { return buttonControl.Label; }
			set { buttonControl.Label = value; }
		}

		public bool Sensitive {
			get { return buttonControl.Sensitive; }
			set { buttonControl.Sensitive = value; }
		}

		IDockToolButtonControl buttonControl;

		public DockToolButton (string stockId) : this (stockId, null)
		{
		}
		
		public DockToolButton (string stockId, string label)
		{
			buttonControl = new DockToolButtonControl ();
			buttonControl.Initialize (stockId, label);
		}

		protected override object CreateNativeWidget<T> ()
		{
			return (T)buttonControl;
		}

		public event EventHandler Clicked {
			add {
				buttonControl.Clicked += value;
			}
			remove {
				buttonControl.Clicked -= value;
			}
		}
	}

	interface IDockToolButtonControl
	{
		void Initialize (string stockId, string label);
		Control Image { get; set; }
		string TooltipText { get; set; }
		string Label { get; set; }
		bool Sensitive { get; set; }

		event EventHandler Clicked;
	}

	class DockToolButtonControl : Button, IDockToolButtonControl
	{
		public void Initialize (string stockId, string label)
		{
			Label = label;
			base.Image = new ImageView (stockId, IconSize.Menu);
			base.Image.Show ();
		}

		public new Control Image {
			get {
				return base.Image;
			}

			set {
				base.Image = value;
			}
		}
	}
}

