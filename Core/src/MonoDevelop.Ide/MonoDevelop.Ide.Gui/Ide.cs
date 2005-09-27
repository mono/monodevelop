
using System;
using System.Collections;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui.ErrorHandlers;
using MonoDevelop.Ide.Gui.Dialogs;

namespace MonoDevelop.Ide.Gui
{
	public abstract class IdeApp
	{
		static Workbench workbench = new Workbench ();
		static ProjectOperations projectOperations = new ProjectOperations ();
		static HelpOperations helpOperations = new HelpOperations ();
		static CommandService commandService = new CommandService ();
		
		IdeApp ()
		{
		}
		
		public static Workbench Workbench {
			get { return workbench; }
		}
		
		public static ProjectOperations ProjectOperations {
			get { return projectOperations; }
		}
		
		public static HelpOperations HelpOperations {
			get { return helpOperations; }
		}
		
		public static CommandService CommandService {
			get { return commandService; }
		}
		
		public static void Run ()
		{
			commandService.LoadCommands ("/SharpDevelop/Commands");

			workbench.Initialize ();
			
			// register string tag provider (TODO: move to add-in tree :)
			Runtime.StringParserService.RegisterStringTagProvider(new MonoDevelop.Ide.Commands.SharpDevelopStringTagProvider());
			
			// load previous combine
			if ((bool)Runtime.Properties.GetProperty("SharpDevelop.LoadPrevProjectOnStartup", false)) {
				RecentOpen recentOpen = Workbench.RecentOpen;

				if (recentOpen.RecentProject != null && recentOpen.RecentProject.Length > 0) { 
					IdeApp.ProjectOperations.OpenCombine(recentOpen.RecentProject[0].ToString());
				}
			}
			
			foreach (string file in StartupInfo.GetRequestedFileList()) {
				//FIXME: use mimetypes
				if (Services.ProjectService.IsCombineEntryFile (file)) {
					try {
						IdeApp.ProjectOperations.OpenCombine (file);
					} catch (Exception e) {
						Services.MessageService.ShowError (e, "Could not load solution: " + file);
					}
				} else {
					try {
						IdeApp.Workbench.OpenDocument (file);
					
					} catch (Exception e) {
						Runtime.LoggingService.InfoFormat("unable to open file {0} exception was :\n{1}", file, e.ToString());
					}
				}
			}

			workbench.Show ("SharpDevelop.Workbench.WorkbenchMemento");
			
			Services.MessageService.RootWindow = workbench.RootWindow;
		
			commandService.EnableUpdate ();
			
			// finally run the workbench window ...
			Gtk.Application.Run ();
		}
		
		public static void Exit ()
		{
			workbench.Close ();
			Gtk.Application.Quit ();
		}
	}
}
