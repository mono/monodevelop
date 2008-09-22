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

using Mono.Cecil;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.AssemblyBrowser
{
	public class DomTypeNodeBuilder : TypeNodeBuilder, IAssemblyBrowserNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(IType); }
		}
		
/*		AssemblyBrowserWidget widget;
		
		public DomTypeNodeBuilder (AssemblyBrowserWidget widget)
		{
			this.widget = widget;
		}*/
		
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			IType type = (IType)dataObject;
			return type.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			IType type = (IType)dataObject;
			label = AssemblyBrowserWidget.FormatText (AmbienceService.GetAmbience ("text/x-csharp").GetString (type, OutputFlags.ClassBrowserEntries));
			if (type.IsPrivate || type.IsInternal)
				label = DomMethodNodeBuilder.FormatPrivate (label);
			icon = MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (type.StockIcon, Gtk.IconSize.Menu);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			IType type = (IType)dataObject;
			ctx.AddChild (new BaseTypeFolder (type));
			foreach (object o in type.Members) {
				if (o is IMember && ((IMember)o).IsSpecialName) {
					continue;
				}
				ctx.AddChild (o);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		#region IAssemblyBrowserNodeBuilder
		internal static void PrintAssembly (StringBuilder result, ITreeNavigator navigator)
		{
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)navigator.GetParentDataItem (typeof (AssemblyDefinition), false);
			if (assemblyDefinition == null)
				return;
			
			result.Append (String.Format (GettextCatalog.GetString ("<b>Assembly:</b>\t{0}, Version={1}"),
			                              assemblyDefinition.Name.Name,
			                              assemblyDefinition.Name.Version));
			result.AppendLine ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDescription (ITreeNavigator navigator)
		{
			IType type = (IType)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			result.Append ("<span font_family=\"monospace\">");
			result.Append (AmbienceService.GetAmbience ("text/x-csharp").GetString (type, OutputFlags.AssemblyBrowserDescription));
			result.Append ("</span>");
			result.AppendLine ();
			result.Append (String.Format (GettextCatalog.GetString ("<b>Name:</b>\t{0}"),
			                              type.FullName));
			result.AppendLine ();
			PrintAssembly (result, navigator);
			return result.ToString ();
		}
		
		string IAssemblyBrowserNodeBuilder.GetDisassembly (ITreeNavigator navigator)
		{
			IType type = (IType)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			Ambience ambience = AmbienceService.GetAmbience ("text/x-csharp");
			result.Append (ambience.GetString (type, OutputFlags.AssemblyBrowserDescription));
			result.AppendLine ();
			bool first = true;
			
			if (type.ClassType == ClassType.Enum) {
				int length = result.Length;
				foreach (IField field in type.Fields) {
					if ((field.Modifiers & Modifiers.SpecialName) == Modifiers.SpecialName)
						continue;
					result.Append ("\t");
					result.Append (field.Name);
					length = result.Length;
					result.Append (",");
					result.AppendLine ();
				}
				result.Length = length;
				return result.ToString ();
			}
			
//			Style colorStyle = TextEditorOptions.Options.GetColorStyle (widget);
//			ChunkStyle comments = colorStyle.GetChunkStyle ("comment");
//			string commentSpan = String.Format ("<span foreground=\"#{0:X6}\">", comments.Color.Pixel);
			
			string commentSpan = "<span foreground=\"#666666\">";
			foreach (IField field in type.Fields) {
				if ((field.Modifiers & Modifiers.SpecialName) == Modifiers.SpecialName)
					continue;
				if (first) {
					result.AppendLine ();
					result.Append ("\t");
					result.Append (commentSpan);
					result.Append (ambience.SingleLineComment (GettextCatalog.GetString ("Fields")));
					result.Append ("</span>");
					result.AppendLine ();
				}
				first = false;
				result.Append ("\t");
				result.Append (ambience.GetString (field, OutputFlags.AssemblyBrowserDescription));
				result.AppendLine ();
			}
			first = true;
			foreach (IEvent evt in type.Events) {
				if (first) {
					result.Append ("\t");
					result.Append (commentSpan);
					result.Append (ambience.SingleLineComment (GettextCatalog.GetString ("Events")));
					result.Append ("</span>");
					result.AppendLine ();
				}
				first = false;
				result.Append ("\t");
				result.Append (ambience.GetString (evt, OutputFlags.AssemblyBrowserDescription));
				result.AppendLine ();
			}
			first = true;
			foreach (IMethod method in type.Methods) {
				if ((method.Modifiers & Modifiers.SpecialName) == Modifiers.SpecialName)
					continue;
				if (first) {
					result.AppendLine ();
					result.Append ("\t");
					result.Append (commentSpan);
					result.Append (ambience.SingleLineComment (GettextCatalog.GetString ("Methods")));
					result.Append ("</span>");
					result.AppendLine ();
				}
				first = false;
				result.Append ("\t");
				result.Append (ambience.GetString (method, OutputFlags.AssemblyBrowserDescription));
				result.AppendLine ();
			}
			first = true;
			foreach (IProperty property in type.Properties) {
				if (first) {
					result.AppendLine ();
					result.Append ("\t");
					result.Append (commentSpan);
					result.Append (ambience.SingleLineComment (GettextCatalog.GetString ("Properties")));
					result.Append ("</span>");
					result.AppendLine ();
				}
				first = false;
				result.Append ("\t");
				result.Append (ambience.GetString (property, OutputFlags.AssemblyBrowserDescription));
				result.AppendLine ();
			}
			return result.ToString ();
		}
		#endregion
		
	}
}
