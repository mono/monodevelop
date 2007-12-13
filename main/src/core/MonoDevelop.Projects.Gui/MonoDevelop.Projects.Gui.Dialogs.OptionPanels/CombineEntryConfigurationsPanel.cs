//
// CombineEntryConfigurationsPanel.cs
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
using System.Reflection;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using Gtk;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal class CombineEntryConfigurationsPanel : AbstractOptionPanel
	{
		CombineEntryConfigurationsPanelWidget widget;
		
		public override void LoadPanelContents()
		{
			Add (widget = new CombineEntryConfigurationsPanelWidget ((Properties) CustomizationObject));
		}

		public override bool StorePanelContents()
		{
	        bool success = widget.Store ();
			return success;			
       	}
	}

	partial class CombineEntryConfigurationsPanelWidget : Gtk.Bin 
	{
		TreeStore store;
		ConfigurationData configData;
		
		public CombineEntryConfigurationsPanelWidget (Properties CustomizationObject)
		{
			Build ();
			
//			combine = (Combine)((Properties)CustomizationObject).Get("Combine");
			configData = ((Properties)CustomizationObject).Get<ConfigurationData>("CombineConfigData");
			
			store = new TreeStore (typeof(object), typeof(string));
			configsList.Model = store;
			configsList.HeadersVisible = true;
			
			TreeViewColumn col = new TreeViewColumn ();
			CellRendererText sr = new CellRendererText ();
			col.PackStart (sr, true);
			col.AddAttribute (sr, "text", 1);
			col.Title = GettextCatalog.GetString ("Configuration");
			configsList.AppendColumn (col);
			
			foreach (CombineConfiguration cc in configData.Configurations)
				store.AppendValues (cc, cc.Name);

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
				
			CombineConfiguration cc = (CombineConfiguration) store.GetValue (iter, 0);
			AddConfiguration (cc.Name);
		}

		void AddConfiguration (string copyFrom)
		{
			NewConfigurationDialog dlg = new NewConfigurationDialog ();
			try {
				bool done = false;
				do {
					if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
						if (dlg.ConfigName.Length == 0) {
							Services.MessageService.ShowWarning (GettextCatalog.GetString ("Please enter a valid configuration name."));
						} else if (configData.Configurations [dlg.ConfigName] != null) {
							Services.MessageService.ShowWarning (GettextCatalog.GetString ("A configuration with the name '{0}' already exists.", dlg.ConfigName));
						} else {
							CombineConfiguration cc = (CombineConfiguration) configData.AddConfiguration (dlg.ConfigName, copyFrom, dlg.CreateChildren);
							store.AppendValues (cc, cc.Name);
							done = true;
						}
					} else
						done = true;
				} while (!done);
			} finally {
				dlg.Destroy ();
			}
		}

		void OnRemoveConfiguration (object sender, EventArgs args)
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!configsList.Selection.GetSelected (out foo, out iter))
				return;
			
			if (configData.Configurations.Count == 1) {
				Services.MessageService.ShowWarning (GettextCatalog.GetString ("There must be at least one configuration."));
				return;
			}
			
			CombineConfiguration cc = (CombineConfiguration) store.GetValue (iter, 0);
			DeleteConfigDialog dlg = new DeleteConfigDialog ();
			
			try {
				if (dlg.Run () == (int) Gtk.ResponseType.Yes) {
					configData.RemoveConfiguration (cc.Name, dlg.DeleteChildren);
					store.Remove (ref iter);
				}
			} finally {
				dlg.Destroy ();
			}
		}
		
		void OnRenameConfiguration (object sender, EventArgs args)
		{
			Gtk.TreeModel foo;
			Gtk.TreeIter iter;
			if (!configsList.Selection.GetSelected (out foo, out iter))
				return;
				
			CombineConfiguration cc = (CombineConfiguration) store.GetValue (iter, 0);
			RenameConfigDialog dlg = new RenameConfigDialog ();
			dlg.ConfigName = cc.Name;
			
			try {
				bool done = false;
				do {
					if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
						if (dlg.ConfigName.Length == 0) {
							Services.MessageService.ShowWarning (GettextCatalog.GetString ("Please enter a valid configuration name."));
						} else if (configData.Configurations [dlg.ConfigName] != null) {
							Services.MessageService.ShowWarning (GettextCatalog.GetString ("A configuration with the name '{0}' already exists.", dlg.ConfigName));
						} else {
							configData.RenameConfiguration (cc.Name, dlg.ConfigName, dlg.RenameChildren);
							store.SetValue (iter, 1, cc.Name);
							done = true;
						}
					} else
						done = true;
				} while (!done);
			} finally {
				dlg.Destroy ();
			}
		}

		public bool Store()
		{
			// Data stored at dialog level
			return true;
		}
	}
}

