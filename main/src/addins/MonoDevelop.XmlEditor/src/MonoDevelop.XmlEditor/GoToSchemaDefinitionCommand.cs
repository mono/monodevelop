//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using System;
using System.Xml.Schema;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Finds the definition of the Xml element or attribute under the cursor,
	/// finds the schema definition for it and then opens the schema and puts the cursor
	/// on the definition.
	/// </summary>
	public class GoToSchemaDefinitionCommand : CommandHandler
	{
		protected override void Run()
		{
			XmlEditorViewContent view = XmlEditorService.GetActiveView();
			if (view != null) {
				try {
					GoToSchemaDefinition(view.FileName, view.XmlEditorView);
				} catch (Exception ex) {
					Console.WriteLine(ex.ToString());
				}
			}
		}
		
		protected override void Update(CommandInfo info)
		{
			info.Enabled = XmlEditorService.IsXmlEditorViewContentActive;
		}
		
		public void GoToSchemaDefinition(string fileName, XmlEditorView xmlEditor)
		{
			// Find schema object for selected xml element or attribute.
			XmlCompletionDataProvider provider = new XmlCompletionDataProvider(xmlEditor.SchemaCompletionDataItems, xmlEditor.DefaultSchemaCompletionData, xmlEditor.DefaultNamespacePrefix);
			XmlSchemaCompletionData currentSchemaCompletionData = provider.FindSchemaFromFileName(fileName);						
			XmlSchemaObject schemaObject = XmlEditorView.GetSchemaObjectSelected(xmlEditor.Buffer.Text, xmlEditor.CursorOffset, provider, currentSchemaCompletionData);
			
			// Open schema.
			if (schemaObject != null && schemaObject.SourceUri != null && schemaObject.SourceUri.Length > 0) {
				string schemaFileName = schemaObject.SourceUri.Replace("file:/", String.Empty);
				IdeApp.Workbench.OpenDocument(schemaFileName, Math.Max(1, schemaObject.LineNumber), Math.Max(1, schemaObject.LinePosition), true);
			}
		}
	}
}
