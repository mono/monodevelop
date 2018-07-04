//
// ProjectNodeBuilder.cs
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.TypeSystem;
using Project = MonoDevelop.Projects.Project;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public class ProjectNodeBuilder : TypeNodeBuilder
	{
		protected override void Initialize ()
		{
			//			TypeSystemService.TypesUpdated += OnClassInformationChanged;
		}

		public override void Dispose ()
		{
			//			TypeSystemService.TypesUpdated -= OnClassInformationChanged;
		}

		public override Type NodeDataType {
			get { return typeof (Project); }
		}

		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ClassPad/Project"; }
		}

		public override void OnNodeAdded (object dataObject)
		{
			Project project = (Project)dataObject;
			project.NameChanged += OnProjectRenamed;
		}

		public override void OnNodeRemoved (object dataObject)
		{
			Project project = (Project)dataObject;
			project.NameChanged -= OnProjectRenamed;
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Project)dataObject).Name;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			Project p = dataObject as Project;
			nodeInfo.Label = p.Name;
			nodeInfo.Icon = Context.GetIcon (p.StockIcon);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = (Project)dataObject;
			BuildChildNodes (builder, project);
		}

		public static void BuildChildNodes (ITreeBuilder builder, Project project)
		{
			if (project is DotNetProject) {
				builder.AddChild (((DotNetProject)project).References);
			}
			bool publicOnly = builder.Options ["PublicApiOnly"];
			var dom = TypeSystemService.GetCompilationAsync (project).Result;
			if (dom == null)
				return;
			bool nestedNamespaces = builder.Options ["NestedNamespaces"];
			HashSet<string> addedNames = new HashSet<string> ();
			foreach (var ns in dom.Assembly.GlobalNamespace.GetNamespaceMembers ()) {
				if (nestedNamespaces) {
					if (!addedNames.Contains (ns.Name)) {
						builder.AddChild (new ProjectNamespaceData (project, ns));
						addedNames.Add (ns.Name);
					}
				} else {
					FillNamespaces (builder, project, ns);
				}
			}
			builder.AddChildren (dom.Assembly.GlobalNamespace.GetTypeMembers ()
								 .Where (type => !publicOnly || type.DeclaredAccessibility == Accessibility.Public)
								 .Where (t => !t.IsImplicitClass)
								 .Select (type => new ClassData (project, type)));
		}

		public static void FillNamespaces (ITreeBuilder builder, Project project, INamespaceSymbol ns)
		{
			var members = ns.GetTypeMembers ();
			//IParserContext ctx = IdeApp.Workspace.ParserDatabase.GetProjectParserContext (project);
			if (members.Any ()) {
				if (builder.Options ["ShowProjects"])
					builder.AddChild (new ProjectNamespaceData (project, ns));
				else {
					if (!builder.HasChild (ns.Name, typeof (NamespaceData)))
						builder.AddChild (new ProjectNamespaceData (null, ns));
				}
			}
			foreach (var nSpace in ns.GetNamespaceMembers ()) {
				FillNamespaces (builder, project, nSpace);
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		void OnProjectRenamed (object sender, SolutionItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null) tb.Update ();
		}

		//		void OnClassInformationChanged (object sender, TypeUpdateInformationEventArgs e)
		//		{
		////			DateTime t = DateTime.Now;
		//			Dictionary<object,bool> oldStatus = new Dictionary<object,bool> ();
		//			List<string> namespacesToClean = new List<string> ();
		//			ITreeBuilder tb = Context.GetTreeBuilder ();
		//						
		//			foreach (IType cls in e.TypeUpdateInformation.Removed) {
		//				if (tb.MoveToObject (new ClassData (e.Project, cls))) {
		//					oldStatus [tb.DataItem] = tb.Expanded;
		//					
		//					ITreeNavigator np = tb.Clone ();
		//					np.MoveToParent ();
		//					oldStatus [np.DataItem] = np.Expanded;
		//					
		//					tb.Remove (true);
		//				}
		//				namespacesToClean.Add (cls.Namespace);
		//			}
		//			
		//			foreach (IType cls in e.TypeUpdateInformation.Modified) {
		//				ClassData ucd = new ClassData (e.Project, cls);
		//				if (tb.MoveToObject (ucd)) {
		//					ClassData cd = (ClassData) tb.DataItem;
		//					cd.UpdateFrom (ucd);
		//					tb.UpdateAll ();
		//				}
		//			}
		//			
		//			foreach (IType cls in e.TypeUpdateInformation.Added) {
		//				AddClass (e.Project, cls);
		//			}
		//			
		//			// Clean empty namespaces
		//			
		//			foreach (string ns in namespacesToClean) {
		//				string subns = ns;
		//				while (subns != null) {
		//					bool found = tb.MoveToObject (new ProjectNamespaceData (e.Project, subns));
		//					if (!found) found = tb.MoveToObject (new ProjectNamespaceData (null, subns));
		//					if (found) {
		//						while (tb.DataItem is NamespaceData && !tb.HasChildren())
		//							tb.Remove (true);
		//						break;
		//					}
		//					int i = subns.LastIndexOf ('.');
		//					if (i != -1) subns = subns.Substring (0,i);
		//					else subns = null;
		//				}
		//			}
		//			
		//			// Restore expand status
		//			
		//			foreach (KeyValuePair<object,bool> de in oldStatus) {
		//				if (de.Value && tb.MoveToObject (de.Key)) {
		//					tb.ExpandToNode ();
		//					tb.Expanded = true;
		//				}
		//			}
		//		}

		void AddClass (Project project, ITypeSymbol cls)
		{
			ITreeBuilder builder = Context.GetTreeBuilder ();
			if (!builder.MoveToObject (project)) {
				return; // The project is not there or may not yet be expanded
			}

			if (cls.ContainingNamespace == null) {
				builder.AddChild (new ClassData (project, cls));
			} else {
				// TODO: Type system conversion.
				/*				if (builder.Options ["NestedNamespaces"]) {
									string[] nsparts = cls.Namespace.Split ('.');
									string ns = "";
									foreach (string nsp in nsparts) {
										if (builder.Filled) {
											if (ns.Length > 0) ns += ".";
											ns += nsp;
											if (!builder.MoveToChild (nsp, typeof(NamespaceData))) {
												builder.AddChild (new ProjectNamespaceData (project, ns), true);
												break;
											}
										} else
											break;
									}
									builder.AddChild (new ClassData (project, cls));
								} else {
									if (builder.MoveToChild (cls.Namespace, typeof(NamespaceData)))
										builder.AddChild (new ClassData (project, cls));
									else
										builder.AddChild (new ProjectNamespaceData (project, cls.Namespace));
								}*/
			}
		}
	}
}
