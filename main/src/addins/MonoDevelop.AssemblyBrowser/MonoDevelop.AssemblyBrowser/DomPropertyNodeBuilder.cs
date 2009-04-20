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

using Mono.Cecil;

using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.AssemblyBrowser
{
	public class DomPropertyNodeBuilder : TypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(IProperty); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			IProperty property = (IProperty)dataObject;
			return property.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			IProperty property = (IProperty)dataObject;
			label = AmbienceService.GetAmbience ("text/x-csharp").GetString (property, OutputFlags.ClassBrowserEntries | OutputFlags.IncludeMarkup);
			if (property.IsPrivate || property.IsInternal)
				label = DomMethodNodeBuilder.FormatPrivate (label);
			icon = PixbufService.GetPixbuf (property.StockIcon, Gtk.IconSize.Menu);
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
			result.Append (AmbienceService.GetAmbience ("text/x-csharp").GetString (property, OutputFlags.AssemblyBrowserDescription));
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
		
		string IAssemblyBrowserNodeBuilder.GetDecompiledCode (ITreeNavigator navigator)
		{
			IProperty property = (IProperty)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append (DomMethodNodeBuilder.GetAttributes (property.Attributes));
			result.Append (DomTypeNodeBuilder.ambience.GetString (property, DomTypeNodeBuilder.settings));
			result.Append ("{");result.AppendLine ();
			DomCecilProperty cecilProperty = property as DomCecilProperty;
			if (property.HasGet) {
				result.Append ("\t<b>get</b> {");result.AppendLine ();
				result.Append (DomMethodNodeBuilder.Decompile (cecilProperty.GetMethod as DomCecilMethod, true).Replace ("\t", "\t\t"));
				result.Append ("\t}");result.AppendLine ();
			}
			if (property.HasSet) {
				result.Append ("\t<b>set</b> {");result.AppendLine ();
				result.Append (DomMethodNodeBuilder.Decompile (cecilProperty.SetMethod as DomCecilMethod, true).Replace ("\t", "\t\t"));
				result.Append ("\t}");result.AppendLine ();
			}
			result.Append ("}");
			return result.ToString ();
		}
		#endregion

	}
}
