// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;

using Gtk;
using Glade;

namespace MonoDevelop.Gui.Dialogs
{
	internal interface IReferencePanel
	{
	}
	
	internal class SelectReferenceDialog
	{
		TreeStore refTreeStore;
		[Widget] Dialog    AddReferenceDialog;
		[Widget] TreeView  ReferencesTreeView;
//		[Widget] Button    okbutton;
//		[Widget] Button    cancelbutton;
		[Widget] Button    RemoveReferenceButton;
		[Widget] Notebook  mainBook;
		GacReferencePanel gacRefPanel;

		ProjectReferencePanel projectRefPanel;
		
		public ProjectReferenceCollection ReferenceInformations {
			get {
				ProjectReferenceCollection referenceInformations = new ProjectReferenceCollection();
				Gtk.TreeIter looping_iter;
				if (!refTreeStore.GetIterFirst (out looping_iter)) {
					return referenceInformations;
				}
				do {
					referenceInformations.Add ((ProjectReference) refTreeStore.GetValue(looping_iter, 3));
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

		public void SetProject (Project configureProject)
		{
			((TreeStore) ReferencesTreeView.Model).Clear ();

			projectRefPanel.SetProject (configureProject);
			gacRefPanel.Reset ();

			foreach (ProjectReference refInfo in configureProject.ProjectReferences) {
				switch (refInfo.ReferenceType) {
					case ReferenceType.Assembly:
					case ReferenceType.Project:
						AddNonGacReference (refInfo);
						break;
					case ReferenceType.Gac:
						AddGacReference (refInfo, configureProject);
						break;
				}
			}

			OnChanged (null, null);
		}
		
		public SelectReferenceDialog(Project configureProject)
		{
			Glade.XML refXML = new Glade.XML (null, "Base.glade", "AddReferenceDialog", null);
			refXML.Autoconnect (this);
			
			refTreeStore = new TreeStore (typeof (string), typeof(string), typeof(string), typeof(ProjectReference));
			ReferencesTreeView.Model = refTreeStore;

			ReferencesTreeView.AppendColumn (GettextCatalog.GetString("Reference Name"), new CellRendererText (), "text", 0);
			ReferencesTreeView.AppendColumn (GettextCatalog.GetString ("Type"), new CellRendererText (), "text", 1);
			ReferencesTreeView.AppendColumn (GettextCatalog.GetString ("Location"), new CellRendererText (), "text", 2);
			
			projectRefPanel = new ProjectReferencePanel (this);
			gacRefPanel = new GacReferencePanel (this);
			SetProject (configureProject);
			
			mainBook.RemovePage (mainBook.CurrentPage);
			mainBook.AppendPage (gacRefPanel, new Label (GettextCatalog.GetString ("Global Assembly Cache")));
			mainBook.AppendPage (projectRefPanel, new Label (GettextCatalog.GetString ("Projects")));
			mainBook.AppendPage (new AssemblyReferencePanel (this), new Label (GettextCatalog.GetString (".Net Assembly")));
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

		void AddNonGacReference (ProjectReference refInfo)
		{
			gacRefPanel.SignalRefChange (refInfo.Reference, true);
			projectRefPanel.SignalRefChange (refInfo.Reference, true);
			refTreeStore.AppendValues (System.IO.Path.GetFileName (refInfo.Reference), refInfo.ReferenceType.ToString (), System.IO.Path.GetFullPath (refInfo.GetReferencedFileName ()), refInfo);
		}

		void AddGacReference (ProjectReference refInfo, Project referencedProject)
		{
			gacRefPanel.SignalRefChange (refInfo.Reference, true);
			projectRefPanel.SignalRefChange (refInfo.Reference, true);
			refTreeStore.AppendValues (System.IO.Path.GetFileNameWithoutExtension (refInfo.GetReferencedFileName ()), refInfo.ReferenceType.ToString (), refInfo.Reference, refInfo);
		}

		public void RemoveReference (ReferenceType referenceType, string referenceName, string referenceLocation)
		{
			TreeIter looping_iter;
			if (!refTreeStore.GetIterFirst (out looping_iter))
				return;
			do {
				if (referenceLocation == (string)refTreeStore.GetValue (looping_iter, 2)) {
					refTreeStore.Remove (ref looping_iter);
					return;
				}
			} while (refTreeStore.IterNext (ref looping_iter));
		}
		
		public void AddReference(ReferenceType referenceType, string referenceName, string referenceLocation)
		{
			TreeIter looping_iter;
			if (refTreeStore.GetIterFirst (out looping_iter)) {
				do {
					try {
						if (referenceLocation == (string)refTreeStore.GetValue (looping_iter, 2) && referenceName == (string)refTreeStore.GetValue (looping_iter, 0)) {
							return;
						}
					} catch {
					}
				} while (refTreeStore.IterNext (ref looping_iter));
			}
			
			ProjectReference tag;
			switch (referenceType) {
				case ReferenceType.Typelib:
					tag = new ProjectReference(referenceType, referenceName + "|" + referenceLocation);
					break;
				case ReferenceType.Project:
					tag = new ProjectReference(referenceType, referenceName);
					break;
				default:
					tag = new ProjectReference(referenceType, referenceLocation);
					break;
					
			}
			TreeIter ni = refTreeStore.AppendValues (referenceName, referenceType.ToString (), referenceLocation, tag);
			ReferencesTreeView.ScrollToCell (refTreeStore.GetPath (ni), null, false, 0, 0);
		}
		
		protected void RemoveReference(object sender, EventArgs e)
		{
			TreeIter iter;
			TreeModel mdl;
			if (ReferencesTreeView.Selection.GetSelected (out mdl, out iter)) {
				switch (((ProjectReference)refTreeStore.GetValue (iter, 3)).ReferenceType) {
					case ReferenceType.Gac:
						gacRefPanel.SignalRefChange ((string)refTreeStore.GetValue (iter, 2), false);
						break;
					case ReferenceType.Project:
						projectRefPanel.SignalRefChange ((string)refTreeStore.GetValue (iter, 0), false);
						break;
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

