//
// AssemblyNodeBuilder.cs
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

using Mono.Cecil;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.AssemblyBrowser
{
	public class AssemblyNodeBuilder : TypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(AssemblyDefinition); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)dataObject;
			return assemblyDefinition.Name.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)dataObject;
			label = assemblyDefinition.Name.Name;
			icon = Context.GetIcon (Stock.Reference);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)dataObject;
			
			foreach (ModuleDefinition moduleDefinition in assemblyDefinition.Modules) {
				ctx.AddChild (moduleDefinition);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)dataObject;
			return assemblyDefinition.Modules.Count > 0;
		}
		
		#region IAssemblyBrowserNodeBuilder
		static void PrintAssemblyHeader (StringBuilder result, AssemblyDefinition assemblyDefinition)
		{
			result.Append ("<span font_family=\"monospace\">");
			
			result.Append (AmbienceService.GetAmbience ("text/x-csharp").SingleLineComment (
                               String.Format (GettextCatalog.GetString ("Assembly <b>{0}</b>, Version {1}"),
			                                  assemblyDefinition.Name.Name,
			                                  assemblyDefinition.Name.Version)));
			result.Append ("</span>");
			result.AppendLine ();
		}
		
		static string GetTypeString (AssemblyKind kind)
		{
			switch (kind) {
			case AssemblyKind.Console:
				return GettextCatalog.GetString ("Console application");
			case AssemblyKind.Dll:
				return GettextCatalog.GetString ("Library");
			case AssemblyKind.Windows:
				return GettextCatalog.GetString ("Application");
			}
			return GettextCatalog.GetString ("Unknown");
		}
		
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			PrintAssemblyHeader (result, assemblyDefinition);
			
			result.Append (String.Format (GettextCatalog.GetString ("<b>Name:</b>\t{0}"),
			                              assemblyDefinition.Name.FullName));
			result.AppendLine ();
			result.Append (String.Format (GettextCatalog.GetString ("<b>Type:</b>\t{0}"),
			                              GetTypeString (assemblyDefinition.Kind)));
			result.AppendLine ();
			return result.ToString ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDisassembly (ITreeNavigator navigator)
		{
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			PrintAssemblyHeader (result, assemblyDefinition);
			foreach (CustomAttribute attr in assemblyDefinition.CustomAttributes) {
				result.Append (AmbienceService.GetAmbience ("text/x-csharp").GetString (new DomCecilAttribute (attr), OutputFlags.AssemblyBrowserDescription));
				result.AppendLine ();
			}
			return result.ToString ();
		}
		string IAssemblyBrowserNodeBuilder.GetDecompiledCode (ITreeNavigator navigator)
		{
			return "";
		}
		#endregion
	}
}
