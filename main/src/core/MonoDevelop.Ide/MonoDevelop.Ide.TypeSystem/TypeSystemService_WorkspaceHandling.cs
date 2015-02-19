//
// TypeSystemService.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System;
using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mono.Addins;
using MonoDevelop.Projects;
using System.IO;
using System.Linq;

namespace MonoDevelop.Ide.TypeSystem
{

//	static class MonoDevelopWorkspaceFeatures
//	{
//		static FeaturePack pack;
//
//		public static FeaturePack Features {
//			get {
//				if (pack == null)
//					Interlocked.CompareExchange (ref pack, ComputePack (), null);
//				return pack;
//			}
//		}
//
//		static FeaturePack ComputePack ()
//		{
//			var assemblies = new List<Assembly> ();
//			var workspaceCoreAssembly = typeof(Workspace).Assembly;
//			assemblies.Add (workspaceCoreAssembly);
//
//			LoadAssembly (assemblies, "Microsoft.CodeAnalysis.CSharp.Workspaces");
//			//LoadAssembly (assemblies, "Microsoft.CodeAnalysis.VisualBasic.Workspaces");
//
//			var catalogs = assemblies.Select (a => new System.ComponentModel.Composition.Hosting.AssemblyCatalog (a));
//
//			return new MefExportPack (catalogs);
//		}
//
//		static void LoadAssembly (List<Assembly> assemblies, string assemblyName)
//		{
//			try {
//				var loadedAssembly = Assembly.Load (assemblyName);
//				assemblies.Add (loadedAssembly);
//			} catch (Exception e) {
//				LoggingService.LogWarning ("Couldn't load assembly:" + assemblyName, e);
//			}
//		}
//	}

	public static partial class TypeSystemService
	{
		static readonly MonoDevelopWorkspace emptyWorkspace;

		static Dictionary<MonoDevelop.Projects.Solution, MonoDevelopWorkspace> workspaces = new Dictionary<MonoDevelop.Projects.Solution, MonoDevelopWorkspace> ();

		internal static MonoDevelopWorkspace GetWorkspace (MonoDevelop.Projects.Solution solution)
		{
			if (solution == null)
				throw new ArgumentNullException ("solution");
			MonoDevelopWorkspace result;
			if (workspaces.TryGetValue (solution, out result))
				return result;
			return emptyWorkspace;
		}

		public static MonoDevelopWorkspace Workspace {
			get {
				var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
				if (solution == null)
					return emptyWorkspace;
				return GetWorkspace (solution);
			}
		}


		public static void NotifyFileChange (string fileName, string text)
		{
			Workspace.UpdateFileContent (fileName, text);
		}

		public static void Load (MonoDevelop.Projects.WorkspaceItem item, IProgressMonitor progressMonitor)
		{
			using (Counters.ParserService.WorkspaceItemLoaded.BeginTiming ()) {
				InternalLoad (item, progressMonitor);
				OnWorkspaceItemLoaded (new WorkspaceItemEventArgs (item));
			}
		}
		public static event EventHandler<WorkspaceItemEventArgs> WorkspaceItemLoaded;

		static void OnWorkspaceItemLoaded (WorkspaceItemEventArgs e)
		{
			var handler = WorkspaceItemLoaded;
			if (handler != null)
				handler (null, e);
		}

		static void InternalLoad (MonoDevelop.Projects.WorkspaceItem item, IProgressMonitor progressMonitor)
		{
			var ws = item as MonoDevelop.Projects.Workspace;
			if (ws != null) {
				foreach (var it in ws.Items)
					InternalLoad (it, progressMonitor);
				ws.ItemAdded += OnWorkspaceItemAdded;
				ws.ItemRemoved += OnWorkspaceItemRemoved;
			} else {
				var solution = item as MonoDevelop.Projects.Solution;
				if (solution != null) {
					var newWorkspace = new MonoDevelopWorkspace ();
					newWorkspace.LoadSolution (solution, progressMonitor);
					workspaces [solution] = newWorkspace;
					solution.SolutionItemAdded += OnSolutionItemAdded;
					solution.SolutionItemRemoved += OnSolutionItemRemoved;
				}
			}
		}

		public static void Unload (MonoDevelop.Projects.WorkspaceItem item)
		{
			var ws = item as MonoDevelop.Projects.Workspace;
			if (ws != null) {
				foreach (var it in ws.Items)
					Unload (it);
				ws.ItemAdded -= OnWorkspaceItemAdded;
				ws.ItemRemoved -= OnWorkspaceItemRemoved;
				MonoDocDocumentationProvider.ClearCommentCache ();
			} else {
				var solution = item as MonoDevelop.Projects.Solution;
				if (solution != null) {
					MonoDevelopWorkspace result;
					if (workspaces.TryGetValue (solution, out result)) {
						workspaces.Remove (solution);
						result.Dispose ();
					}
					solution.SolutionItemAdded -= OnSolutionItemAdded;
					solution.SolutionItemRemoved -= OnSolutionItemRemoved;
					if (solution.ParentWorkspace == null)
						MonoDocDocumentationProvider.ClearCommentCache ();
				}
			}
		}

		public static DocumentId GetDocument (MonoDevelop.Projects.Project project, string fileName)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			var w = Workspace;
			var projectId = w.GetProjectId (project);
			return w.GetDocumentId (projectId, fileName);
		}

		public static Microsoft.CodeAnalysis.Project GetProject (MonoDevelop.Projects.Project project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			var w = Workspace;
			var projectId = w.GetProjectId (project); 
			return w.CurrentSolution.GetProject (projectId);
		}

		public static async Task<Compilation> GetCompilationAsync (MonoDevelop.Projects.Project project, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			var w = Workspace;
			var projectId = w.GetProjectId (project); 
			var roslynProject = w.CurrentSolution.GetProject (projectId);
			return await roslynProject.GetCompilationAsync (cancellationToken).ConfigureAwait (false);
		}

		static void OnWorkspaceItemAdded (object s, MonoDevelop.Projects.WorkspaceItemEventArgs args)
		{
			using (var progressMonitor = IdeApp.Workbench.ProgressMonitors.GetProjectLoadProgressMonitor (false)) {
				Task.Run (() => TypeSystemService.Load (args.Item, progressMonitor));
			}
		}

		static void OnWorkspaceItemRemoved (object s, MonoDevelop.Projects.WorkspaceItemEventArgs args)
		{
			Unload (args.Item);
		}

		static void OnSolutionItemAdded (object sender, MonoDevelop.Projects.SolutionItemChangeEventArgs args)
		{
			var project = args.SolutionItem as MonoDevelop.Projects.Project;
			if (project != null) {
				var ws = GetWorkspace (project.ParentSolution);
				ws.AddProject (project);
			}
		}

		static void OnSolutionItemRemoved (object sender, MonoDevelop.Projects.SolutionItemChangeEventArgs args)
		{
			var project = args.SolutionItem as MonoDevelop.Projects.Project;
			if (project != null) {
				var ws = GetWorkspace (project.ParentSolution);
				ws.RemoveProject (project);
			}
		}

		#region Tracked project handling
		static readonly List<string> outputTrackedProjects = new List<string> ();

		static void IntitializeTrackedProjectHandling ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/TypeSystem/OutputTracking", delegate (object sender, ExtensionNodeEventArgs args) {
				var projectType = ((TypeSystemOutputTrackingNode)args.ExtensionNode).ProjectType;
				switch (args.Change) {
				case ExtensionChange.Add:
					outputTrackedProjects.Add (projectType);
					break;
				case ExtensionChange.Remove:
					outputTrackedProjects.Remove (projectType);
					break;
				}
			});
			if (IdeApp.ProjectOperations != null)
				IdeApp.ProjectOperations.EndBuild += HandleEndBuild;
			if (IdeApp.Workspace != null)
				IdeApp.Workspace.ActiveConfigurationChanged += HandleActiveConfigurationChanged;


		}

		static void HandleEndBuild (object sender, BuildEventArgs args)
		{
			var project = args.SolutionItem as DotNetProject;
			if (project == null)
				return;
			CheckProjectOutput (project, true);
		}

		static void HandleActiveConfigurationChanged (object sender, EventArgs e)
		{
			foreach (var pr in IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllProjects ()) {
				var project = pr as DotNetProject;
				if (project != null)
					CheckProjectOutput (project, true);
			}
		}

		internal static bool IsOutputTrackedProject (DotNetProject project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			return project.GetProjectTypes ().Any (p => outputTrackedProjects.Contains (p, StringComparer.OrdinalIgnoreCase));
		}

		static void CheckProjectOutput (DotNetProject project, bool autoUpdate)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			if (IsOutputTrackedProject (project)) {
				var fileName = project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
				if (!File.Exists (fileName))
					return;
				FileService.NotifyFileChanged (fileName);
				if (autoUpdate) {
					// update documents
					foreach (var openDocument in IdeApp.Workbench.Documents) {
						openDocument.ReparseDocument ();
					}
				}
			}
		}
		#endregion

// TODO: Port framework lookup to NR6
//		#region FrameworkLookup
//		class FrameworkTask
//		{
//			public int RetryCount { get; set; }
//
//			public Task<FrameworkLookup> Task { get; set; }
//		}
//
//		readonly static Dictionary<string, FrameworkTask> frameworkLookup = new Dictionary<string, FrameworkTask> ();
//
//		static void StartFrameworkLookup (DotNetProject netProject)
//		{
//			if (netProject == null)
//				throw new ArgumentNullException ("netProject");
//			lock (frameworkLookup) {
//				FrameworkTask result;
//				if (netProject.TargetFramework == null)
//					return;
//				var frameworkName = netProject.TargetFramework.Name;
//				if (!frameworkLookup.TryGetValue (frameworkName, out result))
//					frameworkLookup [frameworkName] = result = new FrameworkTask ();
//				if (result.Task != null)
//					return;
//				result.Task = Task.Factory.StartNew (delegate {
//					return GetFrameworkLookup (netProject);
//				});
//			}
//		}
//
//		public static bool TryGetFrameworkLookup (DotNetProject project, out FrameworkLookup lookup)
//		{
//			lock (frameworkLookup) {
//				FrameworkTask result;
//				if (frameworkLookup.TryGetValue (project.TargetFramework.Name, out result)) {
//					if (!result.Task.IsCompleted) {
//						lookup = null;
//						return false;
//					}
//					lookup = result.Task.Result;
//					return true;
//				}
//			}
//			lookup = null;
//			return false;
//		}
//
//		public static bool RecreateFrameworkLookup (DotNetProject netProject)
//		{
//			lock (frameworkLookup) {
//				FrameworkTask result;
//				var frameworkName = netProject.TargetFramework.Name;
//				if (!frameworkLookup.TryGetValue (frameworkName, out result))
//					return false;
//				if (result.RetryCount > 5) {
//					LoggingService.LogError ("Can't create framework lookup for:" + frameworkName);
//					return false;
//				}
//				result.RetryCount++;
//				LoggingService.LogInfo ("Trying to recreate framework lookup for {0}, try {1}.", frameworkName, result.RetryCount);
//				result.Task = null;
//				StartFrameworkLookup (netProject);
//				return true;
//			}
//		}
//
//		static FrameworkLookup GetFrameworkLookup (DotNetProject netProject)
//		{
//			FrameworkLookup result;
//			string fileName;
//			var cache = GetCacheDirectory (netProject.TargetFramework);
//			fileName = Path.Combine (cache, "FrameworkLookup_" + FrameworkLookup.CurrentVersion + ".dat");
//			try {
//				if (File.Exists (fileName)) {
//					result = FrameworkLookup.Load (fileName);
//					if (result != null) {
//						return result;
//					}
//				}
//			} catch (Exception e) {
//				LoggingService.LogWarning ("Can't read framework cache - recreating...", e);
//			}
//
//			try {
//				using (var creator = FrameworkLookup.Create (fileName)) {
//					foreach (var assembly in GetFrameworkAssemblies (netProject)) {
//						var ctx = LoadAssemblyContext (assembly.Location);
//						foreach (var type in ctx.Ctx.GetAllTypeDefinitions ()) {
//							if (!type.IsPublic)
//								continue;
//							creator.AddLookup (assembly.Package.Name, assembly.FullName, type);
//						}
//					}
//				}
//			} catch (Exception e) {
//				LoggingService.LogError ("Error while storing framework lookup", e);
//				return FrameworkLookup.Empty;
//			}
//
//			try {
//				result = FrameworkLookup.Load (fileName);
//				return result;
//			} catch (Exception e) {
//				LoggingService.LogError ("Error loading framework lookup", e);
//				return FrameworkLookup.Empty;
//			}
//		}
//		#endregion
	}

}