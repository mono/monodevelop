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
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Composition;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Reflection;

namespace MonoDevelop.Ide.TypeSystem
{
	class MonoDevelopWorkspace : CustomWorkspace
	{
		readonly BackgroundParser parser;
		readonly MetadataReferenceProvider referenceProvider = new MetadataReferenceProvider ();
		Solution currentSolution;

		public override Solution CurrentSolution {
			get {
				return currentSolution;
			}
		}

		public BackgroundParser Parser {
			get {
				return parser;
			}
		}

		public MonoDevelopWorkspace (string activeConfiguration) : base (MonoDevelopWorkspaceFeatures.Features, "MonoDevelopWorkspace")
		{
			parser = new BackgroundParser (this);
		}

		public Solution LoadSolution (MonoDevelop.Projects.Solution solution)
		{
			parser.Stop ();
			var solutionInfo = SolutionInfo.Create (
				                   SolutionId.CreateNewId (solution.Name),
				                   VersionStamp.Create (),
				                   solution.FileName,
				                   solution.GetAllProjects ().AsParallel ().Select (p => LoadProject (p))
			                   );
			currentSolution = CreateSolution (solutionInfo); 
			AddSolution (solutionInfo); 
			parser.Start ();
			return currentSolution;
		}

		Dictionary<MonoDevelop.Projects.Project, ProjectId> projectIdMap = new Dictionary<MonoDevelop.Projects.Project, ProjectId> ();
		Dictionary<ProjectId, ProjectData> projectDataMap = new Dictionary<ProjectId, ProjectData> ();

		internal ProjectId GetProjectId (MonoDevelop.Projects.Project p)
		{
			lock (projectIdMap) {
				ProjectId result;
				if (!projectIdMap.TryGetValue (p, out result)) {
					result = ProjectId.CreateNewId (p.Name);
					projectIdMap [p] = result;
				}
				return result;
			}
		}

		ProjectData GetProjectData (ProjectId id)
		{
			lock (projectIdMap) {
				ProjectData result;
				if (!projectDataMap.TryGetValue (id, out result)) {
					result = new ProjectData (id);
					projectDataMap [id] = result;
				}
				return result;
			}
		}

		class ProjectData
		{
			readonly ProjectId projectId;
			readonly Dictionary<string, DocumentId> documentIdMap = new Dictionary<string, DocumentId> ();

			public ProjectData (ProjectId projectId)
			{
				this.projectId = projectId;
			}

			public DocumentId GetDocumentId (string name)
			{
				lock (documentIdMap) {
					DocumentId result;
					if (!documentIdMap.TryGetValue (name, out result)) {
						result = DocumentId.CreateNewId (projectId, name);
						documentIdMap [name] = result;
					}
					return result;
				}
			}

		}

		internal DocumentId GetDocumentId (ProjectId projectId, string name)
		{
			var data = GetProjectData (projectId);
			return data.GetDocumentId (name);
		}

		ProjectInfo LoadProject (MonoDevelop.Projects.Project p)
		{
			var projectId = GetProjectId (p);
			var projectData = GetProjectData (projectId); 
			return ProjectInfo.Create (
				projectId,
				VersionStamp.Create (),
				p.Name,
				p.Name,
				"C#",
				p.FileName,
				p.GetOutputFileName (IdeApp.Workspace.ActiveConfiguration),
				null,
				null,
				GetDocuments (projectData, p),
				GetProjectReferences (p),
				GetMetadataReferences (p)
			);
		}

		IEnumerable<DocumentInfo> GetDocuments (ProjectData id, MonoDevelop.Projects.Project p)
		{
			return p.Files
				.Where (f => f.BuildAction == MonoDevelop.Projects.BuildAction.Compile)
				.Select (f => DocumentInfo.Create (
				id.GetDocumentId (f.Name),
				f.FilePath.FileNameWithoutExtension,
				null,
				SourceCodeKind.Regular,
				null,
				f.FilePath,
				false
			));
		}

		IEnumerable<MetadataReference> GetMetadataReferences (MonoDevelop.Projects.Project p)
		{
			var netProject = p as MonoDevelop.Projects.DotNetProject;
			if (netProject == null)
				yield break;
			var config = IdeApp.Workspace != null ? netProject.GetConfiguration (IdeApp.Workspace.ActiveConfiguration) as MonoDevelop.Projects.DotNetProjectConfiguration : null;
			bool noStdLib = false;
			if (config != null) {
				var parameters = config.CompilationParameters as MonoDevelop.Projects.DotNetConfigurationParameters;
				if (parameters != null) {
					noStdLib = parameters.NoStdLib;
				}
			}

			if (!noStdLib && netProject.TargetRuntime != null && netProject.TargetRuntime.AssemblyContext != null) {
				var corLibRef = netProject.TargetRuntime.AssemblyContext.GetAssemblyForVersion (
					                typeof(object).Assembly.FullName,
					                null,
					                netProject.TargetFramework
				                );
				if (corLibRef != null) {
					yield return referenceProvider.GetReference (corLibRef.Location);
				}
			}

			foreach (string file in netProject.GetReferencedAssemblies (MonoDevelop.Projects.ConfigurationSelector.Default, false)) {
				string fileName;
				if (!Path.IsPathRooted (file)) {
					fileName = Path.Combine (Path.GetDirectoryName (netProject.FileName), file);
				} else {
					fileName = Path.GetFullPath (file);
				}
				yield return referenceProvider.GetReference (fileName);
			}
		}

		IEnumerable<ProjectReference> GetProjectReferences (MonoDevelop.Projects.Project p)
		{
			foreach (var pr in p.GetReferencedItems (MonoDevelop.Projects.ConfigurationSelector.Default)) {
				var referencedProject = pr as MonoDevelop.Projects.Project;
				if (referencedProject != null) {
					yield return new ProjectReference (GetProjectId (referencedProject));
				}
			}
		}

		public Document GetDocument (DocumentId documentId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var project = currentSolution.GetProject (documentId.ProjectId, cancellationToken); 
			Console.WriteLine ("project:"+ project);
			if (project == null)
				return null;
			Console.WriteLine (" get document id : " + documentId);
			//	parser.Parse (); 
			return project.GetDocument (documentId);
		}
	}

	static class MonoDevelopWorkspaceFeatures
	{
		static FeaturePack pack;
		public static FeaturePack Features {
			get {
				if (pack == null)
					Interlocked.CompareExchange (ref pack, ComputePack (), null);
				return pack;
			}
		}

		static FeaturePack ComputePack ()
		{
			var assemblies = new List<Assembly> ();
			var thisAssembly = typeof(WellKnownFeatures).Assembly;
			assemblies.Add (thisAssembly);

			LoadAssembly (assemblies, "Microsoft.CodeAnalysis.CSharp.Workspaces");
			// LoadAssembly (assemblies, "Microsoft.CodeAnalysis.VisualBasic.Workspaces");

			var catalogs = assemblies.Select(a => new System.ComponentModel.Composition.Hosting.AssemblyCatalog(a));

			return new MefExportPack (catalogs);
		}

		static void LoadAssembly (List<Assembly> assemblies, string assemblyName)
		{
			try {
				var loadedAssembly = Assembly.Load (assemblyName);
				Console.WriteLine (assemblyName  +"/" + assemblies);
				assemblies.Add (loadedAssembly);
			} catch (Exception e) {
				LoggingService.LogWarning ("Couldn't load assembly:" + assemblyName);
			}
		}
	}

	public static class RoslynTypeSystemService
	{
		static MonoDevelopWorkspace workspace;

		public static Workspace Workspace {
			get {
				return workspace;
			}
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
			} else {
				var solution = item as MonoDevelop.Projects.Solution;
				if (solution != null) {
					workspace = new MonoDevelopWorkspace (IdeApp.Workspace.ActiveConfigurationId);
					workspace.LoadSolution (solution);
				}
			}
		}

		public static Document GetDocument (MonoDevelop.Projects.Project project, string fileName)
		{
			var projectId = workspace.GetProjectId (project);
			var documentId = workspace.GetDocumentId (projectId, fileName);

			return workspace.GetDocument (documentId);
		}
	}
}