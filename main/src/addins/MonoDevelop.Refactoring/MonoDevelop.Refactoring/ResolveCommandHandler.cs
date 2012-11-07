// 
// ResolveCommand.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Linq;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.TextEditor;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp.Completion;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Refactoring;

namespace MonoDevelop.Refactoring
{
	public class ResolveCommandHandler : CommandHandler
	{

		public static bool ResolveAt (Document doc, out ResolveResult resolveResult, out AstNode node, CancellationToken token = default (CancellationToken))
		{
			var parsedDocument = doc.ParsedDocument;
			resolveResult = null;
			node = null;
			if (parsedDocument == null)
				return false;
			var unit = parsedDocument.GetAst<SyntaxTree> ();
			var parsedFile = parsedDocument.ParsedFile as CSharpUnresolvedFile;
			if (unit == null || parsedFile == null)
				return false;
			try {
				var location = RefactoringService.GetCorrectResolveLocation (doc, doc.Editor.Caret.Location);
				resolveResult = ResolveAtLocation.Resolve (doc.Compilation, parsedFile, unit, location, out node, token);
				if (resolveResult == null || node is Statement)
					return false;
			} catch (OperationCanceledException) {
				return false;
			} catch (Exception e) {
				Console.WriteLine ("Got resolver exception:" + e);
				return false;
			}
			return true;
		}

		protected override void Update (CommandArrayInfo ainfo)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.ParsedDocument == null)
				return;

			ResolveResult resolveResult;
			AstNode node;
			if (!ResolveAt (doc, out resolveResult, out node)) {
				var location = RefactoringService.GetCorrectResolveLocation (doc, doc.Editor.Caret.Location);
				resolveResult = GetHeuristicResult (doc, location, ref node);
				if (resolveResult == null)
					return;
			}
			var resolveMenu = new CommandInfoSet ();
			resolveMenu.Text = GettextCatalog.GetString ("Resolve");
			
			var possibleNamespaces = GetPossibleNamespaces (doc, node, ref resolveResult);

			bool addUsing = !(resolveResult is AmbiguousTypeResolveResult);
			if (addUsing) {
				foreach (var t in possibleNamespaces.Where (tp => tp.Item2)) {
					string ns = t.Item1;
					var info = resolveMenu.CommandInfos.Add (
						string.Format ("using {0};", ns),
						new System.Action (new AddImport (doc, resolveResult, ns, true, node).Run)
					);
					info.Icon = MonoDevelop.Ide.Gui.Stock.AddNamespace;
				}
			}
			
			bool resolveDirect = !(resolveResult is UnknownMemberResolveResult);
			if (resolveDirect) {
				if (resolveMenu.CommandInfos.Count > 0)
					resolveMenu.CommandInfos.AddSeparator ();
				if (node is ObjectCreateExpression)
					node = ((ObjectCreateExpression)node).Type;
				foreach (var t in possibleNamespaces) {
					string ns = t.Item1;
					resolveMenu.CommandInfos.Add (string.Format ("{0}", ns + "." + doc.Editor.GetTextBetween (node.StartLocation, node.EndLocation)), new System.Action (new AddImport (doc, resolveResult, ns, false, node).Run));
				}
			}
			
			if (resolveMenu.CommandInfos.Count > 0)
				ainfo.Insert (0, resolveMenu);
		}

		static string CreateStub (Document doc, int offset)
		{
			if (offset <= 0)
				return "";
			string text = doc.Editor.GetTextAt (0, Math.Min (doc.Editor.Length, offset));
			var stub = new StringBuilder (text);
			CSharpCompletionEngine.AppendMissingClosingBrackets (stub, text, false);
			return stub.ToString ();
		}

		static ResolveResult GetHeuristicResult (Document doc, DocumentLocation location, ref AstNode node)
		{
			int offset = doc.Editor.Caret.Offset;
			bool wasLetter = false, wasWhitespaceAfterLetter = false;
			while (offset < doc.Editor.Length) {
				char ch = doc.Editor.GetCharAt (offset);
				bool isLetter = char.IsLetterOrDigit (ch) || ch == '_';
				bool isWhiteSpace = char.IsWhiteSpace (ch);
				bool isValidPunc = ch == '.' || ch == '<' || ch == '>';

				if (!(wasLetter && wasWhitespaceAfterLetter) && (isLetter || isWhiteSpace || isValidPunc)) {
					if (isValidPunc) {
						wasWhitespaceAfterLetter = false;
						wasLetter = false;
					}
					offset++;
				} else {
					offset--;
					while (offset > 1) {
						ch = doc.Editor.GetCharAt (offset - 1);
						if (!(ch == '.' || char.IsWhiteSpace (ch)))
							break;
						offset--;
					}
					break;
				}

				wasLetter |= isLetter;
				if (wasLetter)
					wasWhitespaceAfterLetter |= isWhiteSpace;
			}

			var unit = SyntaxTree.Parse (CreateStub (doc, offset), doc.FileName);

			return ResolveAtLocation.Resolve (
				doc.Compilation, 
				doc.ParsedDocument.ParsedFile as CSharpUnresolvedFile,
				unit,
				location, 
				out node);
		}

		public static HashSet<Tuple<string, bool>> GetPossibleNamespaces (Document doc, AstNode node, ref ResolveResult resolveResult)
		{
			var location = RefactoringService.GetCorrectResolveLocation (doc, doc.Editor.Caret.Location);

			if (resolveResult == null || resolveResult.Type.FullName == "System.Void")
				resolveResult = GetHeuristicResult (doc, location, ref node) ?? resolveResult;
			var foundNamespaces = GetPossibleNamespaces (doc, node, resolveResult, location);
			
			if (!(resolveResult is AmbiguousTypeResolveResult)) {
				var usedNamespaces = RefactoringOptions.GetUsedNamespaces (doc, location);
				foundNamespaces = foundNamespaces.Where (n => !usedNamespaces.Contains (n.Item1));
			}

			return new HashSet<Tuple<string, bool>> (foundNamespaces);
		}

		static int GetTypeParameterCount (AstNode node)
		{
			if (node is ObjectCreateExpression)
				node = ((ObjectCreateExpression)node).Type;
			if (node is SimpleType)
				return ((SimpleType)node).TypeArguments.Count;
			if (node is MemberType)
				return ((MemberType)node).TypeArguments.Count;
			if (node is IdentifierExpression)
				return ((IdentifierExpression)node).TypeArguments.Count;
			return 0;
		}

		static IEnumerable<Tuple<string, bool>> GetPossibleNamespaces (Document doc, AstNode node, ResolveResult resolveResult, DocumentLocation location)
		{
			var unit = doc.ParsedDocument.GetAst<SyntaxTree> ();
			if (unit == null)
				yield break;

			int tc = GetTypeParameterCount (node);
			var attribute = unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Attribute> (location);
			bool isInsideAttributeType = attribute != null && attribute.Type.Contains (location);
			var compilation = doc.Compilation;
			var lookup = new MemberLookup (null, compilation.MainAssembly);
			if (resolveResult is AmbiguousTypeResolveResult) {
				var aResult = resolveResult as AmbiguousTypeResolveResult;
				var file = doc.ParsedDocument.ParsedFile as CSharpUnresolvedFile;
				var scope = file.GetUsingScope (location).Resolve (compilation);
				while (scope != null) {
					foreach (var u in scope.Usings) {
						foreach (var typeDefinition in u.Types) {
							if (typeDefinition.Name == aResult.Type.Name && 
								typeDefinition.TypeParameterCount == tc &&
								lookup.IsAccessible (typeDefinition, false)) {
								yield return Tuple.Create (typeDefinition.Namespace, true);
							}
						}
					}
					scope = scope.Parent;
				}
				yield break;
			}

			if (resolveResult is UnknownIdentifierResolveResult) {
				var uiResult = resolveResult as UnknownIdentifierResolveResult;
				string possibleAttributeName = isInsideAttributeType ? uiResult.Identifier + "Attribute" : null;
				foreach (var typeDefinition in compilation.GetAllTypeDefinitions ()) {
					if ((typeDefinition.Name == uiResult.Identifier || typeDefinition.Name == possibleAttributeName) && typeDefinition.TypeParameterCount == tc && 
						lookup.IsAccessible (typeDefinition, false)) {
						if (typeDefinition.DeclaringTypeDefinition != null) {
							var builder = new TypeSystemAstBuilder (new CSharpResolver (doc.Compilation));
							yield return Tuple.Create (builder.ConvertType (typeDefinition.DeclaringTypeDefinition).GetText (), false);
						} else {
							yield return Tuple.Create (typeDefinition.Namespace, true);
						}
					}
				}
				yield break;
			}

			if (resolveResult is UnknownMemberResolveResult) {
				var umResult = (UnknownMemberResolveResult)resolveResult;
				string possibleAttributeName = isInsideAttributeType ? umResult.MemberName + "Attribute" : null;
				foreach (var typeDefinition in compilation.GetAllTypeDefinitions ().Where (t => t.HasExtensionMethods)) {
					foreach (var method in typeDefinition.Methods.Where (m => m.IsExtensionMethod && (m.Name == umResult.MemberName || m.Name == possibleAttributeName))) {
						IType[] inferredTypes;
						if (CSharpResolver.IsEligibleExtensionMethod (
							compilation.Import (umResult.TargetType),
							method,
							true,
							out inferredTypes
						)) {
							yield return Tuple.Create (typeDefinition.Namespace, true);
							goto skipType;
						}
					}
					skipType:
					;
				}
				yield break;
			}
			
			if (resolveResult is ErrorResolveResult) {
				var identifier = unit != null ? unit.GetNodeAt<Identifier> (location) : null;
				if (identifier != null) {
					var uiResult = resolveResult as UnknownIdentifierResolveResult;
					if (uiResult != null) {
						string possibleAttributeName = isInsideAttributeType ? uiResult.Identifier + "Attribute" : null;
						foreach (var typeDefinition in compilation.GetAllTypeDefinitions ()) {
							if ((identifier.Name == uiResult.Identifier || identifier.Name == possibleAttributeName) && 
							    typeDefinition.TypeParameterCount == tc && 
							    lookup.IsAccessible (typeDefinition, false))
								yield return Tuple.Create (typeDefinition.Namespace, true);
						}
					}
				}
				yield break;
			}
		}

		internal class AddImport
		{
			readonly Document doc;
			readonly ResolveResult resolveResult;
			readonly string ns;
			readonly bool addUsing;
			readonly AstNode node;
			
			public AddImport (Document doc, ResolveResult resolveResult, string ns, bool addUsing, AstNode node)
			{
				this.doc = doc;
				this.resolveResult = resolveResult;
				this.ns = ns;
				this.addUsing = addUsing;
				this.node = node;
			}
			
			public void Run ()
			{
				var loc = doc.Editor.Caret.Location;

				if (!addUsing) {
//					var unit = doc.ParsedDocument.GetAst<SyntaxTree> ();
					int offset = doc.Editor.LocationToOffset (node.StartLocation);
					doc.Editor.Insert (offset, ns + ".");
					doc.Editor.Document.CommitLineUpdate (loc.Line);
					return;
				}

				var generator = doc.CreateCodeGenerator ();

				if (resolveResult is NamespaceResolveResult) {
					generator.AddLocalNamespaceImport (doc, ns, loc);
				} else {
					generator.AddGlobalNamespaceImport (doc, ns);
				}
			}
		}
		
		protected override void Run (object data)
		{
			var del = (System.Action)data;
			if (del != null)
				del ();
		}
	}
}

