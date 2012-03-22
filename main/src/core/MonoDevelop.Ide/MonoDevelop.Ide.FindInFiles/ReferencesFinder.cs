// 
// ReferenceFinder.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.TypeSystem;

namespace MonoDevelop.Ide.FindInFiles
{
	public abstract class ReferenceFinder
	{
		public bool IncludeDocumentation {
			get;
			set;
		}
		
		/*
		Project project;
		protected Project Project {
			get {
				if (project == null)
					project = Content.GetProject ();
				return project;
			}
		}*/
		
		static List<ReferenceFinderCodon> referenceFinderCodons = new List<ReferenceFinderCodon> ();
		
		static ReferenceFinder ()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/ReferenceFinder", delegate(object sender, ExtensionNodeEventArgs args) {
				var codon = (ReferenceFinderCodon)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					referenceFinderCodons.Add (codon);
					break;
				case ExtensionChange.Remove:
					referenceFinderCodons.Remove (codon);
					break;
				}
			});
		}
		
		static ReferenceFinder GetReferenceFinder (string mimeType)
		{
			var codon = referenceFinderCodons.FirstOrDefault (c => c.SupportedMimeTypes.Any (mt => mt == mimeType));
			return codon != null ? codon.CreateFinder () : null;
		}
		
		
		public static IEnumerable<MemberReference> FindReferences (object member, IProgressMonitor monitor = null)
		{
			return FindReferences (IdeApp.ProjectOperations.CurrentSelectedSolution, member, RefactoryScope.Unknown, monitor);
		}

		public static IEnumerable<MemberReference> FindReferences (object member, RefactoryScope scope, IProgressMonitor monitor = null)
		{
			return FindReferences (IdeApp.ProjectOperations.CurrentSelectedSolution, member, scope, monitor);
		}
		
		class FileList
		{
			public Project Project {
				get;
				private set;
			}

			public IProjectContent Content {
				get;
				private set;
			}
			
			public IEnumerable<FilePath> Files {
				get;
				private set;
			}
			
			public FileList (Project project, IProjectContent content, IEnumerable<FilePath> files)
			{
				this.Project = project;
				this.Content = content;
				this.Files = files;
			}
		}

		static FileList GetFileNamesForType (ITypeDefinition type, ISet<string> searchedFiles)
		{
			var paths = type.Parts.Select (p => p.Region.FileName).Where (searchedFiles.Add).Distinct ().Select (p => (FilePath)p);
			return new FileList (type.GetSourceProject (), type.GetProjectContent (), paths);
		}

		static FileList GetFileList (string fileName)
		{
			var doc = IdeApp.Workbench.GetDocument (fileName);
			return doc == null ? null : new FileList (doc.Project, doc.ProjectContent, new [] { (FilePath)fileName });
		}

		static IEnumerable<FileList> GetFileNames (Solution solution, IEnumerable<object> searchNodes, RefactoryScope scope, IProgressMonitor monitor)
		{
			var searchedFiles = new HashSet<string> ();
			var searchedProjects = new HashSet<Project> ();
			foreach (var node in searchNodes) {
				if (node is IVariable) {
					// it's okay to give a broader scope than File for variable,
					// but we'll only look for it in the file
					var fileList = GetFileList ((node as IVariable).Region.FileName);
					if (fileList != null)
						yield return fileList;
					continue;
				}

				var entity = (IEntity)node;
				if (scope == RefactoryScope.Unknown)
					scope = GetScope (node);

				switch (scope) {
				case RefactoryScope.File:
					if (!searchedFiles.Add (entity.Region.FileName))
						continue;
					var fileList = GetFileList (entity.Region.FileName);
					if (fileList != null)
						yield return fileList;
					break;
				case RefactoryScope.DeclaringType:
					yield return GetFileNamesForType (entity.DeclaringTypeDefinition, searchedFiles);
					break;
				case RefactoryScope.Project:
					var prj = TypeSystemService.GetProject (entity);
					if (prj == null || !searchedProjects.Add (prj))
						continue;
					var ctx = TypeSystemService.GetProjectContext (prj);
					if (ctx != null)
						yield return new FileList (prj, ctx, prj.Files.Select (f => f.FilePath));
					break;
				case RefactoryScope.Solution:
					var sourceProject = TypeSystemService.GetProject (entity);
					IList<Project> projects;
					if (sourceProject == null) {
						// member is defined in a referenced assembly
						projects = GetAllReferencingProjects (solution, entity.ParentAssembly.AssemblyName);
					} else {
						projects = GetAllReferencingProjects (solution, sourceProject);
					}
					if (monitor != null)
						monitor.BeginTask (GettextCatalog.GetString ("Searching for references in solution..."), projects.Count);
					foreach (var project in projects) {
						if (monitor != null && monitor.IsCancelRequested)
							yield break;
						if (!searchedProjects.Add (project))
							continue;
						var currentDom = TypeSystemService.GetProjectContext (project);
						yield return new FileList (project, currentDom, project.Files.Select (f => f.FilePath));
						if (monitor != null)
							monitor.Step (1);
					}
					if (monitor != null)
						monitor.EndTask ();
					break;
				}
			}
		}

		static IList<Project> GetAllReferencingProjects (Solution solution, string assemblyName)
		{
			return solution.GetAllProjects ().Where (
				project => TypeSystemService.GetCompilation (project).Assemblies.Any (a => a.AssemblyName == assemblyName)).ToList ();
		}
		
		public static List<Project> GetAllReferencingProjects (Solution solution, Project sourceProject)
		{
			var projects = new List<Project> ();
			projects.Add (sourceProject);
			foreach (var project in solution.GetAllProjects ()) {
				if (project.GetReferencedItems (ConfigurationSelector.Default).Any (prj => prj == sourceProject))
					projects.Add (project);
			}
			return projects;
		}
		
		public static IEnumerable<MemberReference> FindReferences (Solution solution, object member, RefactoryScope scope = RefactoryScope.Unknown, IProgressMonitor monitor = null)
		{
			if (member == null)
				yield break;
			IEnumerable<object> searchNodes = new [] { member };
			if (member is IType) {
				var nodes = new List<object> ();
				nodes.Add (member);
				nodes.AddRange (((IType)member).GetConstructors ());
				searchNodes = CollectMembers ((IType)member);
			} else if (member is IEntity) {
				var e = (IEntity)member;
				if (e.EntityType == EntityType.Destructor) {
					foreach (var r in FindReferences (solution, e.DeclaringType, scope, monitor)) {
						yield return r;
					}
					yield break;
				}
				if (member is IMember)
					searchNodes = CollectMembers (solution, (IMember)member);
			}
			
			// prepare references finder
			var preparedFinders = new List<Tuple<ReferenceFinder, Project, IProjectContent, List<FilePath>>> ();
			var curList = new List<FilePath> ();
			foreach (var info in GetFileNames (solution, searchNodes, scope, monitor)) {
				string oldMime = null;
				foreach (var file in info.Files) {
					if (monitor != null && monitor.IsCancelRequested)
						yield break;
					
					string mime = DesktopService.GetMimeTypeForUri (file);
					if (mime != oldMime) {
						var finder = GetReferenceFinder (mime);
						if (finder == null)
							continue;
						
						oldMime = mime;
						
						curList = new List<FilePath> ();
						preparedFinders.Add (Tuple.Create (finder, info.Project, info.Content, curList));
					}
					curList.Add (file);
				}
			}
			
			// execute search
			foreach (var tuple in preparedFinders) {
				var finder = tuple.Item1;
				foreach (var foundReference in finder.FindReferences (tuple.Item2, tuple.Item3, tuple.Item4, searchNodes)) {
					if (monitor != null && monitor.IsCancelRequested)
						yield break;
					yield return foundReference;
				}
			}
		}
		
		public abstract IEnumerable<MemberReference> FindReferences (Project project, IProjectContent content, IEnumerable<FilePath> files, IEnumerable<object> searchedMembers);

		internal static IEnumerable<IMember> CollectMembers (Solution solution, IMember member)
		{
			if (member.EntityType == EntityType.Constructor || member.EntityType == EntityType.Operator)
				return new [] { member };

			return MemberCollector.CollectMembers (solution, member);
		}
		
		internal static IEnumerable<IEntity> CollectMembers (IType type)
		{
			yield return (IEntity)type;
			foreach (var c in type.GetConstructors ()) {
				if (!c.IsSynthetic)
					yield return c;
			}

			foreach (var m in type.GetMethods (m  => m.IsDestructor)) {
				yield return m;
			}
		}


		public enum RefactoryScope{ Unknown, File, DeclaringType, Solution, Project}
		static RefactoryScope GetScope (object o)
		{
			IEntity node = o as IEntity;
			if (node == null)
				return RefactoryScope.File;
			
			// TODO: RefactoringsScope.Hierarchy
			switch (node.Accessibility) {
			case Accessibility.Public:
			case Accessibility.Protected:
			case Accessibility.ProtectedOrInternal:
				if (node.DeclaringTypeDefinition != null) {
					var scope = GetScope (node.DeclaringTypeDefinition);
					if (scope != RefactoryScope.Solution)
						return RefactoryScope.Project;
				}
				return RefactoryScope.Solution;
			case Accessibility.Internal:
			case Accessibility.ProtectedAndInternal:
				return RefactoryScope.Project;
			}
			return RefactoryScope.DeclaringType;
		}
	}
	
	[ExtensionNode (Description="A reference finder. The specified class needs to inherit from MonoDevelop.Projects.CodeGeneration.ReferenceFinder")]
	internal class ReferenceFinderCodon : TypeExtensionNode
	{
		[NodeAttribute("supportedmimetypes", "Mime types supported by this binding (to be shown in the Open File dialog)")]
		string[] supportedMimetypes;
		
		public string[] SupportedMimeTypes {
			get {
				return supportedMimetypes;
			}
			set {
				supportedMimetypes = value;
			}
		}
		
		public ReferenceFinder CreateFinder ()
		{
			return (ReferenceFinder)CreateInstance ();
		}
		
		public override string ToString ()
		{
			return string.Format ("[ReferenceFinderCodon: SupportedMimeTypes={0}]", SupportedMimeTypes);
		}
	}
}
