//
// RefactoryCommands.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Linq;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Editor;
using MonoDevelop.CodeActions;
using MonoDevelop.CodeIssues;
using MonoDevelop.Refactoring;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.CSharp.Refactoring
{
	sealed class CurrentRefactoryOperationsHandler : CommandHandler
	{
		protected override void Run (object dataItem)
		{
			var del = (Action)dataItem;
			if (del != null)
				del ();
		}

		protected override void Update (CommandArrayInfo ainfo)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.ParsedDocument == null)
				return;
			var semanticModel = doc.ParsedDocument.GetAst<SemanticModel> ();
			if (semanticModel == null)
				return;
			var task = RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor);
			if (!task.Wait (2000))
				return;
			var info = task.Result;
			bool added = false;

			var ext = doc.GetContent<CodeActionEditorExtension> ();

			var ciset = new CommandInfoSet ();
			ciset.Text = GettextCatalog.GetString ("Refactor");

			bool canRename = RenameHandler.CanRename (info.Symbol ?? info.DeclaredSymbol);
			if (canRename) {
				ciset.CommandInfos.Add (IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.EditCommands.Rename), new Action (async delegate {
					await new MonoDevelop.Refactoring.Rename.RenameRefactoring ().Rename (info.Symbol ?? info.DeclaredSymbol);
				}));
				added = true;
			}
			bool first = true;

			if (ciset.CommandInfos.Count > 0) {
				ainfo.Add (ciset, null);
				added = true;
			}

			var gotoDeclarationSymbol = info.Symbol;
			if (gotoDeclarationSymbol == null && info.DeclaredSymbol != null && info.DeclaredSymbol.Locations.Length > 1)
				gotoDeclarationSymbol = info.DeclaredSymbol;
			if (IdeApp.ProjectOperations.CanJumpToDeclaration (gotoDeclarationSymbol) || gotoDeclarationSymbol == null && IdeApp.ProjectOperations.CanJumpToDeclaration (info.CandidateSymbols.FirstOrDefault ())) {

				var type = (gotoDeclarationSymbol ?? info.CandidateSymbols.FirstOrDefault ()) as INamedTypeSymbol;
				if (type != null && type.Locations.Length > 1) {
					var declSet = new CommandInfoSet ();
					declSet.Text = GettextCatalog.GetString ("_Go to Declaration");
					foreach (var part in type.Locations) {
						var loc = part.GetLineSpan ();
						declSet.CommandInfos.Add (string.Format (GettextCatalog.GetString ("{0}, Line {1}"), FormatFileName (part.SourceTree.FilePath), loc.StartLinePosition.Line + 1), new Action (() => IdeApp.ProjectOperations.JumpTo (type, part, doc.Project)));
					}
					ainfo.Add (declSet);
				} else {
					ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.GotoDeclaration), new Action (() => GotoDeclarationHandler.Run (doc)));
				}
				added = true;
			}


			if (info.DeclaredSymbol != null && GotoBaseDeclarationHandler.CanGotoBase (info.DeclaredSymbol)) {
				ainfo.Add (GotoBaseDeclarationHandler.GetDescription (info.DeclaredSymbol), new Action (() => GotoBaseDeclarationHandler.GotoBase (doc, info.DeclaredSymbol).Ignore ()));
				added = true;
			}

			var sym = info.Symbol ?? info.DeclaredSymbol;
			if (doc.HasProject && sym != null) {
				ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindReferences), new System.Action (() => {

					if (sym.Kind == SymbolKind.Local || sym.Kind == SymbolKind.Parameter || sym.Kind == SymbolKind.TypeParameter) {
						FindReferencesHandler.FindRefs (sym, doc.AnalysisDocument.Project.Solution);
					} else {
						RefactoringService.FindReferencesAsync (FindReferencesHandler.FilterSymbolForFindReferences (sym).GetDocumentationCommentId ());
					}

				}));
				try {
					if (Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindSimilarSymbols (sym, semanticModel.Compilation).Count () > 1)
						ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindAllReferences), new System.Action (() => RefactoringService.FindAllReferencesAsync (FindReferencesHandler.FilterSymbolForFindReferences (sym).GetDocumentationCommentId ())));
				} catch (Exception) {
					// silently ignore roslyn bug.
				}
			}
			added = true;

		}

		static string FormatFileName (string fileName)
		{
			if (fileName == null)
				return null;
			char [] seperators = { System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar };
			int idx = fileName.LastIndexOfAny (seperators);
			if (idx > 0)
				idx = fileName.LastIndexOfAny (seperators, idx - 1);
			if (idx > 0)
				return "..." + fileName.Substring (idx);
			return fileName;
		}
	}
}
