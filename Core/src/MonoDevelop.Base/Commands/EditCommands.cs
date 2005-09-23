
using System;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Services;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core.AddIns;

namespace MonoDevelop.Commands
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
			new TreeViewOptions ((Gtk.Window)WorkbenchSingleton.Workbench,
				(IProperties)Runtime.Properties.GetProperty("MonoDevelop.TextEditor.Document.Document.DefaultDocumentAggregatorProperties", new DefaultProperties()),
				AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Dialogs/OptionsDialog"));
		}
	}
}
