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

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Components;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Ide.Editor;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Metadata;
using System.Threading.Tasks;

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
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var compilationUnit = (AssemblyLoader)dataObject;

			nodeInfo.Label = GetMarkup (compilationUnit.Assembly);
			nodeInfo.Icon = Context.GetIcon (Stock.Reference);
		}

		static string GetMarkup (PEFile assembly)
		{
			var sb = StringBuilderCache.Allocate ();

			var metadata = assembly.Metadata;
			var def = metadata.GetAssemblyDefinition ();

			sb.Append (Ide.TypeSystem.Ambience.EscapeText (metadata.GetString (def.Name)));

			if (def.Version.Build != 0 || def.Version.Revision != 0 || def.Version.Major != 0 || def.Version.Minor != 0) {
				sb.Append (" <small>(");
				sb.Append (def.Version);
				sb.Append (")</small>");
			}

			return StringBuilderCache.ReturnAndFree (sb);
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var assemblyLoader = (AssemblyLoader)dataObject;
			if (assemblyLoader.Error != null) {
				treeBuilder.AddChild (assemblyLoader.Error);
				return;
			}
			if (assemblyLoader.Assembly == null)
				return;
			var references = new AssemblyReferenceFolder (assemblyLoader.Assembly);
			if (references.AssemblyReferences.Any () || references.ModuleReferences.Any ())
				treeBuilder.AddChild (references);

			var resources = new AssemblyResourceFolder (assemblyLoader.Assembly);
			if (resources.Resources.Any ())
				treeBuilder.AddChild (resources);
			
			var namespaces = new Dictionary<string, NamespaceData> ();
			bool publicOnly = Widget.PublicApiOnly;

			foreach (var type in assemblyLoader.GetMinimalTypeSystem ().MainModule.TopLevelTypeDefinitions) {
				string namespaceName = string.IsNullOrEmpty (type.Namespace) ? "" : type.Namespace;
				if (!namespaces.ContainsKey (namespaceName))
					namespaces [namespaceName] = new NamespaceData (namespaceName);
				
				var ns = namespaces [namespaceName];
				ns.Types.Add ((type.IsPublic (),  type));
			}

			treeBuilder.AddChildren (namespaces.Where (ns => ns.Key != "" && (!publicOnly || ns.Value.Types.Any (t => t.isPublic))).Select (n => n.Value));
			if (namespaces.ContainsKey ("")) {
				foreach (var child in namespaces [""].Types) {
					if (((INamedElement)child.typeObject).Name == "<Module>")
						continue;
					treeBuilder.AddChild (child);
				}
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var compilationUnit = (AssemblyLoader)dataObject;
			return compilationUnit.Assembly.Metadata.TypeDefinitions.Count > 0 || compilationUnit.Error != null;
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
				if (e1 == null || e1.Assembly == null)
					return 1;
				if (e2 == null || e2.Assembly == null)
					return -1;
				
				return string.Compare (e1.Assembly.Name, e2.Assembly.Name, StringComparison.Ordinal);
			} catch (Exception e) {
				LoggingService.LogError ("Exception in assembly browser sort function.", e);
				return -1;
			}
		}
		
		#region IAssemblyBrowserNodeBuilder
		void PrintAssemblyHeader (StringBuilder result, PEFile assemblyDefinition)
		{
			result.Append ("<span style=\"comment\">");
			result.Append ("// ");
			result.Append (string.Format (GettextCatalog.GetString ("Assembly <b>{0}</b>, Version {1}"),
			                              assemblyDefinition.Name,
			                              assemblyDefinition.Metadata.MetadataVersion));
			result.Append ("</span>");
			result.AppendLine ();
		}
		
		public Task<List<ReferenceSegment>> DisassembleAsync (TextEditor data, ITreeNavigator navigator)
		{
			var assemblyLoader = (AssemblyLoader)navigator.DataItem;
			var compilationUnit = assemblyLoader.Assembly;
			if (compilationUnit == null) {
				LoggingService.LogError ("Can't get cecil object for assembly:" + assemblyLoader.Assembly.FullName);
				return Task.FromResult (new List<ReferenceSegment> ());
			}
			return MethodDefinitionNodeBuilder.DisassembleAsync (data, rd => rd.WriteAssemblyHeader (compilationUnit));
		}
		
		
		public Task<List<ReferenceSegment>> DecompileAsync (TextEditor data, ITreeNavigator navigator, DecompileFlags flags)
		{
			var assemblyLoader = (AssemblyLoader)navigator.DataItem;
			return MethodDefinitionNodeBuilder.DecompileAsync (data, MethodDefinitionNodeBuilder.GetAssemblyLoader (navigator), b => 
				b.DecompileModuleAndAssemblyAttributes(), flags: flags);
		}

		#endregion
	}
}
