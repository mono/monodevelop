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
using MonoDevelop.Core;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Documents;
using MonoDevelop.Ide.Navigation;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Ide.TextEditing;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide
{
	public static class IdeServices
	{
		static TextEditorService textEditorService;
		static NavigationHistoryService navigationHistoryManager;
		static DisplayBindingService displayBindingService;
		static FontService fontService;
		static TypeSystemService typeSystemService;
		static DesktopService desktopService;
		static DocumentManager documentManager;
		static RootWorkspace workspace;
		static ProgressMonitorManager progressMonitorManager;
		static TaskService taskService;
		static ProjectOperations projectOperations;
		static HelpOperations helpOperations;
		static DocumentControllerService documentControllerService;
		static DocumentModelRegistry documentModelRegistry;

		static IdeServices ()
		{
			Runtime.ServiceProvider.WhenServiceInitialized<TextEditorService> (s => textEditorService = s);
			Runtime.ServiceProvider.WhenServiceInitialized<NavigationHistoryService> (s => navigationHistoryManager = s);
			Runtime.ServiceProvider.WhenServiceInitialized<DisplayBindingService> (s => displayBindingService = s);
			Runtime.ServiceProvider.WhenServiceInitialized<FontService> (s => fontService = s);
			Runtime.ServiceProvider.WhenServiceInitialized<TypeSystemService> (s => typeSystemService = s);
			Runtime.ServiceProvider.WhenServiceInitialized<DesktopService> (s => desktopService = s);
			Runtime.ServiceProvider.WhenServiceInitialized<DocumentManager> (s => documentManager = s);
			Runtime.ServiceProvider.WhenServiceInitialized<RootWorkspace> (s => workspace = s);
			Runtime.ServiceProvider.WhenServiceInitialized<ProgressMonitorManager> (s => progressMonitorManager = s);
			Runtime.ServiceProvider.WhenServiceInitialized<TaskService> (s => taskService = s);
			Runtime.ServiceProvider.WhenServiceInitialized<ProjectOperations> (s => projectOperations = s);
			Runtime.ServiceProvider.WhenServiceInitialized<HelpOperations> (s => helpOperations = s);
			Runtime.ServiceProvider.WhenServiceInitialized<DocumentControllerService> (s => documentControllerService = s);
			Runtime.ServiceProvider.WhenServiceInitialized<DocumentModelRegistry> (s => documentModelRegistry = s);
		}

		public static TextEditorService TextEditorService => Initialized (textEditorService);

		public static NavigationHistoryService NavigationHistoryService => Initialized (navigationHistoryManager);

		public static DisplayBindingService DisplayBindingService => Initialized (displayBindingService);

		public static FontService FontService => Initialized (fontService);

		public static TypeSystemService TypeSystemService => Initialized (typeSystemService);

		public static DesktopService DesktopService => Initialized (desktopService);

		public static RootWorkspace Workspace => Initialized (workspace);

		public static ProgressMonitorManager ProgressMonitorManager => Initialized (progressMonitorManager);

		static Lazy<TemplatingService> templatingService = new Lazy<TemplatingService> (() => new TemplatingService ());

		public static ProjectService ProjectService => MonoDevelop.Projects.Services.ProjectService;

		public static TemplatingService TemplatingService => templatingService.Value;

		public static DocumentManager DocumentManager => Initialized (documentManager);

		public static TaskService TaskService => Initialized (taskService);

		public static ProjectOperations ProjectOperations => Initialized (projectOperations);

		public static HelpOperations HelpOperations => Initialized (helpOperations);

		public static DocumentControllerService DocumentControllerService => Initialized (documentControllerService);

		public static DocumentModelRegistry DocumentModelRegistry => Initialized (documentModelRegistry);

		public static HelpService HelpService => Core.CoreServices.HelpService;

		static T Initialized<T> (T s) where T : class
		{
			if (s == null)
				throw new InvalidOperationException ("Service " + typeof (T) + " not initialized");
			return s;
		}
	}
}
