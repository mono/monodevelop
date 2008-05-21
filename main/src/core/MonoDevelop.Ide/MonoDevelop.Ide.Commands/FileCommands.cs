//  FileCommands.cs
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
using System.IO;
using System.Collections;
using System.Diagnostics;

using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects.Gui.Dialogs;

namespace MonoDevelop.Ide.Commands
{
	public enum FileCommands
	{
		OpenFile,
		NewFile,
		NewProject,
		CloseFile,
		CloseAllFiles,
		CloseWorkspace,
		CloseWorkspaceItem,
		ReloadFile,
		Save,
		SaveAll,
		SaveAs,
		RecentFileList,
		ClearRecentFiles,
		RecentProjectList,
		ClearRecentProjects,
		Exit,
		ClearCombine,
		OpenInTerminal,
		OpenFolder,
		OpenContainingFolder,
		PrintDocument,
		PrintPreviewDocument
	}
	
	internal class NewProjectHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.NewSolution ();
		}
	}
	
	internal class NewFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			using (NewFileDialog fd = new NewFileDialog (null, null)) {
				fd.Run ();
			}
		}
	}
	
	internal class CloseAllFilesHandler : CommandHandler
	{
		protected override void Run()
		{
			IdeApp.Workbench.CloseAllDocuments (false);
		}
	}
	
	internal class SaveAllHandler : CommandHandler
	{
		protected override void Run()
		{
			IdeApp.Workbench.SaveAll ();
		}
		
		protected override void Update (CommandInfo info)
		{
			bool enabled = false;
			foreach (Document doc in IdeApp.Workbench.Documents)
			{
				if (doc.IsDirty)
				{
					enabled = true;
					break;
				}
			}
			info.Enabled = enabled;
		}
	}	
	

	internal class OpenFileHandler : CommandHandler
	{
		protected override void Run()
		{
			FileSelectorDialog fs = new FileSelectorDialog (GettextCatalog.GetString ("File to Open"));
			try {
				
				int response = fs.Run ();
				string name = fs.Filename;
				fs.Hide ();
				if (response == (int)Gtk.ResponseType.Ok) {
					if (name == null) {
						if (fs.Uri != null)
							MessageService.ShowError (GettextCatalog.GetString ("Only local files can be opened."));
						else
							MessageService.ShowError (GettextCatalog.GetString ("The provided file could not be loaded."));
						return;
					}
					ProjectService ps = MonoDevelop.Projects.Services.ProjectService;
					if ((ps.IsWorkspaceItemFile (name) || ps.IsSolutionItemFile (name)) && fs.SelectedViewer == null)
						IdeApp.Workspace.OpenWorkspaceItem (name, fs.CloseCurrentWorkspace);
					else if (fs.SelectedViewer != null)
						fs.SelectedViewer.OpenFile (name, fs.Encoding);
					else
						IdeApp.Workbench.OpenDocument (name, fs.Encoding);
				}	
			}
			finally {
				fs.Destroy ();
			}
		}
	}
	
	internal class CloseWorkspaceHandler : CommandHandler
	{
		protected override void Run()
		{
			IdeApp.Workspace.Close();
		}
		
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workspace.Items.Count == 0)
				info.Enabled = false;
			else if (IdeApp.Workspace.Items.Count == 1 && IdeApp.Workspace.Items [0] is Solution)
				info.Text = GettextCatalog.GetString ("C_lose Solution");
		}
	}
		
	internal class ExitHandler : CommandHandler
	{
		protected override void Run()
		{
			IdeApp.Exit ();
		}
	}
	
	internal class PrintHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				info.Enabled = false;
				return;
			}
			IPrintable printable = IdeApp.Workbench.ActiveDocument.GetContent <IPrintable> ();
			info.Enabled = printable != null;
		}
		protected override void Run (object doc)
		{
			IPrintable printable = IdeApp.Workbench.ActiveDocument.GetContent <IPrintable> ();
			Debug.Assert (printable != null);
			printable.PrintDocument ();
		}
	}
	
	internal class PrintPreviewHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument == null) {
				info.Enabled = false;
				return;
			}
			IPrintable printable = IdeApp.Workbench.ActiveDocument.GetContent <IPrintable> ();
			info.Enabled = printable != null;
		}
		protected override void Run (object doc)
		{
			IPrintable printable = IdeApp.Workbench.ActiveDocument.GetContent <IPrintable> ();
			Debug.Assert (printable != null);
			printable.PrintPreviewDocument ();
		}
	}
	
	internal class ClearRecentFilesHandler : CommandHandler
	{
		protected override void Run()
		{			
			try {
				if (IdeApp.Workbench.RecentOpen.RecentFile != null && IdeApp.Workbench.RecentOpen.RecentFile.Length > 0 && MessageService.Confirm (GettextCatalog.GetString ("Clear recent files"), GettextCatalog.GetString ("Are you sure you want to clear recent files list?"), AlertButton.Clear)) {
					IdeApp.Workbench.RecentOpen.ClearRecentFiles();
				}
			} catch {}
		}
	
		protected override void Update (CommandInfo info)
		{
			RecentOpen recentOpen = IdeApp.Workbench.RecentOpen;
			info.Enabled = (recentOpen.RecentFile != null && recentOpen.RecentFile.Length > 0);
		}
	}
	
	internal class ClearRecentProjectsHandler : CommandHandler
	{
		protected override void Run()
		{			
			try {
				if (IdeApp.Workbench.RecentOpen.RecentProject != null && IdeApp.Workbench.RecentOpen.RecentProject.Length > 0 && MessageService.Confirm (GettextCatalog.GetString ("Clear recent projects"), GettextCatalog.GetString ("Are you sure you want to clear recent projects list?"), AlertButton.Clear))
				{
					IdeApp.Workbench.RecentOpen.ClearRecentProjects();
				}
			} catch {}
		}
	
		protected override void Update (CommandInfo info)
		{
			RecentOpen recentOpen = IdeApp.Workbench.RecentOpen;
			info.Enabled = (recentOpen.RecentProject != null && recentOpen.RecentProject.Length > 0);
		}
	}

	internal class RecentFileListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			RecentOpen recentOpen = IdeApp.Workbench.RecentOpen;
			if (recentOpen.RecentFile != null && recentOpen.RecentFile.Length > 0) {
				for (int i = 0; i < recentOpen.RecentFile.Length; ++i) {
					string accelaratorKeyPrefix = i < 10 ? "_" + ((i + 1) % 10).ToString() + " " : "";
					RecentItem ri = recentOpen.RecentFile[i];
					string label = ((ri.Private == null || ri.Private.Length < 1) ? Path.GetFileName (ri.ToString ()) : ri.Private);
					CommandInfo cmd = new CommandInfo (accelaratorKeyPrefix + label.Replace ("_", "__"));
					cmd.Description = GettextCatalog.GetString ("Open {0}", ri.ToString ());
					info.Add (cmd, ri);
				}
			}
		}
		
		protected override void Run (object dataItem)
		{
			IdeApp.Workbench.OpenDocument (dataItem.ToString());
		}
	}

	internal class RecentProjectListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			RecentOpen recentOpen = IdeApp.Workbench.RecentOpen;
			if (recentOpen.RecentProject != null && recentOpen.RecentProject.Length > 0) {
				for (int i = 0; i < recentOpen.RecentProject.Length; ++i) {
					string accelaratorKeyPrefix = i < 10 ? "_" + ((i + 1) % 10).ToString() + " " : "";
					RecentItem ri = recentOpen.RecentProject[i];
					string label = ((ri.Private == null || ri.Private.Length < 1) ? Path.GetFileNameWithoutExtension (ri.ToString ()) : ri.Private);
					CommandInfo cmd = new CommandInfo (accelaratorKeyPrefix + label.Replace ("_", "__"));
					string str = GettextCatalog.GetString ("Load solution {0}", ri.ToString ());
					if (IdeApp.Workspace.IsOpen)
						str += " - " + GettextCatalog.GetString ("Hold Control to open in current worspace.");
					cmd.Description = str;
					if (IdeApp.Services.ProjectService.FileFormats.GetFileFormats (ri.LocalPath, typeof(Solution)).Length > 0)
						cmd.Icon = "md-solution";
					else
						cmd.Icon = "md-workspace";
					info.Add (cmd, ri);
				}
			}
		}
		
		protected override void Run (object dataItem)
		{
			string filename = dataItem.ToString();
			Gdk.ModifierType mtype;
			bool inWorkspace = Gtk.Global.GetCurrentEventState (out mtype) && (mtype & Gdk.ModifierType.ControlMask) != 0;
			IdeApp.Workspace.OpenWorkspaceItem (filename, !inWorkspace);
		}
	}
}
