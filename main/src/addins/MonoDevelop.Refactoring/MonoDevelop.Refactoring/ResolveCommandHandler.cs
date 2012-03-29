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
using MonoDevelop.TypeSystem;

namespace MonoDevelop.Refactoring
{
	public class ResolveCommandHandler : CommandHandler
	{
		protected override void Update (CommandArrayInfo ainfo)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.ParsedDocument == null)
				return;
			var caretOffset = doc.Editor.Caret.Offset;
			
			DomRegion region;
			var resolveResult = doc.GetLanguageItem (caretOffset, out region);
			if (resolveResult == null)
				return;

			var resolveMenu = new CommandInfoSet ();
			resolveMenu.Text = GettextCatalog.GetString ("Resolve");
			
			var possibleNamespaces = GetPossibleNamespaces (doc, resolveResult);

			bool addUsing = !(resolveResult is AmbiguousTypeResolveResult);
			if (addUsing) {
				foreach (string ns in possibleNamespaces) {
					var info = resolveMenu.CommandInfos.Add (GettextCatalog.GetString ("Import Namespace {0}", ns), new System.Action (new AddImport (doc, resolveResult, ns, true).Run));
					info.Icon = MonoDevelop.Ide.Gui.Stock.AddNamespace;
				}
			}
			
			bool resolveDirect = !(resolveResult is UnknownMemberResolveResult);
			if (resolveDirect) {
				if (resolveMenu.CommandInfos.Count > 0)
					resolveMenu.CommandInfos.AddSeparator ();
				
				foreach (string ns in possibleNamespaces) {
					resolveMenu.CommandInfos.Add (GettextCatalog.GetString ("Use {0}", ns + "." + doc.Editor.GetTextBetween (region.Begin, region.End)), new System.Action (new AddImport (doc, resolveResult, ns, false).Run));
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

		static ResolveResult GetHeuristicResult (Document doc)
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

			var unit = CompilationUnit.Parse (CreateStub (doc, offset), doc.FileName);
			
			return ResolveAtLocation.Resolve (
				doc.Compilation, 
				doc.ParsedDocument.ParsedFile as CSharpParsedFile,
				unit,
				doc.Editor.Caret.Location);
		}

		public static HashSet<string> GetPossibleNamespaces (Document doc, ResolveResult resolveResult)
		{
			if (resolveResult == null || resolveResult.Type.FullName == "System.Void") 
				resolveResult = GetHeuristicResult (doc) ?? resolveResult;

			var location = doc.Editor.Caret.Location;
			var foundNamespaces = GetPossibleNamespaces (doc, resolveResult, location);
			
			if (!(resolveResult is AmbiguousTypeResolveResult)) {
				var usedNamespaces = RefactoringOptions.GetUsedNamespaces (doc, location);
				foundNamespaces = foundNamespaces.Where (n => !usedNamespaces.Contains (n));
			}

			return new HashSet<string> (foundNamespaces);
		}

		static IEnumerable<string> GetPossibleNamespaces (Document doc, ResolveResult resolveResult, DocumentLocation location)
		{
			var unit = doc.ParsedDocument.GetAst<CompilationUnit> ();
			if (unit == null)
				yield break;
			
			var attribute = unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Attribute> (location);
			bool isInsideAttributeType = attribute != null && attribute.Type.Contains (location);

			if (resolveResult is AmbiguousTypeResolveResult) {
				var aResult = resolveResult as AmbiguousTypeResolveResult;
				var file = doc.ParsedDocument.ParsedFile as CSharpParsedFile;
				var scope = file.GetUsingScope (location).Resolve (doc.Compilation);
				while (scope != null) {
					foreach (var u in scope.Usings) {
						foreach (var typeDefinition  in u.Types) {
							if (typeDefinition.Name == aResult.Type.Name) {
								yield return typeDefinition.Namespace;
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
				foreach (var typeDefinition in doc.Compilation.GetAllTypeDefinitions ()) {
					if (typeDefinition.Name == uiResult.Identifier || typeDefinition.Name == possibleAttributeName)
						yield return typeDefinition.Namespace;
				}
				yield break;
			}

			if (resolveResult is UnknownMemberResolveResult) {
				var umResult = (UnknownMemberResolveResult)resolveResult;
				string possibleAttributeName = isInsideAttributeType ? umResult.MemberName + "Attribute" : null;
				var compilation = doc.Compilation;
				foreach (var typeDefinition in compilation.GetAllTypeDefinitions ().Where (t => t.HasExtensionMethods)) {
					foreach (var method in typeDefinition.Methods.Where (m => m.IsExtensionMethod && (m.Name == umResult.MemberName || m.Name == possibleAttributeName))) {
						IType[] inferredTypes;
						if (CSharpResolver.IsEligibleExtensionMethod (compilation.Import (umResult.TargetType), method, true, out inferredTypes)) {
							yield return typeDefinition.Namespace;
							goto skipType;
						}
					}
					skipType:
					;
				}
				yield break;
			}
			
			if (resolveResult is ErrorResolveResult) {
				var identifier = unit != null ? unit.GetNodeAt<ICSharpCode.NRefactory.CSharp.Identifier> (location) : null;
				if (identifier != null) {
					var uiResult = resolveResult as UnknownIdentifierResolveResult;
					if (uiResult != null) {
						string possibleAttributeName = isInsideAttributeType ? uiResult.Identifier + "Attribute" : null;
						foreach (var typeDefinition in doc.Compilation.GetAllTypeDefinitions ()) {
							if (identifier.Name == uiResult.Identifier || identifier.Name == possibleAttributeName)
								yield return typeDefinition.Namespace;
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
			
			public AddImport (Document doc, ResolveResult resolveResult, string ns, bool addUsing)
			{
				this.doc = doc;
				this.resolveResult = resolveResult;
				this.ns = ns;
				this.addUsing = addUsing;
			}
			
			public void Run ()
			{
				var loc = doc.Editor.Caret.Location;

				if (!addUsing) {
					var unit = doc.ParsedDocument.GetAst<CompilationUnit> ();
					var node = unit.GetNodeAt (loc, n => n is Expression || n is AstType);
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

