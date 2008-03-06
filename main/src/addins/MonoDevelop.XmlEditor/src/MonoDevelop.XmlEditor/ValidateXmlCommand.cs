//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2006 Matthew Ward
//

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Tasks;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Validates the xml in the xml editor against the known schemas.
	/// </summary>
	public class ValidateXmlCommand : CommandHandler
	{		
		IProgressMonitor monitor;
		
		public ValidateXmlCommand()
		{
		}
	
		/// <summary>
		/// Validate the xml.
		/// </summary>
		protected override void Run()
		{
			// Find active Xml View.
			XmlEditorViewContent view = XmlEditorService.GetActiveView();
			if (view != null) {
				// Validate the xml.
				using (monitor = XmlEditorService.GetMonitor()) {
					ValidateXml(view.Text, view.FileName);
				}
			}	
		}
		
		protected override void Update(CommandInfo info)
		{
			info.Enabled = XmlEditorService.IsXmlEditorViewContentActive;
		}

	
		/// <summary>
		/// Validates the xml against known schemas.
		/// </summary>		
		void ValidateXml(string xml, string fileName)
		{
			XmlEditorService.TaskService.ClearTasks();
			OutputWindowWriteLine("Validating XML...");

			try {
				StringReader stringReader = new StringReader(xml);
				XmlTextReader xmlReader = new XmlTextReader(stringReader);
				xmlReader.XmlResolver = null;
				XmlValidatingReader reader = new XmlValidatingReader(xmlReader);
				reader.XmlResolver = null;
				
				XmlSchemaCompletionData schemaData = null;
				try {
					for (int i = 0; i < XmlSchemaManager.SchemaCompletionDataItems.Count; ++i) {
						schemaData = XmlSchemaManager.SchemaCompletionDataItems[i];
						reader.Schemas.Add(schemaData.Schema);
					}
				} catch (XmlSchemaException ex) {
					DisplayValidationError(schemaData.FileName, ex.Message, ex.LinePosition, ex.LineNumber);
					return;
				}
				XmlDocument doc = new XmlDocument();
				doc.Load(reader);
				                   
				OutputWindowWriteLine(String.Empty);
				OutputWindowWriteLine("XML is valid.");
						
			} catch (XmlSchemaException ex) {
				DisplayValidationError(fileName, ex.Message, ex.LinePosition, ex.LineNumber);
			} catch (XmlException ex) {
				DisplayValidationError(fileName, ex.Message, ex.LinePosition, ex.LineNumber);
			}
		}
				
		/// <summary>
        /// Displays the validation error.
        /// </summary>
        void DisplayValidationError(string fileName, string message, int column, int line)
        {
        	OutputWindowWriteLine(String.Empty);
        	OutputWindowWriteLine(message);
			OutputWindowWriteLine(String.Empty);
			OutputWindowWriteLine("Validation failed.");
			XmlEditorService.AddTask(fileName, message, column, line, TaskType.Error);
       	}
       	
       	void OutputWindowWriteLine(string message)
        {
  			monitor.Log.WriteLine(message);
       	}
	}
}
