
using System;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.Commands
{
	public enum EditCommands
	{
		Copy,
		Cut,
		Paste,
		Delete,
		Rename,
		Undo,
		Redo,
		SelectAll,
		CommentCode,
		UncommentCode,
		IndentSelection,
		UnIndentSelection,
		WordCount,
		MonodevelopPreferences
	}
	
	internal class MonodevelopPreferencesHandler: CommandHandler
	{
		protected override void Run ()
		{
			new TreeViewOptions (IdeApp.Workbench.RootWindow,
				(IProperties)Runtime.Properties.GetProperty("MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties", new DefaultProperties()),
				Runtime.AddInService.GetTreeNode("/SharpDevelop/Dialogs/OptionsDialog"));
		}
	}
}
