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
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using Mono.TextEditor;
using System.Text;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Ide;

namespace MonoDevelop.CSharp.Resolver
{
	public class TextEditorResolverProvider : ITextEditorResolverProvider
	{
		#region ITextEditorResolverProvider implementation
		
		public MonoDevelop.Projects.Dom.ResolveResult GetLanguageItem (ProjectDom dom, Mono.TextEditor.TextEditorData data, int offset)
		{
			string fileName = data.Document.FileName;
			
			IParser parser = ProjectDomService.GetParser (fileName, data.Document.MimeType);
			if (parser == null)
				return null;
			
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return null;
			
			IResolver         resolver = parser.CreateResolver (dom, doc, fileName);
			IExpressionFinder expressionFinder = parser.CreateExpressionFinder (dom);
			if (resolver == null || expressionFinder == null) 
				return null;
			
			string txt = data.Document.Text;
			int wordEnd = offset;
			while (wordEnd < txt.Length && (Char.IsLetterOrDigit (txt[wordEnd]) || txt[wordEnd] == '_'))
				wordEnd++;
			
			ExpressionResult expressionResult = expressionFinder.FindExpression (txt, wordEnd);
			if (expressionResult == null)
				return null;
			ResolveResult resolveResult;
			DocumentLocation loc = data.Document.OffsetToLocation (offset);
			string savedExpression = null;
			
			// special handling for 'var' "keyword"
			if (expressionResult.ExpressionContext == ExpressionContext.IdentifierExpected && expressionResult.Expression != null && expressionResult.Expression.Trim () == "var") {
				int endOffset = data.Document.LocationToOffset (expressionResult.Region.End.Line - 1, expressionResult.Region.End.Column - 1);
				StringBuilder identifer = new StringBuilder ();
				for (int i = endOffset; i >= 0 && i < data.Document.Length; i++) {
					char ch = data.Document.GetCharAt (i);
					if (Char.IsWhiteSpace (ch))
						continue;
					if (ch == '=')
						break;
					if (Char.IsLetterOrDigit (ch) || ch =='_') {
						identifer.Append (ch);
						continue;
					}
					identifer.Length = 0;
					break;
				}
				if (identifer.Length > 0) {
					expressionResult.Expression = identifer.ToString ();
					resolveResult = resolver.Resolve (expressionResult, new DomLocation (loc.Line + 1, loc.Column + 1));
					if (resolveResult != null) {
						resolveResult = new MemberResolveResult (dom.GetType (resolveResult.ResolvedType));
						return resolveResult;
					}
				}
			}
			
			if (expressionResult.ExpressionContext == ExpressionContext.Attribute) {
				savedExpression = expressionResult.Expression;
				expressionResult.Expression += "Attribute";
				expressionResult.ExpressionContext = ExpressionContext.ObjectCreation;
			} 
			resolveResult = resolver.Resolve (expressionResult, new DomLocation (loc.Line + 1, loc.Column + 1));
			
			if (savedExpression != null && resolveResult == null) {
				expressionResult.Expression = savedExpression;
				resolveResult = resolver.Resolve (expressionResult, new DomLocation (loc.Line + 1, loc.Column + 1));
			}
			// Search for possible generic parameters.
//			if (this.resolveResult == null || this.resolveResult.ResolvedType == null || String.IsNullOrEmpty (this.resolveResult.ResolvedType.Name)) {
			if (!expressionResult.Region.IsEmpty) {
				int j = data.Document.LocationToOffset (expressionResult.Region.End.Line - 1, expressionResult.Region.End.Column - 1);
				int bracket = 0;
				for (int i = j; i >= 0 && i < data.Document.Length; i++) {
					char ch = data.Document.GetCharAt (i);
					if (Char.IsWhiteSpace (ch))
						continue;
					if (ch == '<') {
						bracket++;
					} else if (ch == '>') {
						bracket--;
						if (bracket == 0) {
							expressionResult.Expression += data.Document.GetTextBetween (j, i + 1);
							expressionResult.ExpressionContext = ExpressionContext.ObjectCreation;
							resolveResult = resolver.Resolve (expressionResult, new DomLocation (loc.Line + 1, loc.Column + 1));
							break;
						}
					} else {
						if (bracket == 0)
							break;
					}
				}
			}
			
			// To resolve method overloads the full expression must be parsed.
			// ex.: Overload (1)/ Overload("one") - parsing "Overload" gives just a MethodResolveResult
			// and for constructor initializers it's tried too to to resolve constructor overloads.
			if (resolveResult is ThisResolveResult || 
			    resolveResult is BaseResolveResult || 
			    resolveResult is MethodResolveResult && ((MethodResolveResult)resolveResult).Methods.Count > 1) {
				// put the search offset at the end of the invocation to be able to find the full expression
				// the resolver finds it itself if spaces are between the method name and the argument opening parentheses.
				if (txt[wordEnd] == '(') {
					int matchingBracket = data.Document.GetMatchingBracketOffset (wordEnd);
					if (matchingBracket > 0)
						wordEnd = matchingBracket;
				}
				ResolveResult possibleResult = resolver.Resolve (expressionFinder.FindFullExpression (txt, wordEnd), new DomLocation (loc.Line + 1, loc.Column + 1)) ?? resolveResult;
				if (possibleResult is MethodResolveResult)
					resolveResult = possibleResult;
			}
			return resolveResult;
		}
		
		public MonoDevelop.Projects.Dom.ResolveResult GetLanguageItem (ProjectDom dom, Mono.TextEditor.TextEditorData data, int offset, string expression)
		{
			string fileName = data.Document.FileName;
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return null;
			
			IParser parser = ProjectDomService.GetParser (fileName, data.Document.MimeType);
			if (parser == null)
				return null;
			
			IResolver         resolver = parser.CreateResolver (dom, doc, fileName);
			IExpressionFinder expressionFinder = parser.CreateExpressionFinder (dom);
			if (resolver == null || expressionFinder == null) 
				return null;
			string txt = data.Document.Text;
			int wordEnd = offset;
			while (wordEnd < txt.Length && (Char.IsLetterOrDigit (txt[wordEnd]) || txt[wordEnd] == '_'))
				wordEnd++;
			ExpressionResult expressionResult = new ExpressionResult (expression);
			expressionResult.ExpressionContext = ExpressionContext.MethodBody;
			
			DocumentLocation loc = data.Document.OffsetToLocation (offset);
			string savedExpression = null;
			ResolveResult resolveResult;
			
			if (expressionResult.ExpressionContext == ExpressionContext.Attribute) {
				savedExpression = expressionResult.Expression;
				expressionResult.Expression += "Attribute";
				expressionResult.ExpressionContext = ExpressionContext.ObjectCreation;
			} 
			resolveResult = resolver.Resolve (expressionResult, new DomLocation (loc.Line + 1, loc.Column + 1));
			if (savedExpression != null && resolveResult == null) {
				expressionResult.Expression = savedExpression;
				resolveResult = resolver.Resolve (expressionResult, new DomLocation (loc.Line + 1, loc.Column + 1));
			}
			// Search for possible generic parameters.
//			if (this.resolveResult == null || this.resolveResult.ResolvedType == null || String.IsNullOrEmpty (this.resolveResult.ResolvedType.Name)) {
				int j = data.Document.LocationToOffset (expressionResult.Region.End.Line - 1, expressionResult.Region.End.Column - 1);
				int bracket = 0;
				for (int i = j; i >= 0 && i < data.Document.Length; i++) {
					char ch = data.Document.GetCharAt (i);
					if (Char.IsWhiteSpace (ch))
						continue;
					if (ch == '<') {
						bracket++;
					} else if (ch == '>') {
						bracket--;
						if (bracket == 0) {
							expressionResult.Expression += data.Document.GetTextBetween (j, i + 1);
							expressionResult.ExpressionContext = ExpressionContext.ObjectCreation;
							resolveResult = resolver.Resolve (expressionResult, new DomLocation (loc.Line + 1, loc.Column + 1));
							break;
						}
					} else {
						if (bracket == 0)
							break;
					}
				}
//			}
			
			// To resolve method overloads the full expression must be parsed.
			// ex.: Overload (1)/ Overload("one") - parsing "Overload" gives just a MethodResolveResult
			if (resolveResult is MethodResolveResult) 
				resolveResult = resolver.Resolve (expressionFinder.FindFullExpression (txt, wordEnd), new DomLocation (loc.Line + 1, loc.Column + 1)) ?? resolveResult;
			return resolveResult;
		}
		
		
		static string paramStr = GettextCatalog.GetString ("Parameter");
		static string localStr = GettextCatalog.GetString ("Local variable");
		static string fieldStr = GettextCatalog.GetString ("Field");
		static string propertyStr = GettextCatalog.GetString ("Property");
		static string methodStr = GettextCatalog.GetString ("Method");
		static string typeStr = GettextCatalog.GetString ("Type");
		static string namespaceStr = GettextCatalog.GetString ("Namespace");
		
		public string CreateTooltip (ProjectDom dom, ICompilationUnit unit, MonoDevelop.Projects.Dom.ResolveResult result, string errorInformations, Ambience ambience, Gdk.ModifierType modifierState)
		{
			OutputSettings settings = new OutputSettings (OutputFlags.ClassBrowserEntries | OutputFlags.IncludeParameterName | OutputFlags.IncludeKeywords | OutputFlags.IncludeMarkup | OutputFlags.UseFullName);
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
				} else if (result is UnresolvedMemberResolveResult) {
					s.Append (String.Format (GettextCatalog.GetString ("Unresolved member '{0}'"), ((UnresolvedMemberResolveResult)result).MemberName));
				} else if (result is MethodResolveResult) {
					MethodResolveResult mrr = (MethodResolveResult)result;
					s.Append("<small><i>");
					s.Append(methodStr);
					s.Append("</i></small>\n");
					s.Append(ambience.GetString(mrr.MostLikelyMethod, settings));
					if (mrr.Methods.Count > 1) {
						int overloadCount = mrr.Methods.Count - 1;
						s.Append(string.Format(GettextCatalog.GetPluralString(" (+{0} overload)", " (+{0} overloads)", overloadCount), overloadCount));
					}
					doc = AmbienceService.GetDocumentationSummary(((MethodResolveResult)result).MostLikelyMethod);
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
				} else {
					s.Append (ambience.GetString (result.ResolvedType, settings));
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

