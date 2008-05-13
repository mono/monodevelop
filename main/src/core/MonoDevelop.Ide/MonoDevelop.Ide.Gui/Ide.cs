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
using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Dialogs;

using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Debugging;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Gui;
using MonoDevelop.Projects.Documentation;



namespace MonoDevelop.Ide.Gui
{
	public abstract class IdeApp
	{
		static bool isInitialized;
		static Workbench workbench;
		static ProjectOperations projectOperations;
		static HelpOperations helpOperations;
		static CommandManager commandService;
		static IdeServices ideServices;
		
		public static event ExitEventHandler Exiting;
		public static event EventHandler Exited;
		public static event EventHandler Initialized;
		
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
		
		public static CommandManager CommandService {
			get { return commandService; }
		}
		
		public static IdeServices Services {
			get { return ideServices; }
		}

		public static bool IsInitialized {
			get {
				return isInitialized;
			}
		}
		
		public static void Initialize (IProgressMonitor monitor)
		{
			//force the ResourceService to load so that it registers stock icons
			MonoDevelop.Core.Gui.Services.Resources.ToString ();
			
			workbench = new Workbench ();
			projectOperations = new ProjectOperations ();
			helpOperations = new HelpOperations ();
			commandService = new CommandManager ();
			ideServices = new IdeServices ();
			
			commandService.CommandError += delegate (object sender, CommandErrorArgs args) {
				MessageService.ShowException (args.Exception, args.ErrorMessage);
			};
			
			FileService.ErrorHandler = FileServiceErrorHandler;
		
			monitor.BeginTask (GettextCatalog.GetString("Loading Workbench"), 5);
			
			commandService.LoadCommands ("/MonoDevelop/Ide/Commands");
			commandService.LoadKeyBindingSchemes ("/MonoDevelop/Ide/KeyBindingSchemes");
			monitor.Step (1);

			workbench.Initialize (monitor);
			monitor.Step (1);
			
			// register string tag provider (TODO: move to add-in tree :)
			StringParserService.RegisterStringTagProvider(new MonoDevelop.Ide.Commands.SharpDevelopStringTagProvider());
			
			InternalLog.EnableErrorNotification ();
			
			monitor.Step (1);

			workbench.Show ("SharpDevelop.Workbench.WorkbenchMemento");
			monitor.Step (1);
			DispatchService.RunPendingEvents ();
			
			MessageService.RootWindow = workbench.RootWindow;
		
			commandService.EnableIdleUpdate = true;
			
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/StartupHandlers", OnExtensionChanged);
			monitor.EndTask ();

			isInitialized = true;
			if (Initialized != null)
				Initialized (null, EventArgs.Empty);
			
			// Load requested files
			foreach (string file in StartupInfo.GetRequestedFileList()) {
				//FIXME: use mimetypes
				if (Services.ProjectService.IsCombineEntryFile (file)) {
					try {
						IdeApp.ProjectOperations.OpenCombine (file).WaitForCompleted ();
					} catch (Exception e) {
						MessageService.ShowException (e, "Could not load solution: " + file);
					}
				} else {
					try {
						IdeApp.Workbench.OpenDocument (file);
					
					} catch (Exception e) {
						LoggingService.LogInfo ("unable to open file {0} exception was :\n{1}", file, e.ToString());
					}
				}
			}
			
			// load previous combine
			if ((bool)PropertyService.Get("SharpDevelop.LoadPrevProjectOnStartup", false)) {
				RecentOpen recentOpen = Workbench.RecentOpen;

				if (recentOpen.RecentProject != null && recentOpen.RecentProject.Length > 0) { 
					IdeApp.ProjectOperations.OpenCombine(recentOpen.RecentProject[0].ToString()).WaitForCompleted ();
				}
			}
			
			commandService.CommandSelected += OnCommandSelected;
			commandService.CommandDeselected += OnCommandDeselected;
		}
		
		static bool FileServiceErrorHandler (string message, Exception ex)
		{
			MessageService.ShowException (ex, message);
			return true;
		}
		
		static void OnExtensionChanged (object s, ExtensionNodeEventArgs args)
		{
			if (args.Change == ExtensionChange.Add) {
				try {
					typeof(CommandHandler).GetMethod ("Run", System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance, null, Type.EmptyTypes, null).Invoke (args.ExtensionObject, null);
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
				}
			}
		}
		
		static void OnCommandSelected (object s, CommandSelectedEventArgs args)
		{
			string msg = args.CommandInfo.Description;
			if (string.IsNullOrEmpty (msg)) {
				msg = args.CommandInfo.Text;
				msg = msg.Replace ("_", "");
			}
			if (!string.IsNullOrEmpty (msg))
				Workbench.StatusBar.ShowMessage (msg);
		}
			
		static void OnCommandDeselected (object s, EventArgs args)
		{
			Workbench.StatusBar.ShowReady ();
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
		
		internal static bool OnExit ()
		{
			if (Exiting != null) {
				ExitEventArgs args = new ExitEventArgs ();
				Exiting (null, args);
				return !args.Cancel;
			}
			return true;
		}
		
		internal static void OnExited ()
		{
			PropertyService.SaveProperties ();
			if (Exited != null)
				Exited (null, EventArgs.Empty);
		}
	}
	
	public class IdeServices
	{
		IconService icons;
		IDocumentationService documentationService;
		DebuggingService debuggingService;
		
		public ResourceService Resources {
			get { return MonoDevelop.Core.Gui.Services.Resources; }
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
	
		public DebuggingService DebuggingService {
			get {
				if (debuggingService == null)
					debuggingService = new DebuggingService ();
				return debuggingService;
			}
		}
	
		public TaskService TaskService {
			get { return MonoDevelop.Ide.Services.TaskService; }
		}
	
		public IParserService ParserService {
			get { return MonoDevelop.Projects.Services.ParserService; }
		}
	
		public IProjectService ProjectService {
			get { return MonoDevelop.Projects.Services.ProjectService; }
		}
		
		public PlatformService PlatformService {
			get { return MonoDevelop.Core.Gui.Services.PlatformService; }
		}
	}
}
