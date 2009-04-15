// ExceptionsDialog.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Collections.Generic;
using Gtk;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Components;
using Mono.Debugging.Client;

namespace MonoDevelop.Debugger
{
	public partial class ExceptionsDialog : Gtk.Dialog
	{
		ListStore storeExceptions;
		ListStore storeSelection;
		bool systemLoaded;
		List<string> classes = new List<string> ();
		TreeViewState tstateExc;
		TreeViewState tstateSel;
		bool updateScheduled;
		HashSet<string> selectedClasses = new HashSet<string> ();
		
		public ExceptionsDialog()
		{
			this.Build();

			storeExceptions = new ListStore (typeof(String));
			treeExceptions.Selection.Mode = SelectionMode.Multiple;
			treeExceptions.Model = storeExceptions;
			treeExceptions.AppendColumn ("", new CellRendererText (), "text", 0);
			tstateExc = new TreeViewState (treeExceptions, 0);
			storeExceptions.SetSortColumnId (0, SortType.Ascending);

			storeSelection = new ListStore (typeof(String));
			treeSelected.Selection.Mode = SelectionMode.Multiple;
			treeSelected.Model = storeSelection;
			treeSelected.AppendColumn ("", new CellRendererText (), "text", 0);
			tstateSel = new TreeViewState (treeSelected, 0);
			storeSelection.SetSortColumnId (0, SortType.Ascending);
			
			foreach (Catchpoint cp in DebuggingService.Breakpoints.GetCatchpoints ())
				selectedClasses.Add (cp.ExceptionName);
			
			LoadExceptions ();

			FillSelection ();
			FillExceptions ();
		}

		void LoadExceptions ()
		{
			ProjectDom dom;
			if (IdeApp.ProjectOperations.CurrentSelectedProject != null)
				dom = ProjectDomService.GetProjectDom (IdeApp.ProjectOperations.CurrentSelectedProject);
			else {
				string asm = typeof(Uri).Assembly.Location;
				if (!systemLoaded) {
					ProjectDomService.LoadAssembly (Runtime.SystemAssemblyService.CurrentRuntime, asm);
					systemLoaded = true;
				}
				dom = ProjectDomService.GetAssemblyDom (Runtime.SystemAssemblyService.CurrentRuntime, asm);
			}
			foreach (IType t in dom.GetSubclasses (dom.GetType ("System.Exception", true)))
				classes.Add (t.FullName);
		}

		void FillExceptions ()
		{
			tstateExc.Save ();
			storeExceptions.Clear ();
			string filter = entryFilter.Text.ToLower ();
			foreach (string t in classes) {
				if ((filter.Length == 0 || t.ToLower().IndexOf (filter) != -1) && !selectedClasses.Contains (t))
					storeExceptions.AppendValues (t);
			}
			tstateExc.Load ();
			if (treeExceptions.Selection.CountSelectedRows () == 0) {
				TreeIter it;
				if (storeExceptions.GetIterFirst (out it))
					treeExceptions.Selection.SelectIter (it);
			}
		}

		void FillSelection ()
		{
			tstateSel.Save ();
			storeSelection.Clear ();
			foreach (string exc in selectedClasses)
				storeSelection.AppendValues (exc);
			tstateSel.Load ();
			if (treeSelected.Selection.CountSelectedRows () == 0) {
				TreeIter it;
				if (storeSelection.GetIterFirst (out it))
					treeSelected.Selection.SelectIter (it);
			}
		}

		protected override void OnDestroyed ()
		{
			if (systemLoaded)
				ProjectDomService.UnloadAssembly (Runtime.SystemAssemblyService.CurrentRuntime, typeof(Uri).Assembly.Location);
			base.OnDestroyed ();
		}

		protected virtual void OnEntryFilterChanged (object sender, System.EventArgs e)
		{
			if (!updateScheduled) {
				updateScheduled = true;
				GLib.Timeout.Add (200, delegate {
					updateScheduled = false;
					FillExceptions ();
					return false;
				});
			}
		}

		protected virtual void OnButtonAddClicked (object sender, System.EventArgs e)
		{
			foreach (TreePath path in treeExceptions.Selection.GetSelectedRows ()) {
				TreeIter it;
				if (storeExceptions.GetIter (out it, path)) {
					string exc = (string) storeExceptions.GetValue (it, 0);
					selectedClasses.Add (exc);
				}
			}
			SelectNearest (treeExceptions);
			FillSelection ();
			FillExceptions ();
		}

		protected virtual void OnButtonRemoveClicked (object sender, System.EventArgs e)
		{
			foreach (TreePath path in treeSelected.Selection.GetSelectedRows ()) {
				TreeIter it;
				if (storeSelection.GetIter (out it, path)) {
					string exc = (string) storeSelection.GetValue (it, 0);
					selectedClasses.Remove (exc);
				}
			}
			SelectNearest (treeSelected);
			FillSelection ();
			FillExceptions ();
		}

		void SelectNearest (TreeView view)
		{
			ListStore store = (ListStore) view.Model;
			TreePath[] paths = view.Selection.GetSelectedRows ();
			if (paths.Length == 0)
				return;
			TreeIter it;
			store.GetIter (out it, paths [paths.Length - 1]);
			if (store.IterNext (ref it)) {
				view.Selection.UnselectAll ();
				view.Selection.SelectIter (it);
				return;
			}
			store.GetIter (out it, paths [0]);
			if (store.IterNext (ref it)) {
				view.Selection.UnselectAll ();
				view.Selection.SelectIter (it);
				return;
			}
		}

		protected virtual void OnEntryFilterActivated (object sender, System.EventArgs e)
		{
			OnButtonAddClicked (null, null);
		}

		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			foreach (Catchpoint cp in new List<Catchpoint> (DebuggingService.Breakpoints.GetCatchpoints ())) {
				if (!selectedClasses.Contains (cp.ExceptionName))
					DebuggingService.Breakpoints.Remove (cp);
				else
					selectedClasses.Remove (cp.ExceptionName);
			}
			foreach (string exc in selectedClasses)
				DebuggingService.Breakpoints.AddCatchpoint (exc);
		}

		[GLib.ConnectBefore]
		protected virtual void OnTreeSelectedKeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.KP_Enter) {
				OnButtonRemoveClicked (null, null);
				args.RetVal = true;
			}
		}

		[GLib.ConnectBefore]
		protected virtual void OnTreeExceptionsKeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.KP_Enter) {
				OnButtonAddClicked (null, null);
				args.RetVal = true;
			}
		}
	}
}
