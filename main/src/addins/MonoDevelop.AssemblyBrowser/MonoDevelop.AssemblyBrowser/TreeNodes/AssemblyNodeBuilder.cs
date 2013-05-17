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
using System.IO;

namespace MonoDevelop.AssemblyBrowser
{
	class AssemblyNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(AssemblyLoader); }
		}
		
		public AssemblyNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var loader = (AssemblyLoader)dataObject;
			return Path.GetFileNameWithoutExtension (loader.FileName);
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			var compilationUnit = (AssemblyLoader)dataObject;
			
			label = Path.GetFileNameWithoutExtension (compilationUnit.FileName);
			icon = Context.GetIcon (Stock.Reference);
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			var compilationUnit = (AssemblyLoader)dataObject;
			
			var references = new AssemblyReferenceFolder (compilationUnit.Assembly);
			if (references.AssemblyReferences.Any () || references.ModuleReferences.Any ())
				builder.AddChild (references);
			
			var resources = new AssemblyResourceFolder (compilationUnit.Assembly);
			if (resources.Resources.Any ())
				builder.AddChild (resources);
			
			var namespaces = new Dictionary<string, Namespace> ();
			bool publicOnly = Widget.PublicApiOnly;
			
			foreach (var type in compilationUnit.UnresolvedAssembly.TopLevelTypeDefinitions) {
				string namespaceName = string.IsNullOrEmpty (type.Namespace) ? "-" : type.Namespace;
				if (!namespaces.ContainsKey (namespaceName))
					namespaces [namespaceName] = new Namespace (namespaceName);
				
				var ns = namespaces [namespaceName];
				ns.Types.Add (type);
			}
			
			foreach (var ns in namespaces.Values) {
				if (publicOnly && !ns.Types.Any (t => t.IsPublic))
					continue;
				builder.AddChild (ns);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var compilationUnit = (AssemblyLoader)dataObject;
			return compilationUnit.Assembly.MainModule.HasTypes;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			try {
				if (thisNode == null || otherNode == null)
					return -1;
				var e1 = thisNode.DataItem as AssemblyLoader;
				var e2 = otherNode.DataItem as AssemblyLoader;
				
				if (e1 == null && e2 == null)
					return 0;
				if (e1 == null)
					return 1;
				if (e2 == null)
					return -1;
				
				return e1.Assembly.Name.Name.CompareTo (e2.Assembly.Name.Name);
			} catch (Exception e) {
				LoggingService.LogError ("Exception in assembly browser sort function.", e);
				return -1;
			}
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
		
		public List<ReferenceSegment> Disassemble (TextEditorData data, ITreeNavigator navigator)
		{
			var assembly = ((AssemblyLoader)navigator.DataItem).UnresolvedAssembly;
			var compilationUnit = Widget.CecilLoader.GetCecilObject (assembly);
			if (compilationUnit == null) {
				LoggingService.LogError ("Can't get cecil object for assembly:" + assembly);
				return new List<ReferenceSegment> ();
			}
			return DomMethodNodeBuilder.Decompile (data, DomMethodNodeBuilder.GetModule (navigator), null, b => {
				if (b != null)
					b.AddAssembly (compilationUnit, true);
			});
		}
		
		
		public List<ReferenceSegment> Decompile (TextEditorData data, ITreeNavigator navigator, bool publicOnly)
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
