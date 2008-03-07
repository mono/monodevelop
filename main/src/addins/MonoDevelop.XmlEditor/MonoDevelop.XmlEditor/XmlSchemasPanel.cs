//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Shows the xml schemas that MonoDevelop knows about.
	/// </summary>
	public class XmlSchemasPanel : AbstractOptionPanel
	{		
		XmlSchemasPanelWidget widget;	
		
		/// <summary>
		/// Initialises the panel.
		/// </summary>
		public override void LoadPanelContents()
		{
			widget = new XmlSchemasPanelWidget();
			widget.AddSchemas(XmlSchemaManager.SchemaCompletionDataItems);
			widget.AddFileExtensions(XmlFileExtensions.Extensions);
			Add(widget);
		}

		/// <summary>
		/// Saves any changes to the configured schemas.
		/// </summary>
		public override bool StorePanelContents()
		{
			if (widget.IsChanged) {
				try {
					return SaveSchemaChanges();
				} catch (Exception ex) {
					MonoDevelop.Core.LoggingService.LogError ("Could not open document.", ex);
					MonoDevelop.Core.Gui.MessageService.ShowError ("Could not open document.", ex.ToString());
					return false;
				}
			}

			// Update schema associations after we have added any new schemas to
			// the schema manager.
			UpdateSchemaAssociations();
		
			return true;
		}

		/// <summary>
		/// Saves any changes to the configured schemas.
		/// </summary>
		/// <returns></returns>
		bool SaveSchemaChanges()
		{
			RemoveUserSchemas();
			AddUserSchemas();

			return true;
		}
	
		/// <summary>
		/// Removes the schemas from the schema manager.
		/// </summary>
		void RemoveUserSchemas()
		{
			while (widget.RemovedSchemaNamespaces.Count > 0) {
				XmlSchemaManager.RemoveUserSchema(widget.RemovedSchemaNamespaces[0]);
				widget.RemovedSchemaNamespaces.RemoveAt(0);
			}
		}
		
		/// <summary>
		/// Adds the schemas to the schema manager.
		/// </summary>
		void AddUserSchemas()
		{
			while (widget.AddedSchemas.Count > 0) {
				XmlSchemaManager.AddUserSchema(widget.AddedSchemas[0]);
				widget.AddedSchemas.RemoveAt(0);
			}
		}
		
		/// <summary>
		/// Updates the configured file extension to namespace mappings.
		/// </summary>
		void UpdateSchemaAssociations()
		{
			foreach (XmlSchemaAssociation item in widget.GetChangedXmlSchemaAssociations()) {
				XmlEditorAddInOptions.SetSchemaAssociation(item);
			}
		}
	}
}
