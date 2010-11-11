// 
// AuthorInformationPanelWidget.cs
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
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	class AuthorInformationPanel : ItemOptionsPanel
	{
		AuthorInformationPanelWidget widget;
		Solution solution;
		
		public override Gtk.Widget CreatePanelWidget ()
		{
			return widget = new AuthorInformationPanelWidget (solution.LocalAuthorInformation);
		}

		public override void ApplyChanges ()
		{
			if (solution != null)
				solution.LocalAuthorInformation = widget.Get ();
		}

		public override void Initialize (MonoDevelop.Ide.Gui.Dialogs.OptionsDialog dialog, object dataObject)
		{
			solution = dataObject as Solution;
			base.Initialize (dialog, dataObject);
		}
		
		public override bool IsVisible ()
		{
			return solution != null && base.IsVisible ();
		}
	}
	
	
	partial class AuthorInformationPanelWidget : Gtk.Bin
	{
		AuthorInformation info;
		
		public AuthorInformationPanelWidget (AuthorInformation info)
		{
			this.Build();
			
			this.info = info;
			checkCustom.Active = (info != null);
			UseDefaultToggled (this, EventArgs.Empty);
		}
		
		public AuthorInformation Get ()
		{
			return checkCustom.Active? new AuthorInformation (nameEntry.Text, emailEntry.Text, copyrightEntry.Text, companyEntry.Text, trademarkEntry.Text) : null;
		}

		void UseDefaultToggled (object sender, System.EventArgs e)
		{
			if (checkCustom.Active) {
				infoTable.Sensitive = true;
				if (info != null) {
					nameEntry.Text = info.Name ?? "";
					emailEntry.Text = info.Email ?? "";
					copyrightEntry.Text = info.Copyright ?? "";
					companyEntry.Text = info.Company ?? "";
					trademarkEntry.Text = info.Trademark ?? "";
				}
			} else {
				infoTable.Sensitive = false;
				info = new AuthorInformation (nameEntry.Text, emailEntry.Text, copyrightEntry.Text, companyEntry.Text, trademarkEntry.Text);
				if (String.IsNullOrEmpty (info.Name) && String.IsNullOrEmpty (info.Email))
					info = null;
				nameEntry.Text = AuthorInformation.Default.Name ?? "";
				emailEntry.Text = AuthorInformation.Default.Email ?? "";
				copyrightEntry.Text = AuthorInformation.Default.Copyright ?? "";
			}
		}
	}
}
