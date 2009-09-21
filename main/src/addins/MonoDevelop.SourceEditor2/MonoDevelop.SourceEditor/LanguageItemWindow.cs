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
	public class LanguageItemWindow: MonoDevelop.Components.TooltipWindow
	{
		OutputSettings settings;
		
		static string paramStr = GettextCatalog.GetString ("Parameter");
		static string localStr = GettextCatalog.GetString ("Local variable");
		static string fieldStr = GettextCatalog.GetString ("Field");
		static string propertyStr = GettextCatalog.GetString ("Property");
		static string methodStr = GettextCatalog.GetString ("Method");
		static string typeStr = GettextCatalog.GetString ("Type");
		static string namespaceStr = GettextCatalog.GetString ("Namespace");
		
		public bool IsEmpty {
			get; 
			set;
		}
		
		public LanguageItemWindow (ProjectDom dom, Gdk.ModifierType modifierState, Ambience ambience, ResolveResult result, string errorInformations, ICompilationUnit unit)
		{
			settings = new OutputSettings (OutputFlags.ClassBrowserEntries | OutputFlags.IncludeParameterName | OutputFlags.IncludeKeywords | OutputFlags.IncludeMarkup | OutputFlags.UseFullName);
			if ((Gdk.ModifierType.ShiftMask & modifierState) == Gdk.ModifierType.ShiftMask) {
				settings.EmitNameCallback = delegate(INode domVisitable, ref string outString) {
					// crop used namespaces.
					if (unit != null) {

						int len = 0;
						foreach (IUsing u in unit.Usings) {
							foreach (string ns in u.Namespaces) {
								if (outString.StartsWith (ns + ".")) {
									len = Math.Max (len, ns.Length + 1);
								}
							}
						}
						string newName = outString.Substring (len);
						int count = 0;
						// check if there is a name clash.
						if (dom.GetType (newName) != null)
							count++;
						foreach (IUsing u in unit.Usings) {
							foreach (string ns in u.Namespaces) {
								if (dom.GetType (ns + "." + newName) != null)
									count++;
							}
						}

						if (len > 0 && count == 1)
							outString = newName;
					}
				};
			}

			// Approximate value for usual case
			StringBuilder s = new StringBuilder (150);
			string doc = null;
			if (result != null) {
				if (result is AggregatedResolveResult)
					result = ((AggregatedResolveResult)result).PrimaryResult;
				if (result is ParameterResolveResult) {
					s.Append ("<small><i>");
					s.Append (paramStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.GetString (((ParameterResolveResult)result).Parameter, settings));
				} else if (result is LocalVariableResolveResult) {
					s.Append ("<small><i>");
					s.Append (localStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.GetString (((LocalVariableResolveResult)result).ResolvedType, settings));
					s.Append (" ");
					s.Append (((LocalVariableResolveResult)result).LocalVariable.Name);
				} else if (result is MemberResolveResult) {
					IMember member = ((MemberResolveResult)result).ResolvedMember;
					if (member == null) {
						IReturnType returnType = ((MemberResolveResult)result).ResolvedType;
						if (returnType != null) {
							IType type = dom.GetType (returnType);
							if (type != null) {
								s.Append ("<small><i>");
								s.Append (typeStr);
								s.Append ("</i></small>\n");
								s.Append (ambience.GetString (type, settings));
								doc = AmbienceService.GetDocumentationSummary (type);
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
						s.Append (ambience.GetString (member, settings));
						doc = AmbienceService.GetDocumentationSummary (member);
					}
				} else if (result is NamespaceResolveResult) {
					s.Append ("<small><i>");
					s.Append (namespaceStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.GetString (new Namespace (((NamespaceResolveResult)result).Namespace), settings));
				} else if (result is MethodResolveResult) {
					MethodResolveResult mrr = (MethodResolveResult)result;
					s.Append ("<small><i>");
					s.Append (methodStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.GetString (mrr.MostLikelyMethod, settings));
					if (mrr.Methods.Count > 1) {
						int overloadCount = mrr.Methods.Count - 1;
						s.Append (string.Format (GettextCatalog.GetPluralString (" (+{0} overload)", " (+{0} overloads)", overloadCount), overloadCount));
					}

					doc = AmbienceService.GetDocumentationSummary (((MethodResolveResult)result).MostLikelyMethod);
				} else {
					s.Append (ambience.GetString (result.ResolvedType, settings));
				}


				if (!string.IsNullOrEmpty (doc)) {
					s.Append ("\n<small>");
					s.Append (AmbienceService.GetDocumentationMarkup (doc));
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
		
		
/*		static string GetCref (string cref)
		{
			if (cref == null)
				return "";
			
			if (cref.Length < 2)
				return cref;
			
			if (cref.Substring(1, 1) == ":")
				return cref.Substring (2, cref.Length - 2);
			
			return cref;
		}*/
	}
}
