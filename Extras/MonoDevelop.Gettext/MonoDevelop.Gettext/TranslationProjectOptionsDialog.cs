//
// TranslationProjectOptionsDialog.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Gettext
{
	public partial class TranslationProjectOptionsDialog : Gtk.Dialog
	{
		TranslationProject project;
		public TranslationProjectOptionsDialog (TranslationProject project)
		{
			this.project = project;
			this.Build();
			
			TranslationProjectConfiguration config = this.project.ActiveConfiguration as TranslationProjectConfiguration;
			entryPackageName.Text        = config.PackageName;
			entryRelPath.Text            = config.RelPath;
			folderentrySystemPath.Path   = config.AbsPath;
			radiobuttonRelPath.Active    = config.OutputType == TranslationOutputType.RelativeToOutput;
			radiobuttonSystemPath.Active = config.OutputType == TranslationOutputType.SystemPath;
			
			entryPackageName.Changed += new EventHandler (UpdateInitString);
			entryRelPath.Changed += new EventHandler (UpdateInitString);
			folderentrySystemPath.PathChanged  += new EventHandler (UpdateInitString);
			radiobuttonRelPath.Activated += new EventHandler (UpdateInitString);
			radiobuttonSystemPath.Activated += new EventHandler (UpdateInitString);
			
			UpdateInitString (this, EventArgs.Empty);
			this.buttonOk.Clicked += delegate {
				config.PackageName = entryPackageName.Text;
				config.RelPath = entryRelPath.Text;
				config.AbsPath = folderentrySystemPath.Path;
				if (radiobuttonRelPath.Active) {
					config.OutputType = TranslationOutputType.RelativeToOutput;
				} else {
					config.OutputType = TranslationOutputType.SystemPath;
				}
				this.Destroy ();
			};
			this.buttonCancel.Clicked += delegate {
				this.Destroy ();
			};
			
			store = new TreeStore (typeof(string), typeof(bool), typeof(string), typeof(CombineEntry), typeof(bool));
			treeviewProjectList.Model = store;
			treeviewProjectList.HeadersVisible = false;
			
			TreeViewColumn col = new TreeViewColumn ();
			
			CellRendererToggle cellRendererToggle = new CellRendererToggle ();
			cellRendererToggle.Toggled += new ToggledHandler (ActiveToggled);
			cellRendererToggle.Activatable = true;
			col.PackStart (cellRendererToggle, false);
			col.AddAttribute (cellRendererToggle, "active", 1);
			col.AddAttribute (cellRendererToggle, "visible", 4);
			
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock_id", 0);
			
			CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", 2);
			
			treeviewProjectList.AppendColumn (col);
			
			FillTree (TreeIter.Zero, project.ParentCombine);
		}
		
		void ActiveToggled (object sender, ToggledArgs e)
		{
			TreeIter iter;
			if (store.GetIterFromString (out iter, e.Path)) {
				bool         isTogglod = (bool)store.GetValue (iter, 1);
				CombineEntry entry     = (CombineEntry)store.GetValue (iter, 3);
				if (entry is Project) {
					TranslationProjectInformation info = project.GetProjectInformation (entry, true);
					info.IsIncluded = !isTogglod;
					store.SetValue (iter, 1, !isTogglod);
				}
			}
		}
		
		TreeStore store;
		string GetIcon (CombineEntry entry)
		{
			if (entry is Combine)
				return MonoDevelop.Core.Gui.Stock.CombineIcon;
			
			if (entry is Project)
				return IdeApp.Services.Icons.GetImageForProjectType (((Project)entry).ProjectType);
			
			return MonoDevelop.Core.Gui.Stock.SolutionIcon;
		}
		
		bool IsIncluded (CombineEntry entry)
		{
			if (entry is Combine) {
				foreach (CombineEntry childEntry in ((Combine)entry).Entries)
					if (!IsIncluded (childEntry))
						return false;
				return true;
			}
			
			TranslationProjectInformation info = project.GetProjectInformation (entry, false);
			if (info != null)
				return info.IsIncluded;
			return true;
		}
			
		void FillTree (TreeIter iter, CombineEntry entry)
		{
			TreeIter curIter;
			if (!iter.Equals (TreeIter.Zero)) {
				curIter = store.AppendValues (iter, GetIcon (entry), entry is Combine ? false : IsIncluded (entry), entry.Name, entry, !(entry is Combine));
			} else {
				curIter = store.AppendValues (GetIcon (entry), entry is Combine ? false : IsIncluded (entry), entry.Name, entry, !(entry is Combine));
			}
			if (entry is Combine) {
				// Add solutions first, then projects
				foreach (CombineEntry childEntry in ((Combine)entry).Entries)
					if (childEntry is Combine)
						FillTree (curIter, childEntry);
				foreach (CombineEntry childEntry in ((Combine)entry).Entries)
					if (!(childEntry is TranslationProject) && (childEntry is Project))
						FillTree (curIter, childEntry);
			}
		}
		
		void UpdateInitString (object sender, EventArgs e)
		{
			string path;
			if (radiobuttonRelPath.Active) {
				path = "./" + entryRelPath.Text;
			} else {
				path = folderentrySystemPath.Path;
			}
			labelInitString.Text = "Mono.Unix.Catalog.Init (\"" + entryPackageName.Text + "\", \"" + path + "\");";
		}
		
	}
}
