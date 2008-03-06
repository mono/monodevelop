//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2006 Matthew Ward
//

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.SourceEditor.Properties;
using System;
using System.Data;
using System.IO;
using System.Text;
using System.Xml;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Creates a schema based on the xml in the currently active view.
	/// </summary>
	public class CreateSchemaCommand : CommandHandler
	{
		public CreateSchemaCommand()
		{
		}
	
		protected override void Run()
		{
			// Find active XmlView.
			XmlEditorViewContent view = XmlEditorService.GetActiveView();
			if (view != null) {			
				try {
					// Create a schema based on the xml.
					string schema = CreateSchema(view.Text, Encoding.UTF8, TextEditorProperties.ConvertTabsToSpaces, TextEditorProperties.TabIndent);
					
					// Create a new file and display the generated schema.
					string fileName = GenerateSchemaFileName(Path.GetFileName(view.FileName));
					OpenNewXmlFile(fileName, schema);
				} catch (Exception ex) {
					XmlEditorService.MessageService.ShowError(ex.Message);
				}
			}
		}
		
		protected override void Update(CommandInfo info)
		{
			info.Enabled = XmlEditorService.IsXmlEditorViewContentActive;
		}
		
		/// <summary>
		/// Creates a schema based on the xml content.
		/// </summary>
		/// <returns>A generated schema.</returns>
		string CreateSchema(string xml, Encoding encoding, bool convertTabsToSpaces, int tabIndent)
		{
			string schema = String.Empty;
			
			using (DataSet dataSet = new DataSet()) {
				dataSet.ReadXml(new StringReader(xml), XmlReadMode.InferSchema);
				EncodedStringWriter writer = new EncodedStringWriter(encoding);
				XmlTextWriter xmlWriter = new XmlTextWriter(writer);
				
				xmlWriter.Formatting = Formatting.Indented;
				if (convertTabsToSpaces) {
					xmlWriter.Indentation = tabIndent;
					xmlWriter.IndentChar = ' ';
				} else {
					xmlWriter.Indentation = 1;
					xmlWriter.IndentChar = '\t';
				}
				
				dataSet.WriteXmlSchema(xmlWriter);
				schema = writer.ToString();
				writer.Close();
				xmlWriter.Close();
			}
			
			return schema;
		}
		
		/// <summary>
		/// Opens a new unsaved xml file in SharpDevelop.
		/// </summary>
		void OpenNewXmlFile(string fileName, string xml)
		{
			IdeApp.Workbench.NewDocument(fileName, "text/xml", xml);
		}	
		/// <summary>
		/// Generates an xsd filename based on the name of the original xml 
		/// file.  If a file with the same name is already open in SharpDevelop
		/// then a new name is generated (e.g. MyXml1.xsd).
		/// </summary>
		string GenerateSchemaFileName(string xmlFileName)
		{
			string baseFileName = Path.GetFileNameWithoutExtension(xmlFileName);
			string schemaFileName = String.Concat(baseFileName, ".xsd");
			
			int count = 1;
			while (File.Exists(schemaFileName)) {
				schemaFileName = String.Concat(baseFileName, count.ToString(), ".xsd");
				++count;
			}
			return schemaFileName;
		}
	}
}
