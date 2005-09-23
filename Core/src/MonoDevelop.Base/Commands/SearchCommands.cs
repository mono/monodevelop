// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;

using MonoDevelop.Gui;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Gui.Search;

namespace MonoDevelop.Commands
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
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			if (window != null && window.ViewContent is ITextBuffer)
			{
				string selectedText = ((ITextBuffer)window.ViewContent).SelectedText;
				if (selectedText != null && selectedText.Length > 0)
					SearchReplaceInFilesManager.SearchOptions.SearchPattern = selectedText.Split ('\n')[0];
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
