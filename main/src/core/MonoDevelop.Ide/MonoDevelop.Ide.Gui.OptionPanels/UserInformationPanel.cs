// 
// UserInformationPanelWidget.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui.Dialogs;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	class UserInformationPanel : ItemOptionsPanel
	{
		UserInformationPanelWidget widget;
		Solution solution;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			widget = new UserInformationPanelWidget ();
			widget.Set (IdeApp.Workspace.GetUserPreferences (solution).GetValue<UserInformation> ("UserInfo"));
			return widget;
		}

		public override void ApplyChanges ()
		{
			if (solution != null)
			 	IdeApp.Workspace.GetUserPreferences (solution).SetValue<UserInformation> ("UserInfo", widget.Get ());
		}

		public override void Initialize (MonoDevelop.Core.Gui.Dialogs.OptionsDialog dialog, object dataObject)
		{
			solution = dataObject as Solution;
			base.Initialize (dialog, dataObject);
		}
		
		public override bool IsVisible ()
		{
			return solution != null && base.IsVisible ();
		}
	}
	
	
	partial class UserInformationPanelWidget : Gtk.Bin
	{
		UserInformation info;
		
		public UserInformationPanelWidget()
		{
			this.Build();
		}
		
		public void Set (UserInformation info)
		{
			this.info = info;
			checkCustom.Active = (info != null);
			UseDefaultToggled (this, EventArgs.Empty);
		}
		
		public UserInformation Get ()
		{
			return checkCustom.Active? new UserInformation (nameEntry.Text, emailEntry.Text) : null;
		}

		void UseDefaultToggled (object sender, System.EventArgs e)
		{
			if (checkCustom.Active) {
				infoTable.Sensitive = true;
				if (info != null) {
					nameEntry.Text = info.Name ?? "";
					emailEntry.Text = info.Email ?? "";
				}
			} else {
				infoTable.Sensitive = false;
				info = new UserInformation (nameEntry.Text, emailEntry.Text);
				nameEntry.Text = UserInformation.Default.Name ?? "";
				emailEntry.Text = UserInformation.Default.Email ?? "";
			}
		}
	}
}
