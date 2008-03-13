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
		public void FormatXml (MonoDevelop.Ide.Gui.Content.IEditableTextBuffer buffer)
		{
			XmlEditorService.TaskService.ClearExceptCommentTasks ();
			
			if (XmlEditorService.IsWellFormed (buffer.Text, buffer.Name)) {
				bool selection = (buffer.SelectionEndPosition == buffer.SelectionStartPosition);
				string xml = selection? buffer.SelectedText : buffer.Text;
				string formattedXml = XmlEditorService.SimpleFormat (XmlEditorService.IndentedFormat (xml));
				if (selection)
					buffer.SelectedText = formattedXml;
				else
					buffer.Text = formattedXml;
			}
		}
	}
}
