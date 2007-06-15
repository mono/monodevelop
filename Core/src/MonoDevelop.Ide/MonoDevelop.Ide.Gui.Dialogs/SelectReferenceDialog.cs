// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections.Generic;

using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;

using Gtk;
using Glade;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal interface IReferencePanel
	{
	}
	
	internal class SelectReferenceDialog
	{
		ListStore refTreeStore;
		[Widget] Dialog    AddReferenceDialog;
		[Widget] TreeView  ReferencesTreeView;
//		[Widget] Button    okbutton;
//		[Widget] Button    cancelbutton;
		[Widget] Button    RemoveReferenceButton;
		[Widget] Notebook  mainBook;
		
		GacReferencePanel gacRefPanel;
		ProjectReferencePanel projectRefPanel;
		AssemblyReferencePanel assemblyRefPanel;
		IProject configureProject;
		
		const int NameColumn = 0;
		const int TypeNameColumn = 1;
		const int LocationColumn = 2;
		const int ProjectReferenceColumn = 3;
		const int IconColumn = 4;
		
		public List<ProjectReferenceProjectItem> ReferenceInformations {
			get {
				List<ProjectReferenceProjectItem> referenceInformations = new List<ProjectReferenceProjectItem>();
				Gtk.TreeIter looping_iter;
				if (!refTreeStore.GetIterFirst (out looping_iter)) {
					return referenceInformations;
				}
				do {
					referenceInformations.Add ((ProjectReferenceProjectItem) refTreeStore.GetValue(looping_iter, ProjectReferenceColumn));
				} while (refTreeStore.IterNext (ref looping_iter));
				return referenceInformations;
			}
		}

		public int Run ()
		{
			return AddReferenceDialog.Run ();
		}

		public void Hide ()
		{
			AddReferenceDialog.Hide ();
		}
		
		public Gtk.Window Window {
			get { return AddReferenceDialog; }
		}

		public void SetProject (IProject configureProject)
		{
			this.configureProject = configureProject;
			((ListStore) ReferencesTreeView.Model).Clear ();

			projectRefPanel.SetProject (configureProject);
			gacRefPanel.SetProject (configureProject);
			gacRefPanel.Reset ();
			assemblyRefPanel.SetBasePath (configureProject.BasePath);

			foreach (ProjectItem item in configureProject.Items) {
				ProjectReferenceProjectItem refInfo = item as ProjectReferenceProjectItem;
				if (refInfo == null)
					continue;
				AddReference (refInfo);
			}

			OnChanged (null, null);
		}
		
		TreeIter AddReference (ReferenceProjectItem refInfo)
		{
			if (refInfo is ProjectReferenceProjectItem)
				return AddProjectReference (refInfo as ProjectReferenceProjectItem);
			
			if (!String.IsNullOrEmpty (refInfo.HintPath))
				return AddAssemplyReference (refInfo);
				
			return AddGacReference (refInfo);
			
/*			switch (refInfo.ReferenceType) {
				case ReferenceType.Assembly:
					return AddAssemplyReference (refInfo);
				case ReferenceType.Project:
					return AddProjectReference (refInfo);
				case ReferenceType.Gac:
					return AddGacReference (refInfo);
				default:
					return TreeIter.Zero;
			}*/
		}

		TreeIter AddAssemplyReference (ReferenceProjectItem refInfo)
		{
			return refTreeStore.AppendValues (System.IO.Path.GetFileName (refInfo.Include), GetTypeText (refInfo), System.IO.Path.GetFullPath (refInfo.HintPath), refInfo, "md-closed-folder");
		}

		TreeIter AddProjectReference (ProjectReferenceProjectItem refInfo)
		{
			Solution c = ProjectService.Solution; 
			if (c == null) return TreeIter.Zero;
			
			IProject p = ProjectService.FindProject (refInfo.Name).Project;
			if (p == null) return TreeIter.Zero;
			
			string iconName = Services.Icons.GetImageForProjectType (p.Language);
			projectRefPanel.SignalRefChange (refInfo.Include, true);
			return refTreeStore.AppendValues (System.IO.Path.GetFileName (refInfo.Include), GetTypeText (refInfo), p.BasePath, refInfo, iconName);
		}

		TreeIter AddGacReference (ReferenceProjectItem refInfo)
		{
			gacRefPanel.SignalRefChange (refInfo.Include, true);
			return refTreeStore.AppendValues (System.IO.Path.GetFileNameWithoutExtension (refInfo.Include), GetTypeText (refInfo), refInfo.Include, refInfo, "md-package");
		}
		
		public SelectReferenceDialog(IProject configureProject)
		{
			Glade.XML refXML = new Glade.XML (null, "Base.glade", "AddReferenceDialog", null);
			refXML.Autoconnect (this);
			
			refTreeStore = new ListStore (typeof (string), typeof(string), typeof(string), typeof(ReferenceProjectItem), typeof(string));
			ReferencesTreeView.Model = refTreeStore;

			TreeViewColumn col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString("Reference");
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			col.PackStart (crp, false);
			col.AddAttribute (crp, "stock-id", IconColumn);
			CellRendererText text_render = new CellRendererText ();
			col.PackStart (text_render, true);
			col.AddAttribute (text_render, "text", NameColumn);
			
			ReferencesTreeView.AppendColumn (col);
			ReferencesTreeView.AppendColumn (GettextCatalog.GetString ("Type"), new CellRendererText (), "text", TypeNameColumn);
			ReferencesTreeView.AppendColumn (GettextCatalog.GetString ("Location"), new CellRendererText (), "text", LocationColumn);
			
			projectRefPanel = new ProjectReferencePanel (this);
			gacRefPanel = new GacReferencePanel (this);
			assemblyRefPanel = new AssemblyReferencePanel (this);
			SetProject (configureProject);
			
			mainBook.RemovePage (mainBook.CurrentPage);
			mainBook.AppendPage (gacRefPanel, new Label (GettextCatalog.GetString ("Packages")));
			mainBook.AppendPage (projectRefPanel, new Label (GettextCatalog.GetString ("Projects")));
			mainBook.AppendPage (assemblyRefPanel, new Label (GettextCatalog.GetString (".Net Assembly")));
			mainBook.Page = 0;
			ReferencesTreeView.Selection.Changed += new EventHandler (OnChanged);
			AddReferenceDialog.ShowAll ();
			OnChanged (null, null);
		}

		void OnChanged (object o, EventArgs e)
		{
			if (ReferencesTreeView.Selection.CountSelectedRows () > 0)
				RemoveReferenceButton.Sensitive = true;
			else
				RemoveReferenceButton.Sensitive = false;
		}
		
		string GetTypeText (ReferenceProjectItem pref)
		{
			if (pref is ProjectReferenceProjectItem)
				return GettextCatalog.GetString ("Project");
			if (!String.IsNullOrEmpty (pref.HintPath))
				return GettextCatalog.GetString ("Assembly");
			return GettextCatalog.GetString ("Package");
//			switch (pref.ReferenceType) {
//				case ReferenceType.Gac: return GettextCatalog.GetString ("Package");
//				case ReferenceType.Assembly: return GettextCatalog.GetString ("Assembly");
//				case ReferenceType.Project: return GettextCatalog.GetString ("Project");
//				default: return "";
//			}
		}

		public void RemoveReference (bool projectReference, string reference)
		{
			TreeIter iter = FindReference (projectReference, reference);
			if (iter.Equals (TreeIter.Zero))
				return;
			refTreeStore.Remove (ref iter);
		}
		
		public void AddReference (bool projectReference, string reference)
		{
			TreeIter iter = FindReference (projectReference, reference);
			if (!iter.Equals (TreeIter.Zero))
				return;
			
			ReferenceProjectItem tag;
			if (projectReference) {
				SolutionProject project = ProjectService.FindProject (reference);
				tag = new ProjectReferenceProjectItem (project.Location, project.Guid, project.Name);
			} else {
				tag = new ReferenceProjectItem (reference);
			}
				
			TreeIter ni = AddReference (tag);
			if (!ni.Equals (TreeIter.Zero))
				ReferencesTreeView.ScrollToCell (refTreeStore.GetPath (ni), null, false, 0, 0);
		}
		
		TreeIter FindReference (bool projectReference, string reference)
		{
			TreeIter looping_iter;
			if (refTreeStore.GetIterFirst (out looping_iter)) {
				do {
					ReferenceProjectItem pref = (ReferenceProjectItem) refTreeStore.GetValue (looping_iter, ProjectReferenceColumn);
					if (pref.Include == reference && (!projectReference || pref is ProjectReferenceProjectItem) ) {
						return looping_iter;
					}
				} while (refTreeStore.IterNext (ref looping_iter));
			}
			return TreeIter.Zero;
		}
		
		protected void RemoveReference (object sender, EventArgs e)
		{
			TreeIter iter;
			TreeModel mdl;
			if (ReferencesTreeView.Selection.GetSelected (out mdl, out iter)) {
				ReferenceProjectItem item = (ReferenceProjectItem)refTreeStore.GetValue (iter, ProjectReferenceColumn);
				if (item is ProjectReferenceProjectItem) {
					projectRefPanel.SignalRefChange ((string)refTreeStore.GetValue (iter, NameColumn), false);
				} else {
					gacRefPanel.SignalRefChange ((string)refTreeStore.GetValue (iter, LocationColumn), false);
				}
				
				TreeIter newIter = iter;
				if (refTreeStore.IterNext (ref newIter)) {
					ReferencesTreeView.Selection.SelectIter (newIter);
					refTreeStore.Remove (ref iter);
				} else {
					TreePath path = refTreeStore.GetPath (iter);
					if (path.Prev ()) {
						ReferencesTreeView.Selection.SelectPath (path);
						refTreeStore.Remove (ref iter);
					} else {
						refTreeStore.Remove (ref iter);
					}
				}
			}
		}
	}
}

