//
// CodeFixMenu.cs
//
// Author:
//       Mikayla Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2017 Microsoft Corp.
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
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using MonoDevelop.CodeIssues;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Refactoring;
using RefactoringEssentials;
using MonoDevelop.AnalysisCore;

namespace MonoDevelop.CodeActions
{
	internal static class CodeFixMenuService
	{
		public static CodeFixMenu CreateFixMenu (TextEditor editor, CodeActionContainer fixes, CancellationToken cancellationToken = default(CancellationToken))
		{
			var menu = new CodeFixMenu ();

			if (editor.DocumentContext.AnalysisDocument == null) {
				return menu;
			}

			var options = ((MonoDevelopWorkspaceDiagnosticAnalyzerProviderService)Ide.Composition.CompositionManager.GetExportedValue<IWorkspaceDiagnosticAnalyzerProviderService> ()).GetOptionsAsync ().Result;
			int mnemonic = 1;

			var suppressLabel = GettextCatalog.GetString ("_Suppress");
			var suppressMenu = new CodeFixMenu (suppressLabel);

			var fixAllLabel = GettextCatalog.GetString ("_Fix all");
			var fixAllMenu = new CodeFixMenu (fixAllLabel);

			var configureLabel = GettextCatalog.GetString ("_Options");
			var configureMenu = new CodeFixMenu (configureLabel);
			var fixAllTasks = new List<Task<CodeAction>> ();

			foreach (var cfa in fixes.CodeFixActions) {
				var scopes = cfa.SupportedScopes;

				// FIXME: No global undo yet to support fixall in project/solution
				var state = scopes.Contains (FixAllScope.Document) ? cfa.FixAllState : null;

				foreach (var fix in cfa.Fixes) {
					var diag = fix.PrimaryDiagnostic;
					if (options.TryGetDiagnosticDescriptor (diag.Id, out var descriptor) && !diag.Descriptor.IsEnabledByDefault)
						continue;
					
					bool isSuppress = fix.Action is TopLevelSuppressionCodeAction;

					CodeFixMenu fixMenu;
					FixAllState fixState;
					if (isSuppress) {
						fixMenu = suppressMenu;
						fixState = null;
					} else {
						fixMenu = menu;
						fixState = state;
					}

					AddFixMenuItem (editor, fixMenu, fixAllMenu, ref mnemonic, fix.Action, fixState, cancellationToken);
				}
			}

			bool first = true;
			foreach (var refactoring in fixes.CodeRefactoringActions) {
				if (options.TryGetRefactoringDescriptor (refactoring.GetType (), out var descriptor) && !descriptor.IsEnabled)
					continue;

				if (first) {
					if (menu.Items.Count > 0)
						menu.Add (CodeFixMenuEntry.Separator);
					first = false;
				}

				foreach (var action in refactoring.Actions) {
					AddFixMenuItem (editor, menu, null, ref mnemonic, action, null, cancellationToken);
				}
			}

			first = true;

			AddMenuWithSeparatorIfNeeded (fixAllMenu, menu, ref first);
			AddMenuWithSeparatorIfNeeded (suppressMenu, menu, ref first);
			AddMenuWithSeparatorIfNeeded (configureMenu, menu, ref first);

			return menu;
		}

		static void AddMenuWithSeparatorIfNeeded (CodeFixMenu toAdd, CodeFixMenu into, ref bool first)
		{
			if (toAdd.Items.Count == 0)
				return;
			
			if (first)
				into.Add (CodeFixMenuEntry.Separator);
			into.Add (toAdd);
			first = false;
		}

		static bool DescriptorHasTag (DiagnosticDescriptor desc, string tag)
		{
			return desc.CustomTags.Any (c => CultureInfo.InvariantCulture.CompareInfo.Compare (c, tag) == 0);
		}

		static CodeFixMenuEntry CreateFixMenuEntry (TextEditor editor, CodeAction fix, ref int mnemonic)
		{
			var label = mnemonic < 0 ? fix.Title : CreateLabel (fix.Title, ref mnemonic);
			var item = new CodeFixMenuEntry (label, async delegate {
				await new ContextActionRunner (editor, fix).Run ();
			});

			item.ShowPreviewTooltip = delegate (Xwt.Rectangle rect) {
				RefactoringPreviewTooltipWindow.ShowPreviewTooltip (editor, fix, rect);
			};

			return item;
		}

		static CodeFixMenuEntry CreateFixAllMenuEntry (TextEditor editor, FixAllState fixState, ref int mnemonic, CancellationToken token)
		{
			var provider = fixState?.FixAllProvider;
			if (provider == null)
				return null;
			

			var title = fixState.GetDefaultFixAllTitle ();
			var label = mnemonic < 0 ? title : CreateLabel (title, ref mnemonic);

			var item = new CodeFixMenuEntry (label, async delegate {
				// Task.Run here so we don't end up binding the whole document on popping the menu, also there is no cancellation token support
				var fix = Task.Run (() => {
					var context = fixState.CreateFixAllContext (new RoslynProgressTracker (), token);
					return provider.GetFixAsync (context);
				});

				await new ContextActionRunner (editor, await fix).Run ();
			});

			return item;
		}

		static void AddFixMenuItem (TextEditor editor, CodeFixMenu menu, CodeFixMenu fixAllMenu, ref int mnemonic, CodeAction fix, FixAllState fixState, CancellationToken token)
		{
			if (fix is CodeAction.CodeActionWithNestedActions nested) {
				// Inline code actions if they are, otherwise add a nested fix menu
				if (nested.IsInlinable) {
					int actionCount = nested.NestedCodeActions.Length;
					foreach (var nestedFix in nested.NestedCodeActions) {
						var nestedFixState = actionCount > 1 && nestedFix.EquivalenceKey == null ? null : fixState;

						AddFixMenuItem (editor, menu, fixAllMenu, ref mnemonic, nestedFix, nestedFixState, token);
					}
					return;
				}

				if (nested.NestedCodeActions.Length > 0)
					AddNestedFixMenu (editor, menu, fixAllMenu, nested, fixState, token);
				return;
			}

			menu.Add (CreateFixMenuEntry (editor, fix, ref mnemonic));

			// TODO: Add support for more than doc when we have global undo.
			fixState = fixState?.WithScopeAndEquivalenceKey (FixAllScope.Document, fix.EquivalenceKey);
			var fixAllMenuEntry = CreateFixAllMenuEntry (editor, fixState, ref mnemonic, token);
			if (fixAllMenuEntry != null) {
				fixAllMenu.Add (new CodeFixMenuEntry (fix.Message, null));
				fixAllMenu.Add (fixAllMenuEntry);
			}
		}

		static void AddNestedFixMenu (TextEditor editor, CodeFixMenu menu, CodeFixMenu fixAllMenu, CodeAction.CodeActionWithNestedActions fixes, FixAllState fixState, CancellationToken token)
		{
			int subMnemonic = 0;
			var subMenu = new CodeFixMenu (fixes.Title);
			foreach (var fix in fixes.NestedCodeActions) {
				AddFixMenuItem (editor, subMenu, fixAllMenu, ref subMnemonic, fix, fixState, token);
			}
			menu.Add (subMenu);
		}

		static string CreateLabel (string title, ref int mnemonic)
		{
			var escapedLabel = title.Replace ("_", "__");
#if MAC
			return escapedLabel;
#else
			return (mnemonic <= 10) ? "_" + mnemonic++ % 10 + " \u2013 " + escapedLabel : "  " + escapedLabel;
#endif
		}

		internal class ContextActionRunner
		{
			readonly CodeAction act;
			readonly TextEditor editor;
			DocumentContext documentContext;

			public ContextActionRunner (TextEditor editor, CodeAction act)
			{
				this.editor = editor;
				this.act = act;
				documentContext = editor.DocumentContext;
			}

			public async Task Run ()
			{
				var token = default (CancellationToken);
				if (act is InsertionAction insertionAction) {
					var insertion = await insertionAction.CreateInsertion (token).ConfigureAwait (false);

					var document = await IdeApp.Workbench.OpenDocument (insertion.Location.SourceTree.FilePath, documentContext.Project);
					var parsedDocument = await document.UpdateParseDocument ();
					var model = await document.AnalysisDocument.GetSemanticModelAsync (token);
					if (parsedDocument != null) {
						var insertionPoints = InsertionPointService.GetInsertionPoints (
							document.Editor,
							model,
							insertion.Type,
							insertion.Location.SourceSpan.Start
						);

						var options = new InsertionModeOptions (
							insertionAction.Title,
							insertionPoints,
							point => {
								if (!point.Success)
									return;
								var node = Formatter.Format (insertion.Node, document.RoslynWorkspace, document.GetOptionSet (), token);
								point.InsertionPoint.Insert (document.Editor, document, node.ToString ());
								// document = await Simplifier.ReduceAsync(document.AnalysisDocument, Simplifier.Annotation, cancellationToken: token).ConfigureAwait(false);
							}
						);

						document.Editor.StartInsertionMode (options);
						return;
					}
				}

				var oldSolution = documentContext.AnalysisDocument.Project.Solution;
				var updatedSolution = oldSolution;
				using (var undo = editor.OpenUndoGroup ()) {
					foreach (var operation in await act.GetOperationsAsync (token)) {
						var applyChanges = operation as ApplyChangesOperation;
						if (applyChanges == null) {
							operation.TryApply (documentContext.RoslynWorkspace, new RoslynProgressTracker (), token);
							continue;
						}
						if (updatedSolution == oldSolution) {
							updatedSolution = applyChanges.ChangedSolution;
						}
						operation.TryApply (documentContext.RoslynWorkspace, new RoslynProgressTracker (), token);
					}
				}
				await TryStartRenameSession (documentContext.RoslynWorkspace, oldSolution, updatedSolution, token);
			}

			static IEnumerable<DocumentId> GetChangedDocuments (Solution newSolution, Solution oldSolution)
			{
				if (newSolution != null) {
					var solutionChanges = newSolution.GetChanges (oldSolution);
					foreach (var projectChanges in solutionChanges.GetProjectChanges ()) {
						foreach (var documentId in projectChanges.GetChangedDocuments ()) {
							yield return documentId;
						}
					}
				}
			}

			async Task TryStartRenameSession (Workspace workspace, Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
			{
				var changedDocuments = GetChangedDocuments (newSolution, oldSolution);
				foreach (var documentId in changedDocuments) {
					var document = newSolution.GetDocument (documentId);
					var root = await document.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
					SyntaxToken? renameTokenOpt = root.GetAnnotatedNodesAndTokens (RenameAnnotation.Kind)
													  .Where (s => s.IsToken)
													  .Select (s => s.AsToken ())
													  .Cast<SyntaxToken?> ()
													  .FirstOrDefault ();
					if (renameTokenOpt.HasValue) {
						var latestDocument = workspace.CurrentSolution.GetDocument (documentId);
						var latestModel = await latestDocument.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
						var latestRoot = await latestDocument.GetSyntaxRootAsync (cancellationToken).ConfigureAwait (false);
						await Runtime.RunInMainThread (async delegate {
							try {
								var node = latestRoot.FindNode (renameTokenOpt.Value.Parent.Span, false, false);
								if (node == null)
									return;
								var info = latestModel.GetSymbolInfo (node);
								var sym = info.Symbol ?? latestModel.GetDeclaredSymbol (node);
								if (sym != null) {
									await new Refactoring.Rename.RenameRefactoring ().Rename (sym);
								} else {
									var links = new List<TextLink> ();
									var link = new TextLink ("name");
									link.AddLink (new TextSegment (node.Span.Start, node.Span.Length));
									links.Add (link);
									var oldVersion = editor.Version;
									editor.StartTextLinkMode (new TextLinkModeOptions (links, (arg) => {
										//If user cancel renaming revert changes
										if (!arg.Success) {
											var textChanges = editor.Version.GetChangesTo (oldVersion).ToList ();
											foreach (var change in textChanges) {
												foreach (var v in change.TextChanges.Reverse ()) {
													editor.ReplaceText (v.Offset, v.RemovalLength, v.InsertedText);
												}
											}
										}
									}) { TextLinkPurpose = TextLinkPurpose.Rename });
								}
							} catch (Exception ex) {
								LoggingService.LogError ("Error while renaming " + renameTokenOpt.Value.Parent, ex);
							}
						});
						return;
					}
				}
			}
		}
	}

	internal class CodeFixMenuEntry
	{
		public static readonly CodeFixMenuEntry Separator = new CodeFixMenuEntry ("-", null);
		public readonly string Label;

		public readonly Action Action;
		public Action<Xwt.Rectangle> ShowPreviewTooltip;

		public CodeFixMenuEntry (string label, Action action)
		{
			Label = label;
			Action = action;
		}
	}

	internal class CodeFixMenu : CodeFixMenuEntry
	{
		readonly List<CodeFixMenuEntry> items = new List<CodeFixMenuEntry> ();

		public IReadOnlyList<CodeFixMenuEntry> Items {
			get {
				return items;
			}
		}

		public CodeFixMenu () : base (null, null)
		{
		}

		public CodeFixMenu (string label) : base (label, null)
		{
		}

		public void Add (CodeFixMenuEntry entry)
		{
			items.Add (entry);
		}

		public object MotionNotifyEvent { get; set; }
	}
}
