//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2006 Matthew Ward
//

using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using System;
namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// Opens the stylesheet associated with the active XML document.
	/// </summary>
	public class OpenStylesheetCommand : CommandHandler
	{
		protected override void Run()
		{
			XmlEditorViewContent view = XmlEditorService.GetActiveView();
			if (view != null) {
				if (view.StylesheetFileName != null) {
					try {
						IdeApp.Workbench.OpenDocument(view.StylesheetFileName);
					} catch (Exception ex) {
						XmlEditorService.MessageService.ShowError(ex);
					}
				}
			}
		}
		
		protected override void Update(CommandInfo info)
		{
			XmlEditorViewContent view = XmlEditorService.GetActiveView();
			if (view != null && view.StylesheetFileName != null) {
				info.Enabled = true;
			} else {
				info.Enabled = false;
			}
		}
	}
}
