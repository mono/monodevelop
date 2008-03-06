//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using System;
using System.IO;
using System.Text;

namespace MonoDevelop.XmlEditor
{
	public class FormatXmlCommand : CommandHandler
	{	
		public FormatXmlCommand()
		{
		}
		
		protected override void Run()
		{
			XmlEditorViewContent view = XmlEditorService.GetActiveView();
			if (view != null) {	
				FormatXml(view);
			}
		}		
		
		protected override void Update(CommandInfo info)
		{
			info.Enabled = XmlEditorService.IsXmlEditorViewContentActive;
		}
		
		/// <summary>
		/// Pretty prints the xml.
		/// </summary>
		public void FormatXml(XmlEditorViewContent view)
		{
			XmlEditorService.TaskService.ClearExceptCommentTasks();			
			string xml = view.Text;
			if (XmlEditorService.IsWellFormed(xml, view.FileName)) {
				string formattedXml = XmlEditorService.SimpleFormat(XmlEditorService.IndentedFormat(xml));
				view.Text = formattedXml;
			}
		}
	}
}
