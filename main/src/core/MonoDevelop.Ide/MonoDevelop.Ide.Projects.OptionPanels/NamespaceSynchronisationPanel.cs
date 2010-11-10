// 
// NamespaceOptionsPanel.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Policies;
using MonoDevelop.Ide.Gui.Dialogs;
using System.Linq;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	class NamespaceSynchronisationPanel : PolicyOptionsPanel<DotNetNamingPolicy>
	{
		NamespaceSynchronisationPanelWidget widget;
		bool migrateIds;
		
		public override bool IsVisible ()
		{
			//FIXME: this GetAllItems lookup is kinda expensive, maybe it should be cached in ParentDialog
			var item = ParentDialog.DataObject;
			if (item is PolicySet || item is DotNetProject)
				return true;
			var slnFolder = item as SolutionFolder ?? ((item is Solution)? ((Solution)item).RootFolder : null);
			if (slnFolder != null)
				return slnFolder.GetAllItems<DotNetProject> ().Any ();
			
			return false;
		}
		
		public override Widget CreatePanelWidget ()
		{
			widget = new NamespaceSynchronisationPanelWidget (this);
			widget.Show ();
			return widget;
		}
		
		protected override string PolicyTitleWithMnemonic {
			get { return GettextCatalog.GetString ("_Policy"); }
		}
		
		protected override void LoadFrom (DotNetNamingPolicy policy)
		{
			widget.LoadFrom (policy);
		}
		
		protected override DotNetNamingPolicy GetPolicy ()
		{
			return widget.GetPolicy ();
		}
		
		public override bool ValidateChanges ()
		{
			if (ConfiguredSolution != null && widget.ResourceNamingChanged) {
				string msg = GettextCatalog.GetString ("The resource naming policy has changed");
				string detail = "Changing the resource naming policy may cause run-time errors if the code using resources is not properly updated. There are two options:\n\n";
				detail += GettextCatalog.GetString ("Update all resource identifiers to match the new policy. This will require changes in the source code that references resources using the old policy. Identifiers explicitly set using the file properties pad won't be changed.\n\n");
				detail += "Keep curent resource identifiers. It doesn't require source code changes. Resources added from now on will use the new policy)";
				AlertButton update = new AlertButton ("Update Identifiers");
				AlertButton keep = new AlertButton ("Keep Current Identifiers");
				AlertButton res = MessageService.AskQuestion (msg, detail, AlertButton.Cancel, update, keep);
				if (res == AlertButton.Cancel)
					return false;
				migrateIds = res == keep;
			}
			return base.ValidateChanges ();
		}
		
		public override void ApplyChanges ()
		{
			base.ApplyChanges ();
			
			if (widget.ResourceNamingChanged) {
				if (ConfiguredProject is DotNetProject) {
					((DotNetProject)ConfiguredProject).UpdateResourceHandler (migrateIds);
				} else if (DataObject is SolutionFolder) {
					foreach (DotNetProject prj in ((SolutionFolder)DataObject).GetAllItems<DotNetProject> ())
						prj.UpdateResourceHandler (migrateIds);
				} else if (ConfiguredSolution != null) {
					foreach (DotNetProject prj in ConfiguredSolution.GetAllSolutionItems<DotNetProject> ())
						prj.UpdateResourceHandler (migrateIds);
				}
			}
		}

	}
	
	partial class NamespaceSynchronisationPanelWidget : Gtk.Bin
	{
		NamespaceSynchronisationPanel panel;
		TreeStore previewStore;
		TreeView previewTree;
		ResourceNamePolicy initialResourceNaming;
		bool firstLoad = true;
		
		public bool ResourceNamingChanged {
			get { return ActiveResourceNamePolicy != initialResourceNaming; }
		}
		
		public NamespaceSynchronisationPanelWidget (NamespaceSynchronisationPanel panel)
		{
			this.panel = panel;
			
			this.Build ();
			
			checkAssociateNamespacesDirectories.Toggled += UpdateNamespaceSensitivity;
			UpdateNamespaceSensitivity (null, EventArgs.Empty);
			
			checkDefaultAsRoot.Toggled += UpdatePreview;
			radioFlat.Toggled += UpdatePreview;
			radioHierarch.Toggled += UpdatePreview;
			
			previewStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (String), typeof (String));
			previewTree = new TreeView (previewStore);
			
			//Cosmetic, not available in GTK+ 2.8
			//previewTree.ShowExpanders = false;
			//previewTree.LevelIndentation = 24;
			
			previewTree.CanFocus = false;
			previewFrame.CanFocus = false;
			namespaceAssociationBox.FocusChain = new Widget[] { checkDefaultAsRoot, hbox1 };
			hbox1.FocusChain =  new Widget[] { radioFlat, radioHierarch };
			previewTree.ButtonPressEvent += SuppressClick;
			
			TreeViewColumn dirCol = new TreeViewColumn ();
			dirCol.Title = GettextCatalog.GetString ("Directory");
			var iconRenderer = new CellRendererPixbuf ();
			CellRendererText textRenderer = new CellRendererText ();
			dirCol.PackStart (iconRenderer, false);
			dirCol.PackStart (textRenderer, false);
			dirCol.AddAttribute (iconRenderer, "pixbuf", 0);
			dirCol.AddAttribute (textRenderer, "text", 1);
			previewTree.AppendColumn (dirCol);
			
			previewTree.AppendColumn (GettextCatalog.GetString ("Namespace"), textRenderer, "text", 2);
			
			previewFrame.Add (previewTree);
			previewFrame.ShowAll ();
			
			UpdatePreview (null, EventArgs.Empty);
		}
		
		public void LoadFrom (DotNetNamingPolicy policy)
		{
			checkAssociateNamespacesDirectories.Active = (policy.DirectoryNamespaceAssociation != DirectoryNamespaceAssociation.None);
			checkDefaultAsRoot.Active = policy.DirectoryNamespaceAssociation == DirectoryNamespaceAssociation.PrefixedFlat
				|| policy.DirectoryNamespaceAssociation == DirectoryNamespaceAssociation.PrefixedHierarchical;
			radioHierarch.Active = policy.DirectoryNamespaceAssociation == DirectoryNamespaceAssociation.Hierarchical
				|| policy.DirectoryNamespaceAssociation == DirectoryNamespaceAssociation.PrefixedHierarchical;
			
			if (policy.ResourceNamePolicy == ResourceNamePolicy.FileFormatDefault) {
				checkVSStyleResourceNames.Inconsistent = true;
			} else {
				checkVSStyleResourceNames.Active = policy.ResourceNamePolicy == ResourceNamePolicy.MSBuild;
				checkVSStyleResourceNames.Inconsistent = false;
			}
			
			if (firstLoad) {
				initialResourceNaming = policy.ResourceNamePolicy;
				firstLoad = false;
			}
		}
		
		public DotNetNamingPolicy GetPolicy ()
		{
			DirectoryNamespaceAssociation assoc;
			if (!checkAssociateNamespacesDirectories.Active) {
				assoc = DirectoryNamespaceAssociation.None;
			} else {
				if (radioHierarch.Active) {
					if (checkDefaultAsRoot.Active)
						assoc = DirectoryNamespaceAssociation.PrefixedHierarchical;
					else
						assoc = DirectoryNamespaceAssociation.Hierarchical;
				} else {
					if (checkDefaultAsRoot.Active)
						assoc = DirectoryNamespaceAssociation.PrefixedFlat;
					else
						assoc = DirectoryNamespaceAssociation.Flat;
				}
				
			}
			
			return new DotNetNamingPolicy (assoc, ActiveResourceNamePolicy);
		}
		
		[GLib.ConnectBefore]
		void SuppressClick (object o, ButtonPressEventArgs args)
		{
			args.RetVal = true;
		}
		
		ResourceNamePolicy ActiveResourceNamePolicy {
			get {
				return checkVSStyleResourceNames.Inconsistent
					? ResourceNamePolicy.FileFormatDefault
					: (checkVSStyleResourceNames.Active
						? ResourceNamePolicy.MSBuild
						: ResourceNamePolicy.FileName);
			}
		}
		
		void UpdateNamespaceSensitivity (object sender, EventArgs args)
		{
			namespaceAssociationBox.Sensitive = checkAssociateNamespacesDirectories.Active;
			UpdatePolicyNameList (null, null);
		}
		
		void UpdatePreview (object sender, EventArgs args)
		{
			previewStore.Clear ();
			TreeIter iter;
			
			string rootNamespace = checkDefaultAsRoot.Active? GettextCatalog.GetString ("Default.Namespace") : "";
			
			Gdk.Pixbuf folderIcon = ImageService.GetPixbuf ("md-open-folder", IconSize.Menu);
			Gdk.Pixbuf projectIcon = ImageService.GetPixbuf ("md-project", IconSize.Menu);
			iter = previewStore.AppendValues (projectIcon, GettextCatalog.GetString ("Project"), rootNamespace);
			
			if (rootNamespace.Length > 0)
				rootNamespace += ".";
			
			if (radioFlat.Active) {
				previewStore.AppendValues (iter, folderIcon, "A", rootNamespace + "A");
				previewStore.AppendValues (iter, folderIcon, "A.B", rootNamespace + "A.B");
			} else {
				iter = previewStore.AppendValues (iter, folderIcon, "A", rootNamespace + "A");
				previewStore.AppendValues (iter, folderIcon, "B", rootNamespace + "A.B");
			}
			
			previewTree.ExpandAll ();
			
			UpdatePolicyNameList (null, null);
		}

		protected void UpdatePolicyNameList (object sender, System.EventArgs e)
		{
			if (sender == checkVSStyleResourceNames)
				checkVSStyleResourceNames.Inconsistent = false;
			
			panel.UpdateSelectedNamedPolicy ();
		}
	}
}
