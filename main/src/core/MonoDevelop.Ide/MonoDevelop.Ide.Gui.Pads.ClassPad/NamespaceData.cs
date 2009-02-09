//
// NamespaceData.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Collections.Generic;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public abstract class NamespaceData
	{
		protected string namesp;
		
		public string Name {
			get {
				int i = namesp.LastIndexOf (".");
				if (i != -1) return namesp.Substring (i+1);
				else return namesp;
			}
		}
		
		public string FullName {
			get { return namesp; }
		}
		
		public NamespaceData (string fullNamespace)
		{
			namesp = fullNamespace;
		}
		
		public abstract void AddProjectContent (ITreeBuilder builder);
		
		public override bool Equals (object ob)
		{
			NamespaceData other = ob as NamespaceData;
			return (other != null && namesp == other.namesp);
		}
		
		public override int GetHashCode ()
		{
			return namesp.GetHashCode ();
		}
		
		public override string ToString ()
		{
			return base.ToString () + " [" + namesp + "]";
		}
	}
	
	public class ProjectNamespaceData : NamespaceData
	{
		Project project;
		
		public Project Project {
			get { return project; }
		}

		public ProjectNamespaceData (Project project, string fullNamespace) : base (fullNamespace)
		{
			this.project = project;
		}
		
		public override void AddProjectContent (ITreeBuilder builder)
		{
			if (project != null) {
				ProjectDom dom = ProjectDomService.GetProjectDom (Project);
				AddProjectContent (builder, dom.GetNamespaceContents (FullName, false, false));
			} else {
				foreach (Project p in IdeApp.Workspace.GetAllProjects ()) {
					ProjectDom dom = ProjectDomService.GetProjectDom (p);
					AddProjectContent (builder, dom.GetNamespaceContents (FullName, false, false));
				}
			}
		}
		
		void AddProjectContent (ITreeBuilder builder, List<IMember> list)
		{
			bool nestedNs = builder.Options ["NestedNamespaces"];
			bool publicOnly = builder.Options ["PublicApiOnly"];

			foreach (IMember ob in list) {
				if (ob is Namespace && nestedNs) {
					Namespace nsob = (Namespace)ob;
					string ns = FullName + "." + nsob.Name;
					if (!builder.HasChild (nsob.Name, typeof(NamespaceData)))
						builder.AddChild (new ProjectNamespaceData (project, ns));
				}
				else if (ob is IType) {
					if (!publicOnly || ((IType)ob).IsPublic)
						builder.AddChild (new ClassData (project, ob as IType));
				}
			}
		}
		
		public override bool Equals (object ob)
		{
			ProjectNamespaceData other = ob as ProjectNamespaceData;
			return (other != null && namesp == other.namesp && project == other.project);
		}
		
		public override int GetHashCode ()
		{
			if (project != null) return (namesp + project.Name).GetHashCode ();
			else return namesp.GetHashCode ();
		}
		
		public override string ToString ()
		{
			return base.ToString () + " [" + namesp + ", " + (project != null ? project.Name : "no project") + "]";
		}
	}
	
	public class CompilationUnitNamespaceData : NamespaceData
	{
		ICompilationUnit unit;
		
		public CompilationUnitNamespaceData (ICompilationUnit unit, string fullNamespace) : base (fullNamespace)
		{
			this.unit = unit;
		}

		public override void AddProjectContent (ITreeBuilder builder)
		{
			bool nestedNs = builder.Options ["NestedNamespaces"];
			bool publicOnly = builder.Options ["PublicApiOnly"];
			Dictionary<string, bool> namespaces = new Dictionary<string, bool> ();
			foreach (IType type in unit.Types) {
				if (publicOnly && !type.IsPublic)
					continue;
				if (type.Namespace == FullName) {
					builder.AddChild (new ClassData (null, type));
					continue;
				}
				if (nestedNs && type.Namespace.StartsWith (FullName)) {
					string ns = type.Namespace.Substring (FullName.Length + 1);
					int idx = ns.IndexOf ('.');
					if (idx >= 0)
						ns = ns.Substring (0, idx);
					if (namespaces.ContainsKey (ns))
						continue;
					namespaces[ns] = true;
					builder.AddChild (new CompilationUnitNamespaceData (unit, FullName + "." + ns));
				}
			}
		}
	}
}
