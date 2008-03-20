//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
//

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Tasks;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MonoDevelop.XmlEditor
{
	public static class XmlEditorService
	{
		#region Task management
		public static void AddTask(string fileName, string message, int column, int line, TaskType taskType)
		{
			// HACK: Use a compiler error since we cannot add an error
			// task otherwise (task type property is read-only and
			// no constructors usable).
			System.CodeDom.Compiler.CompilerError error = new System.CodeDom.Compiler.CompilerError();
			error.Column = column;
			error.Line = line;
			error.ErrorText = message;
			error.FileName = fileName;
			error.IsWarning = false;
			
			//Task task = new Task(fileName, message, column, line);
			Task task = new Task(null, error);
			IdeApp.Services.TaskService.Add(task);
		}
		#endregion
		
		#region View tracking
		
		public static XmlTextEditorExtension ActiveEditor {
			get {
				Document doc = IdeApp.Workbench.ActiveDocument;
				if (doc != null)
					return doc.GetContent<XmlTextEditorExtension>();
				return null;
			}
		}
		
		public static ReadOnlyCollection<XmlTextEditorExtension> OpenXmlEditorViews {
			get {
				List<XmlTextEditorExtension> views = new List<XmlTextEditorExtension> ();				
				foreach (Document doc in IdeApp.Workbench.Documents) {
					XmlTextEditorExtension view = doc.GetContent<XmlTextEditorExtension>();
					if (view != null)
						views.Add (view);
				}
				return views.AsReadOnly();
			}
		}
		
		public static bool IsXmlEditorViewContentActive {
			get {
				return ActiveEditor != null;
			}
		}
		#endregion
		
		/*
		public static bool IsXslOutputViewContentActive {
			get {
				XmlEditorViewContent view = GetActiveView();
				return (view as XslOutputViewContent) != null;
			}
		}*/
		
		public static IProgressMonitor GetMonitor ()
		{
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor ("XML", "XmlFileIcon", true, true);
		}
		
		/// <summary>
		/// Checks that the xml in this view is well-formed.
		/// </summary>
		public static bool IsWellFormed (string xml, string fileName)
		{
			try {
				XmlDocument Document = new XmlDocument ();
				Document.LoadXml (xml);
				return true;
			} catch (XmlException ex) {
				using (IProgressMonitor monitor = GetMonitor ()) {
					monitor.Log.WriteLine (ex.Message);
					monitor.Log.WriteLine ();
					monitor.Log.WriteLine ("XML is not well formed.");
				}
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber, TaskType.Error);
				IdeApp.Services.TaskService.ShowErrors ();
			}
			return false;
		}
		
		#region Formatting utilities
		
		/// <summary>
		/// Returns a formatted xml string using a simple formatting algorithm.
		/// </summary>
		public static string SimpleFormat (string xml)
		{
			return xml.Replace ("><", string.Concat (">", Environment.NewLine, "<"));
		}
		
		/// <summary>
		/// Returns a pretty print version of the given xml.
		/// </summary>
		/// <param name="xml">Xml string to pretty print.</param>
		/// <returns>A pretty print version of the specified xml.  If the
		/// string is not well formed xml the original string is returned.
		/// </returns>
		public static string IndentedFormat (string xml)
		{
			try {
				XmlTextReader reader = new XmlTextReader (new StringReader (xml));
				reader.WhitespaceHandling = WhitespaceHandling.None;

				using (StringWriter indentedXmlWriter = new StringWriter ()) {
					using (XmlTextWriter writer = CreateXmlTextWriter (indentedXmlWriter)) {
						writer.WriteNode (reader, false);
						writer.Flush ();
					}
					return indentedXmlWriter.ToString ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error handling validated XML.", ex);
				return xml;
			}
		}
		
		/// <summary>
		/// Creates a XmlTextWriter using the current text editor
		/// properties for indentation.
		/// </summary>
		public static XmlTextWriter CreateXmlTextWriter (TextWriter textWriter)
		{
			XmlTextWriter xmlWriter = new XmlTextWriter(textWriter);
			xmlWriter.Formatting = Formatting.Indented;
			if (TextEditorProperties.ConvertTabsToSpaces) {
				xmlWriter.Indentation = TextEditorProperties.TabIndent;
				xmlWriter.IndentChar = ' ';
			} else {
				xmlWriter.Indentation = 1;
				xmlWriter.IndentChar = '\t';
			}
			return xmlWriter;
		}
		
		#endregion
		
		/// <summary>
		/// Runs an XSL transform on the input xml.
		/// </summary>
		/// <param name="input">The input xml to transform.</param>
		/// <param name="transform">The transform xml.</param>
		/// <returns>The output of the transform.</returns>
		public static string Transform(string input, string transform)
		{
			StringReader inputString = new StringReader(input);
			XPathDocument sourceDocument = new XPathDocument(inputString);

			StringReader transformString = new StringReader(transform);
			XPathDocument transformDocument = new XPathDocument(transformString);

			XslTransform xslTransform = new XslTransform();
			xslTransform.Load(transformDocument, new XmlUrlResolver(), null);
			
			MemoryStream outputStream = new MemoryStream();
			XmlTextWriter writer = new XmlTextWriter(outputStream, Encoding.UTF8);
			
			xslTransform.Transform(sourceDocument, null, writer, new XmlUrlResolver());

			int preambleLength = Encoding.UTF8.GetPreamble().Length;
			byte[] outputBytes = outputStream.ToArray();
			return UTF8Encoding.UTF8.GetString(outputBytes, preambleLength, outputBytes.Length - preambleLength);
		}
		
		public static string CreateSchema (string xml)
		{
			using (System.Data.DataSet dataSet = new System.Data.DataSet()) {
				dataSet.ReadXml(new StringReader(xml), System.Data.XmlReadMode.InferSchema);
				using (EncodedStringWriter writer = new EncodedStringWriter(Encoding.UTF8)) {
					using (XmlTextWriter xmlWriter = XmlEditorService.CreateXmlTextWriter(writer)) {				
						dataSet.WriteXmlSchema(xmlWriter);
						return writer.ToString();
					}
				}
			}
		}
		
		public static string GenerateSchemaFileName (string xmlFileName)
		{
			string baseFileName = Path.GetFileNameWithoutExtension (xmlFileName);
			string schemaFileName = string.Concat (baseFileName, ".xsd");
			int count = 1;
			while (File.Exists(schemaFileName)) {
				schemaFileName = String.Concat(baseFileName, count.ToString(), ".xsd");
				++count;
			}
			return schemaFileName;
		}
		
		#region Validation
		
		/// <summary>
		/// Validates the xml against known schemas.
		/// </summary>		
		public static void ValidateXml (IProgressMonitor monitor, string xml, string fileName)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Validating XML..."), 1);

			try {
				StringReader stringReader = new StringReader (xml);
				XmlTextReader xmlReader = new XmlTextReader (stringReader);
				xmlReader.XmlResolver = null;
				XmlValidatingReader reader = new XmlValidatingReader (xmlReader);
				reader.XmlResolver = null;
				
				XmlSchemaCompletionData schemaData = null;
				foreach (XmlSchemaCompletionData sd in XmlSchemaManager.SchemaCompletionDataItems) {
					schemaData = sd;
					reader.Schemas.Add (schemaData.Schema);
				}
				
				XmlDocument doc = new XmlDocument();
				doc.Load (reader);
				
			} catch (XmlSchemaException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber,TaskType.Error);
			}
			catch (XmlException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber,TaskType.Error);
			}
			
			if (IdeApp.Services.TaskService.SomethingWentWrong) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Validation failed."));
				IdeApp.Services.TaskService.ShowErrors();
			} else {
				monitor.ReportSuccess (GettextCatalog.GetString ("XML is valid."));
			}
			monitor.EndTask ();
		}
		
		/// <summary>
		/// Validates the schema.
		/// </summary>		
		public static void ValidateSchema (IProgressMonitor monitor, string xml, string fileName)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Validating schema..."), 1);

			try {
				StringReader stringReader = new StringReader (xml);
				XmlTextReader xmlReader = new XmlTextReader (stringReader);
				xmlReader.XmlResolver = null;
				
				ValidationEventHandler callback = delegate (object source, ValidationEventArgs args) {
					if (args.Severity == XmlSeverityType.Warning)
						monitor.ReportWarning (args.Message);
					else
						monitor.ReportError (args.Message, args.Exception);
					AddTask (fileName, args.Message, args.Exception.LinePosition, args.Exception.LineNumber,
					    (args.Severity == XmlSeverityType.Warning)? TaskType.Warning : TaskType.Error);
				};
				XmlSchema schema = XmlSchema.Read (xmlReader, callback);
				schema.Compile (callback);
			} 
			catch (XmlSchemaException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber,TaskType.Error);
			}
			catch (XmlException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber,TaskType.Error);
			}
			
			if (IdeApp.Services.TaskService.SomethingWentWrong) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Validation failed."));
				IdeApp.Services.TaskService.ShowErrors();
			} else {
				monitor.ReportSuccess (GettextCatalog.GetString ("Schema is valid."));
			}
			monitor.EndTask ();
		}
		
		#endregion Validation
	}
}
