//
// ModuleDefinitionNodeBuilder.cs
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
using System.Text;
using System.Collections.Generic;

using Mono.Cecil;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.AssemblyBrowser.Dom;

namespace MonoDevelop.AssemblyBrowser
{
	public class ModuleDefinitionNodeBuilder : TypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ModuleDefinition); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return (string)dataObject;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			ModuleDefinition moduleDefinition = (ModuleDefinition)dataObject;
			label = moduleDefinition.Name;
			icon       = Context.GetIcon (Stock.OpenFolder);
			closedIcon = Context.GetIcon (Stock.ClosedFolder);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			ModuleDefinition moduleDefinition = (ModuleDefinition)dataObject;
			Dictionary<string, Namespace> namespaces = new Dictionary<string, Namespace> ();
			foreach (TypeDefinition type in moduleDefinition.Types) {
				if (!namespaces.ContainsKey (type.Namespace))
					namespaces [type.Namespace] = new Namespace (type.Namespace);
				namespaces [type.Namespace].Types.Add (new DomCecilType (type));
			}
			ctx.AddChild (new ReferenceFolder (moduleDefinition));
			if (moduleDefinition.Resources.Count > 0)
				ctx.AddChild (new ResourceFolder (moduleDefinition));
			foreach (Namespace ns in namespaces.Values) {
				ctx.AddChild (ns);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		#region IAssemblyBrowserNodeBuilder
		static void PrintModuleHeader (StringBuilder result, ModuleDefinition moduleDefinition)
		{
			result.Append ("<span font_family=\"monospace\">");
			result.Append (AmbienceService.Default.SingleLineComment (
			                    String.Format (GettextCatalog.GetString ("Module <b>{0}</b>"),
			                    moduleDefinition.Name)));
			result.Append ("</span>");
			result.AppendLine ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			ModuleDefinition moduleDefinition = (ModuleDefinition)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			PrintModuleHeader (result, moduleDefinition);
			
			result.Append (String.Format (GettextCatalog.GetString ("<b>Version:</b>\t{0}"),
			                              moduleDefinition.Mvid));
			result.AppendLine ();
			return result.ToString ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDisassembly (ITreeNavigator navigator)
		{
			ModuleDefinition moduleDefinition = (ModuleDefinition)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			PrintModuleHeader (result, moduleDefinition);
			return result.ToString ();
		}
		#endregion		
	}
}
