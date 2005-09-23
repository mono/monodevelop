// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System.Drawing;
using System;

using MonoDevelop.Core.Properties;
using MonoDevelop.DefaultEditor.Gui.Editor;
using MonoDevelop.TextEditor.Document;
using MonoDevelop.TextEditor.Actions;
using MonoDevelop.TextEditor;
using MonoDevelop.TextEditor.Gui.CompletionWindow;

namespace MonoDevelop.DefaultEditor.Actions
{
	public class TemplateCompletion : AbstractEditAction
	{
		public override void Execute(TextArea services)
		{
			CompletionWindow completionWindow = new CompletionWindow(services.MotherTextEditorControl, "a.cs", new TemplateCompletionDataProvider());
			completionWindow.ShowCompletionWindow('\0');
		}
	}
}
