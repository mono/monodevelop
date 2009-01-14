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
		internal static Ambience ambience;
		internal static OutputSettings settings;
		
		static DomTypeNodeBuilder ()
		{
			ambience = AmbienceService.GetAmbience ("text/x-csharp");
			DomTypeNodeBuilder.settings = new OutputSettings (OutputFlags.AssemblyBrowserDescription);
			DomTypeNodeBuilder.settings.PostProcessCallback = delegate (IDomVisitable domVisitable, ref string outString) {
				if (domVisitable is IReturnType)
					outString = "<span foreground=\"blue\"><u><a ref=\"T:" + ((IReturnType)domVisitable).FullName + "\">" + outString + "</a></u></span>";
				if (domVisitable is IType) {
					int idx = outString.IndexOf ("class");
					if (idx < 0)
						idx = outString.IndexOf ("interface");
					if (idx < 0)
						idx = outString.IndexOf ("struct");
					if (idx < 0)
						idx = outString.IndexOf ("enum");
					if (idx < 0)
						idx = outString.IndexOf ("delegate");
					if (idx >= 0) {
						idx = outString.IndexOf (' ', idx) + 1;
						int idx2 = outString.IndexOf (' ', idx);
						if (idx2 < 0)
							idx2 = outString.Length;
						System.Console.WriteLine(idx);
						System.Console.WriteLine(idx2);
						outString = outString.Substring (0, idx) +
							        "<span foreground=\"blue\"><u><a ref=\"T:" + ((IType)domVisitable).FullName + "\">" + outString.Substring (idx, idx2 - idx) + "</a></u></span>" + 
								    outString.Substring (idx2);
					}
				}
					
			};
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			IType type = (IType)dataObject;
			return type.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			IType type = (IType)dataObject;
			label = AmbienceService.GetAmbience ("text/x-csharp").GetString (type, OutputFlags.ClassBrowserEntries  | OutputFlags.IncludeMarkup);
			if (type.IsPrivate || type.IsInternal)
				label = DomMethodNodeBuilder.FormatPrivate (label);
			icon = MonoDevelop.Ide.Gui.IdeApp.Services.Resources.GetIcon (type.StockIcon, Gtk.IconSize.Menu);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			IType type = (IType)dataObject;
			ctx.AddChild (new BaseTypeFolder (type));
			bool publicOnly = ctx.Options ["PublicApiOnly"];
			System.Console.WriteLine("Build Child nodes:  " + publicOnly);
			foreach (object o in type.Members) {
				IMember member = o as IMember;
				if (member != null) {
					if (member.IsSpecialName) 
						continue;
					if (publicOnly && !member.IsPublic)
						continue;
					ctx.AddChild (member);
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
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)navigator.GetParentDataItem (typeof (AssemblyDefinition), false);
			if (assemblyDefinition == null)
				return;
			
			result.Append (String.Format (GettextCatalog.GetString ("<b>Assembly:</b>\t{0}, Version={1}"),
			                              assemblyDefinition.Name.Name,
			                              assemblyDefinition.Name.Version));
			result.AppendLine ();
		}
		
		public string GetDescription (ITreeNavigator navigator)
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
		
		public string GetDisassembly (ITreeNavigator navigator)
		{
			IType type = (IType)navigator.DataItem;
			StringBuilder result = new StringBuilder ();
			
			result.Append (ambience.GetString (type, settings));
			bool first = true;
			
			if (type.ClassType == ClassType.Enum) {
				result.Append (" {");
				result.AppendLine ();
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
				result.AppendLine ();
				result.Append ("}");
				return result.ToString ();
			}
			result.AppendLine ();
			result.Append ("{");
			
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
				result.Append (ambience.GetString (field, settings));
				result.Append (";");
				result.AppendLine ();
			}
			first = true;
			foreach (IEvent evt in type.Events) {
				if (first) {
					result.AppendLine ();
					result.Append ("\t");
					result.Append (commentSpan);
					result.Append (ambience.SingleLineComment (GettextCatalog.GetString ("Events")));
					result.Append ("</span>");
					result.AppendLine ();
				}
				first = false;
				result.Append ("\t");
				result.Append (ambience.GetString (evt, settings));
				result.Append (";");
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
				result.Append (ambience.GetString (method, settings));
				result.Append (";");
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
				result.Append (ambience.GetString (property, settings));
				result.Append (" {");
				if (property.HasGet)
					result.Append (" get;");
				if (property.HasSet)
					result.Append (" set;");
				result.Append (" }");
				result.AppendLine ();
			}
			result.Append ("}");
			result.AppendLine ();
			return result.ToString ();
		}
		public string GetDecompiledCode (ITreeNavigator navigator)
		{
			return GetDisassembly (navigator);
		}
		#endregion
		
	}
}
