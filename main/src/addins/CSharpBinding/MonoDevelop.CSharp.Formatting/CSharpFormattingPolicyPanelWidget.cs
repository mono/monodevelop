// 
// CSharpFormattingPolicyPanelWidget.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Ide;
namespace MonoDevelop.CSharp.Formatting
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CSharpFormattingPolicyPanelWidget : Gtk.Bin
	{
		Mono.TextEditor.TextEditor texteditor = new Mono.TextEditor.TextEditor ();
		
		public CSharpFormattingPolicyPanelWidget ()
		{
			this.Build ();
			buttonNew.Clicked += HandleButtonNewClicked;
			buttonImport.Clicked += HandleButtonImportClicked; 
			buttonExport.Clicked += HandleButtonExportClicked;
			buttonEdit.Clicked += HandleButtonEditClicked;
			buttonRemove.Clicked += HandleButtonRemoveClicked;
			
			var options = MonoDevelop.SourceEditor.DefaultSourceEditorOptions.Instance;
			texteditor.Options.FontName = options.FontName;
			texteditor.Options.ColorScheme = options.ColorScheme;
			texteditor.Options.ShowFoldMargin = false;
			texteditor.Options.ShowIconMargin = false;
			scrolledwindow1.Child = texteditor;
			ShowAll ();
			InitComboBox ();
		}

		void HandleButtonRemoveClicked (object sender, EventArgs e)
		{
			if (comboboxProfiles.Active < 0)
				return;
			FormattingProfileService.Remove (FormattingProfileService.Profiles[comboboxProfiles.Active]);
			InitComboBox ();
			comboboxProfiles.Active = 0;
		}

		void HandleButtonEditClicked (object sender, EventArgs e)
		{
			if (comboboxProfiles.Active < 0)
				return;
			var editDialog = new CSharpFormattingProfileDialog (FormattingProfileService.Profiles[comboboxProfiles.Active]);
			MessageService.ShowCustomDialog (editDialog);
			editDialog.Destroy ();
		}

		void HandleButtonImportClicked (object sender, EventArgs e)
		{
			var dialog = new SelectFileDialog (GettextCatalog.GetString ("Profile to import"), Gtk.FileChooserAction.Open) {
				TransientFor = this.Toplevel as Gtk.Window,
			};
			dialog.AddFilter (null, "*.xml");
			if (!dialog.Run ())
				return;
			int selection = comboboxProfiles.Active;
			FormattingProfileService.AddProfile (CSharpFormattingPolicy.Load (dialog.SelectedFile));
			InitComboBox ();
			comboboxProfiles.Active = selection;
		}

		void HandleButtonExportClicked (object sender, EventArgs e)
		{
			if (comboboxProfiles.Active < 0)
				return;
			var dialog = new SelectFileDialog (GettextCatalog.GetString ("Export profile"), Gtk.FileChooserAction.Save) {
				TransientFor = this.Toplevel as Gtk.Window,
			};
			dialog.AddFilter (null, "*.xml");
			if (!dialog.Run ())
				return;
			FormattingProfileService.Profiles[comboboxProfiles.Active].Save (dialog.SelectedFile);
		}
		
		void InitComboBox ()
		{
			comboboxProfiles.Clear ();
			foreach (var p in FormattingProfileService.Profiles) {
				comboboxProfiles.AppendText (p.Name);
			}
			if (FormattingProfileService.Profiles.Count > 0) {
				comboboxProfiles.Active = 0;
			} else {
				comboboxProfiles.Active = -1;
			}
			buttonEdit.Sensitive = buttonRemove.Sensitive = buttonExport.Sensitive = comboboxProfiles.Active >= 0;
		}

		void HandleButtonNewClicked (object sender, EventArgs e)
		{
			var newProfileDialog = new NewFormattingProfileDialog ();
			int result = MessageService.ShowCustomDialog (newProfileDialog);
			if (result == (int)Gtk.ResponseType.Ok) {
				var baseProfile = newProfileDialog.InitializeFrom ?? new CSharpFormattingPolicy ();
				var newProfile = baseProfile.Clone ();
				newProfile.Name = newProfileDialog.NewProfileName;
				FormattingProfileService.AddProfile (newProfile);
				InitComboBox ();
				comboboxProfiles.Active = FormattingProfileService.Profiles.Count - 1;
			}
			newProfileDialog.Destroy ();
		}
	}
}

