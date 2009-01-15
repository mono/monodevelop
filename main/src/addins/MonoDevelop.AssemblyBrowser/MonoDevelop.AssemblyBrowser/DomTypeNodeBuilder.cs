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
using Mono.TextEditor.Highlighting;

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
		static SyntaxMode mode = SyntaxModeService.GetSyntaxMode ("text/x-csharp");

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
			ambience = AmbienceService.GetAmbience ("text/x-csharp");
			DomTypeNodeBuilder.settings = new OutputSettings (OutputFlags.AssemblyBrowserDescription);
			
			DomTypeNodeBuilder.settings.MarkupCallback += delegate (string text) {
				return "<span style=\"default\">" + text + "</span>";
			};
			DomTypeNodeBuilder.settings.EmitModifiersCallback = delegate (string text) {
				return "<span style=\"kw:modifiers\">" + text + "</span>";
			};
			DomTypeNodeBuilder.settings.EmitKeywordCallback = delegate (string text) {
				return MarkupKeyword (text);
			};
			DomTypeNodeBuilder.settings.EmitNameCallback = delegate (IDomVisitable domVisitable, ref string outString) {
				if (domVisitable is IType) {
					outString = "<span style=\"link\"><u><a ref=\"T:" + ((IType)domVisitable).FullName + "\">" + outString + "</a></u></span>";
				} else {
					outString = "<span style=\"default\">" + outString + "</span>";
				}
			};
			DomTypeNodeBuilder.settings.PostProcessCallback = delegate (IDomVisitable domVisitable, ref string outString) {
				if (domVisitable is IReturnType) {
					outString = "<span style=\"link\"><u><a ref=\"T:" + ((IReturnType)domVisitable).FullName + "\">" + outString + "</a></u></span>";
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
				result.Append ("<span style=\"default\"> {</span>");
				result.Append ("");
				result.AppendLine ();
				int length = result.Length;
				foreach (IField field in type.Fields) {
					if ((field.Modifiers & Modifiers.SpecialName) == Modifiers.SpecialName)
						continue;
					result.Append ("<span style=\"default\"> \t");
					result.Append (field.Name);
					length = result.Length;
					result.Append (",</span>");
					result.AppendLine ();
				}
				result.Length = length;
				result.AppendLine ();
				result.Append ("<span style=\"default\">}</span>");
				return result.ToString ();
			}
			result.AppendLine ();
			result.Append ("<span style=\"default\">{</span>");
			
//			Style colorStyle = TextEditorOptions.Options.GetColorStyle (widget);
//			ChunkStyle comments = colorStyle.GetChunkStyle ("comment");
//			string commentSpan = String.Format ("<span foreground=\"#{0:X6}\">", comments.Color.Pixel);
			string commentSpan = "<span style=\"comment\">";
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
				result.Append ("<span style=\"default\">;</span>");
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
				result.Append ("<span style=\"default\">;</span>");
				result.AppendLine ();
			}
			first = true;
			foreach (IMethod method in type.Methods) {
				if (!method.IsConstructor)
					continue;
				if (first) {
					result.AppendLine ();
					result.Append ("\t");
					result.Append (commentSpan);
					result.Append (ambience.SingleLineComment (GettextCatalog.GetString ("Constructors")));
					result.Append ("</span>");
					result.AppendLine ();
				}
				first = false;
				result.Append ("\t");
				result.Append (ambience.GetString (method, settings));
				result.Append ("<span style=\"default\">;</span>");
				result.AppendLine ();
			}
			first = true;
			foreach (IMethod method in type.Methods) {
				if ((method.Modifiers & Modifiers.SpecialName) == Modifiers.SpecialName || method.IsConstructor)
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
				result.Append ("<span style=\"default\">;</span>");
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
				result.Append (" <span style=\"default\">{</span>");
				if (property.HasGet)
					result.Append (" <span style=\"kw:properties\">get</span><span style=\"default\">;</span>");
				if (property.HasSet)
					result.Append (" <span style=\"kw:properties\">set</span><span style=\"default\">;</span>");
				result.Append (" <span style=\"default\">}</span>");
				result.AppendLine ();
			}
			result.Append ("<span style=\"default\">}</span>");
			
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
