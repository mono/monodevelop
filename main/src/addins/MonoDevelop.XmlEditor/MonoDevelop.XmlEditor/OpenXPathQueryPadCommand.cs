//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
//

using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using System;

namespace MonoDevelop.XmlEditor
{
	public class OpenXPathQueryPadCommand : CommandHandler
	{
		static Pad pad;
		
		protected override void Run()
		{
			if (pad == null) {
				GetXPathQueryPad();
			}
			if (pad != null) {
				pad.BringToFront();
			}
		}
		
		protected override void Update(CommandInfo info)
		{
			info.Enabled = XmlEditorService.IsXmlEditorViewContentActive;
		}
		
		void GetXPathQueryPad()
		{
			foreach (Pad currentPad in IdeApp.Workbench.Pads) {
				if (currentPad.Id == "MonoDevelop.XmlEditor.XPathQueryPad") {
					pad = currentPad;
					break;
				}
			}
		}
	}
}
