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
using MonoDevelop.Core;
using MonoDevelop.Components;
using Mono.Debugging.Client;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Debugger
{
	public partial class ExceptionsDialog : Gtk.Dialog
	{
		ListStore storeExceptions;
		ListStore storeSelection;
		HashSet<string> classes = new HashSet<string> ();
		TreeViewState tstateExc;
		TreeViewState tstateSel;
		bool updateScheduled;
		HashSet<string> selectedClasses = new HashSet<string> ();
		
		public ExceptionsDialog()
		{
			this.Build ();
			
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

			var breakpoints = DebuggingService.Breakpoints;
			lock (breakpoints) {
				foreach (Catchpoint cp in breakpoints.GetCatchpoints ())
					selectedClasses.Add (cp.ExceptionName);
			}

			LoadExceptions ();
			
			FillSelection ();
			FillExceptions ();
		}
		
		void LoadExceptions ()
		{
			classes.Add ("System.Exception");
			if (IdeApp.ProjectOperations.CurrentSelectedProject != null) {
				var dom = TypeSystemService.GetCompilation (IdeApp.ProjectOperations.CurrentSelectedProject);
				foreach (var t in dom.FindType (typeof (Exception)).GetSubTypeDefinitions ())
					classes.Add (t.ReflectionName);
			} else {
				// no need to unload this assembly context, it's not cached.
				var unresolvedAssembly = TypeSystemService.LoadAssemblyContext (Runtime.SystemAssemblyService.CurrentRuntime, MonoDevelop.Core.Assemblies.TargetFramework.Default, typeof(Uri).Assembly.Location);
				var mscorlib = TypeSystemService.LoadAssemblyContext (Runtime.SystemAssemblyService.CurrentRuntime, MonoDevelop.Core.Assemblies.TargetFramework.Default, typeof(object).Assembly.Location);
				if (unresolvedAssembly != null && mscorlib != null) {
					var dom = new ICSharpCode.NRefactory.TypeSystem.Implementation.SimpleCompilation (unresolvedAssembly, mscorlib);
					foreach (var t in dom.FindType (typeof (Exception)).GetSubTypeDefinitions ())
						classes.Add (t.ReflectionName);
				}
			}
		}
		
		void FillExceptions ()
		{
			tstateExc.Save ();
			storeExceptions.Clear ();
			string filter = entryFilter.Text;
			foreach (string t in classes) {
				if ((filter.Length == 0 || t.IndexOf (filter, StringComparison.OrdinalIgnoreCase) != -1) && !selectedClasses.Contains (t))
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
		
		protected virtual void OnEntryFilterActivated (object sender, EventArgs e)
		{
			OnButtonAddClicked (null, null);
		}
		
		protected virtual void OnButtonOkClicked (object sender, EventArgs e)
		{
			var breakpoints = DebuggingService.Breakpoints;

			lock (breakpoints) {
				foreach (Catchpoint cp in new List<Catchpoint> (breakpoints.GetCatchpoints ())) {
					if (!selectedClasses.Contains (cp.ExceptionName))
						breakpoints.Remove (cp);
					else
						selectedClasses.Remove (cp.ExceptionName);
				}

				foreach (string exc in selectedClasses)
					breakpoints.AddCatchpoint (exc);
			}
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
		protected virtual void OnTreeSelectedButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Button == 1 && args.Event.Type == Gdk.EventType.TwoButtonPress)
				OnButtonRemoveClicked (o, args);
		}
		
		[GLib.ConnectBefore]
		protected virtual void OnTreeExceptionsKeyPressEvent (object o, Gtk.KeyPressEventArgs args)
		{
			if (args.Event.Key == Gdk.Key.Return || args.Event.Key == Gdk.Key.KP_Enter) {
				OnButtonAddClicked (null, null);
				args.RetVal = true;
			}
		}
		
		[GLib.ConnectBefore]
		protected virtual void OnTreeExceptionsButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			if (args.Event.Button == 1 && args.Event.Type == Gdk.EventType.TwoButtonPress)
				OnButtonAddClicked (o, args);
		}
	}
}
