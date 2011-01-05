//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
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

using System;
using System.Collections.Generic;

using Gtk;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.XmlEditor;

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
			widget.AddFileExtensions (XmlFileAssociationManager.GetAssociations ());
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
						XmlEditorOptions.RemoveFileAssociation (extension);
					foreach (var item in widget.GetChangedXmlFileAssociations())
						XmlEditorOptions.SetFileAssociation (item);
					
				} catch (Exception ex) {
					string msg = MonoDevelop.Core.GettextCatalog.GetString (
					    "Unhandled error saving schema changes.");
					MonoDevelop.Core.LoggingService.LogError (msg, ex);
					MonoDevelop.Ide.MessageService.ShowException (ex, msg);
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
