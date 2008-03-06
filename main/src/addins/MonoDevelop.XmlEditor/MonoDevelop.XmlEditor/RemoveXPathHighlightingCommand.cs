//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Components.Commands;
using System;

namespace MonoDevelop.XmlEditor
{
	public class RemoveXPathHighlightingCommand : CommandHandler
	{
		protected override void Run()
		{
			// Find active Xml View.
			XmlEditorViewContent view = XmlEditorService.GetActiveView();
			if (view != null) {
				view.XmlEditorView.RemoveXPathMarkers();
			}
		}
		
		protected override void Update(CommandInfo info)
		{
			info.Enabled = XmlEditorService.IsXmlEditorViewContentActive;
		}
	}
}
