// NewFileCommands.cs
//
// Author:
//   Carlo Kok (ck@remobjects.com)
//
// Copyright (c) 2009 RemObjects Software
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//


using System;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Content;
using System.IO;
using Gtk;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Ide.Desktop;
using System.Linq;

namespace MonoDevelop.Ide.Commands
{
	/// <summary>
	/// Copied from MonoDevelop.Ide.addin.xml
	/// </summary>
	public enum FileCommands
	{
		OpenFile,
		NewFile,
		Save,
		SaveAll,
		NewProject,
		NewWorkspace,
		CloseFile,
		CloseAllFiles,
		CloseWorkspace,
		CloseWorkspaceItem,
		ReloadFile,
		SaveAs,
		PrintDocument,
		PrintPreviewDocument,
		PrintPageSetup,
		RecentFileList,
		ClearRecentFiles,
		RecentProjectList,
		ClearRecentProjects,
		Exit,
		OpenInTerminal,
		OpenFolder,
		OpenContainingFolder,
		SetBuildAction,
		ShowProperties,
		CopyToOutputDirectory
	}

	// MonoDevelop.Ide.Commands.FileCommands.OpenFile
	public class OpenFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			var dlg = new OpenFileDialog (GettextCatalog.GetString ("File to Open"), Gtk.FileChooserAction.Open) {
				TransientFor = IdeApp.Workbench.RootWindow,
				ShowEncodingSelector = true,
				ShowViewerSelector = true,
			};
			if (!dlg.Run ())
				return;
			
			var file = dlg.SelectedFile;
			
			if (dlg.SelectedViewer != null) {
				dlg.SelectedViewer.OpenFile (file, dlg.Encoding);
				return;
			}
			
			if (Services.ProjectService.IsWorkspaceItemFile (file) || Services.ProjectService.IsSolutionItemFile (file)) {
				IdeApp.Workspace.OpenWorkspaceItem (file, dlg.CloseCurrentWorkspace);
			}
			else
				IdeApp.Workbench.OpenDocument (file, dlg.Encoding);
		}
		
	}
	// MonoDevelop.Ide.Commands.FileCommands.NewFile
	public class NewFileHandler : CommandHandler
	{
		protected override void Run ()
		{
			var dlg = new NewFileDialog (null, null); // new file seems to fail if I pass the project IdeApp.ProjectOperations.CurrentSelectedProject
			MessageService.ShowCustomDialog (dlg, IdeApp.Workbench.RootWindow);
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.SaveAll
	public class SaveAllHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.SaveAll ();
		}

		protected override void Update (CommandInfo info)
		{
			bool hasdirty = false;
			for(int i = 0; i < IdeApp.Workbench.Documents.Count; i++) {
				hasdirty |= IdeApp.Workbench.Documents [i].IsDirty;
			}
			info.Enabled = hasdirty;
		}
	}
	//MonoDevelop.Ide.Commands.FileCommands.NewProject
	public class NewProjectHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.NewSolution ();
		}
	}
	
	//MonoDevelop.Ide.Commands.FileCommands.NewWorkspace
	public class NewWorkspaceHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.ProjectOperations.NewSolution ("MonoDevelop.Workspace");
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.CloseAllFiles
	public class CloseAllFilesHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.CloseAllDocuments (false);
		}

		protected override void Update (CommandInfo info)
		{
			// No point in closing all when there are no documents open
			info.Enabled = IdeApp.Workbench.Documents.Count != 0;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.CloseFile
	public class CloseFileHandler: CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ActiveDocument.Close ();
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workbench.ActiveDocument != null;
		}
	}

	// MonoDevelop.Ide.Commands.FileCommands.CloseWorkspace
	// MonoDevelop.Ide.Commands.FileCommands.CloseWorkspaceItem
	public class CloseWorkspaceHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workspace.Close ();
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = IdeApp.Workspace != null && IdeApp.Workspace.Items.Count > 0;

			if (info.Enabled && IdeApp.Workspace.Items.Count == 1) {
				if (IdeApp.Workspace.Items [0] is Solution)
					info.Text = GettextCatalog.GetString ("C_lose Solution");
				else if (IdeApp.Workspace.Items [0] is Workspace)
					info.Text = GettextCatalog.GetString ("C_lose Workspace");
				else
					info.Text = GettextCatalog.GetString ("C_lose Project");
			} else
				info.Text = GettextCatalog.GetString ("C_lose All Solutions");

		}
	}
	// MonoDevelop.Ide.Commands.FileCommands.PrintDocument
	public class PrintHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ActiveDocument.GetContent<IPrintable> ().PrintDocument (PrintingSettings.Instance);
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = CanPrint ();
		}
		
		internal static bool CanPrint ()
		{
			IPrintable print;
			//HACK: disable printing on Windows while it doesn't work
			return !Platform.IsWindows
				&& IdeApp.Workbench.ActiveDocument != null
				&& (print = IdeApp.Workbench.ActiveDocument.GetContent<IPrintable> ()) != null
				&& print.CanPrint;
		}
	}
	// MonoDevelop.Ide.Commands.FileCommands.PrintPreviewDocument
	public class PrintPreviewHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Workbench.ActiveDocument.GetContent<IPrintable> ().PrintPreviewDocument (PrintingSettings.Instance);
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = PrintHandler.CanPrint ();
		}
	}
	
	//FIXME: on GTK 2.18.x there is an option to integrate print preview with the print dialog, which makes this unnecessary
	// ideally we could only conditionally show it - this is why we handle the PrintingSettings here
	// MonoDevelop.Ide.Commands.FileCommands.PrintPageSetup
	public class PrintPageSetupHandler : CommandHandler
	{
		protected override void Run ()
		{
			var settings = PrintingSettings.Instance;
			settings.PageSetup = Gtk.Print.RunPageSetupDialog (IdeApp.Workbench.RootWindow, settings.PageSetup, 
			                                                   settings.PrintSettings);
		}

		protected override void Update (CommandInfo info)
		{
			info.Enabled = PrintHandler.CanPrint ();
		}
	}
	
	// MonoDevelop.Ide.Commands.FileCommands.RecentFileList
	public class RecentFileListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			var files = DesktopService.RecentFiles.GetFiles ();
			if (files.Count == 0)
				return;
			
			int i = 0;
			foreach (var ri in files) {
				string acceleratorKeyPrefix = i < 10 ? "_" + ((i + 1) % 10).ToString() + " " : "";
				var cmd = new CommandInfo (acceleratorKeyPrefix + ri.DisplayName.Replace ("_", "__")) {
					Description = GettextCatalog.GetString ("Open {0}", ri.FileName)
				};
				Gdk.Pixbuf icon = DesktopService.GetPixbufForFile (ri.FileName, IconSize.Menu);
				if (icon != null)
					cmd.Icon = ImageService.GetStockId (icon, IconSize.Menu);
				info.Add (cmd, ri.FileName);
				i++;
			}
		}
		
		protected override void Run (object dataItem)
		{
			IdeApp.Workbench.OpenDocument ((string)dataItem);
		}
	}
	
	// MonoDevelop.Ide.Commands.FileCommands.ClearRecentFiles
	public class ClearRecentFilesHandler : CommandHandler
	{
		protected override void Run ()
		{
			try {
				string title = GettextCatalog.GetString ("Clear recent files");
				string question = GettextCatalog.GetString ("Are you sure you want to clear recent files list?");
				if (MessageService.GenericAlert (
					MonoDevelop.Ide.Gui.Stock.Question,
					title,
					question,
					AlertButton.No,
					AlertButton.Yes) == AlertButton.Yes) {
					DesktopService.RecentFiles.ClearFiles ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error clearing recent files list", ex);
			}
		}
		
		protected override void Update (CommandInfo info)
		{
			info.Enabled = DesktopService.RecentFiles.GetFiles ().Count > 0;
		}
	}
	
	// MonoDevelop.Ide.Commands.FileCommands.RecentProjectList
	public class RecentProjectListHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo info)
		{
			var projects = DesktopService.RecentFiles.GetProjects ();
			if (projects.Count == 0)
				return;
				
			int i = 0;
			foreach (var ri in projects) {
				//getting the icon requires probing the file, so handle IO errors
				IconId icon;
				try {
					if (!File.Exists (ri.FileName))
						continue;
					icon = IdeApp.Services.ProjectService.FileFormats.GetFileFormats
						(ri.FileName, typeof(Solution)).Length > 0? "md-solution": "md-workspace";
				}
				catch (UnauthorizedAccessException exAccess) {
					LoggingService.LogWarning ("Error building recent solutions list (Permissions)", exAccess);
					continue;					
				}
				catch (IOException ex) {
					LoggingService.LogWarning ("Error building recent solutions list", ex);
					continue;
				}
				
				string acceleratorKeyPrefix = i < 10 ? "_" + ((i + 1) % 10).ToString() + " " : "";
				string str = GettextCatalog.GetString ("Load solution {0}", ri.ToString ());
				if (IdeApp.Workspace.IsOpen)
					str += " - " + GettextCatalog.GetString ("Hold Control to open in current workspace.");
				
				var cmd = new CommandInfo (acceleratorKeyPrefix + ri.DisplayName.Replace ("_", "__")) {
					Icon = icon,
					Description = str,
				};
				
				info.Add (cmd, ri.FileName);
				i++;
			}
		}
		protected override void Run (object dataItem)
		{
			string filename = (string)dataItem;
			Gdk.ModifierType mtype = Mono.TextEditor.GtkWorkarounds.GetCurrentKeyModifiers ();
			bool inWorkspace = (mtype & Gdk.ModifierType.ControlMask) != 0;
			IdeApp.Workspace.OpenWorkspaceItem (filename, !inWorkspace);
		}
	}
	
	// MonoDevelop.Ide.Commands.FileCommands.ClearRecentProjects
	internal class ClearRecentProjectsHandler : CommandHandler
	{
		protected override void Run ()
		{
			try {
				string title = GettextCatalog.GetString ("Clear recent projects");
				string question = GettextCatalog.GetString ("Are you sure you want to clear recent projects list?");
				if (MessageService.GenericAlert (
					MonoDevelop.Ide.Gui.Stock.Question,
					title,
					question,
					AlertButton.No,
					AlertButton.Yes) == AlertButton.Yes) {
					DesktopService.RecentFiles.ClearProjects ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error clearing recent projects list", ex);
			}
		}
	
		protected override void Update (CommandInfo info)
		{
			info.Enabled = DesktopService.RecentFiles.GetProjects ().Count > 0;
		}
	}
	
	// MonoDevelop.Ide.Commands.FileCommands.Exit
	public class ExitHandler : CommandHandler
	{
		protected override void Run ()
		{
			IdeApp.Exit ();
		}
	}
	// MonoDevelop.Ide.Commands.FileTabCommands.CloseAllButThis    Implemented in FileTabCommands.cs
	// MonoDevelop.Ide.Commands.CopyPathNameHandler                Implemented in FileTabCommands.cs
	// MonoDevelop.Ide.Commands.FileTabCommands.ToggleMaximize     Implemented in FileTabCommands.cs
}
