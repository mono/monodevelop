//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
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
				pad = IdeApp.Workbench.ShowPad(new XPathQueryPad());
			} else {
				pad.BringToFront();
			}
		}
		
		protected override void Update(CommandInfo info)
		{
			info.Enabled = XmlEditorService.IsXmlEditorViewContentActive;
		}
	}
}
