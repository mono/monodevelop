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
using System.Collections.Generic;
namespace MonoDevelop.CSharp.Formatting
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CSharpFormattingPolicyPanelWidget : Gtk.Bin
	{
		Mono.TextEditor.TextEditor texteditor = new Mono.TextEditor.TextEditor ();
		Gtk.ListStore model = new Gtk.ListStore (typeof(string));
		List<CSharpFormattingPolicy> policies = new List<CSharpFormattingPolicy> ();
		
		public CSharpFormattingPolicy Policy {
			get {
				if (comboboxProfiles.Active < 0)
					return null;
				return policies[comboboxProfiles.Active];
			}
			set {
				Console.WriteLine ("-----------");
				Console.WriteLine (Environment.StackTrace);
				for (int i = 0; i < policies.Count; i++) {
					if (policies[i].Equals (value)) {
						comboboxProfiles.Active = i;
						return;
					}
				}
			
				if (string.IsNullOrEmpty (value.Name))
					value.Name = GettextCatalog.GetString ("Custom");
				policies.Add (value);
				InitComboBox ();
				
				comboboxProfiles.Active = policies.Count - 1;
			}
		}
		
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
			policies.AddRange (FormattingProfileService.Profiles);
			comboboxProfiles.Model = model;
			ShowAll ();
			InitComboBox ();
		}

		void HandleButtonRemoveClicked (object sender, EventArgs e)
		{
			if (comboboxProfiles.Active < 0)
				return;
			FormattingProfileService.Remove (policies[comboboxProfiles.Active]);
			InitComboBox ();
			comboboxProfiles.Active = 0;
		}

		void HandleButtonEditClicked (object sender, EventArgs e)
		{
			if (comboboxProfiles.Active < 0)
				return;
			var editDialog = new CSharpFormattingProfileDialog (policies[comboboxProfiles.Active]);
			MessageService.ShowCustomDialog (editDialog);
			editDialog.Destroy ();
			InitComboBox ();
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
			var p = CSharpFormattingPolicy.Load (dialog.SelectedFile);
			FormattingProfileService.AddProfile (p);
			policies.Add (p);
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
			policies[comboboxProfiles.Active].Save (dialog.SelectedFile);
		}
		
		void InitComboBox ()
		{
			model.Clear ();
			foreach (var p in policies) {
				model.AppendValues (p.Name);
			}
			if (comboboxProfiles.Active < 0)
				comboboxProfiles.Active = policies.Count > 0 ? 0 : -1;
			buttonEdit.Sensitive = buttonRemove.Sensitive = buttonExport.Sensitive = comboboxProfiles.Active >= 0;
		}

		void HandleButtonNewClicked (object sender, EventArgs e)
		{
			var newProfileDialog = new NewFormattingProfileDialog (policies);
			int result = MessageService.ShowCustomDialog (newProfileDialog);
			if (result == (int)Gtk.ResponseType.Ok) {
				var baseProfile = newProfileDialog.InitializeFrom ?? new CSharpFormattingPolicy ();
				var newProfile = baseProfile.Clone ();
				newProfile.IsBuiltIn = false;
				newProfile.Name = newProfileDialog.NewProfileName;
				policies.Add (newProfile);
				FormattingProfileService.AddProfile (newProfile);
				InitComboBox ();
				comboboxProfiles.Active = policies.Count - 1;
			}
			newProfileDialog.Destroy ();
		}
	}
}

