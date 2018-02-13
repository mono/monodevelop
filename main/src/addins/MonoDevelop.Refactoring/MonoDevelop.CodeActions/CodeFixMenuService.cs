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

			foreach (var fix in fixes.CodeFixActions.OrderByDescending (i => GetUsage (i.CodeAction.EquivalenceKey))) {
				AddFixMenuItem (editor, menu, ref mnemonic, fix.CodeAction);
			}

			bool first = true;
			foreach (var fix in fixes.CodeRefactoringActions) {
				if (first) {
					if (menu.Items.Count > 0)
						menu.Add (CodeFixMenuEntry.Separator);
					first = false;
				}

				AddFixMenuItem (editor, menu, ref mnemonic, fix.CodeAction);
			}

			var warningsAtCaret = (await editor.DocumentContext.AnalysisDocument.GetSemanticModelAsync (cancellationToken))
				.GetDiagnostics (new TextSpan (editor.CaretOffset, 0))
				.Where (diag => diag.Severity == DiagnosticSeverity.Warning).ToList ();

			var caretSpan = new TextSpan (editor.CaretOffset, 0);

			first = true;
			foreach (var warning in warningsAtCaret) {
				if (string.IsNullOrWhiteSpace (warning.Descriptor.Title.ToString ()))
					continue;
				var label = GettextCatalog.GetString ("_Options for \u2018{0}\u2019", warning.Descriptor.Title);
				var subMenu = new CodeFixMenu (label);

				await AddSuppressionMenuItems (subMenu, editor, warning, caretSpan);

				if (subMenu.Items.Count > 0) {

					if (first) {
						menu.Add (CodeFixMenuEntry.Separator);
						first = false;
					}

					menu.Add (subMenu);
				}
			}

			first = true;
			foreach (var diag in fixes.DiagnosticsAtCaret) {
				if (string.IsNullOrWhiteSpace (diag.Descriptor.Title.ToString ()))
					continue;

				var notConfigurable = DescriptorHasTag (diag.Descriptor, WellKnownDiagnosticTags.NotConfigurable);

				var label = GettextCatalog.GetString ("_Options for \u2018{0}\u2019", diag.Descriptor.Title);
				var subMenu = new CodeFixMenu (label);

				if (first) {
					menu.Add (CodeFixMenuEntry.Separator);
					first = false;
				}

				await AddSuppressionMenuItems (subMenu, editor, diag, caretSpan);

				var descriptor = BuiltInCodeDiagnosticProvider.GetCodeDiagnosticDescriptor (diag.Id);

				if (descriptor != null && IsConfigurable (diag.Descriptor)) {
					var optionsMenuItem = new CodeFixMenuEntry (GettextCatalog.GetString ("_Configure Rule"),
						delegate {
							IdeApp.Workbench.ShowGlobalPreferencesDialog (null, "C#", dialog => {
								var panel = dialog.GetPanel<CodeIssuePanel> ("C#");
								if (panel == null)
									return;
								panel.Widget.SelectCodeIssue (diag.Descriptor.Id);
							});
						});
					subMenu.Add (optionsMenuItem);
				}

				foreach (var fix in fixes.CodeFixActions.OrderByDescending (i => GetUsage (i.CodeAction.EquivalenceKey))) {
					if (cancellationToken.IsCancellationRequested)
						return null;
					var provider = fix.Diagnostic.GetCodeFixProvider ().GetFixAllProvider ();
					if (provider == null)
						continue;
					
					if (!provider.GetSupportedFixAllScopes ().Contains (FixAllScope.Document))
						continue;
					
					var language = editor.DocumentContext.AnalysisDocument.Project.Language;
					var diagnosticdDescriptor = fix.Diagnostic?.GetCodeDiagnosticDescriptor (language);
					if (diagnosticdDescriptor == null)
						continue;

					var subMenu2 = new CodeFixMenu (GettextCatalog.GetString ("Fix all"));

					var diagnosticAnalyzer = diagnosticdDescriptor.GetProvider ();
					if (!diagnosticAnalyzer.SupportedDiagnostics.Contains (diag.Descriptor))
						continue;

					var menuItem = new CodeFixMenuEntry (
						GettextCatalog.GetString ("In _Document"),
						async delegate { await FixAll (editor, fix, provider, diagnosticAnalyzer); }
					);
					subMenu2.Add (menuItem);
					subMenu.Add (CodeFixMenuEntry.Separator);
					subMenu.Add (subMenu2);
				}

				menu.Add (subMenu);
			}
			return menu;
		}

		static async Task FixAll (TextEditor editor, ValidCodeDiagnosticAction fix, FixAllProvider provider, DiagnosticAnalyzer diagnosticAnalyzer)
		{
			var diagnosticIds = diagnosticAnalyzer.SupportedDiagnostics.Select (d => d.Id).ToImmutableHashSet ();

			var analyzers = new [] { diagnosticAnalyzer }.ToImmutableArray ();

			var dict = ImmutableDictionary<Document, ImmutableArray<Diagnostic>>.Empty;
			var doc = editor.DocumentContext.AnalysisDocument;
			var diagnostics = await GetDiagnosticsForDocument (analyzers, doc, diagnosticIds, CancellationToken.None);

			dict = dict.Add (doc, diagnostics);
			var fixAllDiagnosticProvider = new FixAllState.FixMultipleDiagnosticProvider (dict);

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

		static async Task AddSuppressionMenuItems (CodeFixMenu menu, TextEditor editor, Diagnostic diag, TextSpan span)
		{
			var workspace = editor.DocumentContext.AnalysisDocument.Project.Solution.Workspace;
			var language = editor.DocumentContext.AnalysisDocument.Project.Language;
			var mefExporter = (IMefHostExportProvider)workspace.Services.HostServices;

			//TODO: cache this
			var suppressionProviders = mefExporter.GetExports<ISuppressionFixProvider, CodeChangeProviderMetadata> ()
				.ToPerLanguageMapWithMultipleLanguages();

			foreach (var suppressionProvider in suppressionProviders[LanguageNames.CSharp].Select (lz => lz.Value)) {
				if (!suppressionProvider.CanBeSuppressedOrUnsuppressed (diag)) {
					continue;
				}
				try {
					var fixes = await suppressionProvider.GetSuppressionsAsync (editor.DocumentContext.AnalysisDocument, span, new [] { diag }, default (CancellationToken)).ConfigureAwait (false);
					foreach (var fix in fixes) {
						AddFixMenuItem (editor, menu, fix.Action);
					}
				} catch (Exception e) {
					LoggingService.LogError ("Error while adding fixes", e);
				}
			}
		}

		static bool DescriptorHasTag (DiagnosticDescriptor desc, string tag)
		{
			return desc.CustomTags.Any (c => CultureInfo.InvariantCulture.CompareInfo.Compare (c, tag) == 0);
		}

		static bool IsConfigurable (DiagnosticDescriptor desc)
		{
			return !DescriptorHasTag (desc, WellKnownDiagnosticTags.NotConfigurable);
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
