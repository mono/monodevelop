// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Collections;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.Properties;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui.ErrorHandlers;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;
using Freedesktop.RecentFiles;
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
		CloseCombine,
		ReloadFile,
		Save,
		SaveAll,
		SaveAs,
		RecentFileList,
		ClearRecentFiles,
		RecentProjectList,
		ClearRecentProjects,
		Exit,
		ClearCombine
	}
	
	internal class NewProjectHandler : CommandHandler
	{
		protected override void Run ()
		{
			NewProjectDialog pd = new NewProjectDialog (true, true, null);
			pd.Run ();
		}
	}
	
	internal class NewFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			NewFileDialog fd = new NewFileDialog (null, null);
			fd.Run ();
		}
	}
	
	internal class CloseAllFilesHandler : CommandHandler
	{
		protected override void Run()
		{
			IdeApp.Workbench.CloseAllDocuments ();
		}
	}
	
	internal class SaveAllHandler : CommandHandler
	{
		protected override void Run()
		{
			IdeApp.Workbench.SaveAll ();
		}
	}	
	

	internal class OpenFileHandler : CommandHandler
	{
		protected override void Run()
		{
			ArrayList filters = new ArrayList ();
			filters.AddRange (AddInTreeSingleton.AddInTree.GetTreeNode ("/SharpDevelop/Workbench/Combine/FileFilter").BuildChildItems (this));
			try
			{
				filters.AddRange (AddInTreeSingleton.AddInTree.GetTreeNode ("/SharpDevelop/Workbench/FileFilter").BuildChildItems (this));
			}
			catch
			{
				//nothing there..	
			}
			
			using (FileSelector fs = new FileSelector (GettextCatalog.GetString ("File to Open"))) {
				
				foreach (string filterStr in filters)
				{
					string[] parts = filterStr.Split ('|');
					Gtk.FileFilter filter = new Gtk.FileFilter ();
					filter.Name = parts[0];
					foreach (string ext in parts[1].Split (';'))
					{
						filter.AddPattern (ext);
					}
					fs.AddFilter (filter);
				}
				
				//Add All Files
				Gtk.FileFilter allFilter = new Gtk.FileFilter ();
                allFilter.Name = GettextCatalog.GetString ("All Files");
                allFilter.AddPattern ("*");
                fs.AddFilter (allFilter);

				int response = fs.Run ();
				string name = fs.Filename;
				fs.Hide ();
				if (response == (int)Gtk.ResponseType.Ok) {
					IProjectService ps = MonoDevelop.Projects.Services.ProjectService;
					if (ps.IsCombineEntryFile (name))
						IdeApp.ProjectOperations.OpenCombine (name);
					else
						IdeApp.Workbench.OpenDocument (name);
				}	
			}
		}
	}
	
	internal class CloseCombineHandler : CommandHandler
	{
		protected override void Run()
		{
			IdeApp.ProjectOperations.CloseCombine();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = (IdeApp.ProjectOperations.CurrentOpenCombine != null);
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
		protected override void Run()
		{/*
			IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
			
			if (window != null) {
				if (window.ViewContent is IPrintable) {
					PrintDocument pdoc = ((IPrintable)window.ViewContent).PrintDocument;
					if (pdoc != null) {
						using (PrintDialog ppd = new PrintDialog()) {
							ppd.Document  = pdoc;
							ppd.AllowSomePages = true;
							if (ppd.ShowDialog() == DialogResult.OK) { // fixed by Roger Rubin
								pdoc.Print();
							}
						}
					} else {
						IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
						messageService.ShowError("Couldn't create PrintDocument");
					}
				} else {
					IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
					messageService.ShowError("Can't print this window content");
				}
			}*/
		}
	}
	
	internal class PrintPreviewHandler : CommandHandler
	{
		protected override void Run()
		{
		/*	try {
				IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
				
				if (window != null) {
					if (window.ViewContent is IPrintable) {
						using (PrintDocument pdoc = ((IPrintable)window.ViewContent).PrintDocument) {
							if (pdoc != null) {
								PrintPreviewDialog ppd = new PrintPreviewDialog();
								ppd.Owner     = (Form)WorkbenchSingleton.Workbench;
								ppd.TopMost   = true;
								ppd.Document  = pdoc;
								ppd.Show();
							} else {
								IMessageService messageService =(IMessageService)ServiceManager.Services.GetService(typeof(IMessageService));
								messageService.ShowError("Couldn't create PrintDocument");
							}
						}
					}
				}
			} catch (System.Drawing.Printing.InvalidPrinterException) {
			}*/
		}
	}
	
	internal class ClearRecentFilesHandler : CommandHandler
	{
		protected override void Run()
		{			
			try {
				if (IdeApp.Workbench.RecentOpen.RecentFile != null && IdeApp.Workbench.RecentOpen.RecentFile.Length > 0 && Services.MessageService.AskQuestion(GettextCatalog.GetString ("Are you sure you want to clear recent files list?"), GettextCatalog.GetString ("Clear recent files")))
				{
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
				if (IdeApp.Workbench.RecentOpen.RecentProject != null && IdeApp.Workbench.RecentOpen.RecentProject.Length > 0 && Services.MessageService.AskQuestion(GettextCatalog.GetString ("Are you sure you want to clear recent projects list?"), GettextCatalog.GetString ("Clear recent projects")))
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
					cmd.Description = String.Format (GettextCatalog.GetString ("load solution {0}"), ri.ToString ());
					info.Add (cmd, ri);
				}
			}
		}
		
		protected override void Run (object dataItem)
		{
			//FIXME:THIS IS BROKEN!!
			
			string filename = dataItem.ToString();
			
			try {
				IdeApp.ProjectOperations.OpenCombine(filename);
			} catch (Exception ex) {
				Services.MessageService.ShowError (ex, "Could not load project or solution: " + filename);
			}
		}
	}
}
