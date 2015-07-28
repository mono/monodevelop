//
// ContextMenuItem.cs
//
// Author:
//       Greg Munn <greg.munn@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc
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

namespace MonoDevelop.Components
{
	public class ContextMenuItemClickedEventArgs : EventArgs
	{
		public object Context {
			get { return context; }
		}

		public ContextMenuItemClickedEventArgs (object context)
		{
			this.context = context;
		}

		public ContextMenuItemClickedEventArgs ()
		{
		}

		object context;
	}

	public delegate void ContextMenuItemClickedEventHandler (object sender, ContextMenuItemClickedEventArgs e);

	public class ContextMenuItem
	{
		ContextMenu subMenu;
		Xwt.Drawing.Image image;

		public ContextMenuItem ()
		{
			this.Visible = true;
			this.Sensitive = true;

			if (!IsSeparator)
				UseMnemonic = true;
		}

		public ContextMenuItem (string label) : this()
		{
			Label = label;
		}

		public bool IsSeparator {
			get { return this is SeparatorContextMenuItem; }
		}

		public string Label { get; set; }
		public object Context { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="MonoDevelop.Components.ContextMenuItem"/> uses a mnemonic.
		/// </summary>
		/// <value><c>true</c> if it uses a mnemonic; otherwise, <c>false</c>.</value>
		/// <remarks>
		/// When set to true, the character after the first underscore character in the Label property value is
		/// interpreted as the mnemonic for that Label.
		/// </remarks>
		public bool UseMnemonic { get; set; }

		public bool Sensitive { get; set; }

		public bool Visible { get; set; }

		public Xwt.Drawing.Image Image {
			get { return image; }
			set {
				if (IsSeparator)
					throw new NotSupportedException ();
				image = value; 
			}
		}

		public ContextMenu SubMenu {
			get { return subMenu; }
			set {
				if (IsSeparator)
					throw new NotSupportedException ();
				subMenu = value;
			}
		}

		public event ContextMenuItemClickedEventHandler Clicked;

		public void Show ()
		{
			Visible = true;
		}

		public void Hide ()
		{
			Visible = false;
		}

		internal void Click ()
		{
			DoClick ();
		}

		protected virtual void DoClick ()
		{
			OnClicked (new ContextMenuItemClickedEventArgs (Context));
		}

		protected virtual void OnClicked (ContextMenuItemClickedEventArgs e)
		{
			if (Clicked != null)
				Clicked (this, e);
		}
	}
}