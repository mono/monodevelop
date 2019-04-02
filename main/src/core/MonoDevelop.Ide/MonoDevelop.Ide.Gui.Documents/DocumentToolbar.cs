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
using MonoDevelop.Ide.Gui.Shell;

namespace MonoDevelop.Ide.Gui.Documents
{
	public class DocumentToolbar
	{
		Gtk.Widget frame;
		Box box;
		bool empty = true;
		readonly IShellDocumentToolbar shellToolbar;

		internal DocumentToolbar (IShellDocumentToolbar shellToolbar)
		{
			this.shellToolbar = shellToolbar;
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
			shellToolbar.Add (widget, fill, padding);
		}
		
		public void AddSpace ()
		{
			shellToolbar.AddSpace ();
		}

		public void Insert (Control w, int index)
		{
			shellToolbar.Insert (w, index);
		}
		
		public void Remove (Control widget)
		{
			shellToolbar.Remove (widget);
		}
		
		public bool Visible {
			get {
				return shellToolbar.Visible;
			}
			set {
				shellToolbar.Visible = value;
			}
		}
		
		public bool Sensitive {
			get { return shellToolbar.Sensitive; }
			set { shellToolbar.Sensitive = value; }
		}
		
		public void ShowAll ()
		{
			shellToolbar.ShowAll ();
		}
		
		public Control[] Children {
			get { return shellToolbar.Children; }
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

	public enum DocumentToolbarKind
	{
		Top,
		Bottom
	}
}

