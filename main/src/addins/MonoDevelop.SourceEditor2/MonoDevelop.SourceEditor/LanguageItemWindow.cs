// LanguageItemWindow.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

using Gtk;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;

namespace MonoDevelop.SourceEditor
{
	public class LanguageItemWindow: MonoDevelop.Projects.Gui.Completion.TooltipWindow
	{
		static OutputFlags WindowConversionFlags = OutputFlags.ClassBrowserEntries | OutputFlags.IncludeParameterName | OutputFlags.IncludeKeywords | OutputFlags.IncludeMarkup | OutputFlags.UseFullName;
		
		static string paramStr = GettextCatalog.GetString ("Parameter");
		static string localStr = GettextCatalog.GetString ("Local variable");
		static string fieldStr = GettextCatalog.GetString ("Field");
		static string propertyStr = GettextCatalog.GetString ("Property");
		static string methodStr = GettextCatalog.GetString ("Method");
		static string namespaceStr = GettextCatalog.GetString ("Namespace");
		
		public bool IsEmpty {
			get; 
			set;
		}
		
		static string GetDocumentation (IMember member)
		{
			if (member == null)
				return null;
			XmlElement node = (XmlElement)member.GetMonodocDocumentation ();
			if (node != null) 
				return node["summary"].InnerXml;
			return member.Documentation;
		}
		
		public LanguageItemWindow (ProjectDom dom, Ambience ambience, ResolveResult result, string errorInformations)
		{
			// Approximate value for usual case
			StringBuilder s = new StringBuilder(150);
			string doc = null;
			if (result != null) {
				if (result is ParameterResolveResult) {
					s.Append ("<small><i>");
					s.Append (paramStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.GetString (((ParameterResolveResult)result).Parameter, WindowConversionFlags));
				} else if (result is LocalVariableResolveResult) {
					s.Append ("<small><i>");
					s.Append (localStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.GetString (((LocalVariableResolveResult)result).ResolvedType, WindowConversionFlags));
					s.Append (" ");
					s.Append (((LocalVariableResolveResult)result).LocalVariable.Name);
				} else if (result is MemberResolveResult) {
					IMember member = ((MemberResolveResult)result).ResolvedMember;
					if (member == null) {
						IReturnType returnType = ((MemberResolveResult)result).ResolvedType;
						if (returnType != null) {
							IType type = dom.GetType (returnType);
							if (type != null) {
								s.Append (ambience.GetString (type, WindowConversionFlags | OutputFlags.UseFullName | OutputFlags.IncludeModifiers));
								doc = GetDocumentation (type);
							}
						}
					} else {
						if (member is IField) {
							s.Append ("<small><i>");
							s.Append (fieldStr);
							s.Append ("</i></small>\n");
						} else if (member is IProperty) {
							s.Append ("<small><i>");
							s.Append (propertyStr);
							s.Append ("</i></small>\n");
						}
						s.Append (ambience.GetString (member, WindowConversionFlags));
						doc = GetDocumentation (member);
					}
				} else if (result is NamespaceResolveResult) {
					s.Append ("<small><i>");
					s.Append (namespaceStr);
					s.Append ("</i></small>\n");
					s.Append (((NamespaceResolveResult)result).Namespace);
				} else if (result is MethodResolveResult) {
					s.Append ("<small><i>");
					s.Append (methodStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.GetString (((MethodResolveResult)result).MostLikelyMethod, WindowConversionFlags));
					doc = GetDocumentation (((MethodResolveResult)result).MostLikelyMethod);
				} else {
					s.Append (ambience.GetString (result.ResolvedType, WindowConversionFlags));
				}
				
				
				if (!string.IsNullOrEmpty (doc)) {
					s.Append ("\n<small>");
					s.Append (GetDocumentation (doc));
					s.Append ("</small>");
				}
			}
			
			if (!string.IsNullOrEmpty (errorInformations)) {
				if (s.Length != 0)
					s.Append ("\n\n");
				s.Append ("<small>");
				s.Append (errorInformations);
				s.Append ("</small>");
			}
			
			if (s.ToString ().Trim ().Length == 0) {
				IsEmpty = true;
				return;
			}
			MonoDevelop.Components.FixedWidthWrapLabel lab = new MonoDevelop.Components.FixedWidthWrapLabel ();
			lab.Wrap = Pango.WrapMode.WordChar;
			lab.Indent = -20;
			lab.BreakOnCamelCasing = true;
			lab.BreakOnPunctuation = true;
			lab.Markup = s.ToString ();
			this.BorderWidth = 3;
			Add (lab);
			
			EnableTransparencyControl = true;
		}
		
		//return the real width
		public int SetMaxWidth (int maxWidth)
		{
			MonoDevelop.Components.FixedWidthWrapLabel l = (MonoDevelop.Components.FixedWidthWrapLabel)Child;
			l.MaxWidth = maxWidth;
			return l.RealWidth;
		}
		
		public static string GetDocumentation (string doc)
		{
			System.IO.StringReader reader = new System.IO.StringReader("<docroot>" + doc + "</docroot>");
			XmlTextReader xml   = new XmlTextReader(reader);
			StringBuilder ret   = new StringBuilder(70);
			try {
				xml.Read();
				do {
					if (xml.NodeType == XmlNodeType.Element) {
						string elname = xml.Name.ToLower();
						if (elname == "remarks") {
							ret.Append("Remarks:\n");
						// skip <example>-nodes
						} else if (elname == "example") {
							xml.Skip();
							xml.Skip();
						} else if (elname == "exception") {
							ret.Append("Exception: " + GetCref(xml["cref"]) + ":\n");
						} else if (elname == "returns") {
							ret.Append("Returns: ");
						} else if (elname == "see") {
							ret.Append(GetCref(xml["cref"]) + xml["langword"]);
						} else if (elname == "seealso") {
							ret.Append("See also: " + GetCref(xml["cref"]) + xml["langword"]);
						} else if (elname == "paramref") {
							ret.Append(xml["name"]);
						} else if (elname == "param") {
							ret.Append(xml["name"].Trim() + ": ");
						} else if (elname == "value") {
							ret.Append("Value: ");
						}
					} else if (xml.NodeType == XmlNodeType.EndElement) {
						string elname = xml.Name.ToLower();
						if (elname == "para" || elname == "param") {
							ret.Append("\n");
						}
					} else if (xml.NodeType == XmlNodeType.Text) {
						ret.Append(xml.Value);
					}
				} while (xml.Read ());
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				return doc;
			}
			return ret.ToString ();
		}
		
		static string GetCref (string cref)
		{
			if (cref == null)
				return "";
			
			if (cref.Length < 2)
				return cref;
			
			if (cref.Substring(1, 1) == ":")
				return cref.Substring (2, cref.Length - 2);
			
			return cref;
		}
	}
}
