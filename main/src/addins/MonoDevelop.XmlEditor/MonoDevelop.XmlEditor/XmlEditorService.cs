//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.XmlEditor.Completion;
using MonoDevelop.Projects;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl;
using MonoDevelop.Components;
using Gtk;
using MonoDevelop.Components.Extensions;

namespace MonoDevelop.XmlEditor
{
	static class XmlEditorService
	{
		#region Task management
		public static void AddTask(string fileName, string message, int column, int line, TaskSeverity taskType)
		{
			// HACK: Use a compiler error since we cannot add an error
			// task otherwise (task type property is read-only and
			// no constructors usable).
			BuildError error = new BuildError ();
			error.Column = column;
			error.Line = line;
			error.ErrorText = message;
			error.FileName = fileName;
			error.IsWarning = false;
			
			//Task task = new Task(fileName, message, column, line);
			Task task = new Task (error);
			TaskService.Errors.Add(task);
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
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor ("XML", "md-xml-file-icon", true, true);
		}
		
		#region Formatting utilities
		
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
		
		public static XmlTextWriter CreateXmlTextWriter ()
		{
			return CreateXmlTextWriter (new EncodedStringWriter (System.Text.Encoding.UTF8));
		}
		
		#endregion
		
		/// <summary>
		/// Runs an XSL transform on the input xml.
		/// </summary>
		/// <param name="input">The input xml to transform.</param>
		/// <param name="transform">The transform xml.</param>
		/// <returns>The output of the transform.</returns>
		public static string Transform (string input, string transform)
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
				dataSet.ReadXml(new StringReader (xml), System.Data.XmlReadMode.InferSchema);
				using (EncodedStringWriter writer = new EncodedStringWriter (Encoding.UTF8)) {
					using (XmlTextWriter xmlWriter = XmlEditorService.CreateXmlTextWriter (writer)) {				
						dataSet.WriteXmlSchema(xmlWriter);
						return writer.ToString();
					}
				}
			}
		}
		
		public static string GenerateFileName (string sourceName, string extensionFormat)
		{
			return GenerateFileName (
			    Path.Combine (Path.GetDirectoryName (sourceName), Path.GetFileNameWithoutExtension (sourceName)) + 
			    extensionFormat);
		}
		
		// newNameFormat should be a string format for the new filename such as 
		// "/some/path/oldname{0}.xsd", where {0} is the index that will be incremented until a
		// non-existing file is found.
		public static string GenerateFileName (string newNameFormat)
		{
			string generatedFilename = string.Format (newNameFormat, "");
			int count = 1;
			while (File.Exists (generatedFilename)) {
				generatedFilename = string.Format (newNameFormat, count);
				++count;
			}
			return generatedFilename;
		}
		
		#region Validation
		
		/// <summary>
		/// Checks that the xml in this view is well-formed.
		/// </summary>
		public static XmlDocument ValidateWellFormedness (IProgressMonitor monitor, string xml, string fileName)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Validating XML..."), 1);
			bool error = false;
			XmlDocument doc = null;
			
			try {
				doc = new XmlDocument ();
				doc.LoadXml (xml);
			} catch (XmlException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber, TaskSeverity.Error);
				error = true;
			}
			
			if (error) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Validation failed."));
				TaskService.ShowErrors ();
			} else {
				monitor.Log.WriteLine (GettextCatalog.GetString ("XML is valid."));
			}
			
			monitor.EndTask ();
			return error? null: doc;
		}
		
		/// <summary>
		/// Validates the xml against known schemas.
		/// </summary>		
		public static XmlDocument ValidateXml (IProgressMonitor monitor, string xml, string fileName)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Validating XML..."), 1);
			bool error = false;
			XmlDocument doc = null;
			StringReader stringReader = new StringReader (xml);
			
			XmlReaderSettings settings = new XmlReaderSettings ();
			settings.ValidationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints
					| XmlSchemaValidationFlags.ProcessInlineSchema
					| XmlSchemaValidationFlags.ProcessSchemaLocation
					| XmlSchemaValidationFlags.ReportValidationWarnings;
			settings.ValidationType = ValidationType.Schema;
			settings.ProhibitDtd = false;
			
			ValidationEventHandler validationHandler = delegate (object sender, System.Xml.Schema.ValidationEventArgs args) {
				if (args.Severity == XmlSeverityType.Warning) {
					monitor.Log.WriteLine (args.Message);
					AddTask (fileName, args.Exception.Message, args.Exception.LinePosition, args.Exception.LineNumber,TaskSeverity.Warning);
				} else {
					AddTask (fileName, args.Exception.Message, args.Exception.LinePosition, args.Exception.LineNumber,TaskSeverity.Error);
					monitor.Log.WriteLine (args.Message);
					error = true;
				}	
			};
			settings.ValidationEventHandler += validationHandler;
			
			try {
				foreach (XmlSchemaCompletionData sd in XmlSchemaManager.SchemaCompletionDataItems)
					settings.Schemas.Add (sd.Schema);
				settings.Schemas.Compile ();
				
				XmlReader reader = XmlReader.Create (stringReader, settings);
				doc = new XmlDocument();
				doc.Load (reader);
				
			} catch (XmlSchemaException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber,TaskSeverity.Error);
				error = true;
			}
			catch (XmlException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber,TaskSeverity.Error);
				error = true;
			}
			finally {
				if (stringReader != null)
					stringReader.Dispose ();
				settings.ValidationEventHandler -= validationHandler;
			}
			
			if (error) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Validation failed."));
				TaskService.ShowErrors ();
			} else {
				monitor.Log.WriteLine  (GettextCatalog.GetString ("XML is valid."));
			}
			
			monitor.EndTask ();
			return error? null: doc;
		}
		
		/// <summary>
		/// Validates the schema.
		/// </summary>		
		public static XmlSchema ValidateSchema (IProgressMonitor monitor, string xml, string fileName)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Validating schema..."), 1);
			bool error = false;
			XmlSchema schema = null;
			try {
				StringReader stringReader = new StringReader (xml);
				XmlTextReader xmlReader = new XmlTextReader (stringReader);
				xmlReader.XmlResolver = null;
				
				ValidationEventHandler callback = delegate (object source, ValidationEventArgs args) {
					if (args.Severity == XmlSeverityType.Warning) {
						monitor.ReportWarning (args.Message);
					} else {
						monitor.ReportError (args.Message, args.Exception);
						error = true;
					}
					AddTask (fileName, args.Message, args.Exception.LinePosition, args.Exception.LineNumber,
					    (args.Severity == XmlSeverityType.Warning)? TaskSeverity.Warning : TaskSeverity.Error);
				};
				schema = XmlSchema.Read (xmlReader, callback);
				XmlSchemaSet sset = new XmlSchemaSet ();
				sset.Add (schema);
				sset.ValidationEventHandler += callback;
				sset.Compile ();
			} 
			catch (XmlSchemaException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber,TaskSeverity.Error);
				error = true;
			}
			catch (XmlException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber,TaskSeverity.Error);
				error = true;
			}
			
			if (error) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Validation failed."));
				TaskService.ShowErrors ();
			} else {
				monitor.Log.WriteLine  (GettextCatalog.GetString ("Schema is valid."));
			}
			
			monitor.EndTask ();
			return error? null: schema;
		}
		
		public static XslTransform ValidateStylesheet (IProgressMonitor monitor, string xml, string fileName)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Validating stylesheet..."), 1);
			bool error = true;
			XslTransform xslt = null;
			
			try {
				StringReader reader = new StringReader (xml);
				XPathDocument doc = new XPathDocument (reader);
				xslt = new XslTransform ();
				xslt.Load (doc, new XmlUrlResolver (), null);
				error = false;
			} catch (XsltCompileException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber,TaskSeverity.Error);
			}
			catch (XsltException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber,TaskSeverity.Error);
			}
			catch (XmlException ex) {
				monitor.ReportError (ex.Message, ex);
				AddTask (fileName, ex.Message, ex.LinePosition, ex.LineNumber,TaskSeverity.Error);
			}
			
			if (error) {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Validation failed."));
				TaskService.ShowErrors ();
			} else {
				monitor.Log.WriteLine (GettextCatalog.GetString ("Stylesheet is valid."));
			}
			return error? null: xslt;
		}
		
		#endregion
		
		#region File browsing utilities
		
		/// <summary>Allows the user to browse the file system for a stylesheet.</summary>
		/// <returns>The stylesheet filename the user selected; otherwise null.</returns>
		public static string BrowseForStylesheetFile ()
		{
			var dlg = new SelectFileDialog (GettextCatalog.GetString ("Select XSLT Stylesheet")) {
				TransientFor = IdeApp.Workbench.RootWindow,
			};
			dlg.AddFilter (new SelectFileDialogFilter (GettextCatalog.GetString ("XML Files", "*.xml")) {
				MimeTypes = { "text/xml", "application/xml" },
			});
			dlg.AddFilter (new SelectFileDialogFilter (GettextCatalog.GetString ("XSL Files"), "*.xslt", "*.xsl") {
				MimeTypes = { "text/x-xslt" },
			});
			dlg.AddAllFilesFilter ();
			
			if (dlg.Run ())
				return dlg.SelectedFile;
			return null;
		}
		
		/// <summary>Allows the user to browse the file system for a schema.</summary>
		/// <returns>The schema filename the user selected; otherwise null.</returns>
		public static string BrowseForSchemaFile ()
		{
			var dlg = new SelectFileDialog (GettextCatalog.GetString ("Select XML Schema"));
			dlg.AddFilter (new SelectFileDialogFilter (
				GettextCatalog.GetString ("XML Files"),
				new string[] { "*.xsd" },
				new string[] { "text/xml", "application/xml" }
			));
			dlg.AddAllFilesFilter ();
			
			if (dlg.Run ())
				return dlg.SelectedFile;
			return null;
		}
		
		#endregion
	}
}
