// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;

using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;

using Gtk;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal class GacReferencePanel : VBox, IReferencePanel
	{
		SelectReferenceDialog selectDialog;
		ClrVersion version;

		ListStore store;
		TreeView treeView;
		
		public GacReferencePanel(SelectReferenceDialog selectDialog)
		{
			this.selectDialog = selectDialog;
			
			store = new ListStore (typeof (string), typeof (string), typeof(string), typeof(bool), typeof(string), typeof(string));
			treeView = new TreeView (store);

			TreeViewColumn firstColumn = new TreeViewColumn ();
			CellRendererToggle tog_render = new CellRendererToggle ();
			tog_render.Toggled += new Gtk.ToggledHandler (AddReference);
			firstColumn.PackStart (tog_render, false);
			firstColumn.AddAttribute (tog_render, "active", 3);

			treeView.AppendColumn (firstColumn);
			
			TreeViewColumn secondColumn = new TreeViewColumn ();
			secondColumn.Title = GettextCatalog.GetString ("Assembly");
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			secondColumn.PackStart (crp, false);
			crp.StockId = "md-package";

			CellRendererText text_render = new CellRendererText ();
			secondColumn.PackStart (text_render, true);
			secondColumn.AddAttribute (text_render, "text", 0);
			
			treeView.AppendColumn (secondColumn);

			treeView.AppendColumn (GettextCatalog.GetString ("Version"), new CellRendererText (), "text", 1);
			treeView.AppendColumn (GettextCatalog.GetString ("Package"), new CellRendererText (), "text", 5);
			
			treeView.Columns[1].Resizable = true;

			store.SetSortColumnId (0, SortType.Ascending);
			store.SetSortFunc (0, new TreeIterCompareFunc (SortTree));
			
			ScrolledWindow sc = new ScrolledWindow ();
			sc.ShadowType = Gtk.ShadowType.In;
			sc.Add (treeView);
			this.PackStart (sc, true, true, 0);
			ShowAll ();
			BorderWidth = 6;
		}
		
		public void SetProject (IProject prj)
		{
// TODO: Project Conversion
//			DotNetProject netProject = prj as DotNetProject;
//	if (netProject != null)
//		version = ((DotNetProjectConfiguration)netProject.ActiveConfiguration).ClrVersion;
		}
		
		int SortTree (TreeModel model, TreeIter first, TreeIter second)
		{
			// first compare by name
			string fname = (string) model.GetValue (first, 0);
			string sname = (string) model.GetValue (second, 0);
			int compare = String.Compare (fname, sname, true);

			// they had the same name, so compare the version
			if (compare == 0) {
				string fversion = (string) model.GetValue (first, 1);
				string sversion = (string) model.GetValue (second, 1);
				compare = String.Compare (fversion, sversion, true);
			}

			return compare;
		}

		public void Reset ()
		{
			store.Clear ();
			PrintCache ();
		}
		
		public void AddReference(object sender, Gtk.ToggledArgs e)
		{
			Gtk.TreeIter iter;
			store.GetIterFromString (out iter, e.Path);
			if ((bool)store.GetValue (iter, 3) == false) {
				store.SetValue (iter, 3, true);
				selectDialog.AddReference (false, (string)store.GetValue (iter, 4));
				
			} else {
				store.SetValue (iter, 3, false);
				selectDialog.RemoveReference (false, (string)store.GetValue (iter, 4));
			}
		}

		public void SignalRefChange (string refLoc, bool newstate)
		{
			Gtk.TreeIter looping_iter;
			
			if (!store.GetIterFirst (out looping_iter)) {
				return;
			}

			do {
				if ((string)store.GetValue (looping_iter, 4) == refLoc) {
					store.SetValue (looping_iter, 3, newstate);
					return;
				}
			} while (store.IterNext (ref looping_iter));
		}
		
		void PrintCache()
		{
			foreach (string assemblyPath in Runtime.SystemAssemblyService.GetAssemblyPaths (version)) {
				try {
					System.Reflection.AssemblyName an = System.Reflection.AssemblyName.GetAssemblyName (assemblyPath);
					SystemPackage package = Runtime.SystemAssemblyService.GetPackageFromFullName (an.FullName);
					store.AppendValues (an.Name, an.Version.ToString (), System.IO.Path.GetFileName (assemblyPath), false, an.FullName, package.Name);
				}catch {
				}
			}
		}
	}
}

