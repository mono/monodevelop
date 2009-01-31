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
using MonoDevelop.Projects.Gui.Dialogs;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	
	class NamespaceSynchronisationPanel : PolicyOptionsPanel<DotNetNamingPolicy>
	{
		NamespaceSynchronisationPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			widget = new NamespaceSynchronisationPanelWidget (this);
			widget.Show ();
			return widget;
		}
		
		protected override string PolicyTitleWithMnemonic {
			get { return "_Policy"; }
		}
		
		protected override void LoadFrom (DotNetNamingPolicy policy)
		{
			widget.LoadFrom (policy);
		}
		
		protected override DotNetNamingPolicy GetPolicy ()
		{
			return widget.GetPolicy ();
		}
	}
	
	partial class NamespaceSynchronisationPanelWidget : Gtk.Bin
	{
		NamespaceSynchronisationPanel panel;
		TreeStore previewStore;
		TreeView previewTree;
		
		public NamespaceSynchronisationPanelWidget (NamespaceSynchronisationPanel panel)
		{
			this.panel = panel;
			
			this.Build ();
			
			//FIXME: implement the feature this maps to. See bug #470860 for partial patch
			checkVSStyleResourceNames.Visible = false;
			
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
			CellRendererPixbuf iconRenderer = new CellRendererPixbuf ();
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
			checkVSStyleResourceNames.Active = policy.VSStyleResourceNames;
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
			
			return new DotNetNamingPolicy (assoc, checkVSStyleResourceNames.Active);
		}
		
		[GLib.ConnectBefore]
		void SuppressClick (object o, ButtonPressEventArgs args)
		{
			args.RetVal = true;
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
			
			Gdk.Pixbuf folderIcon = Services.Resources.GetBitmap ("md-open-folder", IconSize.Menu);
			Gdk.Pixbuf projectIcon = Services.Resources.GetBitmap ("md-project", IconSize.Menu);
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

		protected virtual void UpdatePolicyNameList (object sender, System.EventArgs e)
		{
			panel.UpdateSelectedNamedPolicy ();
		}
	}
}
