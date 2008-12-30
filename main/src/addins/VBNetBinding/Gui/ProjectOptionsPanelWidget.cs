/*
 * ProjectOptionsPanelWidget.cs.
 *
 * Author:
 *   Rolf Bjarne Kvinge <RKvinge@novell.com>
 *
 * Copyright 2008 Novell, Inc. (http://www.novell.com)
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
 */

using System;
using System.IO;

using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Components;

using MonoDevelop.VBNetBinding.Extensions;

namespace MonoDevelop.VBNetBinding
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ProjectOptionsPanelWidget : Gtk.Bin
	{
		DotNetProject project;
		Gtk.ListStore imports = new Gtk.ListStore (typeof (String));
		
		public ProjectOptionsPanelWidget (MonoDevelop.Projects.Project project)
		{
			Gtk.ListStore store;
			Gtk.CellRendererText cr;

			this.Build();

			this.project = (DotNetProject) project;
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues (GettextCatalog.GetString ("Executable"));
			store.AppendValues (GettextCatalog.GetString ("Library"));
			store.AppendValues (GettextCatalog.GetString ("Executable with GUI"));
			store.AppendValues (GettextCatalog.GetString ("Module")); 
			compileTargetCombo.Model = store;
			compileTargetCombo.PackStart (cr, true);
			compileTargetCombo.AddAttribute (cr, "text", 0);
			compileTargetCombo.Active = (int) this.project.CompileTarget;
			compileTargetCombo.Changed += delegate(object sender, EventArgs e) {
				entryMainClass.Sensitive = compileTargetCombo.Active != (int) CompileTarget.Library	&& compileTargetCombo.Active != (int) CompileTarget.Module;
			};

			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("WindowsForms");
			store.AppendValues ("Windows");
			store.AppendValues ("Console");
			txtMyType.Model = store;
			txtMyType.TextColumn = 0;
			switch (this.project.GetMyType ()) {
			case "WindowsForms":
				txtMyType.Active = 0;
				break;
			case "Windows":
				txtMyType.Active = 1;
				break;
			case "Console":
				txtMyType.Active = 2;
				break;
			case null:
			case "":
				break;
			default:
				txtMyType.AppendText (this.project.GetMyType ());
				txtMyType.Active = 3;
				break;
			}

			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("Binary");
			store.AppendValues ("Text");
			cmbOptionCompare.Model = store;
			cmbOptionCompare.PackStart (cr, true);
			cmbOptionCompare.AddAttribute (cr, "text", 0);
			cmbOptionCompare.Active = this.project.GetOptionCompare () == "Text" ? 1 : 0;
				
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("On");
			store.AppendValues ("Off");
			cmbOptionExplicit.Model = store;
			cmbOptionExplicit.PackStart (cr, true);
			cmbOptionExplicit.AddAttribute (cr, "text", 0);
			cmbOptionExplicit.Active = this.project.GetOptionExplicit () == "Off" ? 1 : 0;
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("On");
			store.AppendValues ("Off");
			cmbOptionInfer.Model = store;
			cmbOptionInfer.PackStart (cr, true);
			cmbOptionInfer.AddAttribute (cr, "text", 0);
			cmbOptionInfer.Active = this.project.GetOptionInfer () == "Off" ? 1 : 0;
			
			cr = new Gtk.CellRendererText ();
			store = new Gtk.ListStore (typeof (string));
			store.AppendValues ("On");
			store.AppendValues ("Off");
			cmbOptionStrict.Model = store;
			cmbOptionStrict.PackStart (cr, true);
			cmbOptionStrict.AddAttribute (cr, "text", 0);
			cmbOptionStrict.Active = this.project.GetOptionStrict () == "Off" ? 1 : 0;
			
			treeview1.AppendColumn ("Import", new Gtk.CellRendererText (), "text", 0);
			treeview1.Model = imports;
			imports.SetSortColumnId (0, Gtk.SortType.Ascending);
			LoadImports ();

			// Codepage
			string foundEncoding = null;
			string currentCodepage = this.project.GetCodePage ();
			foreach (TextEncoding e in TextEncoding.SupportedEncodings) {
				if (e.CodePage == -1)
					continue;
				if (e.Id == currentCodepage)
					foundEncoding = e.Id;
				cmbCodePage.AppendText (e.Id);
			}
			if (foundEncoding != null)
				cmbCodePage.Entry.Text = foundEncoding;
			else if (!string.IsNullOrEmpty (currentCodepage))
				cmbCodePage.Entry.Text = currentCodepage;
			
		}
		
		public void StorePanelContents ()
		{
			this.project.SetIsOptionCompareBinary (cmbOptionStrict.ActiveText == "Binary");
			this.project.SetIsOptionExplicit (cmbOptionExplicit.ActiveText == "On");
			this.project.SetIsOptionInfer (cmbOptionInfer.ActiveText == "On");
			this.project.SetIsOptionStrict (cmbOptionStrict.ActiveText == "On");
			this.project.SetMytype (txtMyType.ActiveText);
			this.project.SetMainClass (entryMainClass.ActiveText);
			this.project.CompileTarget = (CompileTarget) compileTargetCombo.Active;
			this.project.SetCodePage (cmbCodePage.Entry.Text);
		}
		
		protected virtual void OnCmdAddClicked (object sender, System.EventArgs e)
		{
			bool exists = false;
			Console.WriteLine ("OnCmdAddClicked");
			
			foreach (ProjectFile file in project.Files) {
				if (file.BuildAction == "Import" && System.IO.Path.GetFileName (file.FilePath) == txtImport.Text) {
					exists = true;
					break;
				}
			}

			if (!exists) {
				project.Files.Add (new ProjectFile (txtImport.Text, "Import"));
				LoadImports ();
			}
		}

		protected virtual void OnCmdRemoveClicked (object sender, System.EventArgs e)
		{
			bool removed = false;

			Console.WriteLine ("OnCmdRemoveClicked");
			treeview1.Selection.SelectedForeach (delegate (Gtk.TreeModel model, Gtk.TreePath path, Gtk.TreeIter iter) 
			{
				string import;
				GLib.Value value = new GLib.Value ();
				
				model.GetValue (iter, 0, ref value);

				import = value.Val as string;

				if (string.IsNullOrEmpty (import))
					return;
				
				foreach (ProjectFile file in project.Files) {
					if (file.BuildAction == "Import" && System.IO.Path.GetFileName (file.FilePath) == import) {
						project.Files.Remove (file);
						removed = true;
						break;
					}
				}
				
			});
			if (removed)
				LoadImports ();
		}

		protected virtual void OnTxtImportChanged (object sender, System.EventArgs e)
		{
			cmdAdd.Sensitive = !string.IsNullOrEmpty (txtImport.Text);
		}

		private void LoadImports ()
		{
			imports.Clear ();
			foreach (ProjectFile file in project.Files) {
				if (file.BuildAction == "Import")
					imports.AppendValues (System.IO.Path.GetFileName (file.FilePath));
			}
		}
	}
}
