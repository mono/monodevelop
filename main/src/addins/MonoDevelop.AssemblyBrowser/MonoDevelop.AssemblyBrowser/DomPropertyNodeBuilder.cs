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

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.AssemblyBrowser
{
	class DomPropertyNodeBuilder : AssemblyBrowserTypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(IProperty); }
		}
		
		public DomPropertyNodeBuilder (AssemblyBrowserWidget widget) : base (widget)
		{
			
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			IProperty property = (IProperty)dataObject;
			return property.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			IProperty property = (IProperty)dataObject;
			label = Ambience.GetString (property, OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup);
			if (property.IsPrivate || property.IsInternal)
				label = DomMethodNodeBuilder.FormatPrivate (label);
			icon = ImageService.GetPixbuf (property.StockIcon, Gtk.IconSize.Menu);
		}
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is IMethod)
				return 1;
			if (otherNode.DataItem is BaseTypeFolder)
				return 1;
			if (otherNode.DataItem is IProperty)
				return ((IProperty)thisNode.DataItem).Name.CompareTo (((IProperty)otherNode.DataItem).Name);
			return -1;
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			DomCecilProperty property = (DomCecilProperty)dataObject;
			if (property.HasGet && property.GetMethod != null)
				ctx.AddChild (property.GetMethod);
			if (property.HasSet && property.SetMethod != null)
				ctx.AddChild (property.SetMethod);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			IProperty property = (IProperty)dataObject;
			return property.HasGet || property.HasSet;
		}
		
		
		#region IAssemblyBrowserNodeBuilder
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			IProperty property = (IProperty)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append ("<span font_family=\"monospace\">");
			result.Append (Ambience.GetString (property, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</span>");
			result.AppendLine ();
			DomMethodNodeBuilder.PrintDeclaringType (result, navigator);
			DomTypeNodeBuilder.PrintAssembly (result, navigator);
			return result.ToString ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDisassembly (ITreeNavigator navigator)
		{
			NetAmbience netAmbience = new NetAmbience ();
			IProperty property = (IProperty)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append (netAmbience.GetString (property, DomTypeNodeBuilder.settings));
			result.AppendLine ();
			result.AppendLine ();
			DomCecilProperty cecilProperty = property as DomCecilProperty;
			if (property.HasGet) {
				result.Append ("Getter:");result.AppendLine ();
				result.Append (DomMethodNodeBuilder.Disassemble (cecilProperty.GetMethod as DomCecilMethod, true).Replace ("\t", "\t\t"));
			}
			if (property.HasSet) {
				result.Append ("Setter:");result.AppendLine ();
				result.Append (DomMethodNodeBuilder.Disassemble (cecilProperty.SetMethod as DomCecilMethod, true).Replace ("\t", "\t\t"));
			}
			
			return result.ToString ();
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

		string IAssemblyBrowserNodeBuilder.GetDecompiledCode (ITreeNavigator navigator)
		{
			IProperty property = (IProperty)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append (DomMethodNodeBuilder.GetAttributes (Ambience, property.Attributes));
			result.Append (Ambience.GetString (property, DomTypeNodeBuilder.settings));
			result.Append (" {");result.AppendLine ();
			DomCecilProperty cecilProperty = property as DomCecilProperty;
			if (property.HasGet) {
				result.Append ("\t");
				if (property.GetterModifier != property.Modifiers) {
					result.Append ("<span style=\"keyword.modifier\">");
					result.Append (Ambience.GetString (property.GetterModifier));
					result.Append ("</span> ");
				}
				result.Append ("<b>get</b> {");result.AppendLine ();
				string text = DomMethodNodeBuilder.Decompile (cecilProperty.GetMethod as DomCecilMethod, true).Replace ("\t", "\t\t");
				
				result.Append (GetBody (text));
				result.Append ("\t}");result.AppendLine ();
			}
			if (property.HasSet) {
				result.Append ("\t");
				if (property.SetterModifier != property.Modifiers) {
					result.Append ("<span style=\"keyword.modifier\">");
					result.Append (Ambience.GetString (property.SetterModifier));
					result.Append ("</span> ");
				}
				result.Append ("<b>set</b> {");result.AppendLine ();
				string text = DomMethodNodeBuilder.Decompile (cecilProperty.SetMethod as DomCecilMethod, true).Replace ("\t", "\t\t");
				result.Append (GetBody (text));
				result.Append ("\t}");result.AppendLine ();
			}
			result.Append ("}");
			return result.ToString ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDocumentationMarkup (ITreeNavigator navigator)
		{
			IProperty property = (IProperty)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append ("<big>");
			result.Append (Ambience.GetString (property, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</big>");
			result.AppendLine ();
			
			AmbienceService.DocumentationFormatOptions options = new AmbienceService.DocumentationFormatOptions ();
			options.MaxLineLength = -1;
			options.BigHeadings = true;
			options.Ambience = Ambience;
			result.AppendLine ();
			
			result.Append (AmbienceService.GetDocumentationMarkup (AmbienceService.GetDocumentation (property), options));
			
			return result.ToString ();
		}
		#endregion

	}
}
