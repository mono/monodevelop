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
using System.Security;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.CSharp;
using System.Threading.Tasks;

namespace MonoDevelop.AssemblyBrowser
{
	class TypeDefinitionNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ITypeDefinition); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/AssemblyBrowser/TypeNode/ContextMenu"; }
		}
		
		public TypeDefinitionNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var type = (ITypeDefinition)dataObject;
			return type.GetDisplayString ();
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var type = (ITypeDefinition)dataObject;
			nodeInfo.Label = Ide.TypeSystem.Ambience.EscapeText (treeBuilder.NodeName);
			if (!type.IsPublic ())
				nodeInfo.Label = MethodDefinitionNodeBuilder.FormatPrivate (nodeInfo.Label);
			nodeInfo.Icon = Context.GetIcon (GetStockIcon(type));
		}

		public static IconId GetStockIcon (ITypeDefinition type)
		{
			return "md-" + type.Accessibility.GetStockIcon () + GetSource (type);
		}

		static string GetSource (ITypeDefinition type)
		{
			if (type.Kind == TypeKind.Interface)
				return "interface";
			if (type.Kind == TypeKind.Struct)
				return "struct";
			if (type.Kind == TypeKind.Enum)
				return "enum";
			if (type.Kind == TypeKind.Delegate)
				return "delegate";
			return "class";
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			var type = (ITypeDefinition)dataObject;
			var list = new System.Collections.ArrayList ();
			if (type.DirectBaseTypes.Any ())
				list.Add (new BaseTypeFolder (type));
			bool publicOnly = Widget.PublicApiOnly;

			foreach (var field in type.Fields.OrderBy (m => m.Name, StringComparer.InvariantCulture)) {
				if (publicOnly && !field.IsPublic ())
					continue;
				builder.AddChild (field);
			}

			foreach (var property in type.Properties.OrderBy (m => m.Name, StringComparer.InvariantCulture)) {
				var accessor = property.Getter ?? property.Setter;
				if (publicOnly && !accessor.IsPublic ())
					continue;
				builder.AddChild (property);
			}

			foreach (var evt in type.Events.OrderBy (m => m.Name, StringComparer.InvariantCulture)) {
				var accessor = evt.AddAccessor ?? evt.RemoveAccessor;
				if (publicOnly && !accessor.IsPublic ())
					continue;
				builder.AddChild (evt);
			}

			var accessorMethods = type.GetAccessors ();
			foreach (var method in type.Methods.OrderBy (m => m.Name, StringComparer.InvariantCulture)) {
				if (publicOnly && !method.IsPublic ())
					continue;
				if (!accessorMethods.Contains (method)) {
					builder.AddChild (method);
				}
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		#region IAssemblyBrowserNodeBuilder
		internal static void PrintAssembly (StringBuilder result, ITreeNavigator navigator)
		{
			var assemblyDefinition = (AssemblyLoader)navigator.GetParentDataItem (typeof (AssemblyLoader), false);
			if (assemblyDefinition == null)
				return;

			result.Append (GettextCatalog.GetString ("<b>Assembly:</b>\t{0}, Version={1}",
			                              assemblyDefinition.Assembly.Name,
			                              assemblyDefinition.Assembly.Metadata.MetadataVersion));
			result.AppendLine ();
		}
		
		public string GetDescription (ITreeNavigator navigator)
		{
			/*			var type = (TypeDefinition)navigator.DataItem;
						var resolved = Resolve (navigator, type);
						StringBuilder result = new StringBuilder ();
						result.Append ("<span font_family=\"monospace\">");
						result.Append (MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (Ambience.ConvertType (resolved)));
						result.Append ("</span>");
						result.AppendLine ();
						result.Append (GettextCatalog.GetString ("<b>Name:</b>\t{0}", type.FullName));
						result.AppendLine ();
						PrintAssembly (result, navigator);
						return result.ToString ();*/
			return "";
		}
		
		public Task<List<ReferenceSegment>> DisassembleAsync (TextEditor data, ITreeNavigator navigator)
		{
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return EmptyReferenceSegmentTask;
			var type = (ITypeDefinition)navigator.DataItem;
			if (type == null)
				return EmptyReferenceSegmentTask;

			return MethodDefinitionNodeBuilder.DisassembleAsync (data, rd => rd.DisassembleType (type.ParentModule.PEFile, (System.Reflection.Metadata.TypeDefinitionHandle)type.MetadataToken));
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

		public Task<List<ReferenceSegment>> DecompileAsync (TextEditor data, ITreeNavigator navigator, DecompileFlags flags)
		{
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return EmptyReferenceSegmentTask;
			var type = (ITypeDefinition)navigator.DataItem;
			if (type == null)
				return EmptyReferenceSegmentTask;
			var settings = MethodDefinitionNodeBuilder.GetDecompilerSettings (data, flags.PublicOnly);
			// CSharpLanguage.Instance.DecompileType (type, output, settings);
			return MethodDefinitionNodeBuilder.DecompileAsync (
				data, 
				MethodDefinitionNodeBuilder.GetAssemblyLoader (navigator), 
				builder => builder.Decompile (type.MetadataToken), flags: flags);
		}

		#endregion
	}
}
