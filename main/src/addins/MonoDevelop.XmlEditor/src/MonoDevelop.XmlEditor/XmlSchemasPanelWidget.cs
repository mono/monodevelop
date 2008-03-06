//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core.Gui.Dialogs;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace MonoDevelop.XmlEditor
{
	public class XmlSchemasPanelWidget : GladeWidgetExtract
	{
		[Glade.Widget] TreeView schemaTreeView;
		[Glade.Widget] Button addButton;
		[Glade.Widget] Button removeButton;
		[Glade.Widget] ComboBox fileExtensionComboBox;
		[Glade.Widget] Entry schemaEntry;
		[Glade.Widget] Button changeSchemaButton;
		[Glade.Widget] Entry namespacePrefixEntry;
		
		bool changed;
		bool ignoreNamespacePrefixTextChanges;
		ListStore schemaList = new ListStore(typeof(string), typeof(string), typeof(XmlSchemaCompletionData));
		XmlSchemaCompletionDataCollection addedSchemas = new XmlSchemaCompletionDataCollection();
		StringCollection removedSchemaNamespaces = new StringCollection();
		ListStore fileExtensionList = new ListStore(typeof(string), typeof(string), typeof(string), typeof(bool));
		
		public XmlSchemasPanelWidget() : base ("XmlEditor.glade", "XmlSchemasPanel")
		{
			// Schema list.
			schemaTreeView.Model = schemaList;
			schemaTreeView.Selection.Mode = SelectionMode.Single;
			CellRendererText renderer = new CellRendererText();
			renderer.Editable = false;
			schemaTreeView.AppendColumn("Namespace", renderer, "text", 0, "foreground", 1);
			schemaTreeView.Selection.Changed += new EventHandler(SchemaTreeViewSelectionChanged);

			// Remove button.		
			removeButton.Sensitive = false;
			removeButton.Clicked += new EventHandler(RemoveButtonClick);
			
			// Add button.
			addButton.Clicked += new EventHandler(AddButtonClick);
			
			// Namespace prefix text entry.
			namespacePrefixEntry.Editable = true;
			namespacePrefixEntry.Changed += new EventHandler(NamespacePrefixEntryChanged);
		
			// Change schema button.
			changeSchemaButton.Clicked += new EventHandler(ChangeSchemaButtonClick);
	
			// File Extensions.
			CellRendererText fileExtRenderer = new CellRendererText();
			fileExtensionComboBox.PackStart(fileExtRenderer, true);
			fileExtensionComboBox.AddAttribute(fileExtRenderer, "text", 0);
			fileExtensionComboBox.Changed += new EventHandler(FileExtensionComboBoxChanged);
		}
		
		public XmlSchemaCompletionDataCollection Schemas {
			get {
				XmlSchemaCompletionDataCollection schemas = new XmlSchemaCompletionDataCollection();
				TreeIter iter;
				bool addSchemas = schemaList.GetIterFirst(out iter);
				while (addSchemas) {
					XmlSchemaCompletionData schema = GetSchema(iter);
					schemas.Add(schema);
					addSchemas = schemaList.IterNext(ref iter);
				}
				return schemas;
			}
		}
		
		public XmlSchemaCompletionDataCollection AddedSchemas {
			get {
				return addedSchemas;
			}
		}
		
		public StringCollection RemovedSchemaNamespaces {
			get {
				return removedSchemaNamespaces;
			}
		}
		
		/// <summary>
		/// Gets whether the schemas have been modified by the
		/// user.
		/// </summary>
		public bool IsChanged {
			get {
				return changed;
			}
		}
		
		public void AddSchemas(XmlSchemaCompletionDataCollection newSchemas)
		{
			schemaTreeView.Model = null;
			foreach (XmlSchemaCompletionData schema in newSchemas) {
				AddRow(schema);
			}
			schemaTreeView.Model = schemaList;
			schemaList.SetSortColumnId(0, SortType.Ascending);
		}
		
		public void AddFileExtensions(StringCollection extensions)
		{
			PopulateFileExtensionComboBox(extensions);
		}
		
		public XmlSchemaAssociation[] GetChangedXmlSchemaAssociations()
		{
			ArrayList items = new ArrayList();
			
			TreeIter iter;
			bool addItems = fileExtensionList.GetIterFirst(out iter);
			while (addItems) {
				if (IsAssociationChanged(iter)) {
					XmlSchemaAssociation item = CreateSchemaAssociation(iter);
					items.Add(item);
				}
				addItems = fileExtensionList.IterNext(ref iter);
			}
			
			XmlSchemaAssociation[] associations = new XmlSchemaAssociation[items.Count];
			items.CopyTo(associations);
			return associations;
		}
		
		TreeIter AddRow(XmlSchemaCompletionData schema)
		{
			return schemaList.AppendValues(schema.NamespaceUri, GetTextColour(schema.ReadOnly), schema);
		}
		
		/// <summary>
		/// Gets the colour to display the text if the schema
		/// is read-only.
		/// </summary>
		string GetTextColour(bool readOnly)
		{
			if (readOnly) {
				return "gray";
			}
			return null;
		}
		
		XmlSchemaCompletionData GetSchema(TreeIter iter)
		{
			return schemaList.GetValue(iter, 2) as XmlSchemaCompletionData;
		}
				
		/// <summary>
		/// Enables the remove button if a list item is selected.
		/// </summary>
		void SchemaTreeViewSelectionChanged(object source, EventArgs e)
		{
			TreeIter iter;
			if (schemaTreeView.Selection.GetSelected(out iter)) {
				XmlSchemaCompletionData schema = GetSchema(iter);
				if (schema != null) {
					removeButton.Sensitive = !schema.ReadOnly;
				}
			}
		}
		
		void AddButtonClick(object source, EventArgs e)
		{
			try {
				string schemaFileName = BrowseForSchema();
				
				// Add schema if the namespace does not already exist.
				if (schemaFileName.Length > 0) {
					changed = AddSchema(schemaFileName);
				}
			} catch (Exception ex) {
				IMessageService messageService = (IMessageService)ServiceManager.GetService(typeof(IMessageService));
				messageService.ShowError(ex, "Failed to add the schema.");
			}
		}
		
		/// <summary>
		/// Allows the user to browse the file system for a schema.
		/// </summary>
		/// <returns>The schema file name the user selected; otherwise an 
		/// empty string.</returns>
		string BrowseForSchema()
		{
			using (FileSelector fs = new FileSelector ()) {
				fs.SelectMultiple = false;
				
				FileFilter xmlFiles = new FileFilter();
				xmlFiles.Name = "XML Files";
				xmlFiles.AddMimeType("text/xml");
				fs.AddFilter(xmlFiles);
				
				FileFilter allFiles = new FileFilter();
				allFiles.Name = "All Files";
				allFiles.AddPattern("*");
				fs.AddFilter(allFiles);
				
				int response = fs.Run ();
				
				string fileName = fs.Filename;
				fs.Hide ();
				if (response == (int)Gtk.ResponseType.Ok) {
					return fileName;
				}
			}
			return String.Empty;
		}
		
		/// <summary>
		/// Loads the specified schema and adds it to an internal collection.
		/// </summary>
		/// <remarks>The schema file is not copied to the user's schema folder
		/// until they click the OK button.</remarks>
		/// <returns><see langword="true"/> if the schema namespace 
		/// does not already exist; otherwise <see langword="false"/>
		/// </returns>
		bool AddSchema(string fileName)
		{			
			// Load the schema.
			XmlSchemaCompletionData schema = new XmlSchemaCompletionData(fileName);
			
			// Make sure the schema has a target namespace.
			if (schema.NamespaceUri == null) {
				IMessageService messageService =(IMessageService)ServiceManager.GetService(typeof(IMessageService));
				messageService.ShowError(String.Concat("Schema has no target namespace: ", System.IO.Path.GetFileName(schema.FileName)));
				return false;
			}
			
			// Check that the schema does not exist.
			if (SchemaNamespaceExists(schema.NamespaceUri)) {	
				IMessageService messageService =(IMessageService)ServiceManager.GetService(typeof(IMessageService));
				messageService.ShowError(String.Concat("A schema already exists with this namespace: ", schema.NamespaceUri));
				return false;
			} 

			// Store the schema so we can add it later.
			TreeIter iter = AddRow(schema);
			ScrollToItem(iter);
			schemaTreeView.Selection.SelectIter(iter);

			addedSchemas.Add(schema);
			if (removedSchemaNamespaces.Contains(schema.NamespaceUri)) {
				removedSchemaNamespaces.Remove(schema.NamespaceUri);
			}
			
			return true;
		}		
		
		/// <summary>
		/// Checks that the schema namespace does not already exist.
		/// </summary>
		bool SchemaNamespaceExists(string namespaceURI)
		{
			foreach (XmlSchemaCompletionData schema in Schemas) {
				if (schema.NamespaceUri == namespaceURI) {
					return true;
				}	
			}

			// Makes sure it has not been flagged for removal.
			if (removedSchemaNamespaces.Count > 0) {
				return !removedSchemaNamespaces.Contains(namespaceURI);
			}
			return false;
		}
				
		void RemoveButtonClick(object source, EventArgs e)
		{
			// Remove selected schema.
			TreeIter iter;
			if (schemaTreeView.Selection.GetSelected(out iter)) {
				RemoveSchema(iter);
				changed = true;
			}
		}
				
		void RemoveSchema(TreeIter iter)
		{
			XmlSchemaCompletionData schema = GetSchema(iter);
			if (schema != null) {
				schemaList.Remove(ref iter);
				OnSchemaRemoved(schema);
			}
		}
		
		/// <summary>
		/// Schedules the schema for removal.
		/// </summary>
		void OnSchemaRemoved(XmlSchemaCompletionData schema)
		{			
			XmlSchemaCompletionData addedSchema = addedSchemas[schema.NamespaceUri];
			if (addedSchema != null) {
				addedSchemas.Remove(addedSchema);
			} else {
				removedSchemaNamespaces.Add(schema.NamespaceUri);
			}
		}
		
		/// <summary>
		/// Scrolls the list so the specified item is visible.
		/// </summary>
		void ScrollToItem(TreeIter iter)
		{
			TreePath path = schemaList.GetPath(iter);
			if (path != null) {
				schemaTreeView.ScrollToCell(path, null, false, 0, 0);
			}
		}
		
		/// <summary>
		/// User has changed the namespace prefix.
		/// </summary>
		void NamespacePrefixEntryChanged(object source, EventArgs e)
		{
			if (!ignoreNamespacePrefixTextChanges) {
				TreeIter iter;
				if (fileExtensionComboBox.GetActiveIter(out iter)) {
					SetNamespacePrefixForSchemaAssociation(iter, namespacePrefixEntry.Text);
					SetAssociationChanged(iter);
				}
			}
		}
		
		/// <summary>
		/// Allows the user to change the schema associated with an xml file 
		/// extension.
		/// </summary>
		void ChangeSchemaButtonClick(object source, EventArgs e)
		{
			string[] namespaces = GetSchemaNamespaces();
			using (SelectXmlSchemaDialog dialog = new SelectXmlSchemaDialog(namespaces)) {
				dialog.SelectedNamespaceUri = schemaEntry.Text;
				if (dialog.Run() == (int)Gtk.ResponseType.Ok) {
					schemaEntry.Text = dialog.SelectedNamespaceUri;
					TreeIter iter;
					if (fileExtensionComboBox.GetActiveIter(out iter)) {
						SetNamespaceUriForSchemaAssociation(iter, dialog.SelectedNamespaceUri);
						SetAssociationChanged(iter);
					}
				}
			}
		}
		
		/// <summary>
		/// Returns an array of schema namespace strings that will be displayed
		/// when the user chooses to associated a namespace to a file extension
		/// by default.
		/// </summary>
		string[] GetSchemaNamespaces()
		{
			XmlSchemaCompletionDataCollection schemas = Schemas;
			string[] namespaces = new string[schemas.Count];
			int count = 0;
			foreach (XmlSchemaCompletionData schema in schemas) {
				namespaces[count] = schema.NamespaceUri;
				++count;
			}
			return namespaces;
		}
		
		/// <summary>
		/// Shows the namespace associated with the selected xml file extension.
		/// </summary>
		void FileExtensionComboBoxChanged(object source, EventArgs e)
		{
			schemaEntry.Text = String.Empty;
			ignoreNamespacePrefixTextChanges = true;
			namespacePrefixEntry.Text = String.Empty;
			
			try {
				TreeIter iter;
				if (fileExtensionComboBox.GetActiveIter(out iter)) {
					schemaEntry.Text = GetNamespaceUriForSchemaAssociation(iter);
					namespacePrefixEntry.Text = GetNamespacePrefixForSchemaAssociation(iter);
				}
			} finally {
				ignoreNamespacePrefixTextChanges = false;			
			}			
		}	
		
		/// <summary>
		/// Reads the configured xml file extensions and their associated namespaces.
		/// </summary>
		void PopulateFileExtensionComboBox(StringCollection extensions)
		{
			int count = 0;
			foreach (string extension in extensions) {
				XmlSchemaAssociation item = XmlEditorAddInOptions.GetSchemaAssociation(extension);
				fileExtensionList.AppendValues(extension, item.NamespaceUri, item.NamespacePrefix);
				++count;
			}
			fileExtensionComboBox.Model = fileExtensionList;
			fileExtensionList.SetSortColumnId(0, SortType.Ascending);
			
			if (count > 0) {
				fileExtensionComboBox.Active = 0;
				FileExtensionComboBoxChanged(fileExtensionComboBox, new EventArgs());
			}
		}	
						
		/// <summary>
		/// Flags the association as having been changed.
		/// </summary>
		void SetAssociationChanged(TreeIter iter)
		{
			fileExtensionList.SetValue(iter, 3, true);
		}
		
		bool IsAssociationChanged(TreeIter iter)
		{	
			return (bool)fileExtensionList.GetValue(iter, 3);
		}
		
		string GetNamespaceUriForSchemaAssociation(TreeIter iter)
		{
			return (string)fileExtensionList.GetValue(iter, 1);			
		}

		void SetNamespaceUriForSchemaAssociation(TreeIter iter, string namespaceUri)
		{
			fileExtensionList.SetValue(iter, 1, namespaceUri);			
		}
		
		string GetNamespacePrefixForSchemaAssociation(TreeIter iter)
		{
			return (string)fileExtensionList.GetValue(iter, 2);
		}
		
		void SetNamespacePrefixForSchemaAssociation(TreeIter iter, string prefix)
		{
			fileExtensionList.SetValue(iter, 2, prefix);
		}
		
		string GetFileExtension(TreeIter iter)
		{
			return (string)fileExtensionList.GetValue(iter, 0);
		}
		
		XmlSchemaAssociation CreateSchemaAssociation(TreeIter iter)
		{
			return new XmlSchemaAssociation(GetFileExtension(iter), 
				GetNamespaceUriForSchemaAssociation(iter), 
				GetNamespacePrefixForSchemaAssociation(iter));
		}
	}
}
