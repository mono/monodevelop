// 
// AddExternalFileDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Projects
{
	internal partial class AddExternalFileDialog : Gtk.Dialog
	{
		public AddExternalFileDialog (string file)
		{
			HasSeparator = true;
			this.Build ();
			radioCopy.Active = true;
			labelTitle.Markup = GettextCatalog.GetString (labelTitle.Text, "<b>" + GLib.Markup.EscapeText (file) + "</b>");
			Resizable = false;
			buttonOk.GrabFocus ();
		}
		
		public void ShowKeepOption (string dir)
		{
			radioKeep.Show();
			string here = "." + System.IO.Path.DirectorySeparatorChar;
			if (!dir.StartsWith (here))
				dir = here + dir;
			labelKeep.Markup = GettextCatalog.GetString (labelKeep.LabelProp, dir);
			radioKeep.Active = true;
		}

		public void DisableLinkOption ()
		{
			radioLink.Hide ();
		}
		
		public bool ShowApplyAll {
			get { return checkApplyAll.Visible; }
			set { checkApplyAll.Visible = value; }
		}
		
		public AddAction SelectedAction {
			get {
				if (radioCopy.Active)
					return AddAction.Copy;
				else if (radioMove.Active)
					return AddAction.Move;
				else if (radioLink.Active)
					return AddAction.Link;
				else
					return AddAction.Keep;
			}
			set {
				switch (value) {
				case AddAction.Copy: radioCopy.Active = true; break;
				case AddAction.Move: radioMove.Active = true; break;
				case AddAction.Link: radioLink.Active = true; break;
				case AddAction.Keep: radioKeep.Active = true; break;
				}
			}
		}
		
		public bool ApplyToAll {
			get { return checkApplyAll.Active; }
			set { checkApplyAll.Active = value; }
		}
	}
	
	enum AddAction
	{
		Copy,
		Move,
		Link,
		Keep
	}
}

