//  GacReferencePanel.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

using System;
using System.IO;

using MonoDevelop.Projects;
using MonoDevelop.Core;

using Gtk;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	internal class GacReferencePanel : VBox, IReferencePanel
	{
		SelectReferenceDialog selectDialog;
		TargetFramework version;
		TargetRuntime runtime;

		ListStore store;
		TreeView treeView;
		
		public GacReferencePanel(SelectReferenceDialog selectDialog)
		{
			this.selectDialog = selectDialog;
			
			store = new ListStore (typeof (string), typeof (string), typeof(SystemAssembly), typeof(bool), typeof(string), typeof(string));
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
		
		public void SetTargetFramework (TargetRuntime runtime, TargetFramework version)
		{
			this.version = version;
			this.runtime = runtime;
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
				ProjectReference pr = new ProjectReference ((SystemAssembly)store.GetValue (iter, 2));
				selectDialog.AddReference (pr);
			} else {
				store.SetValue (iter, 3, false);
				selectDialog.RemoveReference (ReferenceType.Gac, (string)store.GetValue (iter, 4));
			}
		}

		public void SignalRefChange (ProjectReference pref, bool newstate)
		{
			Gtk.TreeIter looping_iter;
			
			if (!store.GetIterFirst (out looping_iter)) {
				return;
			}

			do {
				SystemAssembly asm = (SystemAssembly) store.GetValue (looping_iter, 2);
				if (pref.Reference == asm.FullName && pref.Package == asm.Package) {
					store.SetValue (looping_iter, 3, newstate);
					return;
				}
			} while (store.IterNext (ref looping_iter));
		}
		
		void PrintCache()
		{
			foreach (SystemAssembly asm in runtime.GetAssemblies (version)) {
				string pn = asm.Package.Name;
				if (pn == "mscorlib")
					continue;
				if (asm.Package.IsInternalPackage) pn += " " + GettextCatalog.GetString ("(Provided by MonoDevelop)");
				store.AppendValues (asm.Name, asm.Version, asm, false, asm.FullName, pn);
			}
		}
	}
}

