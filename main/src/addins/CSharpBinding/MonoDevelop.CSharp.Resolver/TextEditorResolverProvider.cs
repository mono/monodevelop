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
			var loc = data.OffsetToLocation (offset);
			var unit       = doc.ParsedDocument.Annotation<CompilationUnit> ();
			var parsedFile = doc.ParsedDocument.Annotation<ParsedFile> ();
			var node       = unit.GetNodeAt<Expression> (loc.Line, loc.Column);
			if (unit == null || parsedFile == null || node == null) {
				expressionRegion = DomRegion.Empty;
				return null;
			}
			
			var csResolver = new CSharpResolver (doc.TypeResolveContext, System.Threading.CancellationToken.None);
			var navigator = new NodeListResolveVisitorNavigator (new[] { node });
			var visitor = new ResolveVisitor (csResolver, parsedFile, navigator);
			unit.AcceptVisitor (visitor, null);
			expressionRegion = new DomRegion (node.StartLocation, node.EndLocation);
			return visitor.Resolve (node);
			
//			IResolver resolver = parser.CreateResolver (dom, doc, fileName);
//			if (resolver == null) 
//				return null;
//			var expressionFinder = new NewCSharpExpressionFinder (dom);
//			
//			int wordEnd = Math.Min (offset, data.Length - 1);
//			if (wordEnd < 0)
//				return null;
//			if (data.GetCharAt (wordEnd) == '@')
//				wordEnd++;
//			while (wordEnd < data.Length && (Char.IsLetterOrDigit (data.GetCharAt (wordEnd)) || data.GetCharAt (wordEnd) == '_'))
//				wordEnd++;
//			
//			while (wordEnd < data.Length - 1 && Char.IsWhiteSpace (data.GetCharAt (wordEnd)))
//				wordEnd++;
//			/* is checked at the end.
//			int saveEnd = wordEnd;
//			if (wordEnd < data.Length && data.GetCharAt (wordEnd) == '<') {
//				int matchingBracket = data.Document.GetMatchingBracketOffset (wordEnd);
//				if (matchingBracket > 0)
//					wordEnd = matchingBracket;
//				while (wordEnd < data.Length - 1 && Char.IsWhiteSpace (data.GetCharAt (wordEnd)))
//					wordEnd++;
//			}
//			
//			bool wasMethodCall = false;
//			if (data.GetCharAt (wordEnd) == '(') {
//				int matchingBracket = data.Document.GetMatchingBracketOffset (wordEnd);
//				if (matchingBracket > 0) {
//					wordEnd = matchingBracket;
//					wasMethodCall = true;
//				}
//			}
//			if (!wasMethodCall)
//				wordEnd = saveEnd;*/
//
//			ExpressionResult expressionResult = expressionFinder.FindExpression (data, wordEnd);
//			if (expressionResult == null)
//				return null;
//			ResolveResult resolveResult;
//			DocumentLocation loc = data.Document.OffsetToLocation (offset);
//			string savedExpression = null;
//			// special handling for 'var' "keyword"
//			if (expressionResult.ExpressionContext == ExpressionContext.IdentifierExpected && expressionResult.Expression != null && expressionResult.Expression.Trim () == "var") {
//				int endOffset = data.Document.LocationToOffset (expressionResult.Region.EndLine, expressionResult.Region.EndColumn);
//				StringBuilder identifer = new StringBuilder ();
//				for (int i = endOffset; i >= 0 && i < data.Document.Length; i++) {
//					char ch = data.Document.GetCharAt (i);
//					if (Char.IsWhiteSpace (ch)) {
//						if (identifer.Length > 0)
//							break;
//						continue;
//					}
//					if (ch == '=' || ch == ';')
//						break;
//					if (Char.IsLetterOrDigit (ch) || ch == '_') {
//						identifer.Append (ch);
//						continue;
//					}
//					identifer.Length = 0;
//					break;
//				}
//				if (identifer.Length > 0) {
//					expressionResult.Expression = identifer.ToString ();
//					resolveResult = resolver.Resolve (expressionResult, new AstLocation (loc.Line, int.MaxValue));
//					if (resolveResult != null) {
//						resolveResult = new MemberResolveResult (dom.GetType (resolveResult.ResolvedType));
//						resolveResult.ResolvedExpression = expressionResult;
//						return resolveResult;
//					}
//				}
//			}
//			
//			if (expressionResult.ExpressionContext == ExpressionContext.Attribute && !string.IsNullOrEmpty (expressionResult.Expression)) {
//				savedExpression = expressionResult.Expression;
//				expressionResult.Expression = expressionResult.Expression.Trim () + "Attribute";
//				expressionResult.ExpressionContext = ExpressionContext.ObjectCreation;
//			}
//			
//			resolveResult = resolver.Resolve (expressionResult, new AstLocation (loc.Line, loc.Column));
//			if (savedExpression != null && resolveResult == null) {
//				expressionResult.Expression = savedExpression;
//				resolveResult = resolver.Resolve (expressionResult, new AstLocation (loc.Line, loc.Column));
//			}
//			
//			// identifier may not be valid at that point, try to resolve it at line end (ex. foreach loop variable)
//			if (resolveResult != null &&  (resolveResult.ResolvedType == null || string.IsNullOrEmpty (resolveResult.ResolvedType.FullName)))
//				resolveResult = resolver.Resolve (expressionResult, new AstLocation (loc.Line, int.MaxValue));
//		
//			// Search for possible generic parameters.
////			if (this.resolveResult == null || this.resolveResult.ResolvedType == null || String.IsNullOrEmpty (this.resolveResult.ResolvedType.Name)) {
//			if (!expressionResult.Region.IsEmpty) {
//				int j = data.Document.LocationToOffset (expressionResult.Region.EndLine, expressionResult.Region.EndColumn);
//				int bracket = 0;
//				for (int i = j; i >= 0 && i < data.Document.Length; i++) {
//					char ch = data.Document.GetCharAt (i);
//					if (Char.IsWhiteSpace (ch))
//						continue;
//					if (ch == '<') {
//						bracket++;
//					} else if (ch == '>') {
//						bracket--;
//						if (bracket == 0) {
//							expressionResult.Expression += data.Document.GetTextBetween (j, i + 1);
//							expressionResult.ExpressionContext = ExpressionContext.ObjectCreation;
//							resolveResult = resolver.Resolve (expressionResult, new AstLocation (loc.Line, loc.Column));
//							break;
//						}
//					} else {
//						if (bracket == 0)
//							break;
//					}
//				}
//			}
//			
//			// To resolve method overloads the full expression must be parsed.
//			// ex.: Overload (1)/ Overload("one") - parsing "Overload" gives just a MethodResolveResult
//			// and for constructor initializers it's tried too to to resolve constructor overloads.
//			if (resolveResult is ThisResolveResult || 
//			    resolveResult is BaseResolveResult || 
//			    resolveResult is MethodResolveResult && ((MethodResolveResult)resolveResult).Methods.Count > 1) {
//				// put the search offset at the end of the invocation to be able to find the full expression
//				// the resolver finds it itself if spaces are between the method name and the argument opening parentheses.
//				while (wordEnd < data.Length - 1 && Char.IsWhiteSpace (data.GetCharAt (wordEnd)))
//					wordEnd++;
//				if (data.GetCharAt (wordEnd) == '(') {
//					int matchingBracket = data.Document.GetMatchingBracketOffset (wordEnd);
//					if (matchingBracket > 0)
//						wordEnd = matchingBracket;
//				}
//				//Console.WriteLine (expressionFinder.FindFullExpression (txt, wordEnd));
//				ResolveResult possibleResult = resolver.Resolve (expressionFinder.FindFullExpression (data, wordEnd), new AstLocation (loc.Line, loc.Column)) ?? resolveResult;
//				//Console.WriteLine ("possi:" + resolver.Resolve (expressionFinder.FindFullExpression (txt, wordEnd), new AstLocation (loc.Line, loc.Column)));
//				if (possibleResult is MethodResolveResult)
//					resolveResult = possibleResult;
//			}
//			return resolveResult;
		}
		
		public ResolveResult GetLanguageItem (ITypeResolveContext dom, Mono.TextEditor.TextEditorData data, int offset, string expression)
		{// TODO: Type system conversion.
			return null;
//			string fileName = data.Document.FileName;
//			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
//			if (doc == null)
//				return null;
//			
//			IParser parser = TypeSystemService.GetParser (fileName);
//			if (parser == null)
//				return null;
//			
//			IResolver         resolver = parser.CreateResolver (dom, doc, fileName);
//			IExpressionFinder expressionFinder = parser.CreateExpressionFinder (dom);
//			if (resolver == null || expressionFinder == null) 
//				return null;
//			int wordEnd = offset;
//			while (wordEnd < data.Length && (Char.IsLetterOrDigit (data.GetCharAt (wordEnd)) || data.GetCharAt (wordEnd) == '_'))
//				wordEnd++;
//			ExpressionResult expressionResult = new ExpressionResult (expression);
//			expressionResult.ExpressionContext = ExpressionContext.MethodBody;
//			
//			DocumentLocation loc = data.Document.OffsetToLocation (offset);
//			string savedExpression = null;
//			ResolveResult resolveResult;
//			
//			if (expressionResult.ExpressionContext == ExpressionContext.Attribute) {
//				savedExpression = expressionResult.Expression;
//				expressionResult.Expression += "Attribute";
//				expressionResult.ExpressionContext = ExpressionContext.ObjectCreation;
//			} 
//			resolveResult = resolver.Resolve (expressionResult, new AstLocation (loc.Line, loc.Column));
//			if (savedExpression != null && resolveResult == null) {
//				expressionResult.Expression = savedExpression;
//				resolveResult = resolver.Resolve (expressionResult, new AstLocation (loc.Line, loc.Column));
//			}
//			if (expressionResult.Region.End.IsEmpty)
//				return resolveResult;
//			// Search for possible generic parameters.
////			if (this.resolveResult == null || this.resolveResult.ResolvedType == null || String.IsNullOrEmpty (this.resolveResult.ResolvedType.Name)) {
//				int j = data.Document.LocationToOffset (expressionResult.Region.EndLine, expressionResult.Region.EndColumn);
//				int bracket = 0;
//				for (int i = j; i >= 0 && i < data.Document.Length; i++) {
//					char ch = data.Document.GetCharAt (i);
//					if (Char.IsWhiteSpace (ch))
//						continue;
//					if (ch == '<') {
//						bracket++;
//					} else if (ch == '>') {
//						bracket--;
//						if (bracket == 0) {
//							expressionResult.Expression += data.Document.GetTextBetween (j, i + 1);
//							expressionResult.ExpressionContext = ExpressionContext.ObjectCreation;
//							resolveResult = resolver.Resolve (expressionResult, new AstLocation (loc.Line, loc.Column)) ?? resolveResult;
//							break;
//						}
//					} else {
//						if (bracket == 0)
//							break;
//					}
//				}
////			}
//			
//			// To resolve method overloads the full expression must be parsed.
//			// ex.: Overload (1)/ Overload("one") - parsing "Overload" gives just a MethodResolveResult
//			if (resolveResult is MethodResolveResult) 
//				resolveResult = resolver.Resolve (expressionFinder.FindFullExpression (data, wordEnd), new AstLocation (loc.Line, loc.Column)) ?? resolveResult;
//			return resolveResult;
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
			if (type.IsReferenceType != null && !type.IsReferenceType.Value)
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
					s.Append(ambience.GetString(mrr.Methods.First (), settings));
					if (mrr.Methods.Count > 1) {
						int overloadCount = mrr.Methods.Count - 1;
						s.Append(string.Format(GettextCatalog.GetPluralString(" (+{0} overload)", " (+{0} overloads)", overloadCount), overloadCount));
					}
					doc = AmbienceService.GetDocumentationSummary(mrr.Methods.First ());
				} else if (result is TypeResolveResult) {
					var tr = (TypeResolveResult)result;
					s.Append ("<small><i>");
					s.Append (GetString (tr.Type));
					s.Append ("</i></small>\n");
					settings.OutputFlags |= OutputFlags.UseFullName;
					s.Append (ambience.GetString (tr.Type, settings));
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

