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
using MonoDevelop.Ide.TypeSystem;
using System.Threading.Tasks;

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
		
		public static IEnumerable<MemberReference> FindReferences (object member, bool searchForAllOverloads, IProgressMonitor monitor = null)
		{
			return FindReferences (IdeApp.ProjectOperations.CurrentSelectedSolution, member, searchForAllOverloads, RefactoryScope.Unknown, monitor);
		}

		public static IEnumerable<MemberReference> FindReferences (object member, bool searchForAllOverloads, RefactoryScope scope, IProgressMonitor monitor = null)
		{
			return FindReferences (IdeApp.ProjectOperations.CurrentSelectedSolution, member, searchForAllOverloads, scope, monitor);
		}
		
		static SearchCollector.FileList GetFileList (string fileName)
		{
			var doc = IdeApp.Workbench.GetDocument (fileName);
			if (doc != null)
				return new SearchCollector.FileList (doc.Project, doc.ProjectContent, new [] { (FilePath)fileName });
			return null;
		}

		static IEnumerable<SearchCollector.FileList> GetFileNames (Solution solution, object node, RefactoryScope scope, 
		                                                           IProgressMonitor monitor, IEnumerable<object> searchNodes)
		{
			if (!(node is IField) && node is IVariable || scope == RefactoryScope.File) {
				string fileName;
				if (node is IEntity) {
					fileName = ((IEntity)node).Region.FileName;
				} else if (node is ITypeParameter) {
					fileName = ((ITypeParameter)node).Region.FileName;
				} else {
					fileName = ((IVariable)node).Region.FileName;
				}
				var fileList =  GetFileList (fileName);
				if (fileList != null)
					yield return fileList;
				yield break;
			}
			
			if (node is ITypeParameter) {
				var typeParameter = node as ITypeParameter;
				if (typeParameter.Owner != null) {
					yield return SearchCollector.CollectDeclaringFiles (typeParameter.Owner);
					yield break;
				}
				var fileList =  GetFileList (typeParameter.Region.FileName);
				if (fileList != null)
					yield return fileList;
				yield break;
			}

			var entity = (IEntity)node;
			switch (scope) {
			case RefactoryScope.DeclaringType:
				if (entity.DeclaringTypeDefinition != null)
					yield return SearchCollector.CollectDeclaringFiles (entity.DeclaringTypeDefinition);
				else
					yield return SearchCollector.CollectDeclaringFiles (entity);
				break;
			case RefactoryScope.Project:
				var sourceProject = TypeSystemService.GetProject (entity.Compilation.MainAssembly.UnresolvedAssembly.Location);
				foreach (var file in SearchCollector.CollectFiles (sourceProject, searchNodes.Cast<IEntity> ()))
					yield return file;
				break;
			default:
				var files = SearchCollector.CollectFiles (solution, searchNodes.Cast<IEntity> ()).ToList ();
				if (monitor != null)
					monitor.BeginTask (GettextCatalog.GetString ("Searching for references in solution..."), files.Count);
				foreach (var file in files) {
					if (monitor != null && monitor.IsCancelRequested)
						yield break;
					yield return file;
					if (monitor != null)
						monitor.Step (1);
				}
				if (monitor != null)
					monitor.EndTask ();
				break;
			}
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

		public static bool HasOverloads (Solution solution, object item)
		{
			if (!(item is IMember))
				return false;

			return CollectMembers (solution, (IMember)item, RefactoryScope.Unknown).Count () > 1;
		}
		
		public static IEnumerable<MemberReference> FindReferences (Solution solution, object member, bool searchForAllOverloads, RefactoryScope scope = RefactoryScope.Unknown, IProgressMonitor monitor = null)
		{
			if (member == null)
				yield break;
			if (solution == null && member is IEntity) {
				var project = TypeSystemService.GetProject ((member as IEntity).Compilation.MainAssembly.UnresolvedAssembly.Location);
				if (project == null)
					yield break;
				solution = project.ParentSolution;
			}
			
			IList<object> searchNodes = new [] { member };
			if (member is ITypeParameter) {
				// nothing
			} else if (member is IType) {
				searchNodes = CollectMembers ((IType)member).ToList<object> ();
			} else if (member is IEntity) {
				var e = (IEntity)member;
				if (e.EntityType == EntityType.Destructor) {
					foreach (var r in FindReferences (solution, e.DeclaringType, searchForAllOverloads, scope, monitor)) {
						yield return r;
					}
					yield break;
				}
				if (member is IMember && searchForAllOverloads)
					searchNodes = CollectMembers (solution, (IMember)member, scope).ToList<object> ();
			}
			// prepare references finder
			var preparedFinders = new List<Tuple<ReferenceFinder, Project, IProjectContent, List<FilePath>>> ();
			var curList = new List<FilePath> ();
			foreach (var info in GetFileNames (solution, member, scope, monitor, searchNodes)) {
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

		internal static IEnumerable<IMember> CollectMembers (Solution solution, IMember member, RefactoryScope scope)
		{
			return MemberCollector.CollectMembers (solution, member, scope);
		}
		
		internal static IEnumerable<IEntity> CollectMembers (IType type)
		{
			yield return (IEntity)type;
			foreach (var c in type.GetDefinition ().GetMembers (m => m.EntityType == EntityType.Constructor, GetMemberOptions.IgnoreInheritedMembers)) {
				if (!c.IsSynthetic)
					yield return c;
			}

			foreach (var m in type.GetMethods (m  => m.IsDestructor)) {
				yield return m;
			}
		}


		public enum RefactoryScope{ Unknown, File, DeclaringType, Solution, Project}
//		static RefactoryScope GetScope (object o)
//		{
//			IEntity node = o as IEntity;
//			if (node == null)
//				return RefactoryScope.File;
//			
//			// TODO: RefactoringsScope.Hierarchy
//			switch (node.Accessibility) {
//			case Accessibility.Public:
//			case Accessibility.Protected:
//			case Accessibility.ProtectedOrInternal:
//				if (node.DeclaringTypeDefinition != null) {
//					var scope = GetScope (node.DeclaringTypeDefinition);
//					if (scope != RefactoryScope.Solution)
//						return RefactoryScope.Project;
//				}
//				return RefactoryScope.Solution;
//			case Accessibility.Internal:
//			case Accessibility.ProtectedAndInternal:
//				return RefactoryScope.Project;
//			}
//			return RefactoryScope.DeclaringType;
//		}
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
