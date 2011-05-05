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
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
using System.Linq;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects;

namespace MonoDevelop.Ide.FindInFiles
{
	public abstract class ReferenceFinder
	{
		public bool IncludeDocumentation {
			get;
			set;
		}
		
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
		
		public static ReferenceFinder GetReferenceFinder (string mimeType)
		{
			var codon = referenceFinderCodons.FirstOrDefault (c => c.SupportedMimeTypes.Any (mt => mt == mimeType));
			return codon != null ? codon.ReferenceFinder : null;
		}
		
		
		public static IEnumerable<MemberReference> FindReferences (INode member, IProgressMonitor monitor = null)
		{
			return FindReferences (IdeApp.ProjectOperations.CurrentSelectedSolution, member, monitor);
		}
		
		
		static IEnumerable<Tuple<ProjectDom, FilePath>> GetFileNames (Solution solution, ProjectDom dom, ICompilationUnit unit, INode member, IProgressMonitor monitor)
		{
			var scope = GetScope (member);
			switch (scope) {
			case RefactoryScope.File:
			case RefactoryScope.DeclaringType:
				if (dom != null && unit != null)
					yield return Tuple.Create (dom, unit.FileName);
				break;
			case RefactoryScope.Project:
				if (dom == null)
					yield break;
				if (monitor != null)
					monitor.BeginTask (GettextCatalog.GetString ("Search reference in project..."), dom.Project.Files.Count);
				int counter = 0;
				foreach (var file in dom.Project.Files) {
					if (monitor != null && monitor.IsCancelRequested)
						yield break;
					yield return Tuple.Create (dom, file.FilePath);
					if (monitor != null) {
						if (counter % 10 == 0)
							monitor.Step (10);
						counter++;
					}
				}
				if (monitor != null)
					monitor.EndTask ();
				break;
			case RefactoryScope.Solution:
				if (monitor != null)
					monitor.BeginTask (GettextCatalog.GetString ("Search reference in solution..."), solution.GetAllProjects ().Count);
				foreach (var project in solution.GetAllProjects ()) {
					if (monitor != null && monitor.IsCancelRequested)
						yield break;
					var currentDom = ProjectDomService.GetProjectDom (project);
					foreach (var file in project.Files) {
						if (monitor != null && monitor.IsCancelRequested)
							yield break;
						yield return Tuple.Create (currentDom, file.FilePath);
					}
					if (monitor != null)
						monitor.Step (1);
				}
				if (monitor != null)
					monitor.EndTask ();
				break;
			}
		}
		
		public static IEnumerable<MemberReference> FindReferences (Solution solution, INode member, IProgressMonitor monitor = null)
		{
			ProjectDom dom = null;
			ICompilationUnit unit = null;
			IEnumerable<INode > searchNodes = new INode[] { member };
			if (member is LocalVariable) {
				dom = ((LocalVariable)member).DeclaringMember.DeclaringType.SourceProjectDom;
				unit = ((LocalVariable)member).CompilationUnit;
			} else if (member is IParameter) {
				dom = ((IParameter)member).DeclaringMember.DeclaringType.SourceProjectDom;
				unit = ((IParameter)member).DeclaringMember.DeclaringType.CompilationUnit;
			} else if (member is IType) {
				dom = ((IType)member).SourceProjectDom;
				unit = ((IType)member).CompilationUnit;
			} else if (member is IMember) {
				dom = ((IMember)member).DeclaringType.SourceProjectDom;
				unit = ((IMember)member).DeclaringType.CompilationUnit;
				searchNodes = CollectMembers (dom, (IMember)member);
			}

			string currentMime = null;
			ReferenceFinder finder = null;
			
			foreach (var info in GetFileNames (solution, dom, unit, member, monitor)) {
				if (monitor != null && monitor.IsCancelRequested)
					yield break;
				string mime = DesktopService.GetMimeTypeForUri (info.Item2);
				if (mime != currentMime) {
					currentMime = mime;
					finder = GetReferenceFinder (currentMime);
				}
				if (finder == null)
					continue;
				foreach (var foundReference in finder.FindReferences (info.Item1, info.Item2, searchNodes)) {
					if (monitor != null && monitor.IsCancelRequested)
						yield break;
					yield return foundReference;
				}
			}
		}
		
		public abstract IEnumerable<MemberReference> FindReferences (ProjectDom dom, FilePath fileName, IEnumerable<INode> searchedMembers);
		
		internal static IEnumerable<INode> CollectMembers (ProjectDom dom, IMember member)
		{
			if (member is IMethod && ((IMethod)member).IsConstructor) {
				yield return member;
			} else {
				bool isOverrideable = member.DeclaringType.ClassType == ClassType.Interface || member.IsOverride || member.IsVirtual || member.IsAbstract;
				bool isLastMember = false;
				// for members we need to collect the whole 'class' of members (overloads & implementing types)
				HashSet<string> alreadyVisitedTypes = new HashSet<string> ();
				foreach (IType type in dom.GetInheritanceTree (member.DeclaringType)) {
					if (type.ClassType == ClassType.Interface || isOverrideable || type.DecoratedFullName == member.DeclaringType.DecoratedFullName) {
						// search in the class for the member
						foreach (IMember interfaceMember in type.SearchMember (member.Name, true)) {
							if (interfaceMember.MemberType == member.MemberType)
								yield return interfaceMember;
						}
						
						// now search in all subclasses of this class for the member
						isLastMember = !member.IsOverride;
						foreach (IType implementingType in dom.GetSubclasses (type)) {
							string name = implementingType.DecoratedFullName;
							if (alreadyVisitedTypes.Contains (name))
								continue;
							alreadyVisitedTypes.Add (name);
							foreach (IMember typeMember in implementingType.SearchMember (member.Name, true)) {
								if (typeMember.MemberType == member.MemberType) {
									isLastMember = type.ClassType != ClassType.Interface && (typeMember.IsVirtual || typeMember.IsAbstract || !typeMember.IsOverride);
									yield return typeMember;
								}
							}
							if (!isOverrideable)
								break;
						}
						if (isLastMember)
							break;
					}
				}
			}
		}
		
		static RefactoryScope GetScope (INode node)
		{
			IMember member = node as IMember;
			if (member == null)
				return RefactoryScope.DeclaringType;
			
			if (member.DeclaringType != null && member.DeclaringType.ClassType == ClassType.Interface)
				return GetScope (member.DeclaringType);
			
			if (member.IsPublic)
				return RefactoryScope.Solution;
			
			if (member.IsProtected || member.IsInternal || member.DeclaringType == null)
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
		
		public ReferenceFinder ReferenceFinder {
			get {
				return (ReferenceFinder)GetInstance ();
			}
		}
		
		public override string ToString ()
		{
			return string.Format ("[ReferenceFinderCodon: SupportedMimeTypes={0}, ReferenceFinder={1}]", SupportedMimeTypes.Count () + ":" + String.Join (";", SupportedMimeTypes), ReferenceFinder);
		}
	}
}
