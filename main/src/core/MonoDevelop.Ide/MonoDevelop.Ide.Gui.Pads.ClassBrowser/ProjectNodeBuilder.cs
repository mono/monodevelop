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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Ide.Dom;
using MonoDevelop.Ide.Dom.Parser;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;


namespace MonoDevelop.Ide.Gui.Pads.ClassBrowser
{
	public class ProjectNodeBuilder : TypeNodeBuilder
	{
		SolutionItemRenamedEventHandler projectNameChanged;
		EventHandler domLoaded;
		
		public ProjectNodeBuilder ()
		{
			projectNameChanged = (SolutionItemRenamedEventHandler) DispatchService.GuiDispatch (new SolutionItemRenamedEventHandler (OnProjectRenamed));
			domLoaded = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnProjectDomLoaded));
		}
		
		public override Type NodeDataType {
			get { return typeof(Project); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ClassPad/Project"; }
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			Project project = (Project) dataObject;
			ProjectDom dom = ProjectDomService.GetDom (project);
			dom.Loaded += domLoaded;
			project.NameChanged += projectNameChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			Project project = (Project) dataObject;
			ProjectDom dom = ProjectDomService.GetDom (project);
			dom.Loaded -= domLoaded;
			project.NameChanged -= projectNameChanged;
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Project)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Project p = dataObject as Project;
			label = p.Name;
			string iconName = Services.Icons.GetImageForProjectType (p.ProjectType);
			icon = Context.GetIcon (iconName);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = (Project) dataObject;
			BuildChildNodes (builder, project);
		}
		
		public static void BuildChildNodes (ITreeBuilder builder, Project project)
		{
			bool publicOnly = builder.Options ["PublicApiOnly"];
			
			ProjectDom dom = ProjectDomService.GetDom (project);
			
			Dictionary <string, ProjectNamespace> namespaces = new Dictionary<string, ProjectNamespace> ();
			
			foreach (CompilationUnit unit in dom.CompilationUnits) {
				foreach (IType type in unit.Types) {
					if (String.IsNullOrEmpty (type.Namespace)) {
						builder.AddChild (type);
					} else {
						if (!namespaces.ContainsKey (type.Namespace)) {
							namespaces [type.Namespace] = new ProjectNamespace (type.Namespace);
						}
						namespaces [type.Namespace].Types.Add (type);
					}
				}
			}
			foreach (ProjectNamespace ns in namespaces.Values) {
				builder.AddChild (ns);
			}
		}
			
		internal class ProjectNamespace
		{
			string      name;
			List<IType> types = new List<IType> ();
			
			public string Name {
				get {
					return name;
				}
			}
				
			public List<IType> Types {
				get {
					return types;
				}
			}
			
			public ProjectNamespace (string name)
			{
				this.name = name;
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		void OnProjectRenamed (object sender, SolutionItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null)
				tb.Update ();
		}
		
		void OnProjectDomLoaded (object sender, EventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder ();
			if (tb != null)
				tb.Update ();
		}
	}
}
