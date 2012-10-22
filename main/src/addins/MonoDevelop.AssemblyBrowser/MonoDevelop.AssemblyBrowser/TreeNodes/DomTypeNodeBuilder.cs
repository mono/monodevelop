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
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler;
using System.Threading;
using Mono.TextEditor;
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.TypeSystem.Implementation;

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
		
		internal static OutputSettings settings;
		static SyntaxMode mode = SyntaxModeService.GetSyntaxMode (null, "text/x-csharp");

		internal static string MarkupKeyword (string text)
		{
			foreach (Keywords words in mode.Keywords) {
				foreach (string word in words.Words) {
					if (word == text) {
						return "<span style=\"" + words.Color +  "\">" + text + "</span>";
					}
				}
			}
			return text;
		}
		
		static DomTypeNodeBuilder ()
		{
			DomTypeNodeBuilder.settings = new OutputSettings (OutputFlags.AssemblyBrowserDescription);
			
			DomTypeNodeBuilder.settings.MarkupCallback += delegate (string text) {
				return "<span style=\"text\">" + text + "</span>";
			};
			DomTypeNodeBuilder.settings.EmitModifiersCallback = delegate (string text) {
				return "<span style=\"keyword.modifier\">" + text + "</span>";
			};
			DomTypeNodeBuilder.settings.EmitKeywordCallback = delegate (string text) {
				return MarkupKeyword (text);
			};
//			DomTypeNodeBuilder.settings.EmitNameCallback = delegate (IEntity domVisitable, ref string outString) {
//				if (domVisitable is IType) {
//					outString = "<span style=\"text.link\"><u><a ref=\"" + ((IType)domVisitable).HelpUrl + "\">" + outString + "</a></u></span>";
//				} else {
//					outString = "<span style=\"text\">" + outString + "</span>";
//				}
//			};
//			DomTypeNodeBuilder.settings.PostProcessCallback = delegate (IEntity domVisitable, ref string outString) {
//				if (domVisitable is IReturnType) {
//					outString = "<span style=\"text.link\"><u><a ref=\"" + ((IReturnType)domVisitable).HelpUrl + "\">" + outString + "</a></u></span>";
//				}
//			};
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var type = (IUnresolvedTypeDefinition)dataObject;
			return type.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			var type = (IUnresolvedTypeDefinition)dataObject;
			var resolved = Resolve (treeBuilder, type);
			label = Ambience.GetString (resolved, OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup | OutputFlags.UseNETTypeNames);
			if (type.IsPrivate)
				label = DomMethodNodeBuilder.FormatPrivate (label);
			icon = ImageService.GetPixbuf (type.GetStockIcon (), Gtk.IconSize.Menu);
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
			
			result.Append (String.Format (GettextCatalog.GetString ("<b>Assembly:</b>\t{0}, Version={1}"),
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
			result.Append (Ambience.GetString (resolved, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</span>");
			result.AppendLine ();
			result.Append (String.Format (GettextCatalog.GetString ("<b>Name:</b>\t{0}"), type.FullName));
			result.AppendLine ();
			PrintAssembly (result, navigator);
			return result.ToString ();
		}
		
		public List<ReferenceSegment> Disassemble (TextEditorData data, ITreeNavigator navigator)
		{
			if (DomMethodNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var type = CecilLoader.GetCecilObject ((IUnresolvedTypeDefinition)navigator.DataItem);
			if (type == null)
				return null;
			
			return DomMethodNodeBuilder.Disassemble (data, rd => rd.DisassembleType (type));
		}

		public List<ReferenceSegment> Decompile (TextEditorData data, ITreeNavigator navigator, bool publicOnly)
		{
			if (DomMethodNodeBuilder.HandleSourceCodeEntity (navigator, data)) 
				return null;
			var type = CecilLoader.GetCecilObject ((IUnresolvedTypeDefinition)navigator.DataItem);
			if (type == null)
				return null;
			var types = DesktopService.GetMimeTypeInheritanceChain (data.Document.MimeType);
			var codePolicy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<MonoDevelop.CSharp.Formatting.CSharpFormattingPolicy> (types);
			var settings = new DecompilerSettings () {
				AnonymousMethods = true,
				AutomaticEvents  = true,
				AutomaticProperties = true,
				ForEachStatement = true,
				LockStatement = true,
				CSharpFormattingOptions = codePolicy.CreateOptions (),
				HideNonPublicMembers = publicOnly
			};
			return DomMethodNodeBuilder.Decompile (data, DomMethodNodeBuilder.GetModule (navigator), type, builder => {
				builder.AddType (type);
			}, settings);
		}
		
		string IAssemblyBrowserNodeBuilder.GetDocumentationMarkup (ITreeNavigator navigator)
		{
			var type = (IUnresolvedTypeDefinition)navigator.DataItem;
			var resolved = Resolve (navigator, type);
			var result = new StringBuilder ();
			result.Append ("<big>");
			result.Append (Ambience.GetString (resolved, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</big>");
			result.AppendLine ();
			
			AmbienceService.DocumentationFormatOptions options = new AmbienceService.DocumentationFormatOptions ();
			options.MaxLineLength = -1;
			options.BigHeadings = true;
			options.Ambience = Ambience;
			result.AppendLine ();
			
			result.Append (AmbienceService.GetDocumentationMarkup (resolved.GetDefinition (), AmbienceService.GetDocumentation (resolved.GetDefinition ()), options));
			
			return result.ToString ();
		}
		#endregion
		
	}
}
