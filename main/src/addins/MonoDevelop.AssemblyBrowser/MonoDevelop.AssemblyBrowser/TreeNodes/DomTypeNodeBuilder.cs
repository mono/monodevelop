//
// DomTypeNodeBuilder.cs
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
using System.Linq;
using Mono.Cecil;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using ICSharpCode.Decompiler;
using System.Threading;
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.Decompiler.TypeSystem;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.Gui.Content;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;

namespace MonoDevelop.AssemblyBrowser
{
	class DomTypeNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(IUnresolvedTypeDefinition); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/AssemblyBrowser/TypeNode/ContextMenu"; }
		}
		
		public DomTypeNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var type = (IUnresolvedTypeDefinition)dataObject;
			return type.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var type = (IUnresolvedTypeDefinition)dataObject;
			try {
				var resolved = Resolve (treeBuilder, type);
				nodeInfo.Label = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (Ambience.ConvertType (resolved));
			} catch (Exception) {
				nodeInfo.Label = type.Name;
			}
			if (type.IsPrivate)
				nodeInfo.Label = DomMethodNodeBuilder.FormatPrivate (nodeInfo.Label);
			nodeInfo.Icon = Context.GetIcon (type.GetStockIcon ());
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			var type = (IUnresolvedTypeDefinition)dataObject;
			var list = new System.Collections.ArrayList ();
			list.Add (new BaseTypeFolder (type));
			bool publicOnly = Widget.PublicApiOnly;
			foreach (var t in type.NestedTypes.Where (m => !m.IsSynthetic && (m.IsPublic || m.IsProtected || !publicOnly))) {
				list.Add (t);
			}
			foreach (var m in type.Members.Where (m => !m.IsSynthetic && (m.IsPublic || m.IsProtected || !publicOnly)))
				list.Add (m);
			builder.AddChildren (list);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		#region IAssemblyBrowserNodeBuilder
		internal static void PrintAssembly (StringBuilder result, ITreeNavigator navigator)
		{
			var assemblyDefinition = (AssemblyDefinition)navigator.GetParentDataItem (typeof (AssemblyDefinition), false);
			if (assemblyDefinition == null)
				return;
			
			result.Append (GettextCatalog.GetString ("<b>Assembly:</b>\t{0}, Version={1}",
			                              assemblyDefinition.Name.Name,
			                              assemblyDefinition.Name.Version));
			result.AppendLine ();
		}
		
		public string GetDescription (ITreeNavigator navigator)
		{
			var type = (IUnresolvedTypeDefinition)navigator.DataItem;
			var resolved = Resolve (navigator, type);
			StringBuilder result = new StringBuilder ();
			result.Append ("<span font_family=\"monospace\">");
			result.Append (MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (Ambience.ConvertType (resolved)));
			result.Append ("</span>");
			result.AppendLine ();
			result.Append (GettextCatalog.GetString ("<b>Name:</b>\t{0}", type.FullName));
			result.AppendLine ();
			PrintAssembly (result, navigator);
			return result.ToString ();
		}
		
		public List<ReferenceSegment> Disassemble (TextEditor data, ITreeNavigator navigator)
		{
			if (DomMethodNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var type = GetCecilLoader (navigator).GetCecilObject<TypeDefinition> ((IUnresolvedTypeDefinition)navigator.DataItem);
			if (type == null)
				return null;
			
			return DomMethodNodeBuilder.Disassemble (data, rd => rd.DisassembleType (type));
		}

		internal static DecompilerSettings CreateDecompilerSettings (bool publicOnly, MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy codePolicy)
		{
			return new DecompilerSettings {
				AnonymousMethods = true,
				AutomaticEvents = true,
				AutomaticProperties = true,
				ExpressionTrees = true,
				YieldReturn = true,
				ForEachStatement = true,
				LockStatement = true,
				AsyncAwait = true,
				ShowXmlDocumentation = true,
				CSharpFormattingOptions = FormattingOptionsFactory.CreateMono ()
			};
		}

		public List<ReferenceSegment> Decompile (TextEditor data, ITreeNavigator navigator, DecompileFlags flags)
		{
			if (DomMethodNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var type = (IUnresolvedTypeDefinition)navigator.DataItem;
			if (type == null)
				return null;
			var settings = DomMethodNodeBuilder.GetDecompilerSettings (data, flags.PublicOnly);
			return DomMethodNodeBuilder.Decompile (data, DomMethodNodeBuilder.GetAssemblyLoader (navigator), builder => builder.DecompileType(type.FullTypeName), flags: flags);
		}

		string IAssemblyBrowserNodeBuilder.GetDocumentationMarkup (ITreeNavigator navigator)
		{
			var type = (IUnresolvedTypeDefinition)navigator.DataItem;
			var resolved = Resolve (navigator, type);
			var result = new StringBuilder ();
			result.Append ("<big>");
			result.Append (MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (Ambience.ConvertType (resolved)));
			result.Append ("</big>");
			result.AppendLine ();
			
			//result.Append (AmbienceService.GetDocumentationMarkup (resolved.GetDefinition (), AmbienceService.GetDocumentation (resolved.GetDefinition ()), options));
			
			return result.ToString ();
		}
		#endregion
		
	}
}
