// 
// CustomExecutionModeManagerDialog.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using MonoDevelop.Ide.Execution;
using MonoDevelop.Core;
using MonoDevelop.Components;


namespace MonoDevelop.Ide.Execution
{


	partial class CustomExecutionModeManagerDialog : Gtk.Dialog
	{
		Gtk.ListStore store;
		CommandExecutionContext ctx;
		TreeViewState treeState;

		public CustomExecutionModeManagerDialog (CommandExecutionContext ctx)
		{
			this.Build ();
			
			this.ctx = ctx;
			
			store = new Gtk.ListStore (typeof(CustomExecutionMode), typeof(string), typeof(string), typeof(string), typeof(string));
			listModes.Model = store;
			
			Gtk.CellRendererText crt = new Gtk.CellRendererText ();
			listModes.AppendColumn (GettextCatalog.GetString ("Name"), crt, "text", 1);
			listModes.AppendColumn (GettextCatalog.GetString ("Execution Mode"), crt, "text", 2);
			listModes.AppendColumn (GettextCatalog.GetString ("Available for"), crt, "text", 3);
			
			listModes.Selection.Changed += delegate {
				UpdateButtons ();
			};
			
			treeState = new TreeViewState (listModes, 4);
			
			Fill ();
		}
		
		public void Fill ()
		{
			treeState.Save ();
			store.Clear ();
			
			foreach (CustomExecutionMode mode in ExecutionModeCommandService.GetCustomModes (ctx)) {
				if (mode.Mode == null)
					continue;
				string scope = "";
				switch (mode.Scope) {
					case CustomModeScope.Project: scope = GettextCatalog.GetString ("Current project"); break;
					case CustomModeScope.Solution: scope = GettextCatalog.GetString ("Current solution"); break;
					case CustomModeScope.Global: scope = GettextCatalog.GetString ("All solutions"); break;
				}
				store.AppendValues (mode, mode.Name, mode.Mode.Name, scope, mode.Id);
			}
			treeState.Load ();
			UpdateButtons ();
		}

		protected virtual void OnButtonAddClicked (object sender, System.EventArgs e)
		{
			var dlg = new CustomExecutionModeDialog ();
			try {
				dlg.Initialize (ctx, null, null);
				if (MessageService.RunCustomDialog (dlg, this) == (int) Gtk.ResponseType.Ok) {
					ExecutionModeCommandService.SaveCustomCommand (ctx.Project, dlg.GetConfigurationData ());
					Fill ();
				}
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}

		protected virtual void OnButtonRemoveClicked (object sender, System.EventArgs e)
		{
			CustomExecutionMode mode = GetSelectedMode ();
			if (mode != null && MessageService.Confirm (GettextCatalog.GetString ("Are you sure you want to delete the custom execution mode '{0}'?", mode.Name), AlertButton.Delete)) {
				ExecutionModeCommandService.RemoveCustomCommand (ctx.Project, mode);
				Fill();
			}
		}

		protected virtual void OnButtonEditClicked (object sender, System.EventArgs e)
		{
			CustomExecutionMode mode = GetSelectedMode ();
			var dlg = new CustomExecutionModeDialog ();
			try {
				dlg.Initialize (ctx, null, mode);
				if (MessageService.RunCustomDialog (dlg, this) == (int) Gtk.ResponseType.Ok) {
					CustomExecutionMode newMode = dlg.GetConfigurationData ();
					ExecutionModeCommandService.SaveCustomCommand (ctx.Project, newMode);
					if (newMode.Scope != mode.Scope)
						ExecutionModeCommandService.RemoveCustomCommand (ctx.Project, mode);
					Fill ();
				}
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}
		
		void UpdateButtons ()
		{
			buttonEdit.Sensitive = buttonRemove.Sensitive = (GetSelectedMode () != null);
		}
		
		CustomExecutionMode GetSelectedMode ()
		{
			Gtk.TreeIter iter;
			if (listModes.Selection.GetSelected (out iter))
				return (CustomExecutionMode) store.GetValue (iter, 0);
			else
				return null;
		}
	}
}
