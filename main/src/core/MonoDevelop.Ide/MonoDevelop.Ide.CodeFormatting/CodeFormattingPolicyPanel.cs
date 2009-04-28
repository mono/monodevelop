// 
// CodeFormattingPolicyPanel.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Ide.CodeFormatting
{
	class CodeFormattingPolicyPanel : PolicyOptionsPanel<CodeFormattingPolicy>
	{
		protected override string PolicyTitleWithMnemonic {
			get { return GettextCatalog.GetString ("Code _Format"); }
		}
		
		CodeFormattingPolicyPanelWidget panel;
		
		public override Widget CreatePanelWidget ()
		{
			panel = new CodeFormattingPolicyPanelWidget ();
			panel.ShowAll ();
			return panel;
		}
		
		protected override void LoadFrom (CodeFormattingPolicy policy)
		{
			panel.Policy = policy;
		}
		
		protected override CodeFormattingPolicy GetPolicy ()
		{
			return panel.Policy;
		}
	}
	
	[System.ComponentModel.ToolboxItem(true)]
	public partial class CodeFormattingPolicyPanelWidget : Gtk.Bin
	{
		List<string> policies = new List<string> ();
		List<CodeFormatSettings> settings;
		CodeFormatDescription description;
		ListStore formatStore = new ListStore (typeof (string), typeof (CodeFormatSettings));
		
		public CodeFormattingPolicy Policy {
			get;
			set;
		}
		
		public void FillFormattingPolicies ()
		{
			formatStore.Clear ();
			foreach (CodeFormatSettings setting in settings) {
				formatStore.AppendValues (setting.Name, setting);
			}
			if (settings.Count == 0) 
				formatStore.AppendValues ("default", null);
			comboboxFormattingPolicies.Active = 0;
		}
		
		public CodeFormattingPolicyPanelWidget()
		{
			this.Build();
			ListStore store = new ListStore (typeof (string), typeof (string));
			store.AppendValues ("text/x-csharp", "default");
			treeviewUsedProfiles.Model = store;
			treeviewUsedProfiles.AppendColumn ("mime type", new CellRendererText (), "text", 0);
			treeviewUsedProfiles.AppendColumn ("profile", new CellRendererText (), "text", 1);
			description = TextFileService.GetFormatDescription ("text/x-csharp");
			settings = new List<CodeFormatSettings> (TextFileService.GetAvailableSettings (description));
			
			
			
			comboboxFormattingPolicies.Model = formatStore;
			/*Gtk.CellRendererText ctx = new Gtk.CellRendererText ();
			comboboxFormattingPolicies.PackStart (ctx, true);
			comboboxFormattingPolicies.AddAttribute (ctx, "text", 0);*/
			
			Mono.TextEditor.TextEditorOptions options = new Mono.TextEditor.TextEditorOptions ();
			options.ShowLineNumberMargin = false;
			options.ShowFoldMargin = false;
			options.ShowIconMargin = false;
			options.ShowInvalidLines = false;
			options.ShowSpaces = options.ShowTabs = options.ShowEolMarkers = false;
			options.ColorScheme = PropertyService.Get ("ColorScheme", "Default");
			texteditor1.Options = options;
			texteditor1.Document.ReadOnly = true;
			texteditor1.Document.MimeType = description.MimeType;
			
			buttonAdd.Clicked += ButtonAddClicked;
			buttonEdit.Clicked += ButtonEditClicked;
			buttonImport.Clicked += ButtonImportClicked;
			buttonRemove.Clicked += ButtonRemoveClicked;
			FillFormattingPolicies ();
		}
		
		void ButtonRemoveClicked (object sender, EventArgs e)
		{
			int a = comboboxFormattingPolicies.Active;
			if (a >= 0 && a < settings.Count)
				settings.RemoveAt (a);
			FillFormattingPolicies ();
			TextFileService.SetSettings (description, settings);
		}
		
		void ButtonImportClicked (object sender, EventArgs e)
		{
			Gtk.FileChooserDialog dialog = new Gtk.FileChooserDialog (GettextCatalog.GetString ("Import Profile"),
			                                                          null,
			                                                          FileChooserAction.Open,
			                                                          Gtk.Stock.Cancel, Gtk.ResponseType.Cancel, Gtk.Stock.Open, Gtk.ResponseType.Ok);
			FileFilter f1 = new FileFilter();
			f1.Name = "*.xml";
			f1.AddPattern ("*.xml");
			dialog.AddFilter (f1);
			FileFilter f2 = new FileFilter();
			f1.Name = "*";
			f2.AddPattern ("*");
			dialog.AddFilter (f2);
			if (ResponseType.Ok == (ResponseType)dialog.Run ()) {
				settings.Add (description.ImportSettings (dialog.Filename));
				int a = comboboxFormattingPolicies.Active;
				FillFormattingPolicies ();
				comboboxFormattingPolicies.Active = a;
			}
			dialog.Destroy ();
		}

		void ButtonEditClicked (object sender, EventArgs e)
		{
			EditFormattingPolicyDialog d = new EditFormattingPolicyDialog ();
			CodeFormatSettings setting = new CodeFormatSettings ("New settings");
			int a = comboboxFormattingPolicies.Active;
			if (a >= 0 && a < settings.Count) 
				setting = settings [a];
			d.SetFormat (description, setting);
			d.Run ();
			d.Destroy ();
			FillFormattingPolicies ();
			comboboxFormattingPolicies.Active = a;
		}

		void ButtonAddClicked (object sender, EventArgs e)
		{
			AddPolicyDialog addPolicy = new AddPolicyDialog (settings);
			ResponseType response = (ResponseType)addPolicy.Run ();
			if (response == ResponseType.Ok) {
				settings.Add (new CodeFormatSettings (TextFileService.GetSettings (description, addPolicy.InitFrom), addPolicy.NewPolicyName));
				FillFormattingPolicies ();
				comboboxFormattingPolicies.Active = settings.Count - 1;
				TextFileService.SetSettings (description, settings);
			}
			addPolicy.Destroy ();
		}
	}
	
}
