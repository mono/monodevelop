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
using System.Xml;
using System.Xml.Xsl;
using System.Xml.XPath;

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
        
        		
		public static XmlEditorViewContent GetActiveView()
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc != null) {
				return doc.Content as XmlEditorViewContent;
			}
			return null;
		}
		
		public static bool IsXmlEditorViewContentActive {
			get {
				return GetActiveView() != null;
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

				StringWriter indentedXmlWriter = new StringWriter();
				XmlTextWriter writer = new XmlTextWriter(indentedXmlWriter);
				if (TextEditorProperties.ConvertTabsToSpaces) {
					writer.Indentation = TextEditorProperties.TabIndent;
					writer.IndentChar = ' ';
				} else {
					writer.Indentation = 1;
					writer.IndentChar = '\t';
				}
				writer.Formatting = Formatting.Indented;
				writer.WriteNode(reader, false);
				writer.Flush();

				indentedText = indentedXmlWriter.ToString();
			}
			catch(Exception) {
				indentedText = xml;
			}
			return indentedText;
		}
	}
}
