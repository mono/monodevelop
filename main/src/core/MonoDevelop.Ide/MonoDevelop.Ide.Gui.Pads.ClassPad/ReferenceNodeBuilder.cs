//
// ReferenceNodeBuilder.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.IO;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public class ReferenceNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ProjectReference); }
		}

		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ClassPad/References"; }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ReferenceNodeCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((ProjectReference)dataObject).Reference;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			ProjectReference pref = (ProjectReference) dataObject;
			
			switch (pref.ReferenceType) {
				case ReferenceType.Project:
					label = pref.Reference;
					break;
				case ReferenceType.Assembly:
					label = Path.GetFileName(pref.Reference);
					break;
				case ReferenceType.Package:
					label = pref.Reference.Split(',')[0];
					break;
				default:
					label = pref.Reference;
					break;
			}
			
			icon = Context.GetIcon (Stock.Reference);
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			// TODO: Type system conversion.
/*			ProjectReference pref = (ProjectReference) dataObject;
			Dictionary<string, bool> namespaces = new Dictionary<string, bool> ();
			bool nestedNs = builder.Options ["NestedNamespaces"];
			foreach (string fileName in pref.GetReferencedFileNames (IdeApp.Workspace.ActiveConfiguration)) {
				var unit = new CecilLoader ().LoadAssemblyFile (fileName);
				if (unit == null)
					continue;
				foreach (var type in unit.GetTypes ()) {
					if (type.IsSynthetic)
						continue;
					if (String.IsNullOrEmpty (type.Namespace)) {
						builder.AddChild (new ClassData (unit, null, type));
						continue;
					}
					string ns = type.Namespace;
					if (nestedNs) {
						int idx = ns.IndexOf ('.');
						if (idx >= 0)
							ns = ns.Substring (0, idx);
					}
					if (namespaces.ContainsKey (ns))
						continue;
					namespaces[ns] = true;
					builder.AddChild (new CompilationUnitNamespaceData (null, ns));
				}
			}*/
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return false;
			//ProjectReference pref = (ProjectReference)dataObject;
			//return pref.ReferenceType != ReferenceType.Project;
		}
	}
	
	public class ReferenceNodeCommandHandler : NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			ProjectReference pref = CurrentNode.DataItem as ProjectReference;
			if (pref != null) {
				foreach (string fileName in pref.GetReferencedFileNames (IdeApp.Workspace.ActiveConfiguration))
					IdeApp.Workbench.OpenDocument (fileName);
			}
		}
	}
}
