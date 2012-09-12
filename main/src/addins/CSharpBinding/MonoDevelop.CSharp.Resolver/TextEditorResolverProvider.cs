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
using MonoDevelop.Ide.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using System.Threading;
using MonoDevelop.Refactoring;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace MonoDevelop.CSharp.Resolver
{
	public class TextEditorResolverProvider : ITextEditorResolverProvider
	{
		#region ITextEditorResolverProvider implementation
		
		public string GetExpression (Mono.TextEditor.TextEditorData data, int offset)
		{
			if (offset < 0)
				return "";
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return "";
			var loc = RefactoringService.GetCorrectResolveLocation (doc, data.OffsetToLocation (offset));
			var unit = doc.ParsedDocument.GetAst<SyntaxTree> ();
			var parsedFile = doc.ParsedDocument.ParsedFile as CSharpUnresolvedFile;
			var node       = unit.GetNodeAt<Expression> (loc.Line, loc.Column);
			if (unit == null || parsedFile == null || node == null)
				return "";
			
			return data.GetTextBetween (node.StartLocation, node.EndLocation);
		}
		
		
		public ResolveResult GetLanguageItem (MonoDevelop.Ide.Gui.Document doc, int offset, out DomRegion expressionRegion)
		{
			if (offset < 0) {
				expressionRegion = DomRegion.Empty;
				return null;
			}
			var loc = RefactoringService.GetCorrectResolveLocation (doc, doc.Editor.OffsetToLocation (offset));
			ResolveResult result;
			AstNode node;

			if (!doc.TryResolveAt (loc, out result, out node)) {
				expressionRegion = DomRegion.Empty;
				return null;
			}
			expressionRegion = new DomRegion (node.StartLocation, node.EndLocation);
			return result;
		}
		
		public ResolveResult GetLanguageItem (MonoDevelop.Ide.Gui.Document doc, int offset, string expression)
		{
			if (offset < 0) {
				return null;
			}

			var parsedDocument = doc.ParsedDocument;
			if (parsedDocument == null)
				return null;
			var data = doc.Editor;
			var loc = data.OffsetToLocation (offset);

			var unit = parsedDocument.GetAst<SyntaxTree> ();
			var parsedFile = parsedDocument.ParsedFile as CSharpUnresolvedFile;
			
			if (unit == null || parsedFile == null) {
				return null;
			}
			var node = unit.GetNodeAt (loc);
			if (node == null) {
				return null;
			}
			
			var resolver = new CSharpAstResolver (doc.Compilation, unit, parsedFile);
			resolver.ApplyNavigator (new NodeListResolveVisitorNavigator (node), CancellationToken.None);
			var state = resolver.GetResolverStateBefore (node, CancellationToken.None);
			return state.LookupSimpleNameOrTypeName (expression, new List<IType> (), NameLookupMode.Expression);
		}
		
		
		static string paramStr = GettextCatalog.GetString ("Parameter");
		static string localStr = GettextCatalog.GetString ("Local variable");
		static string methodStr = GettextCatalog.GetString ("Method");

		static string namespaceStr = GettextCatalog.GetString ("Namespace");		
		static string GetString (IType type)
		{
			switch (type.Kind) {
			case TypeKind.Class:
				return GettextCatalog.GetString ("Class");
			case TypeKind.Interface:
				return GettextCatalog.GetString ("Interface");
			case TypeKind.Struct:
				return GettextCatalog.GetString ("Struct");
			case TypeKind.Delegate:
				return GettextCatalog.GetString ("Delegate");
			case TypeKind.Enum:
				return GettextCatalog.GetString ("Enum");
			
			case TypeKind.Dynamic:
				return GettextCatalog.GetString ("Dynamic");
			case TypeKind.TypeParameter:
				return GettextCatalog.GetString ("Type parameter");
			
			case TypeKind.Array:
				return GettextCatalog.GetString ("Array");
			case TypeKind.Pointer:
				return GettextCatalog.GetString ("Pointer");
			}
			
			return null;
		}
		
		static string GetString (IMember member)
		{
			switch (member.EntityType) {
			case EntityType.Field:
				var field = member as IField;
				if (field.IsConst)
					return GettextCatalog.GetString ("Constant");
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
		
		string GetConst (object obj)
		{
			if (obj is string)
				return '"' + obj.ToString () + '"';
			if (obj is char)
				return "'" + obj + "'";
			return obj.ToString ();
		}
		static CSharpAmbience ambience = new CSharpAmbience ();

		static TypeSystemAstBuilder CreateBuilder (MonoDevelop.Ide.Gui.Document doc, int offset, ICompilation compilation)
		{
			var ctx = doc.ParsedDocument.ParsedFile.GetTypeResolveContext (doc.Compilation, doc.Editor.Caret.Location) as CSharpTypeResolveContext;
			var state = new CSharpResolver (ctx);
			var builder = new TypeSystemAstBuilder (state);
			builder.AddAnnotations = true;
			var dt = state.CurrentTypeDefinition;
			var declaring = ctx.CurrentTypeDefinition != null ? ctx.CurrentTypeDefinition.DeclaringTypeDefinition : null;
			if (declaring != null) {
				while (dt != null) {
					if (dt.Equals (declaring)) {
						builder.AlwaysUseShortTypeNames = true;
						break;
					}
					dt = dt.DeclaringTypeDefinition;
				}
			}
			return builder;
		}

		internal static MonoDevelop.CSharp.Completion.MemberCompletionData.MyAmbience CreateAmbience (Document doc, int offset, ICompilation compilation)
		{
			return new MonoDevelop.CSharp.Completion.MemberCompletionData.MyAmbience (CreateBuilder (doc, offset, compilation));
		}

		public string CreateTooltip (MonoDevelop.Ide.Gui.Document doc, int offset, ResolveResult result, string errorInformations, Gdk.ModifierType modifierState)
		{
			try {
				OutputSettings settings = new OutputSettings (OutputFlags.ClassBrowserEntries | OutputFlags.IncludeParameterName | OutputFlags.IncludeKeywords | OutputFlags.IncludeMarkup | OutputFlags.UseFullName);
				// Approximate value for usual case
				StringBuilder s = new StringBuilder (150);
				string documentation = null;
				if (result != null) {
					if (result is UnknownIdentifierResolveResult) {
						s.Append (String.Format (GettextCatalog.GetString ("Unresolved identifier '{0}'"), ((UnknownIdentifierResolveResult)result).Identifier));
					} else if (result.IsError) {
						s.Append (GettextCatalog.GetString ("Resolve error."));
					} else if (result is LocalResolveResult) {
						var lr = (LocalResolveResult)result;
						s.Append ("<small><i>");
						s.Append (lr.IsParameter ? paramStr : localStr);
						s.Append ("</i></small>\n");
						s.Append (ambience.GetString (lr.Variable.Type, settings));
						s.Append (" ");
						s.Append (lr.Variable.Name);
					} else if (result is MethodGroupResolveResult) {
						var mrr = (MethodGroupResolveResult)result;
						s.Append ("<small><i>");
						s.Append (methodStr);
						s.Append ("</i></small>\n");
						var allMethods = new List<IMethod> (mrr.Methods);
						foreach (var l in mrr.GetExtensionMethods ()) {
							allMethods.AddRange (l);
						}
						var method = allMethods.FirstOrDefault ();
						if (method != null) {
							s.Append (GLib.Markup.EscapeText (CreateAmbience (doc, offset, method.Compilation).ConvertEntity (method)));
							if (allMethods.Count > 1) {
								int overloadCount = allMethods.Count - 1;
								s.Append (string.Format (GettextCatalog.GetPluralString (" (+{0} overload)", " (+{0} overloads)", overloadCount), overloadCount));
							}
							documentation = AmbienceService.GetSummaryMarkup (method);
						}
					} else if (result is MemberResolveResult) {
						var member = ((MemberResolveResult)result).Member;
						s.Append ("<small><i>");
						s.Append (GetString (member));
						s.Append ("</i></small>\n");
						var field = member as IField;
						if (field != null && field.IsConst) {
							s.Append (GLib.Markup.EscapeText (CreateAmbience (doc, offset, field.Compilation).ConvertType (field.Type)));
							s.Append (" ");
							s.Append (field.Name);
							s.Append (" = ");
							s.Append (GetConst (field.ConstantValue));
							s.Append (";");
						} else {
							s.Append (GLib.Markup.EscapeText (CreateAmbience (doc, offset, member.Compilation).ConvertEntity (member)));
						}
						documentation = AmbienceService.GetSummaryMarkup (member);
					} else if (result is NamespaceResolveResult) {
						s.Append ("<small><i>");
						s.Append (namespaceStr);
						s.Append ("</i></small>\n");
						s.Append (ambience.GetString (((NamespaceResolveResult)result).NamespaceName, settings));
					} else {
						var tr = result;
						var typeString = GetString (tr.Type);
						if (!string.IsNullOrEmpty (typeString)) {
							s.Append ("<small><i>");
							s.Append (typeString);
							s.Append ("</i></small>\n");
						}
						settings.OutputFlags |= OutputFlags.UseFullName;
						s.Append (ambience.GetString (tr.Type, settings));
						documentation = AmbienceService.GetSummaryMarkup (tr.Type.GetDefinition ());
					}
					
					if (!string.IsNullOrEmpty (documentation)) {
						s.Append ("\n<small>");
						s.Append (documentation);
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
			} catch (Exception e){
				return e.ToString ();
			}
		}
		
		#endregion
	}
}

