using System;
using System.Collections;
using System.Collections.Generic;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal class DirtyFilesDialog : IdeDialog
	{
		Button btnSaveAndQuit;
		Button btnQuit;
		Button btnCancel;
		TreeView tvFiles;
		TreeStore tsFiles;
		CellRendererToggle togRender;
		CellRendererText textRender;

		public DirtyFilesDialog () : this (IdeApp.Workbench.Documents, true, true)
		{
		}

		public DirtyFilesDialog (IReadOnlyList<Document> docs, bool closeWorkspace, bool groupByProject) :
			base (GettextCatalog.GetString ("Save Files"), IdeApp.Workbench.RootWindow, DialogFlags.Modal)
		{
			Accessible.Name = "Dialog.DirtyFiles";

			string description;
			if (closeWorkspace) {
				description = GettextCatalog.GetString ("Select which files should be saved before closing the workspace");
			} else {
				description = GettextCatalog.GetString ("Select which files should be saved before quitting the application");
			}
			Accessible.Description = description;

			tsFiles = new TreeStore (typeof(string), typeof(bool), typeof(SdiWorkspaceWindow), typeof(bool));
			tvFiles = new TreeView (tsFiles);
			TreeIter topCombineIter = TreeIter.Zero;
			Hashtable projectIters = new Hashtable ();

			tvFiles.Accessible.Name = "Dialog.DirtyFiles.FileList";
			tvFiles.Accessible.SetLabel (GettextCatalog.GetString ("Dirty Files"));
			tvFiles.Accessible.Description = GettextCatalog.GetString ("The list of files which have changes and need saving");
			foreach (Document doc in docs) {
				if (!doc.IsDirty)
					continue;
				
				ViewContent viewcontent = doc.Window.ViewContent;
				 
				if (groupByProject && viewcontent.Owner != null) {
					TreeIter projIter = TreeIter.Zero;
					if (projectIters.ContainsKey (viewcontent.Owner))
						projIter = (TreeIter)projectIters [viewcontent.Owner];
					else {
						if (topCombineIter.Equals (TreeIter.Zero))
							projIter = tsFiles.AppendValues (GettextCatalog.GetString ("Project: {0}", viewcontent.Owner.Name), true, null, false);
						else
							projIter = tsFiles.AppendValues (topCombineIter, GettextCatalog.GetString ("Project: {0}", viewcontent.Owner.Name), true, null, false);
						projectIters [viewcontent.Owner] = projIter;
					}
					tsFiles.AppendValues (projIter, viewcontent.PathRelativeToProject, true, viewcontent.WorkbenchWindow);
				} else {
					tsFiles.AppendValues (GetContentFileName (viewcontent), true, viewcontent.WorkbenchWindow);
				}
			}
			if (!topCombineIter.Equals (TreeIter.Zero)) {
				if (!tsFiles.IterHasChild (topCombineIter))
					tsFiles.Remove (ref topCombineIter); 
			}

			TreeViewColumn mainColumn = new TreeViewColumn ();
			mainColumn.Title = "header";
			
			togRender = new CellRendererToggle ();
			togRender.Toggled += toggled;
			mainColumn.PackStart (togRender, false);
			mainColumn.AddAttribute (togRender, "active", 1);
			mainColumn.AddAttribute (togRender, "inconsistent", 3);
			
			textRender = new CellRendererText ();
			mainColumn.PackStart (textRender, true);
			mainColumn.AddAttribute (textRender, "text", 0);

			tvFiles.AppendColumn (mainColumn);
			tvFiles.HeadersVisible = false;
			tvFiles.ExpandAll ();

			ScrolledWindow sc = new ScrolledWindow ();
			sc.Accessible.SetShouldIgnore (true);
			sc.Add (tvFiles);
			sc.ShadowType = ShadowType.In;

			sc.BorderWidth = 6;
			this.VBox.PackStart (sc, true, true, 6);

			btnSaveAndQuit = new Button (closeWorkspace ? GettextCatalog.GetString ("_Save and Quit") : GettextCatalog.GetString ("_Save and Close"));
			btnSaveAndQuit.Accessible.Name = "Dialog.DirtyFiles.SaveAndQuit";

			if (closeWorkspace) {
				description = GettextCatalog.GetString ("Save the selected files and close the workspace");
			} else {
				description = GettextCatalog.GetString ("Save the selected files and quit the application");
			}
			btnSaveAndQuit.Accessible.Description = description;

			btnQuit = new Button (closeWorkspace ? Gtk.Stock.Quit : Gtk.Stock.Close);
			btnQuit.Accessible.Name = "Dialog.DirtyFiles.Quit";
			if (closeWorkspace) {
				description = GettextCatalog.GetString ("Close the workspace");
			} else {
				description = GettextCatalog.GetString ("Quit the application");
			}
			btnQuit.Accessible.Description = description;

			btnCancel = new Button (Gtk.Stock.Cancel);
			btnCancel.Accessible.Name = "Dialog.DirtyFiles.Cancel";
			if (closeWorkspace) {
				description = GettextCatalog.GetString ("Cancel closing the workspace");
			} else {
				description = GettextCatalog.GetString ("Cancel quitting the application");
			}
			btnCancel.Accessible.Description = description;

			btnSaveAndQuit.Clicked += SaveAndQuit;
			btnQuit.Clicked += Quit;
			btnCancel.Clicked += Cancel;

			this.ActionArea.PackStart (btnCancel);
			this.ActionArea.PackStart (btnQuit);
			this.ActionArea.PackStart (btnSaveAndQuit);
			this.SetDefaultSize (300, 200);
			this.Child.ShowAll ();
		}

		static string GetContentFileName (ViewContent viewcontent)
		{
			return viewcontent.ContentName ?? System.IO.Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.Personal), viewcontent.UntitledName);
		}

		protected override void OnDestroyed ()
		{
			btnSaveAndQuit.Clicked -= SaveAndQuit;
			btnQuit.Clicked -= Quit;
			btnCancel.Clicked -= Cancel;
			if (togRender != null) {
				togRender.Toggled -= toggled;
				togRender.Destroy ();
				togRender = null;
			}
			if (textRender != null) {
				textRender.Destroy ();
				textRender = null;
			}
			base.OnDestroyed ();
		}
		
		async void SaveAndQuit (object o, EventArgs e)
		{
			Sensitive = false;

			List<Task> saveTasks = new List<Task> ();
			tsFiles.Foreach (delegate (TreeModel model, TreePath path, TreeIter iter) {
				var window = tsFiles.GetValue (iter, 2) as SdiWorkspaceWindow;
				if (window == null)
					return false;
				if ((bool)tsFiles.GetValue (iter, 1)) {
					saveTasks.Add (window.ViewContent.Save (GetContentFileName(window.ViewContent)));
				} else {
					window.ViewContent.DiscardChanges ();
				}
				return false;
			});

			try {
				await Task.WhenAll (saveTasks);
			} finally {
				Sensitive = true;
			}
	
			Respond (Gtk.ResponseType.Ok);
			Hide ();
		}

		void Quit (object o, EventArgs e)
		{
			tsFiles.Foreach (delegate (TreeModel model, TreePath path, TreeIter iter) {
				var window = tsFiles.GetValue (iter, 2) as SdiWorkspaceWindow;
				if (window == null)
					return false;
				window.ViewContent.DiscardChanges ();
				return false;
			});
			
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
