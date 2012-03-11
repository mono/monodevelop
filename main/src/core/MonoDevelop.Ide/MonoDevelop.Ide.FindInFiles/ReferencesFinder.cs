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
		
		static IEnumerable<FileList> GetFileNames (Solution solution, IParsedFile unit, object member, RefactoryScope scope, IProgressMonitor monitor)
		{
			if (scope == RefactoryScope.Unknown)
				scope = GetScope (member);
			switch (scope) {
			case RefactoryScope.File:
			case RefactoryScope.DeclaringType:
				string fileName;
				if (member is IEntity) {
					fileName = ((IEntity)member).Region.FileName;
				} else {
					fileName = ((IVariable)member).Region.FileName;
				}
				var doc = IdeApp.Workbench.GetDocument (fileName);

				if (doc != null)
					yield return new FileList (doc.Project, doc.ProjectContent, new [] { (FilePath)unit.FileName });
				break;
			case RefactoryScope.Project:
				var prj = TypeSystemService.GetProject ((IEntity)member);
				if (prj == null)
					yield break;
				var ctx = TypeSystemService.GetProjectContext (prj);
				if (ctx == null)
					yield break;
				yield return new FileList (prj, ctx, prj.Files.Select (f => f.FilePath));
				break;
			case RefactoryScope.Solution:
				var allProjects = solution.GetAllProjects ();
				if (monitor != null)
					monitor.BeginTask (GettextCatalog.GetString ("Searching for references in solution..."), solution.GetAllProjects ().Count);
				var sourceProject = TypeSystemService.GetProject ((IEntity)member);
				foreach (var project in GetAllReferencingProjects (solution, sourceProject)) {
					if (monitor != null && monitor.IsCancelRequested)
						yield break;
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
			IParsedFile unit = null;
			IEnumerable<object> searchNodes = new [] { member };
			if (member is IType) {
				var declaringPart = ((IType)member).GetDefinition ().Parts.FirstOrDefault ();
				if (declaringPart != null)
					unit = declaringPart.ParsedFile;
				var nodes = new List<object> ();
				nodes.Add (member);
				nodes.AddRange (((IType)member).GetConstructors ());
				searchNodes = CollectMembers ((IType)member);
			} else if (member is IEntity) {
				var e = (IEntity)member;
				var declaringPart = e.DeclaringTypeDefinition.Parts.Where (p => p.Region.FileName == e.Region.FileName && p.Region.IsInside (e.Region.Begin)).FirstOrDefault ();
				if (declaringPart != null)
					unit = declaringPart.ParsedFile;
				if (member is IMethod)
					searchNodes = CollectMembers (solution, (IMethod)member);
			} else if (member is IVariable) { 
				var doc = IdeApp.Workbench.GetDocument (((IVariable)member).Region.FileName);
				unit = doc.ParsedDocument.ParsedFile;
			}
			
			// prepare references finder
			var preparedFinders = new List<Tuple<ReferenceFinder, Project, IProjectContent, List<FilePath>>> ();
			var curList = new List<FilePath> ();
			foreach (var info in GetFileNames (solution, unit, member, scope, monitor)) {
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
		
		internal static IEnumerable<IEntity> CollectMembers (Solution solution, IMethod member)
		{
			if (member.IsConstructor || member.IsDestructor || member.IsOperator)
				return new IEntity[] { member };
			
			// For renaming interface members search for all methods implementing the interface method.
			// also search for overrides of the method
			var declaringType = member.DeclaringType.GetDefinition ();
			var methods = new List<IMethod> (declaringType.GetMethods (m => m.Name == member.Name));
			var result = new List<IEntity> (methods);
			if (declaringType.Kind == TypeKind.Interface || (member.IsOverridable && declaringType.Kind == TypeKind.Class)) {
				foreach (var p in solution.GetAllSolutionItems<Project> ()) {
					var compilation = TypeSystemService.GetCompilation (p);
					//skip projects that are not in the same compilation
					//and avoid possible exception in IsDerivedFrom
					if (compilation != declaringType.Compilation) 
						continue;
					foreach (var type in compilation.GetAllTypeDefinitions ()) {
						if (!type.IsDerivedFrom (declaringType)) 
							continue;
						if (type.ReflectionName == declaringType.ReflectionName)
							continue;
						// Parameter list needs to match any parameter list from the interface.
						result.AddRange (type.GetMethods (m => m.Name == member.Name).Where (m => methods.Any (om => ParameterListComparer.Instance.Equals (om.Parameters, m.Parameters))));
					}
				}
			}
			
			return result;
		}
		
		internal static IEnumerable<IEntity> CollectMembers (IType type)
		{
			yield return (IEntity)type;

			foreach (var c in type.GetConstructors ()) {
				if (!c.IsSynthetic)
					yield return c;
			}
		}


		public enum RefactoryScope{ Unknown, File, DeclaringType, Solution, Project}
		static RefactoryScope GetScope (object o)
		{
			IEntity node = o as IEntity;
			if (node == null)
				return RefactoryScope.DeclaringType;
			
			if (node.DeclaringTypeDefinition != null && node.DeclaringTypeDefinition.Kind == TypeKind.Interface)
				return GetScope (node.DeclaringTypeDefinition);
			
			if ((node.Accessibility & Accessibility.Public) == Accessibility.Public)
				return RefactoryScope.Solution;
			
			// TODO: RefactoringsScope.Hierarchy
			if ((node.Accessibility & Accessibility.Protected) == Accessibility.Protected)
				return RefactoryScope.Solution;
			if ((node.Accessibility & Accessibility.Internal) == Accessibility.Protected)
				return RefactoryScope.Project;
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
