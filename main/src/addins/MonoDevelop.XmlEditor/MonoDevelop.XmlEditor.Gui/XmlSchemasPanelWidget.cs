// 
// XmlSchemasPanelWidget.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
//   Matthew Ward
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// Copyright (C) 2005-2007 Matthew Ward
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

using MonoDevelop.XmlEditor;
using MonoDevelop.XmlEditor.Completion;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

using System;
using System.Collections.Generic;
using Gtk;

namespace MonoDevelop.XmlEditor.Gui
{
	
	
	public partial class XmlSchemasPanelWidget : Gtk.Bin
	{
		ListStore registeredSchemasStore;
		ListStore defaultAssociationsStore;
		ListStore registeredSchemasComboModel;
		List<XmlSchemaCompletionData> addedSchemas = new List<XmlSchemaCompletionData> ();
		List<XmlSchemaCompletionData> removedSchemas = new List<XmlSchemaCompletionData> ();
		List<string> removedExtensions = new List<string> ();
		
		public XmlSchemasPanelWidget()
		{
			this.Build();
			
			//set up tree view for default schemas
			CellRendererText textRenderer = new CellRendererText ();
			registeredSchemasStore = new ListStore (typeof (XmlSchemaCompletionData));
			registeredSchemasView.Model = registeredSchemasStore;
			
			registeredSchemasView.AppendColumn (GettextCatalog.GetString ("Namespace"), textRenderer,
			    delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				((Gtk.CellRendererText)cell).Text = ((MonoDevelop.XmlEditor.Completion.XmlSchemaCompletionData)model.GetValue (iter, 0)).NamespaceUri;
			});
			
			registeredSchemasView.AppendColumn (GettextCatalog.GetString ("Type"), textRenderer,
			    delegate (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter) {
				bool builtIn = ((MonoDevelop.XmlEditor.Completion.XmlSchemaCompletionData)model.GetValue (iter, 0)).ReadOnly;
				((Gtk.CellRendererText)cell).Text = builtIn? 
					  GettextCatalog.GetString ("Built in") 
					: GettextCatalog.GetString ("User schema");
			});
			
			registeredSchemasStore.SetSortFunc (0, 
			    delegate (TreeModel model, TreeIter a, TreeIter b) {
				return string.Compare (
				    ((MonoDevelop.XmlEditor.Completion.XmlSchemaCompletionData) model.GetValue (a, 0)).NamespaceUri,
				    ((MonoDevelop.XmlEditor.Completion.XmlSchemaCompletionData) model.GetValue (b, 0)).NamespaceUri
				);
			});
			registeredSchemasStore.SetSortColumnId (0, SortType.Ascending);
			
			//update state of "remove" button depending on whether schema is read-only and anything's slected
			registeredSchemasView.Selection.Changed += delegate {
				MonoDevelop.XmlEditor.Completion.XmlSchemaCompletionData data = GetSelectedSchema ();
				registeredSchemasRemoveButton.Sensitive = (data != null && !data.ReadOnly);
			};
			registeredSchemasRemoveButton.Sensitive = false;
			
			//set up cells for associations
			CellRendererText extensionTextRenderer = new CellRendererText ();
			extensionTextRenderer.Editable = true;
			CellRendererText prefixTextRenderer = new CellRendererText ();
			prefixTextRenderer.Editable = true;
			
			CellRendererCombo comboEditor = new CellRendererCombo ();
			registeredSchemasComboModel = new ListStore (typeof (string));
			comboEditor.Model = registeredSchemasComboModel;
			comboEditor.Mode = CellRendererMode.Editable;
			comboEditor.TextColumn = 0;
			comboEditor.Editable = true;
			comboEditor.HasEntry = false;
			
			//rebuild combo's model from default schemas whenever editing starts
			comboEditor.EditingStarted += delegate (object sender, EditingStartedArgs args) {
				registeredSchemasComboModel.Clear ();
				registeredSchemasComboModel.AppendValues (string.Empty);
				foreach (Gtk.TreeIter iter in WalkStore (registeredSchemasStore))
					registeredSchemasComboModel.AppendValues (
					    ((MonoDevelop.XmlEditor.Completion.XmlSchemaCompletionData)registeredSchemasStore.GetValue (iter, 0)).NamespaceUri
					);
				args.RetVal = true;
				registeredSchemasComboModel.SetSortColumnId (0, Gtk.SortType.Ascending);
			};
			
			//set up tree view for associations
			defaultAssociationsStore = new ListStore (typeof (string), typeof (string), typeof (string), typeof (bool));
			defaultAssociationsView.Model = defaultAssociationsStore;
			defaultAssociationsView.AppendColumn (GettextCatalog.GetString ("File Extension"), extensionTextRenderer, "text", DACols.Extension);
			defaultAssociationsView.AppendColumn (GettextCatalog.GetString ("Namespace"), comboEditor, "text", DACols.Namespace);
			defaultAssociationsView.AppendColumn (GettextCatalog.GetString ("Prefix"), prefixTextRenderer, "text", DACols.Prefix);
			defaultAssociationsStore.SetSortColumnId ((int)DACols.Extension, SortType.Ascending);
			
			//editing handlers
			extensionTextRenderer.Edited += handleExtensionSet;
			comboEditor.Edited += delegate (object sender, EditedArgs args) {
				setAssocValAndMarkChanged (args.Path, DACols.Namespace, args.NewText);
			};
			prefixTextRenderer.Edited += delegate (object sender, EditedArgs args) {
				foreach (char c in args.NewText)
					if (!char.IsLetterOrDigit (c))
						//FIXME: give an error message?
						return;
				setAssocValAndMarkChanged (args.Path, DACols.Prefix, args.NewText);
			};
			
			//update state of "remove" button depending on whether anything's slected
			defaultAssociationsView.Selection.Changed += delegate {
				Gtk.TreeIter iter;
				defaultAssociationsRemoveButton.Sensitive =
					defaultAssociationsView.Selection.GetSelected (out iter);
			};
			defaultAssociationsRemoveButton.Sensitive = false;
		}
		
		IEnumerable<object> WalkStore (TreeModel model, int column)
		{
			foreach (TreeIter iter in WalkStore (model))
				yield return model.GetValue (iter, column);
		}
		
		IEnumerable<TreeIter> WalkStore (TreeModel model)
		{
			TreeIter iter;
			bool valid = model.GetIterFirst (out iter);
			while (valid) {
				yield return iter;
				valid = model.IterNext (ref iter);
			}
		}
		
		#region Schema accessors
		
		public List<XmlSchemaCompletionData> AddedSchemas {
			get { return addedSchemas; }
		}
		
		public List<XmlSchemaCompletionData> RemovedSchemas {
			get { return removedSchemas; }
		}
		
		XmlSchemaCompletionData GetSelectedSchema ()
		{
			TreeIter iter;
			if (registeredSchemasView.Selection.GetSelected (out iter))
				return (XmlSchemaCompletionData) registeredSchemasStore.GetValue (iter, 0);
			return null;
		}
		
		// Checks that the schema namespace does not already exist.
		XmlSchemaCompletionData GetRegisteredSchema (string namespaceUri)
		{
			foreach (XmlSchemaCompletionData schema in WalkStore (registeredSchemasStore, 0))
				if (schema.NamespaceUri == namespaceUri)
					return schema;
			return null;
		}
		
		XmlSchemaCompletionData GetRegisteredSchema (TreeIter iter)
		{
			return (XmlSchemaCompletionData) registeredSchemasStore.GetValue (iter, 0);
		}
		
		public void LoadUserSchemas (XmlSchemaCompletionDataCollection val)
		{
			//add user schemas
			registeredSchemasStore.Clear ();
			foreach (XmlSchemaCompletionData schema in val)
				AppendSchemaToStore (schema);
			
			//add built-in schemas that aren't overriden by a matching user schema
			foreach (XmlSchemaCompletionData schema in XmlSchemaManager.BuiltinSchemas)
				if (val[schema.NamespaceUri] == null)
					AppendSchemaToStore (schema);
		}
		
		#endregion
		
		public bool IsChanged {
			get {
				if ( addedSchemas.Count > 0 || removedSchemas.Count > 0 || removedExtensions.Count > 0)
					return true;
				
				foreach (TreeIter iter in WalkStore (defaultAssociationsStore))
					if ((bool)defaultAssociationsStore.GetValue (iter, (int)DACols.Changed))
						return true;
				
				return false;
			}
		}
		
		#region File associations
		
		public IEnumerable<XmlSchemaAssociation> GetChangedXmlSchemaAssociations ()
		{
			foreach (TreeIter iter in WalkStore (defaultAssociationsStore)) {
				string ext = (string)defaultAssociationsStore.GetValue (iter, (int)DACols.Extension);
				if (!string.IsNullOrEmpty (ext) && (bool)defaultAssociationsStore.GetValue (iter, (int)DACols.Changed)) {
					yield return new XmlSchemaAssociation (
						ext,
						((string)defaultAssociationsStore.GetValue (iter, (int)DACols.Namespace)) ?? string.Empty,
						((string)defaultAssociationsStore.GetValue (iter, (int)DACols.Prefix)) ?? string.Empty
					);
				}
			}
		}
		
		public List<string> RemovedExtensions {
			get { return removedExtensions; }
		}
		
		public void AddFileExtensions (IEnumerable<XmlSchemaAssociation> assocs)
		{
			foreach (XmlSchemaAssociation a in assocs)
				defaultAssociationsStore.AppendValues (a.Extension, a.NamespaceUri, a.NamespacePrefix, false);
		}
		
		void handleExtensionSet (object sender, EditedArgs args)
		{
			//check extension is valid and not a duplicate
			string newval = args.NewText == null? null : args.NewText.ToLowerInvariant ().Trim ();
			if (string.IsNullOrEmpty (newval))
				//FIXME: give an error message?
				return;
			
			foreach (char c in newval)
				if (!char.IsLetterOrDigit (c) && c != '.')
					//FIXME: give an error message?
					return;
			
			foreach (string s in WalkStore (defaultAssociationsStore, (int)DACols.Extension)) {
				if (s == newval)
					//FIXME: give an error message?
					return;
			}
			
			setAssocValAndMarkChanged (args.Path, DACols.Extension, newval);
		}
		
		void setAssocValAndMarkChanged (string path, DACols col, object val)
		{
			Gtk.TreeIter iter;
			if (defaultAssociationsStore.GetIter (out iter, new TreePath (path))) {
				defaultAssociationsStore.SetValue (iter, (int)col, val);
				defaultAssociationsStore.SetValue (iter, (int)DACols.Changed, true);
			} else {
				throw new Exception ("Could not resolve edited path '" + path +"' to TreeIter at " + Environment.StackTrace);
			}
		}
		
		protected virtual void addFileAssociation (object sender, System.EventArgs e)
		{
			bool foundExisting = false;
			TreeIter newIter = TreeIter.Zero;
			foreach (TreeIter iter in WalkStore (defaultAssociationsStore)) {
				if (string.IsNullOrEmpty ((string) defaultAssociationsStore.GetValue (iter, (int) DACols.Extension))) {
					foundExisting = true;
					newIter = iter;
				}
			}
			if (!foundExisting)
				newIter = defaultAssociationsStore.Append ();
			
			defaultAssociationsView.SetCursor (
			    defaultAssociationsStore.GetPath (newIter),
			    defaultAssociationsView.GetColumn ((int) DACols.Extension),
			    true);
		}

		protected virtual void removeFileAssocation (object sender, System.EventArgs e)
		{
			TreeIter iter;
			if (!defaultAssociationsView.Selection.GetSelected (out iter))
				throw new InvalidOperationException
					("Should not be able to activate removeFileAssocation button while no row is selected.");
			
			string ext = (string) defaultAssociationsStore.GetValue (iter, (int)DACols.Extension);
			if (ext != null && ext.Trim ().Length > 0)
				removedExtensions.Add (ext.Trim ());
			defaultAssociationsStore.Remove (ref iter);
		}
		
		#endregion
		
		#region Adding/removing schemas
				
		TreeIter AppendSchemaToStore (XmlSchemaCompletionData schema)
		{
			return registeredSchemasStore.AppendValues (schema);
		}
		
		TreeIter AddRegisteredSchema (XmlSchemaCompletionData schema)
		{
			if (removedSchemas.Contains (schema))
				removedSchemas.Remove (schema);
			else
				addedSchemas.Add (schema);
			
			return AppendSchemaToStore (schema);
		}
		
		void RemoveRegisteredSchema (XmlSchemaCompletionData schema)
		{
			if (addedSchemas.Contains (schema) && !schema.ReadOnly)
				addedSchemas.Remove (schema);
			else
				removedSchemas.Add (schema);
			
			TreeIter iter;
			bool valid = registeredSchemasStore.GetIterFirst (out iter);
			while (valid) {
				if (GetRegisteredSchema (iter) == schema) {
					registeredSchemasStore.Remove (ref iter);
					break;
				}
				valid = registeredSchemasStore.IterNext (ref iter);
			}
			
			//restore built-in schema
			if (!schema.ReadOnly) {
				XmlSchemaCompletionData builtin = XmlSchemaManager.BuiltinSchemas[schema.NamespaceUri];
				if (builtin != null)
					AppendSchemaToStore (builtin);
			}
		}
		
		protected virtual void removeRegisteredSchema (object sender, System.EventArgs e)
		{
			XmlSchemaCompletionData schema = GetSelectedSchema ();
			if (schema == null)
				throw new InvalidOperationException ("Should not be able to activate removeRegisteredSchema button while no row is selected.");
			
			RemoveRegisteredSchema (schema);
		}

		protected virtual void addRegisteredSchema (object sender, System.EventArgs args)
		{
			string fileName = XmlEditorService.BrowseForSchemaFile ();
			if (string.IsNullOrEmpty (fileName))
				return;
			
			string shortName = System.IO.Path.GetFileName (fileName);
			
			//load the schema
			XmlSchemaCompletionData schema = null;
			try {
				schema = new XmlSchemaCompletionData (fileName);
			} catch (Exception ex) {
				string msg = GettextCatalog.GetString ("Error loading schema '{0}'.", shortName);
				MessageService.ShowException (ex, msg);
				return;
			}
			
			// Make sure the schema has a target namespace.
			if (schema.NamespaceUri == null) {
				MessageService.ShowError (
				    GettextCatalog.GetString ("Schema '{0}' has no target namespace.", shortName));
				return;
			}
			
			//if namaspace conflict, ask user whether they want to replace existing schema
			XmlSchemaCompletionData oldSchema = GetRegisteredSchema (schema.NamespaceUri);
			if (oldSchema != null) {
				bool replace = MessageService.Confirm (
				    GettextCatalog.GetString (
				        "A schema is already registered with the namespace '{0}'. Would you like to replace it?",
				        schema.NamespaceUri),
				    new AlertButton (GettextCatalog.GetString ("Replace"))
				);
				if (!replace)
					return;
				
				//remove the old schema
				RemoveRegisteredSchema (oldSchema);
			}
			
			// Store the schema so we can add it for real later, if the "ok" button's clicked
			TreeIter newIter = AddRegisteredSchema (schema);
			registeredSchemasView.Selection.SelectIter (newIter);
			ScrollToSelection (registeredSchemasView);
		}
		
		
		/// <summary>
		/// Scrolls the list so the specified item is visible.
		/// </summary>
		void ScrollToSelection (TreeView view)
		{
			TreeIter iter;
			TreeModel model;
			if (!registeredSchemasView.Selection.GetSelected (out model, out iter))
				return;
			view.ScrollToCell (model.GetPath (iter), null, false, 0, 0);
		}
		
		#endregion
		
		enum DACols {
			Extension,
			Namespace,
			Prefix,
			Changed
		}
	}
}
