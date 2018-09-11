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
using Microsoft.CodeAnalysis.FindSymbols;

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

		protected override async Task UpdateAsync (CommandArrayInfo ainfo, CancellationToken cancelToken)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.AnalysisDocument == null)
				return;
			var semanticModel = await doc.AnalysisDocument.GetSemanticModelAsync (cancelToken);
			if (semanticModel == null)
				return;
			var info = await RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor);

			var ext = doc.GetContent<CodeActionEditorExtension> ();

			bool canRename = RenameHandler.CanRename (info.Symbol ?? info.DeclaredSymbol);
			if (canRename) {
				ainfo.Add (IdeApp.CommandService.GetCommandInfo (MonoDevelop.Ide.Commands.EditCommands.Rename), new Action (async delegate {
					await new MonoDevelop.Refactoring.Rename.RenameRefactoring ().Rename (info.Symbol ?? info.DeclaredSymbol);
				}));
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
			}


			if (info.DeclaredSymbol != null && GotoBaseDeclarationHandler.CanGotoBase (info.DeclaredSymbol)) {
				ainfo.Add (GotoBaseDeclarationHandler.GetDescription (info.DeclaredSymbol), new Action (() => GotoBaseDeclarationHandler.GotoBase (doc, info.DeclaredSymbol).Ignore ()));
			}

			var sym = info.Symbol ?? info.DeclaredSymbol;
			if (doc.HasProject && sym != null) {
				ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindReferences), new System.Action (() => {

					if (sym.Kind == SymbolKind.Local || sym.Kind == SymbolKind.Parameter || sym.Kind == SymbolKind.TypeParameter) {
						FindReferencesHandler.FindRefs (new [] { SymbolAndProjectId.Create (sym, doc.AnalysisDocument.Project.Id) }, doc.AnalysisDocument.Project.Solution).Ignore ();
					} else {
						RefactoringService.FindReferencesAsync (FindReferencesHandler.FilterSymbolForFindReferences (sym).GetDocumentationCommentId ()).Ignore ();
					}

				}));
				try {
					if (Microsoft.CodeAnalysis.FindSymbols.SymbolFinder.FindSimilarSymbols (sym, semanticModel.Compilation).Count () > 1)
						ainfo.Add (IdeApp.CommandService.GetCommandInfo (RefactoryCommands.FindAllReferences), new System.Action (() => RefactoringService.FindAllReferencesAsync (FindReferencesHandler.FilterSymbolForFindReferences (sym).GetDocumentationCommentId ())));
				} catch (Exception) {
					// silently ignore roslyn bug.
				}
			}
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
