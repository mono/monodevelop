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
using ICSharpCode.ILSpy;

namespace MonoDevelop.AssemblyBrowser
{
	class TypeDefinitionNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(TypeDefinition); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/AssemblyBrowser/TypeNode/ContextMenu"; }
		}
		
		public TypeDefinitionNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var type = (TypeDefinition)dataObject;
			return type.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var type = (TypeDefinition)dataObject;
			nodeInfo.Label = MonoDevelop.Ide.TypeSystem.Ambience.EscapeText (CSharpLanguage.Instance.FormatTypeName (type));
			if (!type.IsPublic)
				nodeInfo.Label = MethodDefinitionNodeBuilder.FormatPrivate (nodeInfo.Label);
			nodeInfo.Icon = Context.GetIcon (GetStockIcon(type));
		}

		public static IconId GetStockIcon (TypeDefinition type)
		{
			return "md-" + GetAccess (type.Attributes) + GetSource (type);
		}

		static string GetSource (TypeDefinition type)
		{
			if (type.IsInterface)
				return "interface";
			if (type.IsValueType)
				return "struct";
			if (type.IsEnum)
				return "enum";
			if (type.IsDelegate ())
				return "delegate";
			return "class";
		}

		static string GetAccess (TypeAttributes attributes)
		{
			switch (attributes & TypeAttributes.VisibilityMask) {
			case TypeAttributes.NestedPrivate:
				return "private-";
			case TypeAttributes.Public:
			case TypeAttributes.NestedPublic:
				return "";
			case TypeAttributes.NestedFamily:
				return "protected-";
			case TypeAttributes.NestedAssembly:
				return "internal-";
			case TypeAttributes.NestedFamORAssem:
			case TypeAttributes.NestedFamANDAssem:
				return "ProtectedOrInternal-";
			default:
				return "";
			}
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			var type = (TypeDefinition)dataObject;
			var list = new System.Collections.ArrayList ();
			if (type.BaseType != null || type.HasInterfaces)
				list.Add (new BaseTypeFolder (type));
			bool publicOnly = Widget.PublicApiOnly;

			foreach (var nestedType in type.NestedTypes.OrderBy (m => m.Name, StringComparer.InvariantCulture)) {
				if (publicOnly && !nestedType.IsPublic)
					continue;
				builder.AddChild (nestedType);
			}

			foreach (var field in type.Fields.OrderBy (m => m.Name, StringComparer.InvariantCulture)) {
				if (publicOnly && !field.IsPublic)
					continue;
				builder.AddChild (field);
			}

			foreach (var property in type.Properties.OrderBy (m => m.Name, StringComparer.InvariantCulture)) {
				var accessor = property.GetMethod ?? property.SetMethod;
				if (publicOnly && !accessor.IsPublic)
					continue;
				builder.AddChild (property);
			}

			foreach (var evt in type.Events.OrderBy (m => m.Name, StringComparer.InvariantCulture)) {
				var accessor = evt.AddMethod ?? evt.RemoveMethod;
				if (publicOnly && !accessor.IsPublic)
					continue;
				builder.AddChild (evt);
			}

			var accessorMethods = type.GetAccessorMethods ();
			foreach (var method in type.Methods.OrderBy (m => m.Name, StringComparer.InvariantCulture)) {
				if (publicOnly && !method.IsPublic)
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
		
		public List<ReferenceSegment> Disassemble (TextEditor data, ITreeNavigator navigator)
		{
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var type = (TypeDefinition)navigator.DataItem;
			if (type == null)
				return null;
			
			return MethodDefinitionNodeBuilder.Disassemble (data, rd => rd.DisassembleType (type));
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
			if (MethodDefinitionNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var type = (TypeDefinition)navigator.DataItem;
			if (type == null)
				return null;
			var settings = MethodDefinitionNodeBuilder.GetDecompilerSettings (data, flags.PublicOnly);
			// CSharpLanguage.Instance.DecompileType (type, output, settings);
			return MethodDefinitionNodeBuilder.Decompile (
				data, 
				MethodDefinitionNodeBuilder.GetAssemblyLoader (navigator), 
				builder => builder.DecompileType (type.GetFullTypeName ()), flags: flags);
		}
		#endregion
	}
}
