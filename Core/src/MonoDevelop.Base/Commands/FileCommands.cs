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
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.Services;
using MonoDevelop.Services;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Widgets;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.Gui.ErrorHandlers;
using Freedesktop.RecentFiles;

namespace MonoDevelop.Commands
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
			NewProjectDialog pd = new NewProjectDialog (true);
			pd.Run ();
		}
	}
	
	internal class NewFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			NewFileDialog fd = new NewFileDialog ();
			fd.Run ();
		}
	}
	
	internal class CloseAllFilesHandler : CommandHandler
	{
		protected override void Run()
		{
			if ( WorkbenchSingleton.Workbench != null ) {
				WorkbenchSingleton.Workbench.CloseAllViews();
			}
		}
	}
	
	internal class SaveAllHandler : CommandHandler
	{
		protected override void Run()
		{
			foreach (IViewContent content in WorkbenchSingleton.Workbench.ViewContentCollection) {
				if (content.IsViewOnly) {
					continue;
				}
				
				if (content.ContentName == null)
				{
					using (FileSelector fdiag = new FileSelector (GettextCatalog.GetString ("Save File As...")))
					{
						fdiag.SetFilename (System.Environment.GetEnvironmentVariable ("HOME"));
						if (fdiag.Run () == (int) Gtk.ResponseType.Ok)
						{
							string fileName = fdiag.Filename;

							// currently useless
							if (Path.GetExtension(fileName).StartsWith("?") || Path.GetExtension(fileName) == "*")
							{
								fileName = Path.ChangeExtension(fileName, "");
							}

							if (Runtime.FileUtilityService.ObservedSave (new NamedFileOperationDelegate(content.Save), fileName) == FileOperationResult.OK)
							{
								Runtime.MessageService.ShowMessage(fileName, GettextCatalog.GetString ("File saved"));
							}
						}
					
						fdiag.Hide ();
					}
				}
				else
				{
					Runtime.FileUtilityService.ObservedSave (new FileOperationDelegate(content.Save), content.ContentName);
				}
			}
		}
	}	
	

	internal class OpenFileHandler : CommandHandler
	{
		protected override void Run()
		{
			//string[] fileFilters  = (string[])(AddInTreeSingleton.AddInTree.GetTreeNode("/SharpDevelop/Workbench/FileFilter").BuildChildItems(this)).ToArray(typeof(string));
			//bool foundFilter      = false;
			// search filter like in the current selected project
			/*
			IProjectService projectService = (IProjectService)MonoDevelop.Core.Services.ServiceManager.GetService(typeof(IProjectService));
			
			if (projectService.CurrentSelectedProject != null) {
				LanguageBindingService languageBindingService = (LanguageBindingService)MonoDevelop.Core.Services.ServiceManager.GetService(typeof(LanguageBindingService));
				
				LanguageBindingCodon languageCodon = languageBindingService.GetCodonPerLanguageName(projectService.CurrentSelectedProject.ProjectType);
				for (int i = 0; !foundFilter && i < fileFilters.Length; ++i) {
					for (int j = 0; !foundFilter && j < languageCodon.Supportedextensions.Length; ++j) {
						if (fileFilters[i].IndexOf(languageCodon.Supportedextensions[j]) >= 0) {
							break;
						}
					}
				}
			}
			
			// search filter like in the current open file
			if (!foundFilter) {
				IWorkbenchWindow window = WorkbenchSingleton.Workbench.ActiveWorkbenchWindow;
				if (window != null) {
					for (int i = 0; i < fileFilters.Length; ++i) {
						if (fileFilters[i].IndexOf(Path.GetExtension(window.ViewContent.ContentName == null ? window.ViewContent.UntitledName : window.ViewContent.ContentName)) >= 0) {
							break;
						}
					}
				}
			}*/
			using (FileSelector fs = new FileSelector (GettextCatalog.GetString ("File to Open"))) {
				int response = fs.Run ();
				string name = fs.Filename;
				fs.Hide ();
				if (response == (int)Gtk.ResponseType.Ok) {
					if (Runtime.ProjectService.IsCombineEntryFile (name))
						Runtime.ProjectService.OpenCombine (name);
					else
						Runtime.FileService.OpenFile(name);
				}	
			}
		}
	}
	
	internal class CloseCombineHandler : CommandHandler
	{
		protected override void Run()
		{
			Runtime.ProjectService.CloseCombine();
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = (Runtime.ProjectService.CurrentOpenCombine != null);
		}
	}
		
	internal class ExitHandler : CommandHandler
	{
		protected override void Run()
		{			
			if (((DefaultWorkbench)WorkbenchSingleton.Workbench).Close()) {
				Gtk.Application.Quit();
			}
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
				if (Runtime.FileService.RecentOpen.RecentFile != null && Runtime.FileService.RecentOpen.RecentFile.Length > 0 && Runtime.MessageService.AskQuestion(GettextCatalog.GetString ("Are you sure you want to clear recent files list?"), GettextCatalog.GetString ("Clear recent files")))
				{
					Runtime.FileService.RecentOpen.ClearRecentFiles();
				}
			} catch {}
		}
	
		protected override void Update (CommandInfo info)
		{
			RecentOpen recentOpen = Runtime.FileService.RecentOpen;
			info.Enabled = (recentOpen.RecentFile != null && recentOpen.RecentFile.Length > 0);
		}
	}
	
	internal class ClearRecentProjectsHandler : CommandHandler
	{
		protected override void Run()
		{			
			try {
				if (Runtime.FileService.RecentOpen.RecentProject != null && Runtime.FileService.RecentOpen.RecentProject.Length > 0 && Runtime.MessageService.AskQuestion(GettextCatalog.GetString ("Are you sure you want to clear recent projects list?"), GettextCatalog.GetString ("Clear recent projects")))
				{
					Runtime.FileService.RecentOpen.ClearRecentProjects();
				}
			} catch {}
		}
	
		protected override void Update (CommandInfo info)
		{
			RecentOpen recentOpen = Runtime.FileService.RecentOpen;
			info.Enabled = (recentOpen.RecentProject != null && recentOpen.RecentProject.Length > 0);
		}
	}

	internal class RecentFileListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			RecentOpen recentOpen = Runtime.FileService.RecentOpen;
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
			Runtime.FileService.OpenFile (dataItem.ToString());
		}
	}

	internal class RecentProjectListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			RecentOpen recentOpen = Runtime.FileService.RecentOpen;
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
				Runtime.ProjectService.OpenCombine(filename);
			} catch (Exception ex) {
				CombineLoadError.HandleError(ex, filename);
			}
		}
	}
}
