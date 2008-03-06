//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2006 Matthew Ward
//

using Gtk;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using System;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Allows the user to browse for an XSLT stylesheet.  The selected
	/// stylesheet will be assigned to the currently open xml file.
	/// </summary>
	public class AssignStylesheetCommand : CommandHandler
	{
		public static string BrowseForStylesheetFile()
		{
			using (FileSelector fs = new FileSelector ("Assign XSLT Stylesheet")) {	
				
				FileFilter xmlFiles = new FileFilter();
				xmlFiles.Name = "XML Files";
				xmlFiles.AddMimeType("text/xml");
				fs.AddFilter(xmlFiles);
				
				FileFilter xslFiles = new FileFilter();
				xslFiles.Name = "XSL Files";
				xslFiles.AddMimeType("text/x-xslt");
				xslFiles.AddPattern("*.xslt;*.xsl");
				fs.AddFilter(xslFiles);

				FileFilter allFiles = new FileFilter();
				allFiles.Name = "All Files";
				allFiles.AddPattern("*");
				fs.AddFilter(allFiles);

				
				int response = fs.Run ();
				string name = fs.Filename;
				fs.Hide ();
				if (response == (int)Gtk.ResponseType.Ok) {
					return name;
				}
			}
			return null;
		}
		
		protected override void Run()
		{
			// Get active xml document.
			XmlEditorViewContent view = XmlEditorService.GetActiveView();
			if (view != null) {
				
				// Prompt user for filename.
				string stylesheetFileName = BrowseForStylesheetFile();
				
				// Assign stylesheet.
				if (stylesheetFileName != null) {
					view.StylesheetFileName = stylesheetFileName;
				}
			}
		}
		
		protected override void Update(CommandInfo info)
		{
			info.Enabled = XmlEditorService.IsXmlEditorViewContentActive && !XmlEditorService.IsXslOutputViewContentActive;
		}
	}
}
