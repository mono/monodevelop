// 
// TypeSystemService.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Mike Krüger <mkrueger@novell.com>
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
using System.Collections.Generic;
using System.Linq;
using System.IO;
using MonoDevelop.Projects;
using ICSharpCode.Decompiler.Ast;
using Mono.Cecil;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.TypeSystem
{
	public static class TypeSystemService
	{
		static List<TypeSystemProviderNode> parsers;
		
		static IEnumerable<TypeSystemProviderNode> Parsers {
			get {
				if (parsers == null) {
//					Counters.ParserServiceInitialization.BeginTiming ();
					parsers = new List<TypeSystemProviderNode> ();
					AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/TypeSystemProvider", delegate (object sender, ExtensionNodeEventArgs args) {
						switch (args.Change) {
						case ExtensionChange.Add:
							parsers.Add ((TypeSystemProviderNode)args.ExtensionNode);
							break;
						case ExtensionChange.Remove:
							parsers.Remove ((TypeSystemProviderNode)args.ExtensionNode);
							break;
						}
					});
//					Counters.ParserServiceInitialization.EndTiming ();
				}
				return parsers;
			}
		}
		
		static ITypeSystemProvider GetProvider (string mimeType)
		{
			var provider = Parsers.FirstOrDefault (p => p.MimeType == mimeType);
			return provider != null ? provider.Provider : null;
		}
		
		public static IParsedFile ParseFile (IProjectContent projectContent, string fileName, string mimeType, TextReader content)
		{
			var provider = GetProvider (mimeType);
			if (provider == null)
				return null;
			return provider.Parse (projectContent, fileName, content);
		}
		
		public static IParsedFile ParseFile (IProjectContent projectContent, string fileName, string mimeType, string content)
		{
			using (var reader = new StringReader (content))
				return ParseFile (projectContent, fileName, mimeType, reader);
		}
		
		#region Project loading
		public static void Load (WorkspaceItem item)
		{
			if (item is Workspace) {
				var ws = (Workspace)item;
				foreach (WorkspaceItem it in ws.Items)
					Load (it);
				ws.ItemAdded += OnWorkspaceItemAdded;
				ws.ItemRemoved += OnWorkspaceItemRemoved;
			} else if (item is Solution) {
				var solution = (Solution)item;
				foreach (Project project in solution.GetAllProjects ())
					Load (project);
				solution.SolutionItemAdded += OnSolutionItemAdded;
				solution.SolutionItemRemoved += OnSolutionItemRemoved;
			}
		}
		
		static Dictionary<Project, IProjectContent> projectContents = new Dictionary<Project, IProjectContent> ();
		static Dictionary<Project, int> referenceCounter = new Dictionary<Project, int> ();

		public static IProjectContent LoadContent (Project project)
		{
			var content = new SimpleProjectContent ();
					
			foreach (var file in project.Files) {
				if (!string.Equals (file.BuildAction, "compile", StringComparison.OrdinalIgnoreCase)) 
					continue;
				
				var provider = GetProvider (DesktopService.GetMimeTypeForUri (file.FilePath));
				if (provider == null)
					continue;
				
				using (var stream = new System.IO.StreamReader (file.FilePath)) {
					var parsedFile = provider.Parse (content, file.FilePath, stream);
					content.UpdateProjectContent (null, parsedFile);
				}
			}
			return content;
		}
		
		public static void Load (Project project)
		{
			if (IncLoadCount (project) != 1)
				return;
			lock (rwLock) {
				if (projectContents.ContainsKey (project))
					return;
				try {
					var content = LoadContent (project);
					projectContents [project] = content;
					referenceCounter [project] = 1;
					OnProjectContentLoaded (new ProjectContentEventArgs (project, content));
					if (project is DotNetProject) {
						((DotNetProject)project).ReferenceAddedToProject += OnProjectReferenceAdded;
						((DotNetProject)project).ReferenceRemovedFromProject += OnProjectReferenceRemoved;
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Parser database for project '" + project.Name + " could not be loaded", ex);
				}
			}
		}
		
		public static event EventHandler<ProjectContentEventArgs> ProjectContentLoaded;
		static void OnProjectContentLoaded (ProjectContentEventArgs e)
		{
			var handler = ProjectContentLoaded;
			if (handler != null)
				handler (null, e);
		}
		
		public static void Unload (WorkspaceItem item)
		{
			if (item is Workspace) {
				var ws = (Workspace)item;
				foreach (WorkspaceItem it in ws.Items)
					Unload (it);
				ws.ItemAdded -= OnWorkspaceItemAdded;
				ws.ItemRemoved -= OnWorkspaceItemRemoved;
			} else if (item is Solution) {
				Solution solution = (Solution)item;
				foreach (Project project in solution.GetAllProjects ()) {
					Unload (project);
				}
				solution.SolutionItemAdded -= OnSolutionItemAdded;
				solution.SolutionItemRemoved -= OnSolutionItemRemoved;
			}
		}
		
		public static void Unload (Project project)
		{
			if (DecLoadCount (project) != 0)
				return;
			
			if (--referenceCounter [project] <= 0) {
				if (project is DotNetProject) {
					((DotNetProject)project).ReferenceAddedToProject -= OnProjectReferenceAdded;
					((DotNetProject)project).ReferenceRemovedFromProject -= OnProjectReferenceRemoved;
				}
				projectContents.Remove (project);
				referenceCounter.Remove (project);
			}
		}
		
		static void OnWorkspaceItemAdded (object s, WorkspaceItemEventArgs args)
		{
			Load (args.Item);
		}
		
		static void OnWorkspaceItemRemoved (object s, WorkspaceItemEventArgs args)
		{
			Unload (args.Item);
		}
		
		static void OnSolutionItemAdded (object sender, SolutionItemChangeEventArgs args)
		{
			if (args.SolutionItem is Project)
				Load ((Project)args.SolutionItem);
		}
		
		static void OnSolutionItemRemoved (object sender, SolutionItemChangeEventArgs args)
		{
			if (args.SolutionItem is Project)
				Unload ((Project)args.SolutionItem);
		}
		
		static void OnProjectReferenceAdded (object sender, ProjectReferenceEventArgs args)
		{
//			ProjectDom db = GetProjectDom (args.Project);
//			if (db != null) 
//				db.OnProjectReferenceAdded (args.ProjectReference);
		}
		
		static void OnProjectReferenceRemoved (object sender, ProjectReferenceEventArgs args)
		{
//			ProjectDom db = GetProjectDom (args.Project);
//			if (db != null) 
//				db.OnProjectReferenceRemoved (args.ProjectReference);
		}
		#endregion

		#region Reference Counting
		static Dictionary<object,int> loadCount = new Dictionary<object,int> ();
		static object rwLock = new object ();
		
		static int DecLoadCount (object ob)
		{
			lock (rwLock) {
				int c;
				if (loadCount.TryGetValue (ob, out c)) {
					c--;
					if (c == 0)
						loadCount.Remove (ob);
					else
						loadCount [ob] = c;
					return c;
				}
				LoggingService.LogError ("DecLoadCount: Object not registered.");
				return 0;
			}
		}
		
		static int IncLoadCount (object ob)
		{
			lock (rwLock) {
				int c;
				if (loadCount.TryGetValue (ob, out c)) {
					c++;
					loadCount [ob] = c;
					return c;
				} else {
					loadCount [ob] = 1;
					return 1;
				}
			}
		}
		#endregion
		
		static Dictionary<string, ITypeResolveContext> assemblyContents = new Dictionary<string, ITypeResolveContext> ();
		
		static AssemblyDefinition ReadAssembly (string fileName)
		{
			ReaderParameters parameters = new ReaderParameters ();
//			parameters.AssemblyResolver = new SimpleAssemblyResolver (Path.GetDirectoryName (fileName));
			using (var stream = new MemoryStream (File.ReadAllBytes (fileName))) {
				return AssemblyDefinition.ReadAssembly (stream, parameters);
			}
		}
		
		static ITypeResolveContext LoadAssemblyContext (string fileName)
		{
			List<ITypeResolveContext> contexts = new List<ITypeResolveContext> ();
			var asm = ReadAssembly (fileName);
			if (asm == null)
				return null;
			foreach (var module in asm.Modules) 
				contexts.Add (new CecilTypeResolveContext (module));
			return new CompositeTypeResolveContext (contexts);
		}
		
		public static IProjectContent GetProjectContext (Project project)
		{
			IProjectContent content;
			projectContents.TryGetValue (project, out content);
			return content;
		}
		
		public static ITypeResolveContext GetContext (Project project)
		{
			List<ITypeResolveContext> contexts = new List<ITypeResolveContext> ();
			
			IProjectContent content;
			if (projectContents.TryGetValue (project, out content))
				contexts.Add (content);
			
			foreach (var pr in project.GetReferencedItems (ConfigurationSelector.Default)) {
				var referencedProject = pr as Project;
				if (referencedProject == null)
					continue;
				if (projectContents.TryGetValue (referencedProject, out content))
					contexts.Add (content);
			}
			
			if (project is DotNetProject) {
				var netProject = (DotNetProject)project;
				// Get the assembly references throught the project, since it may have custom references
				foreach (string file in netProject.GetReferencedAssemblies (ConfigurationSelector.Default, false)) {
					string fileName;
					if (!Path.IsPathRooted (file)) {
						fileName = Path.Combine (Path.GetDirectoryName (netProject.FileName), file);
					} else {
						fileName = Path.GetFullPath (file);
					}
					string refId = "Assembly:" + netProject.TargetRuntime.Id + ":" + fileName;
					ITypeResolveContext ctx;
					if (!assemblyContents.TryGetValue (refId, out ctx)) {
						try {
							assemblyContents [refId] = ctx = LoadAssemblyContext (fileName);
						} catch (Exception) {
						}
					}
					
					if (ctx != null)
						contexts.Add (ctx);
				}
			}
			return new CompositeTypeResolveContext (contexts);
		}
	}
}

