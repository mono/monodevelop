// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Ide.Commands
{
	public enum SearchCommands
	{
		Find,
		FindNext,
		FindPrevious,
		Replace,
		FindInFiles,
		FindNextSelection,
		FindPreviousSelection,
		FindBox,
		ReplaceInFiles
	}

	internal class FindInFilesHandler : CommandHandler
	{
		public static void SetSearchPattern ()
		{
			if (IdeApp.Workbench.ActiveDocument != null) {
				IWorkbenchWindow window = IdeApp.Workbench.ActiveDocument.Window;
				if (window != null && window.ViewContent is ITextBuffer)
				{
					string selectedText = ((ITextBuffer)window.ViewContent).SelectedText;
					if (selectedText != null && selectedText.Length > 0)
						SearchReplaceInFilesManager.SearchOptions.SearchPattern = selectedText.Split ('\n')[0];
				}
			}
		}

		protected override void Run ()
		{
			SetSearchPattern ();
			if (SearchReplaceInFilesManager.ReplaceDialog != null) {
				if (SearchReplaceInFilesManager.ReplaceDialog.replaceMode == false) {
					SearchReplaceInFilesManager.ReplaceDialog.SetSearchPattern(SearchReplaceInFilesManager.SearchOptions.SearchPattern);
					SearchReplaceInFilesManager.ReplaceDialog.Present ();
				} else {
					SearchReplaceInFilesManager.ReplaceDialog.Destroy ();
					ReplaceInFilesDialog rd = new ReplaceInFilesDialog (false);
					rd.ShowAll ();
				}
			} else {
				ReplaceInFilesDialog rd = new ReplaceInFilesDialog(false);
				rd.ShowAll();
			}
		}
	}
	
	internal class ReplaceInFilesHandler : CommandHandler
	{
		protected override void Run()
		{
			FindInFilesHandler.SetSearchPattern ();
			
			if (SearchReplaceInFilesManager.ReplaceDialog != null) {
				if (SearchReplaceInFilesManager.ReplaceDialog.replaceMode == true) {
					SearchReplaceInFilesManager.ReplaceDialog.SetSearchPattern(SearchReplaceInFilesManager.SearchOptions.SearchPattern);
					SearchReplaceInFilesManager.ReplaceDialog.Present ();
				} else {
					SearchReplaceInFilesManager.ReplaceDialog.Destroy ();
					ReplaceInFilesDialog rd = new ReplaceInFilesDialog (true);
					rd.ShowAll ();
				}
			} else {
				ReplaceInFilesDialog rd = new ReplaceInFilesDialog (true);
				rd.ShowAll ();
			}
		}
	}
	
}
