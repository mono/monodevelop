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

namespace MonoDevelop.CodeActions
{
	internal static class CodeFixMenuService
	{
		static readonly Dictionary<string, int> CodeActionUsages = new Dictionary<string, int> ();

		static CodeFixMenuService ()
		{
			var usages = PropertyService.Get ("CodeActionUsages", new Properties ());
			foreach (var key in usages.Keys) {
				CodeActionUsages [key] = usages.Get<int> (key);
			}
		}

		static void ConfirmUsage (string id)
		{
			if (id == null)
				return;
			if (!CodeActionUsages.ContainsKey (id)) {
				CodeActionUsages [id] = 1;
			} else {
				CodeActionUsages [id]++;
			}
			var usages = PropertyService.Get ("CodeActionUsages", new Properties ());
			usages.Set (id, CodeActionUsages [id]);
		}

		public static async Task<CodeFixMenu> CreateFixMenu (TextEditor editor, CodeActionContainer fixes, CancellationToken cancellationToken = default(CancellationToken))
		{
			var menu = new CodeFixMenu ();

			if (editor.DocumentContext.AnalysisDocument == null) {
				return menu;
			}

			int mnemonic = 1;

			var suppressLabel = GettextCatalog.GetString ("_Suppress");
			var suppressMenu = new CodeFixMenu (suppressLabel);

			var fixAllLabel = GettextCatalog.GetString ("_Fix all");
			var fixAllMenu = new CodeFixMenu (fixAllLabel);

			var configureLabel = GettextCatalog.GetString ("_Options");
			var configureMenu = new CodeFixMenu (configureLabel);

			foreach (var cfa in fixes.CodeFixActions) {
				// .SelectMany (x => x.Fixes).OrderByDescending (i => GetUsage (i.Action.EquivalenceKey))
				var state = cfa.FixAllState;
				var scopes = cfa.SupportedScopes;

				foreach (var fix in cfa.Fixes) {
					bool isSuppress = fix.Action is TopLevelSuppressionCodeAction;

					if (isSuppress) {
						AddFixMenuItem (editor, suppressMenu, ref mnemonic, fix.Action);
						continue;
					}

					AddFixMenuItem (editor, menu, ref mnemonic, fix.Action);

					var diag = fix.PrimaryDiagnostic;
					var configurable = !DescriptorHasTag (diag.Descriptor, WellKnownDiagnosticTags.NotConfigurable);

					var descriptor = BuiltInCodeDiagnosticProvider.GetCodeDiagnosticDescriptor (diag.Id);

					if (descriptor != null && configurable) {
						var optionsMenuItem = new CodeFixMenuEntry (GettextCatalog.GetString ("_Configure Rule \u2018{0}\u2019", diag.Descriptor.Title),
							delegate {
								IdeApp.Workbench.ShowGlobalPreferencesDialog (null, "C#", dialog => {
									var panel = dialog.GetPanel<CodeIssuePanel> ("C#");
									if (panel == null)
										return;
									panel.Widget.SelectCodeIssue (fix.PrimaryDiagnostic.Descriptor.Id);
								});
							});
						configureMenu.Add (optionsMenuItem);
					}

					if (!scopes.Contains (FixAllScope.Document))
						continue;
					
					// FIXME: No global undo yet to support fixall in project/solution
					var fixState = state.WithScopeAndEquivalenceKey (FixAllScope.Document, fix.Action.EquivalenceKey);

					var provider = state.FixAllProvider;
					if (provider == null)
						continue;

					// FIXME: Use a real progress tracker.
					var fixAll = await provider.GetFixAsync (fixState.CreateFixAllContext (new Microsoft.CodeAnalysis.Shared.Utilities.ProgressTracker (), cancellationToken));
					AddFixMenuItem (editor, fixAllMenu, ref mnemonic, fixAll);
				}
			}

			bool first = true;
			foreach (var refactoring in fixes.CodeRefactoringActions) {
				if (first) {
					if (menu.Items.Count > 0)
						menu.Add (CodeFixMenuEntry.Separator);
					first = false;
				}

				foreach (var action in refactoring.Actions)
					AddFixMenuItem (editor, menu, ref mnemonic, action);
			}

			first = true;

			if (fixAllMenu.Items.Count != 0) {
				if (first)
					menu.Add (CodeFixMenuEntry.Separator);
				menu.Add (fixAllMenu);
				first = false;
			}
			if (suppressMenu.Items.Count != 0) {
				if (first)
					menu.Add (CodeFixMenuEntry.Separator);
				menu.Add (suppressMenu);
				first = false;
			}
			if (configureMenu.Items.Count != 0) {
				if (first)
					menu.Add (CodeFixMenuEntry.Separator);
				menu.Add (configureMenu);
				first = false;
			}
			return menu;
		}

		static async Task FixAll (TextEditor editor, ValidCodeDiagnosticAction fix, FixAllProvider provider, DiagnosticAnalyzer diagnosticAnalyzer)
		{
			var diagnosticIds = diagnosticAnalyzer.SupportedDiagnostics.Select (d => d.Id).ToImmutableHashSet ();

			var analyzers = new [] { diagnosticAnalyzer }.ToImmutableArray ();

			var codeFixService = CompositionManager.GetExportedValue<ICodeFixService> () as CodeFixService;
			var fixAllDiagnosticProvider = codeFixService.CreateFixAllState (
				provider,
				editor.DocumentContext.AnalysisDocument,
				FixAllProviderInfo.Create (null),
				null,
				null,
				async (doc, diagnostics, token) => await GetDiagnosticsForDocument (analyzers, doc, diagnostics, token).ConfigureAwait (false),
				(Project arg1, bool arg2, ImmutableHashSet<string> arg3, CancellationToken arg4) => {
					return Task.FromResult ((IEnumerable<Diagnostic>)new Diagnostic[] { });
				}).DiagnosticProvider;

			var ctx = new FixAllContext (
				editor.DocumentContext.AnalysisDocument,
				fix.Diagnostic.GetCodeFixProvider (),
				FixAllScope.Document,
				fix.CodeAction.EquivalenceKey,
				diagnosticIds,
				fixAllDiagnosticProvider,
				default (CancellationToken)
			);

			var fixAll = await provider.GetFixAsync (ctx);
			using (var undo = editor.OpenUndoGroup ()) {
				await CodeDiagnosticDescriptor.RunAction (editor.DocumentContext, fixAll, default (CancellationToken));
			}
		}

		static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsForDocument (ImmutableArray<DiagnosticAnalyzer> analyzers, Microsoft.CodeAnalysis.Document doc, ImmutableHashSet<string> diagnostics, CancellationToken token)
		{
			var sol = doc.Project.Solution;
			var options = new CompilationWithAnalyzersOptions (
				new WorkspaceAnalyzerOptions (
					new AnalyzerOptions (ImmutableArray<AdditionalText>.Empty),
					sol.Options,
					sol),
				delegate (Exception exception, DiagnosticAnalyzer analyzer, Diagnostic diag) {
					LoggingService.LogError ("Exception in diagnostic analyzer " + diag.Id + ":" + diag.GetMessage (), exception);
				},
				true,
				false
			);

			var model = await doc.GetSemanticModelAsync (token).ConfigureAwait (false);
			var compilationWithAnalyzer = model.Compilation.WithAnalyzers (analyzers, options);

			var diagnosticList = new List<Diagnostic> ();
			diagnosticList.AddRange (await compilationWithAnalyzer.GetAnalyzerSemanticDiagnosticsAsync (model, null, token).ConfigureAwait (false));
			diagnosticList.AddRange (await compilationWithAnalyzer.GetAnalyzerSemanticDiagnosticsAsync (model, null, token).ConfigureAwait (false));

			return diagnosticList.ToImmutableArray ();
		}

		static bool DescriptorHasTag (DiagnosticDescriptor desc, string tag)
		{
			return desc.CustomTags.Any (c => CultureInfo.InvariantCulture.CompareInfo.Compare (c, tag) == 0);
		}

		static CodeFixMenuEntry CreateFixMenuEntry (TextEditor editor, CodeAction fix)
		{
			int mnemonic = -1;
			return CreateFixMenuEntry (editor, fix, ref mnemonic);
		}

		static CodeFixMenuEntry CreateFixMenuEntry (TextEditor editor, CodeAction fix, ref int mnemonic)
		{
			var label = mnemonic < 0 ? fix.Title : CreateLabel (fix.Title, ref mnemonic);
			var item = new CodeFixMenuEntry (label, async delegate {
				await new ContextActionRunner (editor, fix).Run ();
				ConfirmUsage (fix.EquivalenceKey);
			});

			item.ShowPreviewTooltip = delegate (Xwt.Rectangle rect) {
				RefactoringPreviewTooltipWindow.ShowPreviewTooltip (editor, fix, rect);
			};

			return item;
		}

		static void AddFixMenuItem (TextEditor editor, CodeFixMenu menu, CodeAction fix)
		{
			int _m = 0;
			AddFixMenuItem (editor, menu, ref _m, fix);
		}

		static void AddFixMenuItem (TextEditor editor, CodeFixMenu menu, ref int mnemonic, CodeAction fix)
		{
			var nested = fix as CodeAction.CodeActionWithNestedActions;
			if (nested != null) {
				AddNestedFixMenu (editor, menu, nested);
				return;
			}

			menu.Add (CreateFixMenuEntry (editor, fix, ref mnemonic));
		}

		static void AddNestedFixMenu (TextEditor editor, CodeFixMenu menu, CodeAction.CodeActionWithNestedActions fixes)
		{
			int subMnemonic = 0;
			var subMenu = new CodeFixMenu (fixes.Title);
			foreach (var fix in fixes.NestedCodeActions) {
				AddFixMenuItem (editor, subMenu, ref subMnemonic, fix);
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

		static int GetUsage (string id)
		{
			int result;
			if (id == null || !CodeActionUsages.TryGetValue (id, out result))
				return 0;
			return result;
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
				var insertionAction = act as InsertionAction;
				if (insertionAction != null) {
					var insertion = await insertionAction.CreateInsertion (token).ConfigureAwait (false);

					var document = await IdeApp.Workbench.OpenDocument (insertion.Location.SourceTree.FilePath, documentContext.Project);
					var parsedDocument = await document.UpdateParseDocument ();
					if (parsedDocument != null) {
						var insertionPoints = InsertionPointService.GetInsertionPoints (
							document.Editor,
							parsedDocument,
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
