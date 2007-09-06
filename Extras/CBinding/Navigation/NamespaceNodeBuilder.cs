//
// NamespaceNodeBuilder.cs
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
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

using Mono.Addins;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects;

using CBinding.Parser;

namespace CBinding.Navigation
{
	public class NamespaceNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(Namespace); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof (LanguageItemCommandHandler); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			if (thisNode.Options["NestedNamespaces"])
				return ((Namespace)dataObject).Name;
			else
				return ((Namespace)dataObject).FullName;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder,
		                                object dataObject,
		                                ref string label,
		                                ref Gdk.Pixbuf icon,
		                                ref Gdk.Pixbuf closedIcon)
		{
			if (treeBuilder.Options["NestedNamespaces"])
				label = ((Namespace)dataObject).Name;
			else
				label = ((Namespace)dataObject).FullName;
			
			icon = Context.GetIcon (Stock.NameSpace);
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			CProject p = treeBuilder.GetParentDataItem (typeof(CProject), false) as CProject;
			
			if (p == null) return;
			
			ProjectInformation info = ProjectInformationManager.Instance.Get (p);
			
			Namespace thisNamespace = ((Namespace)dataObject);
			
			// Namespaces
			if (treeBuilder.Options["NestedNamespaces"])
				foreach (Namespace n in info.Namespaces)
					if (n.Parent != null && n.Parent.Equals (thisNamespace))
						treeBuilder.AddChild (n);
			
			// Classes
			foreach (Class c in info.Classes)
				if (c.Parent != null && c.Parent.Equals (thisNamespace))
					treeBuilder.AddChild (c);
			
			// Structures
			foreach (Structure s in info.Structures)
				if (s.Parent != null && s.Parent.Equals (thisNamespace))
					treeBuilder.AddChild (s);
			
			// Unions
			foreach (Union u in info.Unions)
				if (u.Parent != null && u.Parent.Equals (thisNamespace))
					treeBuilder.AddChild (u);
			
			// Enumerations
			foreach (Enumeration e in info.Enumerations)
				if (e.Parent != null && e.Parent.Equals (thisNamespace))
					treeBuilder.AddChild (e);
			
			// Typedefs
			foreach (Typedef t in info.Typedefs)
				if (t.Parent != null && t.Parent.Equals (thisNamespace))
					treeBuilder.AddChild (t);
			
			// Functions
			foreach (Function f in info.Functions)
				if (f.Parent != null && f.Parent.Equals (thisNamespace))
					treeBuilder.AddChild (f);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			return -1;
		}
	}
}
