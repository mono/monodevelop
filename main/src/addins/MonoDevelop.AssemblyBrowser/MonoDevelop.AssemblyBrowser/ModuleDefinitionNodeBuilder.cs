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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using Mono.TextEditor;
using Mono.Cecil;
using System.Linq;

namespace MonoDevelop.AssemblyBrowser
{
	class ModuleDefinitionNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ModuleDefinition); }
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
			var module = (ModuleDefinition)dataObject;
			label      = module.Name;
			icon       = Context.GetIcon (Stock.OpenFolder);
			closedIcon = Context.GetIcon (Stock.ClosedFolder);
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			var module = (ModuleDefinition)dataObject;
			Dictionary<string, Namespace> namespaces = new Dictionary<string, Namespace> ();
			bool publicOnly = builder.Options ["PublicApiOnly"];
			var ctx = GetContent (builder);
			foreach (var ns in ctx.GetNamespaces ()) {
				var newSpace = new Namespace (ns);
				newSpace.Types.AddRange (ctx.GetClasses (ns, StringComparer.Ordinal).Where (c => !publicOnly || c.IsPublic ));
				if (newSpace.Types.Count > 0)
					namespaces [ns] = newSpace;
			}
			builder.AddChild (new ReferenceFolder (module));
			if (module.Resources.Count > 0)
				builder.AddChild (new ResourceFolder (module));
			foreach (Namespace ns in namespaces.Values) {
				builder.AddChild (ns);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		#region IAssemblyBrowserNodeBuilder
		void PrintModuleHeader (StringBuilder result, ModuleDefinition module)
		{
			result.Append (Ambience.SingleLineComment (
			                    String.Format (GettextCatalog.GetString ("Module <b>{0}</b>"),
			                    module.Name)));
			result.AppendLine ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			var module = (ModuleDefinition)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			PrintModuleHeader (result, module);
			
			result.Append (String.Format (GettextCatalog.GetString ("<b>Version:</b>\t{0}"),
			                              module.Mvid));
			result.AppendLine ();
			
			return result.ToString ();
		}
		
		public List<ReferenceSegment> Disassemble (TextEditorData data, ITreeNavigator navigator)
		{
			var module = (ModuleDefinition)navigator.DataItem;
			var result = new StringBuilder ();
			PrintModuleHeader (result, module);
			
			HashSet<string> namespaces = new HashSet<string> ();
			
			foreach (var type in module.Types) {
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
			var module = (ModuleDefinition)navigator.DataItem;
			return "<big>" + String.Format (GettextCatalog.GetString ("Module <b>{0}</b>"), module.Name) + "</big>";
		}
		#endregion
	}
}
