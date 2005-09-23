// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Markus Palme" email="MarkusPalme@gmx.de"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;
using System.Text;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.Properties;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.Gui;
using MonoDevelop.TextEditor;

namespace MonoDevelop.DefaultEditor.Commands
{
	public class CommentRegion : AbstractMenuCommand
	{ 
		public override void Run ()
		{
			Console.WriteLine ("Not ported to the new editor yet");
			/*
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			
			if (window == null || !(window.ViewContent is ITextEditorControlProvider)) {
				return;
			}
			
			TextEditorControl textarea = ((ITextEditorControlProvider)window.ViewContent).TextEditorControl;
			new MonoDevelop.TextEditor.Actions.ToggleComment().Execute(textarea.ActiveTextAreaControl.TextArea);
			*/
		}
	}
}
