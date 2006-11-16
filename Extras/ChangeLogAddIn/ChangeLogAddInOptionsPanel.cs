/*
Copyright (C) 2006  Jacob Ilsø Christensen

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;

namespace MonoDevelop.ChangeLogAddIn
{
	public class ChangeLogAddInOptionPanel : AbstractOptionPanel
	{
		Entry nameEntry = new Entry();
		Entry emailEntry = new Entry();
		
		public override void LoadPanelContents()
		{
			VBox hBox = new VBox();
			this.Add(hBox);
						
			Label descriptionLabel = new Label(GettextCatalog.GetString("Specify personal information used in ChangeLog entries"));
			descriptionLabel.SetAlignment(0.0f, 0.5f);
			hBox.PackStart(descriptionLabel, false, false, 10);
			
			Label nameLabel = new Label (GettextCatalog.GetString("Full Name:"));
			nameLabel.SetAlignment(0.0f, 0.5f);
			nameEntry.Text = Runtime.Properties.GetProperty("ChangeLogAddIn.Name", "Full Name");

			Label emailLabel = new Label(GettextCatalog.GetString("Email Address:"));
			emailLabel.SetAlignment(0.0f, 0.5f);
			emailEntry.Text = Runtime.Properties.GetProperty("ChangeLogAddIn.Email", "Email Address");

			Table table = new Table(2, 2, false);
			table.RowSpacing = 6;
			table.ColumnSpacing = 6;
			table.Attach(nameLabel, 0, 1, 0, 1, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
			table.Attach(nameEntry, 1, 2, 0, 1);
			table.Attach(emailLabel, 0, 1, 1, 2, AttachOptions.Fill, AttachOptions.Fill, 0, 0);
			table.Attach(emailEntry, 1, 2, 1, 2);

			hBox.PackStart(table, false, false, 0);
		}
		
		public override bool StorePanelContents()
		{
			Runtime.Properties.SetProperty("ChangeLogAddIn.Name", nameEntry.Text);
			Runtime.Properties.SetProperty("ChangeLogAddIn.Email", emailEntry.Text);
			Runtime.Properties.SaveProperties ();
			return true;
		}
	}
}
