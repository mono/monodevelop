//
// NamespaceBuilder.cs
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
using System.Text;

using Mono.Cecil;

using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.AssemblyBrowser
{
	public class NamespaceBuilder : TypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(Namespace); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			Namespace ns = (Namespace)dataObject;
			return ns.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Namespace ns = (Namespace)dataObject;
			label = ns.Name;
			icon = Context.GetIcon (Stock.NameSpace);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			Namespace ns = (Namespace)dataObject;
			foreach (IType type in ns.Types) {
				ctx.AddChild (type);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		#region IAssemblyBrowserNodeBuilder
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			Namespace ns = (Namespace)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			if (!String.IsNullOrEmpty (ns.Name)) {
				result.Append ("<span font_family=\"monospace\">");
				result.Append (AmbienceService.GetAmbience ("text/x-csharp").GetString (ns.Name, OutputFlags.AssemblyBrowserDescription));
				result.Append ("</span>");
				result.AppendLine ();
			}
			DomTypeNodeBuilder.PrintAssembly (result, navigator);
			return result.ToString ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDisassembly (ITreeNavigator navigator)
		{
			Namespace ns = (Namespace)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			Ambience ambience = AmbienceService.GetAmbience ("text/x-csharp");
			if (!String.IsNullOrEmpty (ns.Name)) {
				result.Append (ambience.GetString (ns.Name, OutputFlags.AssemblyBrowserDescription));
				result.AppendLine ();
			}
			foreach (IType type in ns.Types) {
				if (!String.IsNullOrEmpty (ns.Name))
					result.Append ("\t");
				result.Append (ambience.GetString (type, OutputFlags.AssemblyBrowserDescription));
				result.AppendLine ();
			}
			result.AppendLine ();
			return result.ToString ();
		}
		#endregion
	}
}
