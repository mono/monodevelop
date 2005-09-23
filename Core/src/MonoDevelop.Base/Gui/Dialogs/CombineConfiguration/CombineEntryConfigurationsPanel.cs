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

using MonoDevelop.Internal.Project;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Services;
using Gtk;
using Glade;

namespace MonoDevelop.Gui.Dialogs.OptionPanels
{
	public class CombineEntryConfigurationsPanel : AbstractOptionPanel
	{
		CombineEntryConfigurationsPanelWidget widget;
		
		class CombineEntryConfigurationsPanelWidget : GladeWidgetExtract 
		{
   			[Glade.Widget] Button addButton;
 			[Glade.Widget] Button removeButton;
 			[Glade.Widget] Button renameButton;
 			[Glade.Widget] Button copyButton;
 			[Glade.Widget] Gtk.TreeView configsList;
			
			TreeStore store;
			ConfigurationData configData;
			
			public CombineEntryConfigurationsPanelWidget (IProperties CustomizationObject): base ("Base.glade", "CombineEntryConfigurationsPanel")
			{
//				combine = (Combine)((IProperties)CustomizationObject).GetProperty("Combine");
				configData = (ConfigurationData)((IProperties)CustomizationObject).GetProperty("CombineConfigData");
				
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
				AddConfigurationDialog dlg = new AddConfigurationDialog ();
				try {
					bool done = false;
					do {
						if (dlg.Run () == Gtk.ResponseType.Ok) {
							if (dlg.Name.Length == 0) {
								Runtime.MessageService.ShowWarning (GettextCatalog.GetString ("Please enter a valid configuration name."));
							} else if (configData.Configurations [dlg.Name] != null) {
								Runtime.MessageService.ShowWarning (string.Format (GettextCatalog.GetString ("A configuration with the name '{0}' already exists."), dlg.Name));
							} else {
								CombineConfiguration cc = (CombineConfiguration) configData.AddConfiguration (dlg.Name, copyFrom, dlg.CreateChildren);
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
					Runtime.MessageService.ShowWarning (GettextCatalog.GetString ("There must be at least one configuration."));
					return;
				}
				
				CombineConfiguration cc = (CombineConfiguration) store.GetValue (iter, 0);
				DeleteConfigDialog dlg = new DeleteConfigDialog ();
				
				try {
					if (dlg.Run () == Gtk.ResponseType.Yes) {
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
				dlg.Name = cc.Name;
				
				try {
					bool done = false;
					do {
						if (dlg.Run () == Gtk.ResponseType.Ok) {
							if (dlg.Name.Length == 0) {
								Runtime.MessageService.ShowWarning (GettextCatalog.GetString ("Please enter a valid configuration name."));
							} else if (configData.Configurations [dlg.Name] != null) {
								Runtime.MessageService.ShowWarning (string.Format (GettextCatalog.GetString ("A configuration with the name '{0}' already exists."), dlg.Name));
							} else {
								configData.RenameConfiguration (cc.Name, dlg.Name, dlg.RenameChildren);
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
				configData.Update ();
				return true;
			}
		}

		public override void LoadPanelContents()
		{
			Add (widget = new CombineEntryConfigurationsPanelWidget ((IProperties) CustomizationObject));
		}

		public override bool StorePanelContents()
		{
	        bool success = widget.Store ();
			return success;			
       	}
	}
	
	class AddConfigurationDialog
	{
		Glade.XML glade;
		[Glade.Widget] Dialog NewConfigurationDialog;
		[Glade.Widget] CheckButton createChildrenCheck;
		[Glade.Widget] Entry nameEntry;
		
		public AddConfigurationDialog ()
		{
			// we must do it from *here* otherwise, we get this assembly, not the caller
			glade = new XML (Assembly.GetCallingAssembly (), "Base.glade", "NewConfigurationDialog", null);
			glade.Autoconnect (this);
		}
		
		public string Name {
			get { return nameEntry.Text; }
			set { nameEntry.Text = value; }
		}
		
		public bool CreateChildren {
			get { return createChildrenCheck.Active; }
		}
		
		public Gtk.ResponseType Run ()
		{
			return (Gtk.ResponseType) NewConfigurationDialog.Run ();
		}
		
		public void Destroy ()
		{
			NewConfigurationDialog.Destroy ();
		}
	}
	
	class RenameConfigDialog
	{
		Glade.XML glade;
		[Glade.Widget] Dialog RenameConfigurationDialog;
		[Glade.Widget] CheckButton renameChildrenCheck;
		[Glade.Widget] Entry nameEntry;
		
		public RenameConfigDialog ()
		{
			// we must do it from *here* otherwise, we get this assembly, not the caller
			glade = new XML (Assembly.GetCallingAssembly (), "Base.glade", "RenameConfigurationDialog", null);
			glade.Autoconnect (this);
		}
		
		public string Name {
			get { return nameEntry.Text; }
			set { nameEntry.Text = value; }
		}
		
		public bool RenameChildren {
			get { return renameChildrenCheck.Active; }
		}
		
		public Gtk.ResponseType Run ()
		{
			return (Gtk.ResponseType) RenameConfigurationDialog.Run ();
		}
		
		public void Destroy ()
		{
			RenameConfigurationDialog.Destroy ();
		}
	}
	
	class DeleteConfigDialog
	{
		Glade.XML glade;
		[Glade.Widget] Dialog DeleteConfigurationDialog;
		[Glade.Widget] CheckButton deleteChildrenCheck;
		
		public DeleteConfigDialog ()
		{
			// we must do it from *here* otherwise, we get this assembly, not the caller
			glade = new XML (Assembly.GetCallingAssembly (), "Base.glade", "DeleteConfigurationDialog", null);
			glade.Autoconnect (this);
		}
		
		public bool DeleteChildren {
			get { return deleteChildrenCheck.Active; }
		}
		
		public Gtk.ResponseType Run ()
		{
			return (Gtk.ResponseType) DeleteConfigurationDialog.Run ();
		}
		
		public void Destroy ()
		{
			DeleteConfigurationDialog.Destroy ();
		}
	}
}

