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

using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using Mono.TextEditor;

namespace MonoDevelop.AssemblyBrowser
{
	class ModuleDefinitionNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(DomCecilCompilationUnit.Module); }
		}
		
		public ModuleDefinitionNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
			
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return dataObject != null ? dataObject.ToString () : "null module";
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			DomCecilCompilationUnit.Module module = (DomCecilCompilationUnit.Module)dataObject;
			label      = module.ModuleDefinition.Name;
			icon       = Context.GetIcon (Stock.OpenFolder);
			closedIcon = Context.GetIcon (Stock.ClosedFolder);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			DomCecilCompilationUnit.Module module = (DomCecilCompilationUnit.Module)dataObject;
			Dictionary<string, Namespace> namespaces = new Dictionary<string, Namespace> ();
			bool publicOnly = ctx.Options ["PublicApiOnly"];
			foreach (IType type in module.Types) {
				if (publicOnly && !type.IsPublic)
					continue;
				if (!namespaces.ContainsKey (type.Namespace))
					namespaces [type.Namespace] = new Namespace (type.Namespace);
				namespaces [type.Namespace].Types.Add (type);
			}
			ctx.AddChild (new ReferenceFolder (module.ModuleDefinition));
			if (module.ModuleDefinition.Resources.Count > 0)
				ctx.AddChild (new ResourceFolder (module.ModuleDefinition));
			foreach (Namespace ns in namespaces.Values) {
				ctx.AddChild (ns);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		#region IAssemblyBrowserNodeBuilder
		void PrintModuleHeader (StringBuilder result, DomCecilCompilationUnit.Module module)
		{
			result.Append (Ambience.SingleLineComment (
			                    String.Format (GettextCatalog.GetString ("Module <b>{0}</b>"),
			                    module.ModuleDefinition.Name)));
			result.AppendLine ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			DomCecilCompilationUnit.Module module = (DomCecilCompilationUnit.Module)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			PrintModuleHeader (result, module);
			
			result.Append (String.Format (GettextCatalog.GetString ("<b>Version:</b>\t{0}"),
			                              module.ModuleDefinition.Mvid));
			result.AppendLine ();
			
			return result.ToString ();
		}
		
		public List<ReferenceSegment> Disassemble (TextEditorData data, ITreeNavigator navigator)
		{
			DomCecilCompilationUnit.Module module = (DomCecilCompilationUnit.Module)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			PrintModuleHeader (result, module);
			
			HashSet<string> namespaces = new HashSet<string> ();
			
			foreach (IType type in module.Types) {
/*				if ((type.Attributes & TypeAttributes.NestedPrivate) == TypeAttributes.NestedPrivate)
					continue;*/
				if (String.IsNullOrEmpty (type.Namespace))
					continue;
				namespaces.Add (type.Namespace);
			}
			
			foreach (string ns in namespaces) {
				result.Append ("namespace ");
				result.Append (ns);
				result.AppendLine ();
			}
			
			data.Text = result.ToString ();
			return null;
		}
		public List<ReferenceSegment> Decompile (TextEditorData data, ITreeNavigator navigator)
		{
			return Disassemble (data, navigator);
		}
		public string GetDocumentationMarkup (ITreeNavigator navigator)
		{
			DomCecilCompilationUnit.Module module = (DomCecilCompilationUnit.Module)navigator.DataItem;
			return "<big>" + String.Format (GettextCatalog.GetString ("Module <b>{0}</b>"), module.ModuleDefinition.Name) + "</big>";
		}
		#endregion
	}
}
