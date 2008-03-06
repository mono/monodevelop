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
		string schemaFileName = String.Empty;
		
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
					if (view.IsSchema) {
						ValidateSchema(view.Text, view.FileName);
					} else {
						ValidateXml(view.Text, view.FileName);
					}
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
					ShowValidationError(schemaData.FileName, ex.Message, ex.LinePosition, ex.LineNumber);
					ShowValidationFailedMessage();
					return;
				}
				XmlDocument doc = new XmlDocument();
				doc.Load(reader);
				                   
				OutputWindowWriteLine(String.Empty);
				OutputWindowWriteLine("XML is valid.");
						
			} catch (XmlSchemaException ex) {
				ShowValidationError(fileName, ex.Message, ex.LinePosition, ex.LineNumber);
				ShowValidationFailedMessage();
			} catch (XmlException ex) {
				ShowValidationError(fileName, ex.Message, ex.LinePosition, ex.LineNumber);
				ShowValidationFailedMessage();
			}
		}
				
		/// <summary>
		/// Displays the validation failed message.
		/// </summary>
		void ShowValidationFailedMessage()
		{
			OutputWindowWriteLine(String.Empty);
			OutputWindowWriteLine("Validation failed.");
		}
       	
		void ShowValidationError(string fileName, string message, int column, int line)
		{
			OutputWindowWriteLine(message);
			XmlEditorService.AddTask(fileName, message, column, line, TaskType.Error);
		}
       	
		void ShowValidationWarning(string fileName, string message, int line, int column)
		{
			OutputWindowWriteLine(message);
			XmlEditorService.AddTask(fileName, message, column, line, TaskType.Warning);
		}       	
       	
		void OutputWindowWriteLine(string message)
		{
			monitor.Log.WriteLine(message);
		}
       	
		/// <summary>
		/// Validates the schema.
		/// </summary>		
		void ValidateSchema(string xml, string fileName)
		{
			XmlEditorService.TaskService.ClearTasks();
			OutputWindowWriteLine("Validating schema...");

			try {
				StringReader stringReader = new StringReader(xml);
				XmlTextReader xmlReader = new XmlTextReader(stringReader);
				xmlReader.XmlResolver = null;
				schemaFileName = fileName;
				XmlSchema schema = XmlSchema.Read(xmlReader, new ValidationEventHandler(SchemaValidation));
				schema.Compile(new ValidationEventHandler(SchemaValidation));
			} catch (XmlSchemaException ex) {
				ShowValidationError(fileName, ex.Message, ex.LinePosition, ex.LineNumber);
			} catch (XmlException ex) {
				ShowValidationError(fileName, ex.Message, ex.LinePosition, ex.LineNumber);
			}
			
			if (XmlEditorService.TaskService.SomethingWentWrong) {
				ShowValidationFailedMessage();
			} else {
				OutputWindowWriteLine(String.Empty);
				OutputWindowWriteLine("Schema is valid.");
			}
		}
		
		void SchemaValidation(object source, ValidationEventArgs e)
		{
			if (e.Severity == XmlSeverityType.Warning) {
				ShowValidationWarning(schemaFileName, e.Message, e.Exception.LinePosition, e.Exception.LineNumber);				
			} else {
				ShowValidationError(schemaFileName, e.Message, e.Exception.LinePosition, e.Exception.LineNumber);				
			}
		}
	}
}
