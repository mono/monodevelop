//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2006 Matthew Ward
//

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using System;
using System.IO;
using System.Xml;
using System.Xml.XPath;using System.Xml.Xsl;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Runs an XSL transform on an xml document.
	/// </summary>
	public class RunXslTransformCommand : CommandHandler
	{
		IProgressMonitor monitor;
		
		/// <summary>
		/// Runs the transform on the xml file using the assigned stylesheet. 
		/// If no stylesheet is assigned the user is prompted to choose one.
		/// If the view represents a stylesheet that is currently assigned to an
		/// opened document then run the transform on that document.
		/// </summary>
		protected override void Run()
		{
			XmlEditorViewContent view = XmlEditorService.GetActiveView();
			if (view != null) {
				
				if (view is XslOutputViewContent) {
					return;
				}
				
				// Check to see if this view is actually a referenced stylesheet.
				if (view.FileName != null && view.FileName.Length > 0) {
					XmlEditorViewContent associatedView = GetAssociatedXmlEditorView(view.FileName);
					if (associatedView != null) {
						Console.WriteLine("Using associated xml view.");
						view = associatedView;
					}
				}
				
				// Assign a stylesheet.
				if (view.StylesheetFileName == null) {
					view.StylesheetFileName = AssignStylesheetCommand.BrowseForStylesheetFile();
				}
				
				if (view.StylesheetFileName != null) {
					try {
						using (monitor = XmlEditorService.GetMonitor()) {
							RunXslTransform(view.FileName, view.Text, view.StylesheetFileName, GetStylesheetContent(view.StylesheetFileName));
						}
					} catch (Exception ex) {
						XmlEditorService.MessageService.ShowError(ex);
					}
				}
			}
		}
		
		protected override void Update(CommandInfo info)
		{
			info.Enabled = XmlEditorService.IsXmlEditorViewContentActive;
		}
		
		/// <summary>
		/// Gets the xml view that is currently referencing the 
		/// specified stylesheet view.
		/// </summary>
		XmlEditorViewContent GetAssociatedXmlEditorView(string stylesheetFileName)
		{
			foreach (XmlEditorViewContent view in XmlEditorService.OpenXmlEditorViews) {
				if (view.StylesheetFileName != null) {
					if (view.StylesheetFileName == stylesheetFileName) {
						return view;
					}
				}
			}
			return null;
		}
		
		string GetStylesheetContent(string fileName)
		{
			// File already open?
			XmlEditorViewContent view = XmlEditorService.GetOpenDocument(fileName);
			if (view != null) {
				return view.Text;
			} else {
				Console.WriteLine("Stylesheet file not opened in xml editor.");
			}
			
			// Read in file contents.
			StreamReader reader = new StreamReader(fileName, true);
			return reader.ReadToEnd();
		}
		
		/// <summary>
		/// Applies the stylesheet to the xml and displays the resulting output.
		/// </summary>
		void RunXslTransform(string fileName, string xml, string xslFileName, string xsl)
		{
			XmlEditorService.TaskService.ClearTasks();				
			if (IsWellFormed(fileName, xml)) {
				if (IsValidXsl(xslFileName, xsl)) {
					string transformedXml = XmlEditorService.Transform(xml, xsl);
					ShowTransformOutput(transformedXml);
				}
			}
		}
		
		void OutputWindowWriteLine(string message)
		{
			monitor.Log.WriteLine(message);
		}
		
		/// <summary>
		/// Displays the transformed output.
		/// </summary>
		void ShowTransformOutput(string xml)
		{
			// Pretty print the xml.
			xml = XmlEditorService.SimpleFormat(XmlEditorService.IndentedFormat(xml));
			
			// Display the output xml.
			XslOutputViewContent view = XslOutputViewContent.Instance;
			if (view == null) {
				view = new XslOutputViewContent();
				view.LoadContent(xml);
				XmlEditorService.ShowView(view);
			} else {
				// Transform output window already opened.
				view.LoadContent(xml);
				view.JumpTo(1, 1);
				view.WorkbenchWindow.SelectWindow();
			}
		}
		
		/// <summary>
		/// Checks that the xml in this view is well-formed.
		/// </summary>
		bool IsWellFormed(string fileName, string xml)
		{
			try	{
				XmlDocument Document = new XmlDocument( );
				Document.LoadXml(xml);
				return true;
			} catch(XmlException ex) {
				OutputWindowWriteLine(ex.Message);
				XmlEditorService.AddTask(fileName, ex.Message, ex.LinePosition, ex.LineNumber, TaskType.Error);
			}
			return false;
		}
		
		/// <summary>
		/// Validates the given xsl string,.
		/// </summary>
		bool IsValidXsl(string fileName, string xml)
		{
			try {
				StringReader reader = new StringReader(xml);
				XPathDocument doc = new XPathDocument(reader);

				XslTransform xslTransform = new XslTransform();
				xslTransform.Load(doc, new XmlUrlResolver(), null);
				return true;
			} catch(XsltCompileException ex) {
				string message = String.Empty;
				if(ex.InnerException != null) {
					message = ex.InnerException.Message;
				} else {
					message = ex.ToString();
				}
				OutputWindowWriteLine(message);
				XmlEditorService.AddTask(fileName, message, ex.LineNumber, ex.LinePosition, TaskType.Error);
			} catch(XsltException ex) {
				OutputWindowWriteLine(ex.Message);
				XmlEditorService.AddTask(fileName, ex.Message, ex.LinePosition, ex.LineNumber, TaskType.Error);
			} catch(XmlException ex) {
				OutputWindowWriteLine(ex.Message);
				XmlEditorService.AddTask(fileName, ex.Message, ex.LinePosition, ex.LineNumber, TaskType.Error);
			}
			return false;
		}
	}
}
