//  WindowCommands.cs
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
using System.Collections;
using System.CodeDom.Compiler;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.Ide.Commands
{
	public enum WindowCommands
	{
		NextWindow,
		PrevWindow,
		OpenWindowList,
		SplitWindowVertically,
		SplitWindowHorizontally,
		UnsplitWindow,
		SwitchSplitWindow
	}
	
	internal class NextWindowHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				return;
			}
			int index = IdeApp.Workbench.Documents.IndexOf (IdeApp.Workbench.ActiveDocument);
			IdeApp.Workbench.Documents [(index + 1) % IdeApp.Workbench.Documents.Count].Select ();
		}
	}
	
	internal class PrevWindowHandler: CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				return;
			}
			int index = IdeApp.Workbench.Documents.IndexOf (IdeApp.Workbench.ActiveDocument);
			IdeApp.Workbench.Documents [(index + IdeApp.Workbench.Documents.Count - 1) % IdeApp.Workbench.Documents.Count].Select ();
		}
	}
	
	internal class OpenWindowListHandler: CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			int contentCount = IdeApp.Workbench.Documents.Count;
			if (contentCount == 0) return;
			
			for (int i = 0; i < contentCount; ++i) {
				Document doc = IdeApp.Workbench.Documents [i];
				
				string escapedWindowTitle = doc.Window.Title.Replace("_", "__");
				CommandInfo item = null;
				if (doc.Window.ShowNotification) {
					item = new CommandInfo ("<span foreground=\"blue\">" + escapedWindowTitle + "</span>");
					item.UseMarkup = true;
				} else {
					item = new CommandInfo (escapedWindowTitle);
				}
				
				item.Checked = (IdeApp.Workbench.ActiveDocument == doc);
				item.Description = GettextCatalog.GetString ("Activate window '{0}'", escapedWindowTitle);
				
				string prefix = PropertyService.IsMac? "Meta|" : "Alt|";
				if (i + 1 <= 9)
					item.AccelKey = prefix + (i+1);
				
				info.Add (item, doc);
			}
		}
		
		protected override void Run (object doc)
		{
			((Document)doc).Select ();
		}
	}
	
	internal class SplitWindowVertically : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				info.Enabled = false;
				return;
			}
			ISplittable split = IdeApp.Workbench.ActiveDocument.GetContent <ISplittable> ();
			info.Enabled = split != null && split.EnableSplitHorizontally;
		}
		protected override void Run (object doc)
		{
			ISplittable split = IdeApp.Workbench.ActiveDocument.GetContent <ISplittable> ();
			Debug.Assert (split != null);
			split.SplitHorizontally ();
		}
	}
	
	internal class SplitWindowHorizontally : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				info.Enabled = false;
				return;
			}
			ISplittable split = IdeApp.Workbench.ActiveDocument.GetContent <ISplittable> ();
			info.Enabled = split != null && split.EnableSplitVertically;
		}
		protected override void Run (object doc)
		{
			ISplittable split = IdeApp.Workbench.ActiveDocument.GetContent <ISplittable> ();
			Debug.Assert (split != null);
			split.SplitVertically ();
		}
	}
	
	internal class UnsplitWindow : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				info.Enabled = false;
				return;
			}
			ISplittable split = IdeApp.Workbench.ActiveDocument.GetContent <ISplittable> ();
			info.Enabled = split != null && split.EnableUnsplit;
		}
		protected override void Run (object doc)
		{
			ISplittable split = IdeApp.Workbench.ActiveDocument.GetContent <ISplittable> ();
			Debug.Assert (split != null);
			split.Unsplit ();
		}
	}
	
	internal class SwitchSplitWindow : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				info.Enabled = false;
				return;
			}
			ISplittable split = IdeApp.Workbench.ActiveDocument.GetContent <ISplittable> ();
			info.Enabled = split != null && split.EnableUnsplit;
		}
		protected override void Run (object doc)
		{
			ISplittable split = IdeApp.Workbench.ActiveDocument.GetContent <ISplittable> ();
			Debug.Assert (split != null);
			split.SwitchWindow ();
		}
	}
}
