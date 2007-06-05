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
using MonoDevelop.Ide.Projects;
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
		ReplaceInFiles,
		GotoType
	}

	internal class FindInFilesHandler : CommandHandler
	{
		protected override void Run ()
		{
			SearchReplaceInFilesManager.ShowFindDialog ();
		}
	}
	
	internal class ReplaceInFilesHandler : CommandHandler
	{
		protected override void Run()
		{
			SearchReplaceInFilesManager.ShowReplaceDialog ();
		}
	}
	
	internal class GotoTypeHandler : CommandHandler
	{
	    protected override void Run()
		{
			OpenFileInSolutionDialog.Show (false);
	    }
	    
	    protected override void Update(CommandInfo info)
	    {
	    	info.Enabled = (ProjectService.Solution != null);
	    }
	}
}
