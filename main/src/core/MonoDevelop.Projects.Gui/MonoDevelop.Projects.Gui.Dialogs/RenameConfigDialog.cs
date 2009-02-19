// RenameConfigDialog.cs
//
//Author:
//  Lluis Sanchez Gual
//
//Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Gui.Dialogs
{
	partial class RenameConfigDialog: Gtk.Dialog
	{
		string platform;
		ItemConfigurationCollection<ItemConfiguration> configurations;
		
		public RenameConfigDialog(ItemConfigurationCollection<ItemConfiguration> configurations)
		{
			Build ();
			this.configurations = configurations;
		}
		
		public string ConfigName {
			get {
				if (string.IsNullOrEmpty (platform))
					return nameEntry.Text;
				else
					return nameEntry.Text + "|" + platform;
			}
			set {
				int i = value.LastIndexOf ('|');
				if (i == -1) {
					nameEntry.Text = value;
					platform = string.Empty;
				} else {
					nameEntry.Text = value.Substring (0, i);
					platform = value.Substring (i+1);
				}
			}
		}
		
		public bool RenameChildren {
			get { return renameChildrenCheck.Active; }
		}
	
		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			if (nameEntry.Text.Trim ().Length == 0 || nameEntry.Text.IndexOf ('|') != -1) {
				MessageService.ShowWarning (GettextCatalog.GetString ("Please enter a valid configuration name."));
			} else if (configurations [ConfigName] != null) {
				MessageService.ShowWarning (GettextCatalog.GetString ("A configuration with the name '{0}' already exists.", ConfigName));
			} else
				Respond (Gtk.ResponseType.Ok);
		}
	}
}
