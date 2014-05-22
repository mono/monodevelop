//
// RoslynTypeSystemService.cs
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
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using Microsoft.CodeAnalysis.Text;
using Mono.TextEditor;
using Microsoft.CodeAnalysis.Host.Mef;
using MonoDevelop.Ide.Tasks;
using System.Threading.Tasks;

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

	public static class RoslynTypeSystemService
	{
		static readonly MonoDevelopWorkspace emptyWorkspace;

		static Dictionary<MonoDevelop.Projects.Solution, MonoDevelopWorkspace> workspaces = new Dictionary<MonoDevelop.Projects.Solution, MonoDevelopWorkspace> ();

		static readonly List<string> outputTrackedProjects = new List<string> ();

		static MonoDevelopWorkspace GetWorkspace (MonoDevelop.Projects.Solution solution)
		{
			MonoDevelopWorkspace result;
			if (workspaces.TryGetValue (solution, out result))
				return result;
			return emptyWorkspace;
		}

		public static MonoDevelopWorkspace Workspace {
			get {
				return GetWorkspace (IdeApp.ProjectOperations.CurrentSelectedSolution);
			}
		}
		
		static RoslynTypeSystemService ()
		{
			try {
				emptyWorkspace = new MonoDevelopWorkspace ();
			} catch (Exception e) {
				LoggingService.LogFatalError ("Can't create roslyn workspace", e); 
			}

			FileService.FileChanged += delegate(object sender, FileEventArgs e) {
//				if (!TrackFileChanges)
//					return;
				foreach (var file in e) {
					// Open documents are handled by the Document class itself.
					if (IdeApp.Workbench != null && IdeApp.Workbench.GetDocument (file.FileName) != null)
						continue;
					emptyWorkspace.UpdateFileContent (file.FileName);
				}
			};
		}

		public static void Load (MonoDevelop.Projects.WorkspaceItem item)
		{
			using (Counters.ParserService.WorkspaceItemLoaded.BeginTiming ()) {
				InternalLoad (item);
			}
		}

		static void InternalLoad (MonoDevelop.Projects.WorkspaceItem item)
		{
			var ws = item as MonoDevelop.Projects.Workspace;
			if (ws != null) {
				foreach (var it in ws.Items)
					InternalLoad (it);
				ws.ItemAdded += OnWorkspaceItemAdded;
				ws.ItemRemoved += OnWorkspaceItemRemoved;
			} else {
				var solution = item as MonoDevelop.Projects.Solution;
				if (solution != null) {
					var newWorkspace = new MonoDevelopWorkspace ();
					newWorkspace.LoadSolution (solution);
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
				}
			}
		}

		public static DocumentId GetDocument (MonoDevelop.Projects.Project project, string fileName)
		{
			var projectId = emptyWorkspace.GetProjectId (project);
			return emptyWorkspace.GetDocumentId (projectId, fileName);
		}

		public static Project GetProject (MonoDevelop.Projects.Project project)
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			var projectId = emptyWorkspace.GetProjectId (project); 
			return emptyWorkspace.CurrentSolution.GetProject (projectId);
		}

		public static void UpdateDocument (Project project, FilePath fileName, string currentParseText)
		{
		}

		public static async Task<Compilation> GetCompilationAsync (MonoDevelop.Projects.Project project, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (project == null)
				throw new ArgumentNullException ("project");
			var projectId = emptyWorkspace.GetProjectId (project); 
			Project roslynProject = emptyWorkspace.CurrentSolution.GetProject (projectId);
			return await roslynProject.GetCompilationAsync (cancellationToken);
		}

		static void OnWorkspaceItemAdded (object s, MonoDevelop.Projects.WorkspaceItemEventArgs args)
		{
			Load (args.Item);
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


//		static void CheckProjectOutput (DotNetProject project, bool autoUpdate)
//		{
//			if (project == null)
//				throw new ArgumentNullException ("project");
//			if (project.GetProjectTypes ().Any (p => outputTrackedProjects.Contains (p, StringComparer.OrdinalIgnoreCase))) {
//				var fileName = project.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration);
//
//				var wrapper = GetProjectContentWrapper (project);
//				bool update = wrapper.UpdateTrackedOutputAssembly (fileName);
//				if (autoUpdate && update) {
//					wrapper.ReconnectAssemblyReferences ();
//
//					// update documents
//					foreach (var openDocument in IdeApp.Workbench.Documents) {
//						openDocument.ReparseDocument ();
//					}
//				}
//			}
//		}
	}

}