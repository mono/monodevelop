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
using Mono.Cecil;
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Mono.TextEditor;

namespace MonoDevelop.TypeSystem
{
	public static class TypeSystemServiceExt
	{
		public static Project GetProject (this IProjectContent content)
		{
			foreach (var pair in TypeSystemService.projectContents) {
				if (pair.Value == content)
					return pair.Key;
			}
			return null;
		}
		
		public static Project GetSourceProject (this ITypeDefinition type)
		{
			return type.ProjectContent.GetProject ();
		}
		
		public static Project GetSourceProject (this IType type)
		{
			return type.GetDefinition ().GetSourceProject ();
		}
		
		public static IProjectContent GetProjectContent (this IType type)
		{
			return type.GetDefinition ().ProjectContent;
		}
		
		public static AstLocation GetLocation (this IType type)
		{
			return new AstLocation (type.GetDefinition ().Region.BeginLine, type.GetDefinition ().Region.BeginColumn);
		}
		
		public static string GetDocumentation (this IType type)
		{
			return "TODO";
		}
		
		public static bool IsBaseType (this IType type, ITypeResolveContext ctx, IType potentialBase)
		{
			return type.GetAllBaseTypes (ctx).Any (t => t.Equals (potentialBase));
		}
		
		public static bool IsObsolete (this IEntity member)
		{
			// TODO: Implement me!
			return false;
		}
	}
	
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
		
		public static ParsedDocument ParseFile (Project project, string fileName)
		{
			return ParseFile (GetProjectContext (project), fileName, DesktopService.GetMimeTypeForUri (fileName), File.ReadAllText (fileName));
		}
		
		public static ParsedDocument ParseFile (IProjectContent projectContent, string fileName, string mimeType, TextReader content)
		{
			if (projectContent == null)
				throw new ArgumentNullException ("projectContent");
			var provider = GetProvider (mimeType);
			if (provider == null)
				return null;
			if (ParseOperationStarted != null)
				ParseOperationStarted (null, EventArgs.Empty);
			ParsedDocument result = null;
			try {
				IsParsing = true;
				result = provider.Parse (projectContent, true, fileName, content);
				((SimpleProjectContent)projectContent).UpdateProjectContent (projectContent.GetFile (fileName), result);
				if (ParseOperationFinished != null)
					ParseOperationFinished (null, EventArgs.Empty);
			} finally {
				IsParsing = false;
			}
			return result;
		}
		public static bool IsParsing = false;
		public static event EventHandler ParseOperationStarted;
		public static event EventHandler ParseOperationFinished;

		public static ParsedDocument ParseFile (IProjectContent projectContent, string fileName, string mimeType, string content)
		{
			using (var reader = new StringReader (content))
				return ParseFile (projectContent, fileName, mimeType, reader);
		}
		
		public static ParsedDocument ParseFile (IProjectContent projectContent, TextEditorData data)
		{
			return ParseFile (projectContent, data.FileName, data.MimeType, data.Text);
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
				DateTime start = DateTime.Now;
				var solution = (Solution)item;
				foreach (Project project in solution.GetAllProjects ())
					Load (project);
				solution.SolutionItemAdded += OnSolutionItemAdded;
				solution.SolutionItemRemoved += OnSolutionItemRemoved;
				Console.WriteLine ("solution parse: " + (DateTime.Now - start).TotalMilliseconds);
			}
		}
		
		internal static Dictionary<Project, IProjectContent> projectContents = new Dictionary<Project, IProjectContent> ();
		static Dictionary<Project, int> referenceCounter = new Dictionary<Project, int> ();

		public static IProjectContent LoadContent (Project project)
		{
			DateTime start = DateTime.Now;
			var content = new SimpleProjectContent ();
			int files = 0;
			foreach (var file in project.Files) {
				if (!string.Equals (file.BuildAction, "compile", StringComparison.OrdinalIgnoreCase)) 
					continue;
				
				var provider = GetProvider (DesktopService.GetMimeTypeForUri (file.FilePath));
				if (provider == null)
					continue;
				files++;
				using (var stream = new System.IO.StreamReader (file.FilePath)) {
					var parsedFile = provider.Parse (content, false, file.FilePath, stream);
					content.UpdateProjectContent (null, parsedFile);
				}
			}
			Console.WriteLine (files + " project parse: " + (DateTime.Now - start).TotalMilliseconds);
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
//			ITypeResolveContext db = GetProjectDom (args.Project);
//			if (db != null) 
//				db.OnProjectReferenceAdded (args.ProjectReference);
		}
		
		static void OnProjectReferenceRemoved (object sender, ProjectReferenceEventArgs args)
		{
//			ITypeResolveContext db = GetProjectDom (args.Project);
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
		
		public static ITypeResolveContext GetAssemblyContext (string fileName)
		{
			var asm = ReadAssembly (fileName);
			if (asm == null)
				return null;
			return new CecilLoader ().LoadAssembly (asm);
		}
		
		public static ITypeResolveContext GetAssemblyContext (MonoDevelop.Core.Assemblies.TargetRuntime runtime, string fileName)
		{ // TODO: Runtimes
			var asm = ReadAssembly (fileName);
			if (asm == null)
				return null;
			return new CecilLoader ().LoadAssembly (asm);
		}
		
		public static IProjectContent GetProjectContext (Project project)
		{
			if (project == null)
				return null;
			IProjectContent content;
			projectContents.TryGetValue (project, out content);
			return content;
		}
		
		public static ITypeResolveContext GetContext (FilePath file, string mimeType, string text)
		{
			SimpleProjectContent content = new SimpleProjectContent ();
			var parsedFile = ParseFile (content, file, mimeType, text);
			content.UpdateProjectContent (null, parsedFile);
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
							assemblyContents [refId] = ctx = GetAssemblyContext (fileName);
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

