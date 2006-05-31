//
// IdeApp.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Collections;

using System.Net;
using System.Net.Sockets;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui.ErrorHandlers;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Dialogs;

using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Gui;
using MonoDevelop.Projects.Documentation;



namespace MonoDevelop.Ide.Gui
{
	public abstract class IdeApp
	{
		static Workbench workbench;
		static ProjectOperations projectOperations;
		static HelpOperations helpOperations;
		static CommandService commandService;
		static IdeServices ideServices;
		
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
		
		public static IdeServices Services {
			get { return ideServices; }
		}
		
		public static void Initialize (IProgressMonitor monitor)
		{
			workbench = new Workbench ();
			projectOperations = new ProjectOperations ();
			helpOperations = new HelpOperations ();
			commandService = new CommandService ();
			ideServices = new IdeServices ();
		
			monitor.BeginTask (GettextCatalog.GetString("Loading Workbench"), 6);
			
			commandService.LoadCommands ("/SharpDevelop/Commands");
			monitor.Step (1);

			workbench.Initialize (monitor);
			monitor.Step (1);
			
			// register string tag provider (TODO: move to add-in tree :)
			Runtime.StringParserService.RegisterStringTagProvider(new MonoDevelop.Ide.Commands.SharpDevelopStringTagProvider());
			
			// load previous combine
			if ((bool)Runtime.Properties.GetProperty("SharpDevelop.LoadPrevProjectOnStartup", false)) {
				RecentOpen recentOpen = Workbench.RecentOpen;

				if (recentOpen.RecentProject != null && recentOpen.RecentProject.Length > 0) { 
					IdeApp.ProjectOperations.OpenCombine(recentOpen.RecentProject[0].ToString());
				}
			}
			monitor.Step (1);
			
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
			monitor.Step (1);

			workbench.Show ("SharpDevelop.Workbench.WorkbenchMemento");
			monitor.Step (1);
			Services.DispatchService.RunPendingEvents ();
			
			Services.MessageService.RootWindow = workbench.RootWindow;
		
			commandService.EnableUpdate ();
			
			foreach (CommandHandler handler in Runtime.AddInService.GetTreeItems ("/MonoDevelop/IDE/StartupHandlers", typeof(CommandHandler))) {
				try {
					typeof(CommandHandler).GetMethod ("Run", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke (handler, null);
				} catch (Exception ex) {
					Runtime.LoggingService.Error (ex);
				}
			}
			monitor.EndTask ();
		}
			
		public static void Run ()
		{
			// finally run the workbench window ...
			Gtk.Application.Run ();
		}
		
		public static void Exit ()
		{
			if (workbench.Close ())
				Gtk.Application.Quit ();
		}
	}
	
	public class IdeServices
	{
		MessageService messageService;
		DisplayBindingService displayBindingService;
		ResourceService resourceService;
		IStatusBarService statusBarService;
		IconService icons;
		IDocumentationService documentationService;
		IDebuggingService debuggingService;
		TaskService taskService;
		IParserService parserService;
		DispatchService dispatchService;
		IProjectService projectService;
		IFileService fileService;
	
		public IFileService FileService {
			get {
				if (fileService == null)
					fileService = (IFileService) ServiceManager.GetService (typeof(IFileService));
				return fileService;
			}
		}

		public IStatusBarService StatusBar {
			get {
				if (statusBarService == null)
					statusBarService = (IStatusBarService) ServiceManager.GetService (typeof(IStatusBarService));
				return statusBarService;
			}
		}

		public MessageService MessageService {
			get {
				if (messageService == null)
					messageService = (MessageService) ServiceManager.GetService (typeof(MessageService));
				return messageService;
			}
		}

		internal DisplayBindingService DisplayBindings {
			get {
				if (displayBindingService == null)
					displayBindingService = (DisplayBindingService) ServiceManager.GetService (typeof(DisplayBindingService));
				return displayBindingService;
			}
		}
	
		public ResourceService Resources {
			get {
				if (resourceService == null)
					resourceService = (ResourceService) ServiceManager.GetService (typeof(ResourceService));
				return resourceService;
			}
		}
	
		public IconService Icons {
			get {
				if (icons == null)
					icons = (IconService) ServiceManager.GetService (typeof(IconService));
				return icons;
			}
		}
	
		public IDocumentationService Documentation {
			get {
				if (documentationService == null)
					documentationService = (IDocumentationService) ServiceManager.GetService (typeof(IDocumentationService));
				return documentationService;
			}
		}
	
		public IDebuggingService DebuggingService {
			get {
				if (debuggingService == null)
					debuggingService = (IDebuggingService) ServiceManager.GetService (typeof(IDebuggingService));
				return debuggingService;
			}
		}
	
		public TaskService TaskService {
			get {
				if (taskService == null)
					taskService = (TaskService) ServiceManager.GetService (typeof(TaskService));
				return taskService;
			}
		}
	
		public IParserService ParserService {
			get {
				if (parserService == null)
					parserService = (IParserService) ServiceManager.GetService (typeof(IParserService));
				return parserService;
			}
		}
	
		public DispatchService DispatchService {
			get {
				if (dispatchService == null)
					dispatchService = (DispatchService) ServiceManager.GetService (typeof(DispatchService));
				return dispatchService;
			}
		}
	
		public IProjectService ProjectService {
			get {
				if (projectService == null)
					projectService = (IProjectService) ServiceManager.GetService (typeof(IProjectService));
				return projectService;
			}
		}
	}
	
}
