// 
// HyperlinkWidget.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
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
using Gtk;
using MonoDevelop.Ide;

namespace MonoDevelop.PackageManagement
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class HyperlinkWidget : Gtk.Bin
	{
		LinkButton linkButton;

		public HyperlinkWidget (string uri, string label)
		{
			this.Build ();
			AddLinkButton (uri, label);
		}
		
		public HyperlinkWidget ()
		{
			this.Build ();
			AddLinkButton ();
		}
		
		void AddLinkButton (string uri = "", string label = "")
		{
			linkButton = new LinkButton (uri, label);
			linkButton.Relief = ReliefStyle.None;
			linkButton.CanFocus = false;
			linkButton.SetAlignment (0, 0);
			linkButton.Clicked += LinkButtonClicked;
			this.Add (linkButton);
		}
		
		void LinkButtonClicked (object sender, EventArgs e)
		{
			DesktopService.ShowUrl (linkButton.Uri);
		}
		
		public string Uri {
			get { return linkButton.Uri; }
			set { linkButton.Uri = value; }
		}
		
		public string Label {
			get { return linkButton.Label; }
			set { linkButton.Label = value; }
		}
	}
}

