// 
// DocumentToolbar.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc
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
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Ide.Gui
{
	public class DocumentToolbar
	{
		Gtk.Widget frame;
		Box box;
		bool empty = true;

		internal DocumentToolbar ()
		{
			box = new HBox ();
			box.Spacing = 3;
			box.Show ();
			var al = new ToolbarBox (0, 0, 1, 1);
			al.LeftPadding = 5;
			al.TopPadding = 4;
			al.BottomPadding = 4;
			al.Add (box);
			frame = al;
		}
		
		internal Control Container {
			get { return frame; }
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
			Add (widget, fill, padding, -1);
		}
		
		void Add (Control control, bool fill, int padding, int index)
		{
			int defaultPadding = 3;

			Gtk.Widget widget = control;
			if (widget is Button) {
				((Button)widget).Relief = ReliefStyle.None;
				((Button)widget).FocusOnClick = false;
				defaultPadding = 0;
				ChangeColor (widget);
			}
			else if (widget is Entry) {
				((Entry)widget).HasFrame = false;
			}
			else if (widget is ComboBox) {
				((ComboBox)widget).HasFrame = false;
			}
			else if (widget is VSeparator) {
				((VSeparator)widget).HeightRequest = 10;
				ChangeColor (widget);
			}
			else
				ChangeColor (widget);
			
			if (padding == -1)
				padding = defaultPadding;
			
			box.PackStart (widget, fill, fill, (uint)padding);
			if (empty) {
				empty = false;
				frame.Show ();
			}
			if (index != -1) {
				Box.BoxChild bc = (Box.BoxChild) box [widget];
				bc.Position = index;
			}
		}

		public void AddSpace ()
		{
			var spacer = new HBox ();
			spacer.Accessible.SetShouldIgnore (true);
			Add (spacer, true); 
		}

		void ChangeColor (Gtk.Widget w)
		{
			w.Realized += delegate {
				w.ModifyText (StateType.Normal, Styles.BreadcrumbTextColor.ToGdkColor ());
				w.ModifyFg (StateType.Normal, Styles.BreadcrumbTextColor.ToGdkColor ());
			};
			if (w is Gtk.Container) {
				foreach (var c in ((Gtk.Container)w).Children)
					ChangeColor (c);
			}
		}
		
		public void Insert (Control w, int index)
		{
			Add (w, false, 0, index);
		}
		
		public void Remove (Control widget)
		{
			box.Remove (widget);
		}
		
		public bool Visible {
			get {
				return empty || frame.Visible;
			}
			set {
				frame.Visible = value;
			}
		}
		
		public bool Sensitive {
			get { return frame.Sensitive; }
			set { frame.Sensitive = value; }
		}
		
		public void ShowAll ()
		{
			frame.ShowAll ();
		}
		
		public Control[] Children {
			get { return box.Children.Select (w => (Control)w).ToArray (); }
		}

		class ToolbarBox: Gtk.Alignment
		{
			public ToolbarBox (float xa, float ya, float sx, float sy): base (xa, ya, sx, sy)
			{
			}

			protected override bool OnExposeEvent (Gdk.EventExpose evnt)
			{
				using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
					ctx.Rectangle (0, 0, Allocation.Width, Allocation.Height);
					ctx.SetSourceColor (Styles.BreadcrumbBackgroundColor.ToCairoColor ());
					ctx.Fill ();

					ctx.MoveTo (0.5, Allocation.Height - 0.5);
					ctx.RelLineTo (Allocation.Width, 0);
					ctx.SetSourceColor (Styles.BreadcrumbBottomBorderColor.ToCairoColor ());
					ctx.LineWidth = 1;
					ctx.Stroke ();
				}
				return base.OnExposeEvent (evnt);
			}
		}
	}

	public class DocumentToolButton : Control
	{
		public ImageView Image {
			get { return (ImageView)button.Image; }
			set { button.Image = value; }
		}

		public string TooltipText {
			get { return button.TooltipText; }
			set { button.TooltipText = value; }
		}

		public string Label {
			get { return button.Label; }
			set { button.Label = value; }
		}

		Gtk.Button button;

		public DocumentToolButton (string stockId) : this (stockId, null)
		{
		}

		public DocumentToolButton (string stockId, string label)
		{
			button = new Button ();
			Label = label;
			Image = new ImageView (stockId, IconSize.Menu);
			Image.Show ();
		}

		protected override object CreateNativeWidget<T> ()
		{
			return button;
		}

		public event EventHandler Clicked {
			add {
				button.Clicked += value;
			}
			remove {
				button.Clicked -= value;
			}
		}

		public class DocumentToolButtonImage : Control
		{
			ImageView image;
			internal DocumentToolButtonImage (ImageView image)
			{
				this.image = image;
			}

			protected override object CreateNativeWidget<T> ()
			{
				return image;
			}

			public static implicit operator Gtk.Widget (DocumentToolButtonImage d)
			{
				return d.GetNativeWidget<Gtk.Widget> ();
			}

			public static implicit operator DocumentToolButtonImage (ImageView d)
			{
				return new DocumentToolButtonImage (d);
			}
		}
	}
}

