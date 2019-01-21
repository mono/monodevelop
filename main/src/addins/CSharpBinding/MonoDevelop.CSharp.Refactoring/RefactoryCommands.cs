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
using Microsoft.CodeAnalysis.Editor.Shared;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.CSharp.Refactoring
{
	enum Commands
	{
		SortAndRemoveImports,
	}

	abstract class RefactoringHandler : CommandHandler
	{
		protected bool TryGetDocument (out Document analysisDocument, out Ide.Gui.Document doc)
		{
			doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == null) {
				analysisDocument = null;
				return false;
			}

			analysisDocument = doc.AnalysisDocument;
			return doc != null;
		}
	}

	sealed class RemoveAndSortUsingsHandler : RefactoringHandler
	{
		protected override void Update (CommandInfo info)
		{
			info.Enabled = TryGetDocument (out var doc, out var _) && IsSortAndRemoveImportsSupported (doc);
		}

		protected override void Run ()
		{
			if (TryGetDocument (out var doc, out var _))
				SortAndRemoveUnusedImports (doc, CancellationToken.None).Ignore ();
		}

		internal static bool IsSortAndRemoveImportsSupported (Document document)
		{
			var workspace = document.Project.Solution.Workspace;

			if (!workspace.CanApplyChange (ApplyChangesKind.ChangeDocument)) {
				return false;
			}

			if (workspace.Kind == WorkspaceKind.MiscellaneousFiles) {
				return false;
			}

			return workspace.Services.GetService<IDocumentSupportsFeatureService> ().SupportsRefactorings (document);
		}

		internal static async Task SortAndRemoveUnusedImports (Document originalDocument, CancellationToken cancellationToken)
		{
			if (originalDocument == null)
				return;

			var workspace = originalDocument.Project.Solution.Workspace;

			var unnecessaryImportsService = originalDocument.GetLanguageService<IRemoveUnnecessaryImportsService> ();

			// Remove unnecessary imports and sort them
			var removedImportsDocument = await unnecessaryImportsService.RemoveUnnecessaryImportsAsync (originalDocument, cancellationToken);
			var resultDocument = await OrganizeImportsService.OrganizeImportsAsync (removedImportsDocument, cancellationToken);

			// Apply the document change if needed
			if (resultDocument != originalDocument) {
				workspace.ApplyDocumentChanges (resultDocument, cancellationToken);
			}
		}
	}

	sealed class CurrentRefactoryOperationsHandler : RefactoringHandler
	{
		protected override void Run (object dataItem)
		{
			var del = (Action)dataItem;
			if (del != null)
				del ();
		}

		protected override async Task UpdateAsync (CommandArrayInfo ainfo, CancellationToken cancelToken)
		{
			if (!TryGetDocument (out var analysisDocument, out var doc))
				return;
			var semanticModel = await analysisDocument.GetSemanticModelAsync (cancelToken);
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

			bool isSortAndRemoveUsingsSupported = RemoveAndSortUsingsHandler.IsSortAndRemoveImportsSupported (analysisDocument);
			if (isSortAndRemoveUsingsSupported) {
				var sortAndRemoveImportsInfo = IdeApp.CommandService.GetCommandInfo (Commands.SortAndRemoveImports);
				sortAndRemoveImportsInfo.Enabled = true;
				ainfo.Add (sortAndRemoveImportsInfo, new Action (async delegate {
					await RemoveAndSortUsingsHandler.SortAndRemoveUnusedImports (analysisDocument, cancelToken);
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
						FindReferencesHandler.FindRefs (new [] { SymbolAndProjectId.Create (sym, analysisDocument.Project.Id) }, analysisDocument.Project.Solution).Ignore ();
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
