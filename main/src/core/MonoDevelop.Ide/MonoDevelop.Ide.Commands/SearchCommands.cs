//  SearchCommands.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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
		EmacsFindNext,
		EmacsFindPrevious,
		Replace,
		FindInFiles,
		FindNextSelection,
		FindPreviousSelection,
		FindBox,
		ReplaceInFiles,
		
		GotoType,
		GotoFile,
		GotoLineNumber,
		
		ToggleBookmark,
		PrevBookmark,
		NextBookmark,
		ClearBookmarks,
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
			GoToDialog.Run (false);
	    }
	    
	    protected override void Update(CommandInfo info)
	    {
	    	info.Enabled = (IdeApp.ProjectOperations.CurrentOpenCombine != null || IdeApp.Workbench.Documents.Count != 0);
	    }
	}
	
	internal class GotoFileHandler : CommandHandler
	{
	    protected override void Run()
		{
			GoToDialog.Run (true);
	    }
	    
	    protected override void Update(CommandInfo info)
	    {
	    	info.Enabled = (IdeApp.ProjectOperations.CurrentOpenCombine != null || IdeApp.Workbench.Documents.Count != 0);
	    }
	}
}
