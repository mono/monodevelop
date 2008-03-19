//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.XmlEditor;

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using Gtk;

namespace MonoDevelop.XmlEditor.Gui
{
	/// <summary>
	/// Shows the xml schemas that MonoDevelop knows about.
	/// </summary>
	public class XmlSchemasPanel : OptionsPanel
	{		
		XmlSchemasPanelWidget widget;
		
		public override Widget CreatePanelWidget ()
		{
			widget = new XmlSchemasPanelWidget ();
			widget.LoadUserSchemas (XmlSchemaManager.UserSchemas);
			
			List<XmlSchemaAssociation> assocs = new List<XmlSchemaAssociation> ();
			foreach (string s in XmlFileExtensions.Extensions)
				assocs.Add (XmlEditorAddInOptions.GetSchemaAssociation (s));
			widget.AddFileExtensions (assocs);
			
			return widget;
		}
		
		public override void ApplyChanges ()
		{
			if (widget.IsChanged) {
				try {
					//need to remove then add, in case we're replacing
					RemoveUserSchemas();
					AddUserSchemas();
					
					// Update schema associations after we have added any new schemas to the schema manager.
					foreach (string extension in widget.RemovedExtensions)
						XmlEditorAddInOptions.RemoveSchemaAssociation (extension);
					foreach (XmlSchemaAssociation item in widget.GetChangedXmlSchemaAssociations())
						XmlEditorAddInOptions.SetSchemaAssociation (item);
					
				} catch (Exception ex) {
					string msg = MonoDevelop.Core.GettextCatalog.GetString (
					    "Unhandled error saving schema changes.");
					MonoDevelop.Core.LoggingService.LogError (msg, ex);
					MonoDevelop.Core.Gui.MessageService.ShowException (ex, msg);
					return;
				}
			}
		}
	
		// Removes the schemas from the schema manager.
		void RemoveUserSchemas ()
		{
			while (widget.RemovedSchemas.Count > 0) {
				XmlSchemaManager.RemoveUserSchema (widget.RemovedSchemas [0].NamespaceUri);
				widget.RemovedSchemas.RemoveAt (0);
			}
		}
		
		// Adds the schemas to the schema manager.
		void AddUserSchemas ()
		{
			while (widget.AddedSchemas.Count > 0) {
				XmlSchemaManager.AddUserSchema (widget.AddedSchemas[0]);
				widget.AddedSchemas.RemoveAt (0);
			}
		}
	}
}
