
using System;
using System.Collections;
using Mono.Addins;
using Mono.Addins.Description;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using MonoDevelop.Projects;

namespace MonoDevelop.AddinAuthoring
{
	
	
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ExtensionEditorWidget : Gtk.Bin
	{
		AddinData data;
		AddinDescription adesc, compiledDesc;
		TreeStore store;
		TreeViewState state;
		Gdk.Pixbuf pixAddin;
		Gdk.Pixbuf pixLocalAddin;
		Gdk.Pixbuf pixExtensionPoint;
		Gdk.Pixbuf pixExtensionNode;
		
		const int ColLabel = 0;
		const int ColAddin = 1;
		const int ColExtension = 2;
		const int ColNode = 3;
		const int ColIcon = 4;
		const int ColShowIcon = 5;
		const int ColExtensionPoint = 6;
		
		public event EventHandler Changed;
			
		public ExtensionEditorWidget()
		{
			this.Build();
			
			pixAddin = PixbufService.GetPixbuf (MonoDevelop.Core.Gui.Stock.Addin, IconSize.Menu);
			pixLocalAddin = PixbufService.GetPixbuf ("md-addinauthoring-current-addin", IconSize.Menu);
			pixExtensionPoint = PixbufService.GetPixbuf ("md-extension-point", IconSize.Menu);
			pixExtensionNode = PixbufService.GetPixbuf ("md-extension-node", IconSize.Menu);
			
			store = new TreeStore (typeof(string), typeof(string), typeof(Extension), typeof(ExtensionNodeDescription), typeof(Gdk.Pixbuf), typeof(bool), typeof(ExtensionPoint));
			state = new TreeViewState (tree, 0);

			TreeViewColumn col = new TreeViewColumn ();
			CellRendererPixbuf cpix = new CellRendererPixbuf ();
			col.PackStart (cpix, false);
			col.AddAttribute (cpix, "pixbuf", ColIcon);
			col.AddAttribute (cpix, "visible", ColShowIcon);
			
			CellRendererExtension crt = new CellRendererExtension ();
			crt.Yalign = 0;
			col.PackStart (crt, true);
			col.AddAttribute (crt, "markup", ColLabel);
			
			tree.AppendColumn (col);
			tree.Model = store;
			tree.HeadersVisible = false;
			
			tree.RowActivated += OnActivated;
			tree.Selection.Changed += OnSelectionChanged;
			
			IdeApp.ProjectOperations.EndBuild += OnEndBuild;
		}
		
		public override void Dispose ()
		{
			IdeApp.ProjectOperations.EndBuild -= OnEndBuild;
			base.Dispose ();
		}
		
		public void OnEndBuild (object s, BuildEventArgs args)
		{
			compiledDesc = null;
			Fill ();
		}
		
		AddinDescription CompiledAddinDesc {
			get {
				if (compiledDesc == null) {
					if (System.IO.File.Exists (data.Project.GetOutputFileName (data.Project.DefaultConfigurationId)))
						compiledDesc = data.AddinRegistry.GetAddinDescription (new Mono.Addins.ConsoleProgressStatus (false), data.Project.GetOutputFileName (data.Project.DefaultConfigurationId));
				}
				return compiledDesc;
			}
		}
		
		public void SetData (AddinDescription desc, AddinData data)
		{
			this.data = data;
			this.adesc = desc;
			Fill ();
		}
		
		public void Fill ()
		{
			state.Save ();
			store.Clear ();
			ArrayList deps = new ArrayList ();
			deps.Add (CompiledAddinDesc);
			
			foreach (Dependency dep in adesc.MainModule.Dependencies) {
				AddinDependency adep = dep as AddinDependency;
				if (adep == null) continue;
				Addin ad = data.AddinRegistry.GetAddin (adep.FullAddinId);
				if (ad != null && ad.Description != null) {
					deps.Add (ad.Description);
				}
			}
			
			foreach (Extension ext in adesc.MainModule.Extensions) {
				AddExtension (ext, deps);
			}
			UpdateButtons ();
			state.Load ();
		}
		
		void ShowPopupMenu ()
		{
			TreeIter it;
			if (!tree.Selection.GetSelected (out it))
				return;
			
			Menu menu = new Menu ();
			Gtk.ImageMenuItem mi = new Gtk.ImageMenuItem (AddinManager.CurrentLocalizer.GetString ("Select Extension Points..."));
			menu.Insert (mi, -1);
			
			string aid = (string) store.GetValue (it, ColAddin);
			if (aid != null) {
			}
			
			PopulateNodeTypes (menu, it);
			
			menu.Insert (new Gtk.SeparatorMenuItem (), -1);
			mi = new Gtk.ImageMenuItem (Stock.Remove, null);
			menu.Insert (mi, -1);
			mi.Activated += delegate { DeleteSelection (); };
			
			menu.ShowAll ();
			menu.Popup ();
		}
		
		void PopulateNodeTypes (Gtk.Menu menu, TreeIter it)
		{
			ExtensionNodeTypeCollection types = GetAllowedChildTypes (it);
			Extension ext = (Extension) store.GetValue (it, ColExtension);
			ExtensionNodeDescription node = (ExtensionNodeDescription) store.GetValue (it, ColNode);
			
			if (types != null && types.Count > 0) {
				if (menu.Children.Length > 0)
					menu.Insert (new Gtk.SeparatorMenuItem (), -1);
				foreach (ExtensionNodeType nt in types) {
					Gtk.ImageMenuItem mi = new Gtk.ImageMenuItem (AddinManager.CurrentLocalizer.GetString ("Add node '{0}'", nt.NodeName));
					menu.Insert (mi, -1);
					ExtensionNodeType ntc = nt;
					mi.Activated += delegate {
						CreateNode (it, ext, node, ntc);
					};
				}
			}
		}
		
		ExtensionNodeTypeCollection GetAllowedChildTypes (TreeIter it)
		{
			ExtensionNodeTypeCollection types = null;
			
			Extension ext = (Extension) store.GetValue (it, ColExtension);
			if (ext != null) {
				if (ext.Parent == null) {
					ExtensionPoint ep = (ExtensionPoint) store.GetValue (it, ColExtensionPoint);
					types = ep.NodeSet.GetAllowedNodeTypes ();
				} else
					types = ext.GetAllowedNodeTypes ();
			}
			
			ExtensionNodeDescription node = (ExtensionNodeDescription) store.GetValue (it, ColNode);
			if (node != null) {
				ExtensionNodeType tn = node.GetNodeType ();
				if (tn != null)
					types = tn.GetAllowedNodeTypes ();
			}
			return types;
		}
		
		void DeleteSelection ()
		{
			TreeIter it;
			if (!tree.Selection.GetSelected (out it))
				return;
			
			string aid = (string) store.GetValue (it, ColAddin);
			Extension ext = (Extension) store.GetValue (it, ColExtension);
			ExtensionNodeDescription node = (ExtensionNodeDescription) store.GetValue (it, ColNode);
			
			if (aid != null) {
				if (store.IterChildren (out it, it)) {
					do {
						Extension aext = (Extension) store.GetValue (it, ColExtension);
						adesc.MainModule.Extensions.Remove (aext);
					}
					while (store.IterNext (ref it));
				}
			}
			else if (ext != null) {
				adesc.MainModule.Extensions.Remove (ext);
			}
			else if (node != null) {
				if (node.Parent is ExtensionNodeDescription)
					((ExtensionNodeDescription)node.Parent).ChildNodes.Remove (node);
				else if (node.Parent is Extension)
					((Extension)node.Parent).ExtensionNodes.Remove (node);
			}
			
			store.Remove (ref it);
			if (!it.Equals (TreeIter.Zero))
				tree.Selection.SelectIter (it);
			NotifyChanged ();
		}
		
		void CreateNode (TreeIter it, Extension ext, ExtensionNodeDescription node, ExtensionNodeType nt)
		{
			ExtensionNodeDescription newNode = new ExtensionNodeDescription (nt.NodeName);
			string ppath = ext != null ? ext.Path : node.GetParentPath () + "/" + node.Id;
			
			NodeEditorDialog dlg = new NodeEditorDialog (data.Project, data.AddinRegistry, nt, adesc, ppath, newNode);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					dlg.Save ();
					if (ext != null) {
						if (ext.Parent == null)
							adesc.MainModule.Extensions.Add (ext);
						ext.ExtensionNodes.Add (newNode);
					}
					else
						node.ChildNodes.Add (newNode);
					TreeIter nit = AddNode (it, newNode);
					tree.ExpandRow (store.GetPath (it), false);
					tree.Selection.SelectIter (nit);
					NotifyChanged ();
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		void AddExtension (Extension ext, ArrayList deps)
		{
			ExtensionPoint extep = null;
			foreach (AddinDescription desc in deps) {
				foreach (ExtensionPoint ep in desc.ExtensionPoints) {
					if (ep.Path == ext.Path || ext.Path.StartsWith (ep.Path + "/")) {
						extep = ep;
						break;
					}
				}
				if (extep != null)
					break;
			}
			if (extep != null) {
				TreeIter it = AddExtensionPoint (extep, ext);
				foreach (ExtensionNodeDescription node in ext.ExtensionNodes)
					AddNode (it, node);
			}
			else {
				string txt = "<b>" + GLib.Markup.EscapeText (ext.Path) + "</b>\n";
				txt += "<small><span foreground='red'>" + AddinManager.CurrentLocalizer.GetString ("Unknown extension point") + "</span></small>";
				store.AppendValues (txt, null, null, null, pixExtensionPoint, true, null);
			}
		}
		
		TreeIter AddExtensionPoint (ExtensionPoint extep, Extension ext)
		{
			string spath = ext.Path.Substring (extep.Path.Length);
			spath = spath.Trim ('/').Replace ("/", " / ");
			TreeIter ait = AddAddin (extep.ParentAddinDescription);
			string name;
			if (extep.Name.Length > 0 && extep.Description.Length > 0) {
				name = "<b>" + GLib.Markup.EscapeText (extep.Name) + "</b>";
				if (spath.Length > 0)
					name += " / " + GLib.Markup.EscapeText (spath);
				name += "\n<small>" + GLib.Markup.EscapeText (extep.Description) + "</small>";
			}
			else if (extep.Name.Length > 0) {
				name = "<b>" + GLib.Markup.EscapeText (extep.Name) + "</b>";
				if (spath.Length > 0)
					name += " / " + GLib.Markup.EscapeText (spath);
			}
			else if (extep.Description.Length > 0) {
				name = "<b>" + GLib.Markup.EscapeText (extep.Description) + "</b>";
				if (spath.Length > 0)
					name += " / " + GLib.Markup.EscapeText (spath);
			}
			else
				name = "<b>" + GLib.Markup.EscapeText (extep.Path) + "</b>";
			
			return store.AppendValues (ait, name, null, ext, null, pixExtensionPoint, false, extep);
		}
		
		TreeIter AddAddin (AddinDescription adesc)
		{
			TreeIter it;
			if (store.GetIterFirst (out it)) {
				do {
					if ((string)store.GetValue (it, ColAddin) == adesc.AddinId)
						return it;
				}
				while (store.IterNext (ref it));
			}
			if (adesc != CompiledAddinDesc) {
				string txt = GLib.Markup.EscapeText (adesc.Name);
				return store.AppendValues (txt, adesc.AddinId, null, null, pixAddin, true, null);
			} else {
				string txt = AddinManager.CurrentLocalizer.GetString ("Local extension points");
				return store.AppendValues (txt, adesc.AddinId, null, null, pixLocalAddin, true, null);
			}
		}
		
		TreeIter AddNode (TreeIter it, ExtensionNodeDescription node)
		{
			string txt = "<b><span foreground='#204a87'>" + GLib.Markup.EscapeText (node.NodeName) + "</span></b> ";
			foreach (NodeAttribute at in node.Attributes)
				txt += "<b>" + at.Name + "</b>=\"" + GLib.Markup.EscapeText (at.Value) + "\"  ";
			it = store.AppendValues (it, txt, null, null, node, pixExtensionNode, true, null);
			
			foreach (ExtensionNodeDescription cnode in node.ChildNodes)
				AddNode (it, cnode);
			return it;
		}

		protected virtual void OnButtonAddClicked(object sender, System.EventArgs e)
		{
			ExtensionSelectorDialog dlg = new ExtensionSelectorDialog (data.AddinRegistry, CompiledAddinDesc, CompiledAddinDesc.IsRoot, false);
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				foreach (object ob in dlg.GetSelection ()) {
					AddinDescription desc = null;
					if (ob is ExtensionPoint) {
						ExtensionPoint ep = (ExtensionPoint) ob;
						Extension ext = new Extension (ep.Path);
						adesc.MainModule.Extensions.Add (ext);
						desc = (AddinDescription) ep.Parent;
					}
					else if (ob is ExtensionNodeDescription) {
						ExtensionNodeDescription node = (ExtensionNodeDescription) ob;
						desc = node.ParentAddinDescription;
						string path = "";
						while (node != null && !(node.Parent is Extension)) {
							if (!node.IsCondition)
								path = "/" + node.Id + path;
							node = node.Parent as ExtensionNodeDescription;
						}
						Extension eext = (Extension) node.Parent;
						Extension ext = new Extension (eext.Path + "/" + node.Id + path);
						adesc.MainModule.Extensions.Add (ext);
					}
					if (!adesc.MainModule.DependsOnAddin (desc.AddinId))
						adesc.MainModule.Dependencies.Add (new AddinDependency (desc.AddinId));
				}
				NotifyChanged ();
				Fill ();
			}
			dlg.Destroy ();
		}
		
		void NotifyChanged ()
		{
			if (Changed != null)
				Changed (this, EventArgs.Empty);
		}
		
		void OnSelectionChanged (object s, EventArgs a)
		{
			UpdateButtons ();
		}
		
		void UpdateButtons ()
		{
			TreeIter iter;
			if (!tree.Selection.GetSelected (out iter)) {
				addNodeButton.Sensitive = false;
				buttonRemove.Sensitive = false;
				buttonProperties.Sensitive = false;
				return;
			}
			
			ExtensionNodeDescription node = store.GetValue (iter, ColNode) as ExtensionNodeDescription;
			if (node == null) {
				ExtensionPoint ep = (ExtensionPoint) store.GetValue (iter, ColExtensionPoint);
				if (ep != null) {
					addNodeButton.Sensitive = true;
					buttonRemove.Sensitive = false;
					buttonProperties.Sensitive = false;
				} else {
					addNodeButton.Sensitive = false;
					buttonRemove.Sensitive = false;
					buttonProperties.Sensitive = false;
				}
				return;
			}
			ExtensionNodeTypeCollection types = GetAllowedChildTypes (iter);
			addNodeButton.Sensitive = types != null && types.Count > 0;
			buttonRemove.Sensitive = true;
			buttonProperties.Sensitive = true;
		}
		
		void OnActivated (object s, Gtk.RowActivatedArgs args)
		{
			ShowProperties ();			
		}
		
		void ShowProperties ()
		{
			TreeIter iter;
			if (!tree.Selection.GetSelected (out iter))
				return;
			
			ExtensionNodeDescription node = store.GetValue (iter, ColNode) as ExtensionNodeDescription;
			if (node == null)
				return;
			
			ExtensionNodeType nt = node.GetNodeType ();
			if (nt == null)
				return;
			
			NodeEditorDialog dlg = new NodeEditorDialog (data.Project, data.AddinRegistry, nt, adesc, node.GetParentPath(), node);
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					dlg.Save ();
					Fill ();
					NotifyChanged ();
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		protected virtual void OnTreePopupMenu(object o, Gtk.PopupMenuArgs args)
		{
			ShowPopupMenu ();
		}

		protected virtual void OnTreeButtonReleaseEvent(object o, Gtk.ButtonReleaseEventArgs args)
		{
			if (args.Event.Button == 3)
				ShowPopupMenu ();
		}

		protected virtual void OnButtonRemoveClicked(object sender, System.EventArgs e)
		{
			DeleteSelection ();
		}

		protected virtual void OnButtonPropertiesClicked (object sender, System.EventArgs e)
		{
			ShowProperties ();
		}

		protected virtual void OnAddNodeButtonPressed (object sender, System.EventArgs e)
		{
			TreeIter it;
			if (!tree.Selection.GetSelected (out it))
				return;
			
			Menu menu = new Menu ();
			PopulateNodeTypes (menu, it);
			
			menu.ShowAll ();
			menu.Popup (null, null, GetAddMenuPosition, 1, Global.CurrentEventTime);
		}
		
		void GetAddMenuPosition (Menu menu, out int x, out int y, out bool pushIn)
		{
			addNodeButton.ParentWindow.GetOrigin (out x, out y);
			x += addNodeButton.Allocation.X;
			y += addNodeButton.Allocation.Bottom;
			pushIn = true;
		}
	}
}
