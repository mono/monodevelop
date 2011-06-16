//
// DomPropertyNodeBuilder.cs
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

using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Text;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler;
using System.Threading;
using Mono.TextEditor;
using System.Collections.Generic;
using Mono.Cecil;

namespace MonoDevelop.AssemblyBrowser
{
	class DomPropertyNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(PropertyDefinition); }
		}
		
		public DomPropertyNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
			
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var property = (PropertyDefinition)dataObject;
			return property.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			var property = (PropertyDefinition)dataObject;
			label = property.Name;
//			label = Ambience.GetString (property, OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup);
//			if (property.IsPrivate || property.IsInternal)
//				label = DomMethodNodeBuilder.FormatPrivate (label);
//			icon = ImageService.GetPixbuf (property.StockIcon, Gtk.IconSize.Menu);
		}
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is MethodDefinition)
				return 1;
			if (otherNode.DataItem is BaseTypeFolder)
				return 1;
			if (otherNode.DataItem is PropertyDefinition)
				return ((PropertyDefinition)thisNode.DataItem).Name.CompareTo (((PropertyDefinition)otherNode.DataItem).Name);
			return -1;
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			var property = (PropertyDefinition)dataObject;
			if (property.GetMethod != null)
				ctx.AddChild (property.GetMethod);
			if (property.SetMethod != null)
				ctx.AddChild (property.SetMethod);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var property = (PropertyDefinition)dataObject;
			return property.GetMethod != null || property.SetMethod != null;
		}
		
		
		#region IAssemblyBrowserNodeBuilder
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			var property = (PropertyDefinition)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append ("<span font_family=\"monospace\">");
//			result.Append (Ambience.GetString (property, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</span>");
			result.AppendLine ();
			DomMethodNodeBuilder.PrintDeclaringType (result, navigator);
			DomTypeNodeBuilder.PrintAssembly (result, navigator);
			return result.ToString ();
		}
		
		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Disassemble (TextEditorData data, ITreeNavigator navigator)
		{
			var property = (PropertyDefinition)navigator.DataItem;
			return DomMethodNodeBuilder.Disassemble (data, rd => rd.DisassembleProperty (property));
		}
		
		static string GetBody (string text)
		{
			
			int idx = text.IndexOf ('{') + 1;
			int idx2 = text.LastIndexOf ('}');
			if (idx2 - idx <= 0)
				return text;
			string result = text.Substring (idx, idx2 - idx);
			if (result.StartsWith ("\n"))
				result = result.Substring (1);
			return result;
		}

		List<ReferenceSegment> IAssemblyBrowserNodeBuilder.Decompile (TextEditorData data, ITreeNavigator navigator)
		{
			var property = (PropertyDefinition)navigator.DataItem;
			var parent = (TypeDefinition)navigator.GetParentDataItem (typeof(TypeDefinition), false);
			return DomMethodNodeBuilder.Decompile (data, DomMethodNodeBuilder.GetModule (navigator), parent, b => b.AddProperty (property));
		}
		
		string IAssemblyBrowserNodeBuilder.GetDocumentationMarkup (ITreeNavigator navigator)
		{
			var property = (PropertyDefinition)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append ("<big>");
//			result.Append (Ambience.GetString (property, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</big>");
			result.AppendLine ();
			
//			AmbienceService.DocumentationFormatOptions options = new AmbienceService.DocumentationFormatOptions ();
//			options.MaxLineLength = -1;
//			options.BigHeadings = true;
//			options.Ambience = Ambience;
			result.AppendLine ();
			
//			result.Append (AmbienceService.GetDocumentationMarkup (AmbienceService.GetDocumentation (property), options));
			
			return result.ToString ();
		}
		#endregion

	}
}
