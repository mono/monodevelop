
using System;
using System.Collections;
using Gtk;
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AddinAuthoring
{
	public partial class ExtensionSelectorDialog : Gtk.Dialog
	{
		TreeStore store;
		AddinRegistry registry;
		AddinDescription adesc;
		bool showTypedOnly = false;
		bool isRoot;

		bool showNamespaces = true;
		bool showCategories = true;
		bool showExtensionPoints = true;
		bool showExtensionNodes = true;
		bool addinSelector = false;
		
		Hashtable selection = new Hashtable ();
		Gdk.Pixbuf pixCategory;
		Gdk.Pixbuf pixNamespace;
		Gdk.Pixbuf pixAddin;
		Gdk.Pixbuf pixLocalAddin;
		
		const int ColLabel = 0;
		const int ColObject = 1;
		const int ColExtension = 2;
		const int ColShowCheck = 3;
		const int ColChecked = 4;
		const int ColIcon = 5;
		const int ColShowIcon = 6;
		const int ColFilled = 7;
		
		public ExtensionSelectorDialog (AddinRegistry reg, AddinDescription adesc, bool isRoot, bool addinSelector)
		{
			this.Build();
			
			this.addinSelector = addinSelector;
			this.registry = reg;
			this.adesc = adesc;
			this.isRoot = isRoot;
			
			pixCategory = ImageService.GetPixbuf (MonoDevelop.Core.Gui.Stock.ClosedFolder, IconSize.Menu);
			pixNamespace = ImageService.GetPixbuf (MonoDevelop.Core.Gui.Stock.NameSpace, IconSize.Menu);
			pixAddin = ImageService.GetPixbuf (MonoDevelop.Core.Gui.Stock.Addin, IconSize.Menu);
			pixLocalAddin = ImageService.GetPixbuf ("md-addinauthoring-current-addin", IconSize.Menu);
			
			store = new TreeStore (typeof(string), typeof(object), typeof(ExtensionPoint), typeof(bool), typeof(bool), typeof(Gdk.Pixbuf), typeof(bool), typeof(bool));
			
			TreeViewColumn col = new TreeViewColumn ();
			CellRendererPixbuf cpix = new CellRendererPixbuf ();
			col.PackStart (cpix, false);
			col.AddAttribute (cpix, "pixbuf", ColIcon);
			col.AddAttribute (cpix, "visible", ColShowIcon);
			
			CellRendererToggle ctog = new CellRendererToggle ();
			ctog.Toggled += OnToggled;
			ctog.Yalign = 0;
			ctog.Ypad = 5;
			col.PackStart (ctog, false);
			col.AddAttribute (ctog, "active", ColChecked);
			col.AddAttribute (ctog, "visible", ColShowCheck);
			CellRendererText crt = new CellRendererText ();
			crt.Yalign = 0;
			col.PackStart (crt, true);
			col.AddAttribute (crt, "markup", ColLabel);
			
			tree.AppendColumn (col);
			Fill ();
			
			tree.HeadersVisible = false;
			tree.Model = store;
			
			tree.TestExpandRow += new Gtk.TestExpandRowHandler (OnTestExpandRow);
		}
		
		public object[] GetSelection ()
		{
			ArrayList list = new ArrayList ();
			list.AddRange (selection.Values);
			return list.ToArray ();
		}
		
		void Fill ()
		{
			foreach (Addin addin in registry.GetAddins ())
				AddAddin (addin);
			foreach (Addin addin in registry.GetAddinRoots ())
				AddAddin (addin);
			
			if (adesc != null && showExtensionPoints) {
				string txt = AddinManager.CurrentLocalizer.GetString ("Local extension points");
				TreeIter iter = store.AppendValues (GLib.Markup.EscapeText (txt), adesc, null, false, false, pixLocalAddin, true, false);
				// Add a dummy node to make sure the expand button is shown
				store.AppendValues (iter, "", null, null, false, false, null, true, true);
			}
		}
		
		void AddAddin (Addin addin)
		{
			AddinDescription desc = addin.Description;
			if (desc == null || desc.ExtensionPoints.Count == 0)
				return;
			
			if (isRoot && !desc.IsRoot)
				return;
			
			if (showTypedOnly) {
				bool istyped = false;
				for (int n=0; n<desc.ExtensionPoints.Count && !istyped; n++) {
					ExtensionPoint ep = desc.ExtensionPoints [n];
					for (int m=0; m<ep.NodeSet.NodeTypes.Count && !istyped; m++) {
						if (((ExtensionNodeType)ep.NodeSet.NodeTypes [m]).ObjectTypeName.Length > 0)
							istyped = true;
					}
				}
				if (!istyped)
					return;
			}
				
			TreeIter iter = TreeIter.Zero;
			if (showNamespaces) {
				if (desc.Namespace.Length == 0)
					iter = GetBranch (iter, AddinManager.CurrentLocalizer.GetString ("Global namespace"), pixNamespace);
				else
					iter = GetBranch (iter, desc.Namespace, pixNamespace);
			}
			if (showCategories) {
				if (desc.Category.Length > 0) {
					foreach (string cat in desc.Category.Split ('/')) {
						string tcat = cat.Trim ();
						if (tcat.Length == 0)
							continue;
						iter = GetBranch (iter, tcat, pixCategory);
					}
				} else {
					iter = GetBranch (iter, AddinManager.CurrentLocalizer.GetString ("Miscellaneous"), pixCategory);
				}
			}
			
			iter = GetBranch (iter, desc.Name, pixAddin);
			store.SetValue (iter, ColObject, addin);
			
			if (addinSelector) {
				store.SetValue (iter, ColShowCheck, true);
				store.SetValue (iter, ColChecked, selection.Contains (addin));
				store.SetValue (iter, ColFilled, true);
				return;
			}
			
			if (showExtensionPoints) {
				store.SetValue (iter, ColFilled, false);
				// Add a dummy node to make sure the expand button is shown
				store.AppendValues (iter, "", null, null, false, false, null, true, true);
			}
		}
		
		void FillExtensionPoint (TreeIter iter, ExtensionPoint ep)
		{
			// Add extension node types

			FillExtensionNodeSet (iter, ep.NodeSet);
			
			// Add extension nodes from known add-ins
			
			ArrayList extensions = new ArrayList ();
			
			CollectExtensions (ep.ParentAddinDescription, ep.Path, extensions);
			foreach (Dependency dep in ep.ParentAddinDescription.MainModule.Dependencies) {
				AddinDependency adep = dep as AddinDependency;
				if (adep == null) continue;
				Addin addin = registry.GetAddin (adep.FullAddinId);
				if (addin != null)
					CollectExtensions (addin.Description, ep.Path, extensions);
			}
			
			// Sort the extensions, to make sure they are added in the correct order
			// That is, deepest children last.
			extensions.Sort (new ExtensionComparer ());
			
//			iter = store.AppendValues (iter, "Extensions", null, ep, false, false, null, false, true);
			
			// Add the nodes
			foreach (Extension ext in extensions) {
				string subp = ext.Path.Substring (ep.Path.Length);
				TreeIter citer = iter;
				ExtensionNodeSet ns = ep.NodeSet;
				bool found = true;
				foreach (string p in subp.Split ('/')) {
					if (p.Length == 0) continue;
					if (!GetNodeBranch (citer, p, ns, out citer, out ns)) {
						found = false;
						break;
					}
				}
				if (found)
					FillNodes (citer, ns, ext.ExtensionNodes);
			}
		}
		
		void CollectExtensions (AddinDescription desc, string path, ArrayList extensions)
		{
			foreach (Extension ext in desc.MainModule.Extensions) {
				if (ext.Path == path || ext.Path.StartsWith (path + "/"))
					extensions.Add (ext);
			}
		}
		
		void FillExtensionNodeSet (TreeIter iter, ExtensionNodeSet ns)
		{
			// Add extension node types
/*			ExtensionNodeTypeCollection col = ns.GetAllowedNodeTypes ();
			foreach (ExtensionNodeType nt in col) {
				string nname;
				if (nt.Description.Length > 0) {
					nname = GLib.Markup.EscapeText (nt.NodeName) + "\n";
					nname += "<small>" + GLib.Markup.EscapeText (nt.Description) + "</small>";
				}
				else
					nname = GLib.Markup.EscapeText (nt.NodeName);
				
				store.AppendValues (iter, nname, nt, null, true, selection.Contains (nt), null, false, true);
			}
*/		}
		
		void FillNodes (TreeIter iter, ExtensionNodeSet ns, ExtensionNodeDescriptionCollection nodes)
		{
			ExtensionNodeTypeCollection ntypes = ns.GetAllowedNodeTypes ();
			
			foreach (ExtensionNodeDescription node in nodes) {
				
				if (node.IsCondition) {
					FillNodes (iter, ns, nodes);
					continue;
				}
				
				string id = node.Id;
				ExtensionNodeType nt = ntypes [node.NodeName];
				
				// The node can only be extended if it has an ID and if it accepts child nodes
				if (id.Length > 0 && (nt.NodeTypes.Count > 0 || nt.NodeSets.Count > 0)) {
					TreeIter citer = GetBranch (iter, id + " (" + nt.NodeName + ")", null);
					store.SetValue (citer, ColObject, node);
					store.SetValue (citer, ColShowCheck, true);
					store.SetValue (citer, ColChecked, false);
					FillExtensionNodeSet (citer, nt);
					FillNodes (citer, nt, node.ChildNodes);
				}
			}
		}

		bool GetNodeBranch (TreeIter parent, string name, ExtensionNodeSet nset, out TreeIter citer, out ExtensionNodeSet cset)
		{
			TreeIter iter;
			bool more;
			if (!parent.Equals (TreeIter.Zero))
				more = store.IterChildren (out iter, parent);
			else
				more = store.GetIterFirst (out iter);
			
			if (more) {
				do {
					if (((string)store.GetValue (iter, ColLabel)) == name) {
						ExtensionNodeDescription node = (ExtensionNodeDescription) store.GetValue (iter, ColObject);
						ExtensionNodeType nt = nset.GetAllowedNodeTypes () [node.NodeName];
						cset = nt;
						citer = iter;
						return true;
					}
				} while (store.IterNext (ref iter));
			}
			citer = iter;
			cset = null;
			return false;
		}
		
		void FillAddin (TreeIter iter, AddinDescription addin)
		{
			foreach (ExtensionPoint ep in addin.ExtensionPoints) {
				string name;
				if (ep.Name.Length > 0 && ep.Description.Length > 0) {
					name = "<b>" + GLib.Markup.EscapeText (ep.Name) + "</b>\n";
					name += "<small>" + GLib.Markup.EscapeText (ep.Description) + "</small>";
				}
				else if (ep.Name.Length > 0)
					name = "<b>" + GLib.Markup.EscapeText (ep.Name) + "</b>";
				else if (ep.Description.Length > 0)
					name = "<b>" + GLib.Markup.EscapeText (ep.Description) + "</b>";
				else
					name = "<b>" + GLib.Markup.EscapeText (ep.Path) + "</b>";
				
				TreeIter epIter = store.AppendValues (iter, name, ep, ep, true, selection.Contains (ep), null, false, !showExtensionNodes);
				if (showExtensionNodes) {
					// Add a dummy node to make sure the expand button is shown
					store.AppendValues (epIter, "", null, null, false, false, null, true, true);
				}
			}
		}
		
		private void OnTestExpandRow (object sender, Gtk.TestExpandRowArgs args)
		{
			bool filled = (bool) store.GetValue (args.Iter, ColFilled);
			if (!filled) {
				store.SetValue (args.Iter, ColFilled, true);
				
				// Remove the dummy child
				TreeIter iter;
				store.IterChildren (out iter, args.Iter);
				store.Remove (ref iter);
				
				object ob = store.GetValue (args.Iter, ColObject);
				if (ob is ExtensionPoint)
					FillExtensionPoint (args.Iter, (ExtensionPoint) ob);
				else if (ob is Addin)
					FillAddin (args.Iter, ((Addin) ob).Description);
				else if (ob is AddinDescription)
					FillAddin (args.Iter, (AddinDescription) ob);
				
				// If after all there are no children, return false
				if (!store.IterChildren (out iter, args.Iter)) {
					args.RetVal = true;
				}
			} else
				args.RetVal = false;
		}
		
		TreeIter GetBranch (TreeIter parent, string name, Gdk.Pixbuf icon)
		{
			TreeIter iter;
			bool more;
			if (!parent.Equals (TreeIter.Zero))
				more = store.IterChildren (out iter, parent);
			else
				more = store.GetIterFirst (out iter);
			
			if (more) {
				do {
					if (((string)store.GetValue (iter, ColLabel)) == name)
						return iter;
				} while (store.IterNext (ref iter));
			}
			
			if (!parent.Equals (TreeIter.Zero))
				return store.AppendValues (parent, GLib.Markup.EscapeText (name), null, null, false, false, icon, icon != null, true);
			else
				return store.AppendValues (GLib.Markup.EscapeText (name), null, null, false, false, icon, icon != null, true);
		}
		
		void OnToggled (object s, ToggledArgs args)
		{
			TreeIter it;
			if (!store.GetIterFromString (out it, args.Path))
				return;
			object ob = store.GetValue (it, ColObject);
			if (selection.Contains (ob)) {
				selection.Remove (ob);
				store.SetValue (it, ColChecked, false);
			} else {
				selection [ob] = ob;
				store.SetValue (it, ColChecked, true);
			}
		}
	}

	class ExtensionComparer: IComparer
	{
		public int Compare (object x, object y)
		{
			return ((Extension)x).Path.CompareTo (((Extension)y).Path);
		}
	}
}
