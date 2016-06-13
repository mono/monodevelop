//
// SolutionItemConfigurationsPanel.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui.Dialogs;
using Gtk;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	internal class SolutionItemConfigurationsPanel : ItemOptionsPanel
	{
		CombineEntryConfigurationsPanelWidget widget;
		
		public override Control CreatePanelWidget ()
		{
			MultiConfigItemOptionsDialog dlg = (MultiConfigItemOptionsDialog) ParentDialog;
			return (widget = new CombineEntryConfigurationsPanelWidget (dlg));
		}

		public override void ApplyChanges()
		{
	        widget.Store ();
       	}
	}

	partial class CombineEntryConfigurationsPanelWidget : Gtk.Bin 
	{
		TreeStore store;
		ConfigurationData configData;
		
		public CombineEntryConfigurationsPanelWidget (MultiConfigItemOptionsDialog dlg)
		{
			Build ();
			
			configData = dlg.ConfigurationData;
			
			store = new TreeStore (typeof(object), typeof(string));
			configsList.Model = store;
			configsList.SearchColumn = -1; // disable the interactive search
			configsList.HeadersVisible = true;
			store.SetSortColumnId (1, SortType.Ascending);
			
			TreeViewColumn col = new TreeViewColumn ();
			CellRendererText sr = new CellRendererText ();
			col.PackStart (sr, true);
			col.AddAttribute (sr, "text", 1);
			col.Title = GettextCatalog.GetString ("Configuration");
			col.SortColumnId = 1;
			configsList.AppendColumn (col);

			foreach (ItemConfiguration cc in configData.Configurations)
				store.AppendValues (cc, cc.Id);

			addButton.Clicked += new EventHandler (OnAddConfiguration);
			removeButton.Clicked += new EventHandler (OnRemoveConfiguration);
			renameButton.Clicked += new EventHandler (OnRenameConfiguration);
			copyButton.Clicked += new EventHandler (OnCopyConfiguration);
		}
		
		void OnAddConfiguration (object sender, EventArgs args)
		{
			AddConfiguration (null);
		}

		void OnCopyConfiguration (object sender, EventArgs args)
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!configsList.Selection.GetSelected (out foo, out iter))
				return;
				
			ItemConfiguration cc = (ItemConfiguration) store.GetValue (iter, 0);
			AddConfiguration (cc.Id);
		}

		void AddConfiguration (string copyFrom)
		{
			var dlg = new NewConfigurationDialog (configData.Entry, configData.Configurations);
			try {
				bool done = false;
				do {
					if (MessageService.RunCustomDialog (dlg, Toplevel as Gtk.Window) == (int) Gtk.ResponseType.Ok) {
						var cc = configData.AddConfiguration (dlg.ConfigName, copyFrom, dlg.CreateChildren);
						store.AppendValues (cc, cc.Id);
						done = true;
					} else
						done = true;
				} while (!done);
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}

		void OnRemoveConfiguration (object sender, EventArgs args)
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!configsList.Selection.GetSelected (out foo, out iter))
				return;
			
			if (configData.Configurations.Count == 1) {
				MessageService.ShowWarning (GettextCatalog.GetString ("There must be at least one configuration."));
				return;
			}
			
			var cc = (ItemConfiguration) store.GetValue (iter, 0);
			var dlg = new DeleteConfigDialog ();
			
			try {
				if (MessageService.RunCustomDialog (dlg, Toplevel as Gtk.Window)== (int) Gtk.ResponseType.Yes) {
					configData.RemoveConfiguration (cc.Id, dlg.DeleteChildren);
					store.Remove (ref iter);
				}
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}
		
		void OnRenameConfiguration (object sender, EventArgs args)
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!configsList.Selection.GetSelected (out foo, out iter))
				return;
				
			ItemConfiguration cc = (ItemConfiguration) store.GetValue (iter, 0);
			RenameConfigDialog dlg = new RenameConfigDialog (configData.Configurations);
			dlg.ConfigName = cc.Id;
			
			try {
				bool done = false;
				do {
					if (MessageService.RunCustomDialog (dlg, Toplevel as Gtk.Window) == (int) Gtk.ResponseType.Ok) {
						var newConf = configData.RenameConfiguration (cc.Id, dlg.ConfigName, dlg.RenameChildren);
						store.SetValue (iter, 0, newConf);
						store.SetValue (iter, 1, newConf.Id);
						done = true;
					} else
						done = true;
				} while (!done);
			} finally {
				dlg.Destroy ();
				dlg.Dispose ();
			}
		}

		public bool Store()
		{
			// Data stored at dialog level
			return true;
		}
	}
}

