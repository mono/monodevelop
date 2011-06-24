// 
// TextEditorResolverProvider.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using Mono.TextEditor;
using System.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Resolver
{
	public class TextEditorResolverProvider : ITextEditorResolverProvider
	{
		#region ITextEditorResolverProvider implementation
		
		public string GetExpression (ICSharpCode.NRefactory.TypeSystem.ITypeResolveContext dom, Mono.TextEditor.TextEditorData data, int offset)
		{
			if (offset < 0)
				return "";
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return "";
			var loc = data.OffsetToLocation (offset);
			var unit       = doc.ParsedDocument.Annotation<CompilationUnit> ();
			var parsedFile = doc.ParsedDocument.Annotation<ParsedFile> ();
			var node       = unit.GetNodeAt<Expression> (loc.Line, loc.Column);
			if (unit == null || parsedFile == null || node == null)
				return "";
			
			var csResolver = new CSharpResolver (doc.TypeResolveContext, System.Threading.CancellationToken.None);
			var navigator = new NodeListResolveVisitorNavigator (new[] { node });
			var visitor = new ResolveVisitor (csResolver, parsedFile, navigator);
			unit.AcceptVisitor (visitor, null);
			
			return data.GetTextBetween (node.StartLocation.Line, node.StartLocation.Column, node.EndLocation.Line, node.EndLocation.Column);
		}
		public ResolveResult GetLanguageItem (ITypeResolveContext dom, Mono.TextEditor.TextEditorData data, int offset, out DomRegion expressionRegion)
		{
			if (offset < 0) {
				expressionRegion = DomRegion.Empty;
				return null;
			}

			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null) {
				expressionRegion = DomRegion.Empty;
				return null;
			}
			var parsedDocument = doc.ParsedDocument;
			if (parsedDocument == null) {
				expressionRegion = DomRegion.Empty;
				return null;
			}
			var loc = data.OffsetToLocation (offset);
			
			var unit       = parsedDocument.Annotation<CompilationUnit> ();
			var parsedFile = parsedDocument.Annotation<ParsedFile> ();
			
			if (unit == null || parsedFile == null) {
				expressionRegion = DomRegion.Empty;
				return null;
			}
			var node   = unit.GetResolveableNodeAt (loc.Line, loc.Column);
			if (node == null) {
				expressionRegion = DomRegion.Empty;
				return null;
			}
			
			var csResolver = new CSharpResolver (doc.TypeResolveContext, System.Threading.CancellationToken.None);
			var navigator = new NodeListResolveVisitorNavigator (new[] { node });
			var visitor = new ResolveVisitor (csResolver, parsedFile, navigator);
			unit.AcceptVisitor (visitor, null);
			expressionRegion = new DomRegion (node.StartLocation, node.EndLocation);
			return visitor.Resolve (node);
		}
		
		public ResolveResult GetLanguageItem (ITypeResolveContext dom, Mono.TextEditor.TextEditorData data, int offset, string expression)
		{
			if (offset < 0) {
				return null;
			}

			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null) {
				return null;
			}
			var parsedDocument = doc.ParsedDocument;
			if (parsedDocument == null)
				return null;
			var loc = data.OffsetToLocation (offset);
			
			var unit       = parsedDocument.Annotation<CompilationUnit> ();
			var parsedFile = parsedDocument.Annotation<ParsedFile> ();
			
			if (unit == null || parsedFile == null) {
				return null;
			}
			var node   = unit.GetResolveableNodeAt (loc.Line, loc.Column);
			if (node == null) {
				return null;
			}
			
			var csResolver = new CSharpResolver (doc.TypeResolveContext, System.Threading.CancellationToken.None);
			var navigator = new NodeListResolveVisitorNavigator (new[] { node });
			var visitor = new ResolveVisitor (csResolver, parsedFile, navigator);
			unit.AcceptVisitor (visitor, null);
			var state = visitor.GetResolverStateBefore (node);
			return state.LookupSimpleNamespaceOrTypeName (expression, new List<IType> (), false);
		}
		
		
		static string paramStr = GettextCatalog.GetString ("Parameter");
		static string localStr = GettextCatalog.GetString ("Local variable");
		static string methodStr = GettextCatalog.GetString ("Method");
		
		static string namespaceStr = GettextCatalog.GetString ("Namespace");		
		static string GetString (IType type)
		{
			if (type.IsDelegate ())
				return GettextCatalog.GetString ("Delegate");
			if (type.IsEnum ())
				return GettextCatalog.GetString ("Enum");
			if (type.GetDefinition () != null && type.GetDefinition ().ClassType == ClassType.Struct)
				return GettextCatalog.GetString ("Struct");
			return GettextCatalog.GetString ("Class");
		}
		
		static string GetString (IMember member)
		{
			switch (member.EntityType) {
			case EntityType.Field:
				return GettextCatalog.GetString ("Field");
			case EntityType.Property:
				return GettextCatalog.GetString ("Property");
			case EntityType.Indexer:
				return GettextCatalog.GetString ("Indexer");
				
			case EntityType.Event:
				return GettextCatalog.GetString ("Event");
			}
			return GettextCatalog.GetString ("Member");
		}
		
		public string CreateTooltip (ITypeResolveContext dom, IParsedFile unit, ResolveResult result, string errorInformations, Ambience ambience, Gdk.ModifierType modifierState)
		{
			OutputSettings settings = new OutputSettings (OutputFlags.ClassBrowserEntries | OutputFlags.IncludeParameterName | OutputFlags.IncludeKeywords | OutputFlags.IncludeMarkup | OutputFlags.UseFullName) { Context = dom };
//			if ((Gdk.ModifierType.ShiftMask & modifierState) == Gdk.ModifierType.ShiftMask) {
//				settings.EmitNameCallback = delegate(object domVisitable, ref string outString) {
//					// crop used namespaces.
//					if (unit != null) {
//						int len = 0;
//						foreach (var u in unit.Usings) {
//							foreach (string ns in u.Namespaces) {
//								if (outString.StartsWith (ns + ".")) {
//									len = Math.Max (len, ns.Length + 1);
//								}
//							}
//						}
//						string newName = outString.Substring (len);
//						int count = 0;
//						// check if there is a name clash.
//						if (dom.GetType (newName) != null)
//							count++;
//						foreach (IUsing u in unit.Usings) {
//							foreach (string ns in u.Namespaces) {
//								if (dom.GetType (ns + "." + newName) != null)
//									count++;
//							}
//						}
//						if (len > 0 && count == 1)
//							outString = newName;
//					}
//				};
//			}
			
			// Approximate value for usual case
			StringBuilder s = new StringBuilder (150);
			string doc = null;
			if (result != null) {
				if (result is LocalResolveResult) {
					var lr = (LocalResolveResult)result;
					s.Append ("<small><i>");
					s.Append (lr.IsParameter ? paramStr : localStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.GetString (lr.Variable.Type, settings));
					s.Append (" ");
					s.Append (lr.Variable.Name);
				} else if (result is UnknownIdentifierResolveResult) {
					s.Append (String.Format (GettextCatalog.GetString ("Unresolved identifier '{0}'"), ((UnknownIdentifierResolveResult)result).Identifier));
				} else if (result is MethodGroupResolveResult) {
					var mrr = (MethodGroupResolveResult)result;
					s.Append("<small><i>");
					s.Append(methodStr);
					s.Append("</i></small>\n");
					var allMethods = new List<IMethod> (mrr.Methods);
					if (mrr.ExtensionMethods != null) {
						foreach (var l in mrr.ExtensionMethods) {
							allMethods.AddRange (l);
						}
					}
					var method = allMethods.FirstOrDefault ();
					if (method != null) {
						s.Append(ambience.GetString(method, settings));
						if (allMethods.Count > 1) {
							int overloadCount = allMethods.Count - 1;
							s.Append(string.Format(GettextCatalog.GetPluralString(" (+{0} overload)", " (+{0} overloads)", overloadCount), overloadCount));
						}
						doc = AmbienceService.GetDocumentationSummary(method);
					}
				} else if (result is TypeResolveResult) {
					var tr = (TypeResolveResult)result;
					s.Append ("<small><i>");
					s.Append (GetString (tr.Type));
					s.Append ("</i></small>\n");
					settings.OutputFlags |= OutputFlags.UseFullName;
					s.Append (ambience.GetString (tr.Type, settings));
					doc = AmbienceService.GetDocumentationSummary (tr.Type.GetDefinition ());
				} else if (result is MemberResolveResult) {
					var member = ((MemberResolveResult)result).Member;
					s.Append ("<small><i>");
					s.Append (GetString (member));
					s.Append ("</i></small>\n");
					s.Append (ambience.GetString (member, settings));
					doc = AmbienceService.GetDocumentationSummary (member);
				} else if (result is NamespaceResolveResult) {
					s.Append ("<small><i>");
					s.Append (namespaceStr);
					s.Append ("</i></small>\n");
					s.Append (ambience.GetString (((NamespaceResolveResult)result).NamespaceName, settings));
				} else {
					s.Append (ambience.GetString (result.Type, settings));
				}
				
				if (!string.IsNullOrEmpty (doc)) {
					s.Append ("\n<small>");
					s.Append (AmbienceService.GetDocumentationMarkup ( "<summary>" + doc +  "</summary>"));
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
			return s.ToString ();
		}
		
		#endregion
	}
}

