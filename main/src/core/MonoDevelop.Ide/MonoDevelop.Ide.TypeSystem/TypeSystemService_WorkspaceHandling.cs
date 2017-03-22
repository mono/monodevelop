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
using System.Collections.Immutable;
using System.Collections.Concurrent;

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

		static object workspaceLock = new object();
		static ImmutableList<MonoDevelopWorkspace> workspaces = ImmutableList<MonoDevelopWorkspace>.Empty;
		static ConcurrentDictionary<MonoDevelop.Projects.Solution, TaskCompletionSource<MonoDevelopWorkspace>> workspaceRequests = new ConcurrentDictionary<MonoDevelop.Projects.Solution, TaskCompletionSource<MonoDevelopWorkspace>> ();

		public static ImmutableArray<Microsoft.CodeAnalysis.Workspace> AllWorkspaces {
			get {
				return workspaces.ToImmutableArray<Microsoft.CodeAnalysis.Workspace> ();
			}
		}


		public static MonoDevelopWorkspace GetWorkspace (MonoDevelop.Projects.Solution solution)
		{
			if (solution == null)
				throw new ArgumentNullException ("solution");
			foreach (var ws in workspaces) {
				if (ws.MonoDevelopSolution == solution)
					return ws;
			}
			return emptyWorkspace;
			}
		
		public static async Task<MonoDevelopWorkspace> GetWorkspaceAsync (MonoDevelop.Projects.Solution solution, CancellationToken cancellationToken = default (CancellationToken))
		{
			var workspace = GetWorkspace (solution);
			if (workspace != emptyWorkspace)
				return workspace;
			var tcs = workspaceRequests.GetOrAdd (solution, _ => new TaskCompletionSource<MonoDevelopWorkspace> ());
			try {
				workspace = GetWorkspace (solution);
				if (workspace != emptyWorkspace)
					return workspace;
				cancellationToken.ThrowIfCancellationRequested ();
				cancellationToken.Register (() => tcs.TrySetCanceled ());
				workspace = await tcs.Task;
			} finally {
				workspaceRequests.TryRemove (solution, out tcs);
			}
			return workspace;
		}

		internal static MonoDevelopWorkspace GetWorkspace (WorkspaceId id)
		{
			foreach (var ws in workspaces) {
				if (ws.Id.Equals (id))
					return ws;
			}
			return emptyWorkspace;
		}

		public static Microsoft.CodeAnalysis.Workspace Workspace {
			get {
				var solution = IdeApp.ProjectOperations?.CurrentSelectedSolution;
				if (solution == null)
					return emptyWorkspace;
				return GetWorkspace (solution);
			}
		}


		public static void NotifyFileChange (string fileName, string text)
		{
			try {
				foreach (var ws in workspaces)
					ws.UpdateFileContent (fileName, text);
			} catch (Exception e) {
				LoggingService.LogError ("Error while notify file change.", e);
			}
		}

		internal static async Task<List<MonoDevelopWorkspace>> Load (WorkspaceItem item, ProgressMonitor progressMonitor, CancellationToken cancellationToken = default(CancellationToken))
		{
			using (Counters.ParserService.WorkspaceItemLoaded.BeginTiming ()) {
				var wsList = new List<MonoDevelopWorkspace> ();
				//If we want BeginTiming to work correctly we need to `await`
				return await InternalLoad (wsList, item, progressMonitor, cancellationToken).ContinueWith (t => { t.Wait (); return wsList; });
			}
		}

		static Task InternalLoad (List<MonoDevelopWorkspace> list, MonoDevelop.Projects.WorkspaceItem item, ProgressMonitor progressMonitor, CancellationToken cancellationToken = default(CancellationToken))
		{
			return Task.Run (async () => {
				var ws = item as MonoDevelop.Projects.Workspace;
				if (ws != null) {
					foreach (var it in ws.Items) {
						await InternalLoad (list, it, progressMonitor, cancellationToken).ConfigureAwait (false);
					}
					ws.ItemAdded += OnWorkspaceItemAdded;
					ws.ItemRemoved += OnWorkspaceItemRemoved;
				} else {
					var solution = item as MonoDevelop.Projects.Solution;
					if (solution != null) {
						var workspace = new MonoDevelopWorkspace (solution);
						lock (workspaceLock)
							workspaces = workspaces.Add (workspace);
						list.Add (workspace);
						workspace.ShowStatusIcon ();
						await workspace.TryLoadSolution (cancellationToken).ConfigureAwait (false);
						solution.SolutionItemAdded += OnSolutionItemAdded;
						solution.SolutionItemRemoved += OnSolutionItemRemoved;
						TaskCompletionSource<MonoDevelopWorkspace> request;
						if (workspaceRequests.TryGetValue (solution, out request))
							request.TrySetResult (workspace);
						workspace.HideStatusIcon ();
					}
				}
			});
		}

		internal static void Unload (MonoDevelop.Projects.WorkspaceItem item)
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
					MonoDevelopWorkspace result = GetWorkspace (solution);
					if (result != emptyWorkspace) {
						lock (workspaceLock)
							workspaces = workspaces.Remove (result);
						result.Dispose ();
					}
					solution.SolutionItemAdded -= OnSolutionItemAdded;
					solution.SolutionItemRemoved -= OnSolutionItemRemoved;
					if (solution.ParentWorkspace == null)
						MonoDocDocumentationProvider.ClearCommentCache ();
				}
			}
		}

		public static DocumentId GetDocumentId (MonoDevelop.Projects.Project project, string fileName)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			fileName = FileService.GetFullPath (fileName);
			foreach (var w in workspaces) {
				var projectId = w.GetProjectId (project);
				if (projectId != null)
					return w.GetDocumentId (projectId, fileName);
			}
			return null;
		}

		public static DocumentId GetDocumentId (Microsoft.CodeAnalysis.Workspace workspace, MonoDevelop.Projects.Project project, string fileName)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			fileName = FileService.GetFullPath (fileName);
			var projectId = ((MonoDevelopWorkspace)workspace).GetProjectId (project);
			if (projectId != null) {
				return ((MonoDevelopWorkspace)workspace).GetDocumentId (projectId, fileName);
			} else {
				LoggingService.LogWarning ("Warning can't find " + fileName + " in project " + project.Name + "("+ projectId +")");
			}
			return null;
		}


		public static DocumentId GetDocumentId (ProjectId projectId, string fileName)
		{
			if (projectId == null)
				throw new ArgumentNullException ("projectId");
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			foreach (var w in workspaces) {
				if (w.Contains (projectId))
					return w.GetDocumentId (projectId, fileName);
			}
			return null;
		}

		public static IEnumerable<DocumentId> GetDocuments (string fileName)
		{
			if (fileName == null)
				throw new ArgumentNullException ("fileName");
			fileName = FileService.GetFullPath (fileName);
			foreach (var w in workspaces) {
				foreach (var projectId in w.CurrentSolution.ProjectIds) {
					var docId = w.GetDocumentId (projectId, fileName);
					if (docId != null)
						yield return docId;
				}
			}
		}

		public static Microsoft.CodeAnalysis.Project GetCodeAnalysisProject (MonoDevelop.Projects.Project project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			foreach (var w in workspaces) {
				var projectId = w.GetProjectId (project); 
				if (projectId != null)
					return w.CurrentSolution.GetProject (projectId);
			}
			return null;
		}

		public static async Task<Microsoft.CodeAnalysis.Project> GetCodeAnalysisProjectAsync (MonoDevelop.Projects.Project project, CancellationToken cancellationToken = default (CancellationToken))
		{
			var parentSolution = project.ParentSolution;
			var workspace = await GetWorkspaceAsync (parentSolution, cancellationToken);
			var projectId = workspace.GetProjectId (project);
			return projectId == null ? null : workspace.CurrentSolution.GetProject (projectId);
		}

		public static Task<Compilation> GetCompilationAsync (MonoDevelop.Projects.Project project, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			foreach (var w in workspaces) {
				var projectId = w.GetProjectId (project); 
				if (projectId == null)
					continue;
				var roslynProject = w.CurrentSolution.GetProject (projectId);
				if (roslynProject == null)
					continue;
				return roslynProject.GetCompilationAsync (cancellationToken);
			}
			return Task.FromResult<Compilation> (null);
		}

		static void OnWorkspaceItemAdded (object s, MonoDevelop.Projects.WorkspaceItemEventArgs args)
		{
			Task.Run (() => TypeSystemService.Load (args.Item, null));
		}

		static void OnWorkspaceItemRemoved (object s, MonoDevelop.Projects.WorkspaceItemEventArgs args)
		{
			Unload (args.Item);
		}

		static async void OnSolutionItemAdded (object sender, MonoDevelop.Projects.SolutionItemChangeEventArgs args)
		{
			var project = args.SolutionItem as MonoDevelop.Projects.Project;
			if (project != null) {
				Unload (project.ParentSolution);
				await Load (project.ParentSolution,  new ProgressMonitor()); 
			}
		}

		static void OnSolutionItemRemoved (object sender, MonoDevelop.Projects.SolutionItemChangeEventArgs args)
		{
			var project = args.SolutionItem as MonoDevelop.Projects.Project;
			var solution = sender as MonoDevelop.Projects.Solution;
			if (project != null) {
				var ws = GetWorkspace (solution);
				ws.RemoveProject (project);
			}
		}

		#region Tracked project handling
		static readonly List<TypeSystemOutputTrackingNode> outputTrackedProjects = new List<TypeSystemOutputTrackingNode> ();

		static void IntitializeTrackedProjectHandling ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/TypeSystem/OutputTracking", delegate (object sender, ExtensionNodeEventArgs args) {
				var node = (TypeSystemOutputTrackingNode)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					outputTrackedProjects.Add (node);
					break;
				case ExtensionChange.Remove:
					outputTrackedProjects.Remove (node);
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
			if (IdeApp.ProjectOperations.CurrentSelectedSolution != null) {
				foreach (var pr in IdeApp.ProjectOperations.CurrentSelectedSolution.GetAllProjects ()) {
					var project = pr as DotNetProject;
					if (project != null)
						CheckProjectOutput (project, true);
				}
			}
		}

		internal static bool IsOutputTrackedProject (DotNetProject project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			return outputTrackedProjects.Any (otp => string.Equals (otp.LanguageName, project.LanguageName, StringComparison.OrdinalIgnoreCase)) || 
				project.GetTypeTags().Any (tag => outputTrackedProjects.Any (otp => string.Equals (otp.ProjectType, tag, StringComparison.OrdinalIgnoreCase)));
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