// 
// GlobalAuthorInformationPanelWidget.cs
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
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.Gui.OptionPanels
{
	class GlobalAuthorInformationPanel : OptionsPanel
	{
		GlobalAuthorInformationPanelWidget widget;

		public override Control CreatePanelWidget ()
		{
			return widget = new GlobalAuthorInformationPanelWidget ();
		}

		public override void ApplyChanges ()
		{
			widget.Save ();
		}
	}
	
	partial class GlobalAuthorInformationPanelWidget : Gtk.Bin
	{
		
		public GlobalAuthorInformationPanelWidget()
		{
			this.Build ();
			
			nameEntry.Text = AuthorInformation.Default.Name ?? "";
			emailEntry.Text = AuthorInformation.Default.Email ?? "";
			copyrightEntry.Text = AuthorInformation.Default.Copyright ?? "";
			companyEntry.Text = AuthorInformation.Default.Company ?? "";
			trademarkEntry.Text = AuthorInformation.Default.Trademark ?? "";

			SetupAccessibility ();
		}

		void SetupAccessibility ()
		{
			nameEntry.SetCommonAccessibilityAttributes ("AuthorInformationPanel.nameEntry", "",
														GettextCatalog.GetString ("Enter the author name"));
			nameEntry.SetAccessibilityLabelRelationship (label2);

			emailEntry.SetCommonAccessibilityAttributes ("AuthorInformationPanel.emailEntry", "",
														 GettextCatalog.GetString ("Enter the author's email address"));
			emailEntry.SetAccessibilityLabelRelationship (label4);

			copyrightEntry.SetCommonAccessibilityAttributes ("AuthorInformationPanel.copyrightEntry", "",
															 GettextCatalog.GetString ("Enter the copyright statement"));
			copyrightEntry.SetAccessibilityLabelRelationship (label3);

			companyEntry.SetCommonAccessibilityAttributes ("AuthorInformationPanel.companyEntry", "",
														   GettextCatalog.GetString ("Enter the company name"));
			companyEntry.SetAccessibilityLabelRelationship (label5);

			trademarkEntry.SetCommonAccessibilityAttributes ("AuthorInformationPanel.trademarkEntry", "",
															 GettextCatalog.GetString ("Enter the trademark statement"));
			trademarkEntry.SetAccessibilityLabelRelationship (label6);
		}

		public void Save ()
		{
			Runtime.Preferences.AuthorName.Value = nameEntry.Text;
			Runtime.Preferences.AuthorEmail.Value = emailEntry.Text;
			Runtime.Preferences.AuthorCopyright.Value = copyrightEntry.Text;
			Runtime.Preferences.AuthorCompany.Value = companyEntry.Text;
			Runtime.Preferences.AuthorTrademark.Value = trademarkEntry.Text;
		}
	}
}
