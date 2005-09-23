using System;
using System.Collections;

using Gtk;

using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Gui;

namespace MonoDevelop.Gui.Dialogs
{
	internal class DirtyFilesDialog : Gtk.Dialog
	{

		Button btnSaveAndQuit;
		Button btnQuit;
		Button btnCancel;
		TreeView tvFiles;
		TreeStore tsFiles;

		public DirtyFilesDialog() : base (GettextCatalog.GetString ("Save Files"), (Gtk.Window)WorkbenchSingleton.Workbench, DialogFlags.Modal)
		{
			tsFiles = new TreeStore (typeof (string), typeof (bool), typeof (SdiWorkspaceWindow), typeof (bool));
			tvFiles = new TreeView (tsFiles);
			TreeIter topCombineIter = TreeIter.Zero;
			Hashtable projectIters = new Hashtable ();
			if (Runtime.ProjectService.CurrentOpenCombine != null) {
				topCombineIter = tsFiles.AppendValues (String.Format (GettextCatalog.GetString ("Solution: {0}"), Runtime.ProjectService.CurrentOpenCombine.Name), true, null, false);
			}
			foreach (IViewContent viewcontent in WorkbenchSingleton.Workbench.ViewContentCollection) {
				if (!viewcontent.IsDirty)
					continue;
				
				if (viewcontent.HasProject) {
					TreeIter projIter = TreeIter.Zero;
					if (projectIters.ContainsKey (viewcontent.Project))
						projIter = (TreeIter) projectIters[viewcontent.Project];
					else {
						if (topCombineIter.Equals (TreeIter.Zero))
							projIter = tsFiles.AppendValues (String.Format (GettextCatalog.GetString ("Project: {0}"), viewcontent.Project.Name), true, null, false);
						else
							projIter = tsFiles.AppendValues (topCombineIter, String.Format (GettextCatalog.GetString ("Project: {0}"), viewcontent.Project.Name), true, null, false);
						projectIters[viewcontent.Project] = projIter;
					}
					tsFiles.AppendValues (projIter, viewcontent.PathRelativeToProject, true, viewcontent.WorkbenchWindow);
				} else {
					if (viewcontent.ContentName == null) {
						viewcontent.ContentName = System.IO.Path.Combine (Environment.GetEnvironmentVariable ("HOME"), viewcontent.UntitledName);
					}
					tsFiles.AppendValues (viewcontent.ContentName, true, viewcontent.WorkbenchWindow);
				}
			}
			if (!topCombineIter.Equals (TreeIter.Zero)) {
				if (!tsFiles.IterHasChild (topCombineIter))
					tsFiles.Remove (ref topCombineIter); 
			}

			TreeViewColumn mainColumn = new TreeViewColumn ();
			mainColumn.Title = "header";
			
			CellRendererToggle togRender = new CellRendererToggle ();
			togRender.Toggled += new ToggledHandler (toggled);
			mainColumn.PackStart (togRender, false);
			mainColumn.AddAttribute (togRender, "active", 1);
			mainColumn.AddAttribute (togRender, "inconsistent", 3);
			
			CellRendererText textRender = new CellRendererText ();
			mainColumn.PackStart (textRender, true);
			mainColumn.AddAttribute (textRender, "text", 0);

			tvFiles.AppendColumn (mainColumn);
			tvFiles.HeadersVisible = false;
			tvFiles.ExpandAll ();

			ScrolledWindow sc = new ScrolledWindow ();
			sc.Add (tvFiles);
			sc.ShadowType = ShadowType.In;

			sc.BorderWidth = 6;
			this.VBox.PackStart (sc, true, true, 6);
			
			btnSaveAndQuit = new Button (GettextCatalog.GetString ("_Save and Quit"));
			btnQuit = new Button (Gtk.Stock.Quit);
			btnCancel = new Button (Gtk.Stock.Cancel);

			btnSaveAndQuit.Clicked += new EventHandler (SaveAndQuit);
			btnQuit.Clicked += new EventHandler (Quit);
			btnCancel.Clicked += new EventHandler (Cancel);

			this.ActionArea.PackStart (btnCancel);
			this.ActionArea.PackStart (btnQuit);
			this.ActionArea.PackStart (btnSaveAndQuit);
			this.SetDefaultSize (300, 200);
			this.ShowAll ();
		}

		ArrayList arrSaveWorkbenches = new ArrayList ();
		void SaveAndQuit (object o, EventArgs e)
		{
			tsFiles.Foreach (new TreeModelForeachFunc (CollectWorkbenches));
			foreach (SdiWorkspaceWindow window in arrSaveWorkbenches) {
				Runtime.FileUtilityService.ObservedSave(new FileOperationDelegate(window.ViewContent.Save), window.ViewContent.ContentName , FileErrorPolicy.ProvideAlternative);
			}

			Respond (Gtk.ResponseType.Ok);
			Hide ();
		}

		bool CollectWorkbenches (TreeModel model, TreePath path, TreeIter iter)
		{
			if ((bool)tsFiles.GetValue (iter, 1)) {
				if (tsFiles.GetValue (iter, 2) != null)
					arrSaveWorkbenches.Add (tsFiles.GetValue (iter, 2));
			}
			
			return false;
		}

		void Quit (object o, EventArgs e)
		{
			Respond (Gtk.ResponseType.Ok);
			Hide ();
		}

		void Cancel (object o, EventArgs e)
		{
			Respond (Gtk.ResponseType.Cancel);
			Hide ();
		}

		void toggled (object o, ToggledArgs e)
		{
			TreeIter iter;
			tsFiles.GetIterFromString (out iter, e.Path);
			bool newsetting = !(bool)tsFiles.GetValue (iter, 1);
			tsFiles.SetValue (iter, 1, newsetting);
			if (tsFiles.IterHasChild (iter))
				ToggleChildren (iter, newsetting);
			
			TreeIter iterFirst;
			tsFiles.GetIterFirst (out iterFirst);
			if (tsFiles.IterHasChild (iterFirst))
				NewCheckStatus (iterFirst);
		}

		void NewCheckStatus (TreeIter iter)
		{
			if (tsFiles.IterHasChild (iter)) {
				TreeIter childIter;
				tsFiles.IterNthChild (out childIter, iter, 0);
				if (tsFiles.IterHasChild (childIter))
					NewCheckStatus (childIter);
				bool lastsetting = (bool)tsFiles.GetValue (childIter, 1);
				bool inconsistant = (bool)tsFiles.GetValue (childIter, 3);
				bool anytrue;
				anytrue = lastsetting;
				while (tsFiles.IterNext (ref childIter)) {
					if (tsFiles.IterHasChild (childIter))
						NewCheckStatus (childIter);
					bool newsetting = (bool)tsFiles.GetValue (childIter, 1);
					if (newsetting != lastsetting || (bool)tsFiles.GetValue (childIter, 3) == true)
						inconsistant = true;
					if (newsetting)
						anytrue = true;
					lastsetting = newsetting;
				}
				
				tsFiles.SetValue (iter, 3, inconsistant);
				tsFiles.SetValue (iter, 1, anytrue);
			}
		}

		void ToggleChildren (TreeIter iter, bool setting)
		{
			TreeIter newIter;
			tsFiles.IterNthChild (out newIter, iter, 0);
			do {
				tsFiles.SetValue (newIter, 1, setting);
				if (tsFiles.IterHasChild (newIter))
					ToggleChildren (newIter, setting);
			}
			while (tsFiles.IterNext (ref newIter));
		}
	}
}
