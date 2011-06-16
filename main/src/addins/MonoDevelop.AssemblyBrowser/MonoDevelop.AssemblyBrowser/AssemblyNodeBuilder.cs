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
using System.Linq;
using System.Text;

using Mono.Cecil;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;
using Mono.TextEditor;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;

namespace MonoDevelop.AssemblyBrowser
{
	class AssemblyNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(Tuple<AssemblyDefinition, IProjectContent>); }
		}
		
		public AssemblyNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var compilationUnit = ((Tuple<AssemblyDefinition, IProjectContent>)dataObject).Item1;
			return compilationUnit.Name.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			var compilationUnit = ((Tuple<AssemblyDefinition, IProjectContent>)dataObject).Item1;
			label = compilationUnit.Name.Name;
			icon = Context.GetIcon (Stock.Reference);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			var compilationUnit = ((Tuple<AssemblyDefinition, IProjectContent>)dataObject).Item1;
			
			foreach (var module in compilationUnit.Modules) {
				ctx.AddChild (module);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var compilationUnit = ((Tuple<AssemblyDefinition, IProjectContent>)dataObject).Item1;
			return compilationUnit.Modules.Any ();
		}
		
		#region IAssemblyBrowserNodeBuilder
		void PrintAssemblyHeader (StringBuilder result, AssemblyDefinition assemblyDefinition)
		{
			result.Append ("<span style=\"comment\">");
			result.Append (Ambience.SingleLineComment (
                               String.Format (GettextCatalog.GetString ("Assembly <b>{0}</b>, Version {1}"),
			                                  assemblyDefinition.Name.Name,
			                                  assemblyDefinition.Name.Version)));
			result.Append ("</span>");
			result.AppendLine ();
		}
		
		static string GetTypeString (ModuleKind kind)
		{
			switch (kind) {
			case ModuleKind.Console:
				return GettextCatalog.GetString ("Console application");
			case ModuleKind.Dll:
				return GettextCatalog.GetString ("Library");
			case ModuleKind.Windows:
				return GettextCatalog.GetString ("Application");
			}
			return GettextCatalog.GetString ("Unknown");
		}
		
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			var compilationUnit = ((Tuple<AssemblyDefinition, IProjectContent>)navigator.DataItem).Item1;
			StringBuilder result = new StringBuilder ();
			PrintAssemblyHeader (result, compilationUnit);
			
			result.Append (String.Format (GettextCatalog.GetString ("<b>Name:</b>\t{0}"),
			                              compilationUnit.Name.FullName));
			result.AppendLine ();
			result.Append (String.Format (GettextCatalog.GetString ("<b>Type:</b>\t{0}"),
			                              GetTypeString (compilationUnit.MainModule.Kind)));
			result.AppendLine ();
			return result.ToString ();
		}

		public List<ReferenceSegment> Disassemble (TextEditorData data, ITreeNavigator navigator)
		{
			var compilationUnit = ((Tuple<AssemblyDefinition, IProjectContent>)navigator.DataItem).Item1;
			return DomMethodNodeBuilder.Decompile (data, DomMethodNodeBuilder.GetModule (navigator), null, b => b.AddAssembly (compilationUnit, true));
		}
		
		
		public List<ReferenceSegment> Decompile (TextEditorData data, ITreeNavigator navigator)
		{
			return Disassemble (data, navigator);
		}
		
		public string GetDocumentationMarkup (ITreeNavigator navigator)
		{
			return null;
		}
		#endregion
	}
}
