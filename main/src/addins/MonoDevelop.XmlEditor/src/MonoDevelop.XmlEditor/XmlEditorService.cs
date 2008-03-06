//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.SourceEditor.Properties;
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace MonoDevelop.XmlEditor
{
	public class XmlEditorService
	{		
		XmlEditorService()
		{
		}
		
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
 			TaskService.AddTask(task);
			TaskService.NotifyTaskChange();
        }
       	
       	public static TaskService TaskService {
        	get {
        		return (TaskService)ServiceManager.GetService(typeof(TaskService));
        	}
        }
        
        public static void ShowView(IViewContent view)
        {
        	IdeApp.Workbench.OpenDocument(view, true);
        }
        		
		public static XmlEditorViewContent GetActiveView()
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc != null) {
				return doc.Content as XmlEditorViewContent;
			}
			return null;
		}
		
		public static ViewContentCollection OpenXmlEditorViews {
			get {
				ViewContentCollection views = new ViewContentCollection();
				foreach (Document doc in IdeApp.Workbench.Documents) {
					XmlEditorViewContent view = doc.Content as XmlEditorViewContent;
					if (view != null) {
						views.Add(view);
					}
				}
				return views;
			}
		}
		
		public static XmlEditorViewContent GetOpenDocument(string fileName)
		{
			foreach (Document doc in IdeApp.Workbench.Documents) {
				XmlEditorViewContent view = doc.Content as XmlEditorViewContent;
				if (view != null) {
					if (view.FileName != null && doc.FileName == fileName) {
						return view;
					}
				}
			}
			return null;
		}
		
		public static bool IsXmlEditorViewContentActive {
			get {
				return GetActiveView() != null;
			}
		}
		
		public static bool IsXslOutputViewContentActive {
			get {
				XmlEditorViewContent view = GetActiveView();
				return (view as XslOutputViewContent) != null;
			}
		}
		
		public static IProgressMonitor GetMonitor()
		{
			return IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor("XML", "XmlFileIcon", true, true);
		}
		
		public static IMessageService MessageService {
			get {
				return (IMessageService)ServiceManager.GetService(typeof(IMessageService));
			}
		}
		
		/// <summary>
		/// Checks that the xml in this view is well-formed.
		/// </summary>
		public static bool IsWellFormed(string xml, string fileName)
		{
			try	{
				XmlDocument Document = new XmlDocument();
				Document.LoadXml(xml);
				return true;
			} catch(XmlException ex) {
				using (IProgressMonitor monitor = GetMonitor()) {
					monitor.Log.WriteLine(ex.Message);
					monitor.Log.WriteLine(String.Empty);
					monitor.Log.WriteLine("XML is not well formed.");
				}
				AddTask(fileName, ex.Message, ex.LinePosition, ex.LineNumber, TaskType.Error);
			}
			return false;
		}
		
		/// <summary>
		/// Returns a formatted xml string using a simple formatting algorithm.
		/// </summary>
		public static string SimpleFormat(string xml)
		{
			return xml.Replace("><", ">\r\n<");
		}
		
		/// <summary>
		/// Returns a pretty print version of the given xml.
		/// </summary>
		/// <param name="xml">Xml string to pretty print.</param>
		/// <returns>A pretty print version of the specified xml.  If the
		/// string is not well formed xml the original string is returned.
		/// </returns>
		public static string IndentedFormat(string xml)
		{
			string indentedText = String.Empty;

			try	{
				XmlTextReader reader = new XmlTextReader(new StringReader(xml));
				reader.WhitespaceHandling = WhitespaceHandling.None;

				using (StringWriter indentedXmlWriter = new StringWriter()) {
					using (XmlTextWriter writer = CreateXmlTextWriter(indentedXmlWriter)) {
						writer.WriteNode(reader, false);
						writer.Flush();
						indentedText = indentedXmlWriter.ToString();
					}
				}
			} catch(Exception) {
				indentedText = xml;
			}
			return indentedText;
		}
		
		/// <summary>
		/// Creates a XmlTextWriter using the current text editor
		/// properties for indentation.
		/// </summary>
		public static XmlTextWriter CreateXmlTextWriter(TextWriter textWriter)
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
	}
}
