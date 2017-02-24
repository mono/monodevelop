// 
// QuickFixEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Components.Commands;
using System.Linq;
using MonoDevelop.Refactoring;
using System.Threading;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis.CodeFixes;
using MonoDevelop.CodeIssues;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide;
using Microsoft.CodeAnalysis.CodeActions;
using RefactoringEssentials;
using MonoDevelop.AnalysisCore;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Extension;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis;
using System.Reflection;
using MonoDevelop.Ide.Gui;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoDevelop.Core.Text;

namespace MonoDevelop.CodeActions
{
	class CodeActionEditorExtension : TextEditorExtension
	{
		const int menuTimeout = 250;
		uint smartTagPopupTimeoutId;
		uint menuCloseTimeoutId;

		static CodeActionEditorExtension ()
		{
			var usages = PropertyService.Get<Properties> ("CodeActionUsages", new Properties ());
			foreach (var key in usages.Keys) {
				CodeActionUsages [key] = usages.Get<int> (key);
			}
		}

		void CancelSmartTagPopupTimeout ()
		{

			if (smartTagPopupTimeoutId != 0) {
				GLib.Source.Remove (smartTagPopupTimeoutId);
				smartTagPopupTimeoutId = 0;
			}
		}

		void CancelMenuCloseTimer ()
		{
			if (menuCloseTimeoutId != 0) {
				GLib.Source.Remove (menuCloseTimeoutId);
				menuCloseTimeoutId = 0;
			}
		}

		void RemoveWidget ()
		{
			if (currentSmartTag != null) {
				Editor.RemoveMarker (currentSmartTag);
				currentSmartTag.CancelPopup -= CurrentSmartTag_CancelPopup;
				currentSmartTag.ShowPopup -= CurrentSmartTag_ShowPopup;

				currentSmartTag = null;
				currentSmartTagBegin = -1;
			}
			CancelSmartTagPopupTimeout ();
		}

		public override void Dispose ()
		{
			CancelMenuCloseTimer ();
			CancelQuickFixTimer ();
			HidePreviewTooltip ();
			Editor.CaretPositionChanged -= HandleCaretPositionChanged;
			Editor.SelectionChanged -= HandleSelectionChanged;
			DocumentContext.DocumentParsed -= HandleDocumentDocumentParsed;
			Editor.MouseMoved -= HandleBeginHover;
			Editor.TextChanged -= Editor_TextChanged;
			Editor.EndAtomicUndoOperation -= Editor_EndAtomicUndoOperation;
			RemoveWidget ();
			base.Dispose ();
		}

		static readonly Dictionary<string, int> CodeActionUsages = new Dictionary<string, int> ();

		static void ConfirmUsage (string id)
		{
			if (id == null)
				return;
			if (!CodeActionUsages.ContainsKey (id)) {
				CodeActionUsages [id] = 1;
			} else {
				CodeActionUsages [id]++;
			}
			var usages = PropertyService.Get<Properties> ("CodeActionUsages", new Properties ());
			usages.Set (id, CodeActionUsages [id]);
		}

		internal static int GetUsage (string id)
		{
			int result;
			if (id == null || !CodeActionUsages.TryGetValue (id, out result))
				return 0;
			return result;
		}

		public void CancelQuickFixTimer ()
		{
			quickFixCancellationTokenSource.Cancel ();
			quickFixCancellationTokenSource = new CancellationTokenSource ();
			smartTagTask = null;
		}

		Task<CodeActionContainer> smartTagTask;
		CancellationTokenSource quickFixCancellationTokenSource = new CancellationTokenSource ();
		List<CodeDiagnosticFixDescriptor> codeFixes;

		void HandleCaretPositionChanged (object sender, EventArgs e)
		{
			if (Editor.IsInAtomicUndo)
				return;
			CancelQuickFixTimer ();
			if (AnalysisOptions.EnableFancyFeatures && DocumentContext.ParsedDocument != null) {
				var token = quickFixCancellationTokenSource.Token;
				var curOffset = Editor.CaretOffset;
				if (HasCurrentFixes) {
					foreach (var fix in GetCurrentFixes ().AllValidCodeActions) {
						if (!fix.ValidSegment.Contains (curOffset)) {
							RemoveWidget ();
							break;
						}
					}
				}

				var loc = Editor.CaretOffset;
				var ad = DocumentContext.AnalysisDocument;
				if (ad == null) {
					return;
				}

				TextSpan span;

				if (Editor.IsSomethingSelected) {
					var selectionRange = Editor.SelectionRange;
					span = selectionRange.Offset >= 0 ? TextSpan.FromBounds (selectionRange.Offset, selectionRange.EndOffset) : TextSpan.FromBounds (loc, loc);
				} else {
					span = TextSpan.FromBounds (loc, loc);
				}

				var diagnosticsAtCaret =
					Editor.GetTextSegmentMarkersAt (Editor.CaretOffset)
						  .OfType<IGenericTextSegmentMarker> ()
						  .Select (rm => rm.Tag)
						  .OfType<DiagnosticResult> ()
						  .Select (dr => dr.Diagnostic)
						  .ToList ();

				var errorList = Editor
					.GetTextSegmentMarkersAt (Editor.CaretOffset)
					.OfType<IErrorMarker> ()
					.Where (rm => !string.IsNullOrEmpty (rm.Error.Id)).ToList ();
				int editorLength = Editor.Length;

				smartTagTask = Task.Run (async delegate {
					try {
						var codeIssueFixes = new List<ValidCodeDiagnosticAction> ();
						var diagnosticIds = diagnosticsAtCaret.Select (diagnostic => diagnostic.Id).Concat (errorList.Select (rm => rm.Error.Id)).ToList ();
						if (codeFixes == null) {
							codeFixes = (await CodeRefactoringService.GetCodeFixesAsync (DocumentContext, CodeRefactoringService.MimeTypeToLanguage (Editor.MimeType), token).ConfigureAwait (false)).ToList ();
						}
						foreach (var cfp in codeFixes) {
							if (token.IsCancellationRequested)
								return CodeActionContainer.Empty;
							var provider = cfp.GetCodeFixProvider ();
							if (!provider.FixableDiagnosticIds.Any (diagnosticIds.Contains))
								continue;
							try {
								var groupedDiagnostics = diagnosticsAtCaret
									.Concat (errorList.Select (em => em.Error.Tag)
									.OfType<Diagnostic> ())
									.GroupBy (d => d.Location.SourceSpan);
								foreach (var g in groupedDiagnostics) {
									if (token.IsCancellationRequested)
										return CodeActionContainer.Empty;
									var diagnosticSpan = g.Key;

									var validDiagnostics = g.Where (d => provider.FixableDiagnosticIds.Contains (d.Id)).ToImmutableArray ();
									if (validDiagnostics.Length == 0)
										continue;
									await provider.RegisterCodeFixesAsync (new CodeFixContext (ad, diagnosticSpan, validDiagnostics, (ca, d) => codeIssueFixes.Add (new ValidCodeDiagnosticAction (cfp, ca, validDiagnostics, diagnosticSpan)), token));

									// TODO: Is that right ? Currently it doesn't really make sense to run one code fix provider on several overlapping diagnostics at the same location
									//       However the generate constructor one has that case and if I run it twice the same code action is generated twice. So there is a dupe check problem there.
									// Work around for now is to only take the first diagnostic batch.
									break;
								}
							} catch (OperationCanceledException) {
								return CodeActionContainer.Empty;
							} catch (AggregateException ae) {
								ae.Flatten ().Handle (aex => aex is OperationCanceledException);
								return CodeActionContainer.Empty;
							} catch (Exception ex) {
								LoggingService.LogError ("Error while getting refactorings from code fix provider " + cfp.Name, ex);
								continue;
							}
						}
						var codeActions = new List<ValidCodeAction> ();
						foreach (var action in await CodeRefactoringService.GetValidActionsAsync (Editor, DocumentContext, span, token).ConfigureAwait (false)) {
							codeActions.Add (action);
						}
						var codeActionContainer = new CodeActionContainer (codeIssueFixes, codeActions, diagnosticsAtCaret);
						Application.Invoke (delegate {
							if (token.IsCancellationRequested)
								return;
							if (codeActionContainer.IsEmpty) {
								RemoveWidget ();
								return;
							}
							CreateSmartTag (codeActionContainer, loc);
						});
						return codeActionContainer;

					} catch (AggregateException ae) {
						ae.Flatten ().Handle (aex => aex is OperationCanceledException);
						return CodeActionContainer.Empty;
					} catch (OperationCanceledException) {
						return CodeActionContainer.Empty;
					} catch (TargetInvocationException ex) {
						if (ex.InnerException is OperationCanceledException)
							return CodeActionContainer.Empty;
						throw;
					}

				}, token);
			} else {
				RemoveWidget ();
			}
		}

		internal static bool IsAnalysisOrErrorFix (Microsoft.CodeAnalysis.CodeActions.CodeAction act)
		{
			return false;
		}

		internal class FixMenuEntry
		{
			public static readonly FixMenuEntry Separator = new FixMenuEntry ("-", null);
			public readonly string Label;

			public readonly System.Action Action;
			public Action<Xwt.Rectangle> ShowPreviewTooltip;

			public FixMenuEntry (string label, System.Action action)
			{
				this.Label = label;
				this.Action = action;
			}
		}

		internal class FixMenuDescriptor : FixMenuEntry
		{
			readonly List<FixMenuEntry> items = new List<FixMenuEntry> ();

			public IReadOnlyList<FixMenuEntry> Items {
				get {
					return items;
				}
			}

			public FixMenuDescriptor () : base (null, null)
			{
			}

			public FixMenuDescriptor (string label) : base (label, null)
			{
			}

			public void Add (FixMenuEntry entry)
			{
				items.Add (entry);
			}

			public object MotionNotifyEvent {
				get;
				set;
			}
		}

		void PopupQuickFixMenu (Gdk.EventButton evt, Action<FixMenuDescriptor> menuAction)
		{
			FixMenuDescriptor menu = new FixMenuDescriptor ();
			var fixMenu = menu;
			//ResolveResult resolveResult;
			//ICSharpCode.NRefactory.CSharp.AstNode node;
			int items = 0;

			//			if (AddPossibleNamespace != null) {
			//				AddPossibleNamespace (Editor, DocumentContext, menu);
			//				items = menu.Items.Count;
			//			}

			PopulateFixes (fixMenu, ref items);

			if (items == 0) {
				return;
			}
			Editor.SuppressTooltips = true;
			if (menuAction != null)
				menuAction (menu);

			var p = Editor.LocationToPoint (Editor.OffsetToLocation (currentSmartTagBegin));
			Gtk.Widget widget = Editor;
			var rect = new Gdk.Rectangle (
				(int)p.X + widget.Allocation.X,
				(int)p.Y + widget.Allocation.Y, 0, 0);

			ShowFixesMenu (widget, rect, menu);
		}

		bool ShowFixesMenu (Gtk.Widget parent, Gdk.Rectangle evt, FixMenuDescriptor entrySet)
		{
			if (parent == null || parent.GdkWindow == null) {
				Editor.SuppressTooltips = false;
				return true;
			}

			try {
				parent.GrabFocus ();
				int x, y;
				x = (int)evt.X;
				y = (int)evt.Y;

				// Explicitly release the grab because the menu is shown on the mouse position, and the widget doesn't get the mouse release event
				Gdk.Pointer.Ungrab (Gtk.Global.CurrentEventTime);
				var menu = CreateContextMenu (entrySet);
				HidePreviewTooltip ();
				menu.Show (parent, x, y, () => { Editor.SuppressTooltips = false; HidePreviewTooltip ();}, true);
			} catch (Exception ex) {
				LoggingService.LogError ("Error while context menu popup.", ex);
			}
			return true;
		}

		ContextMenu CreateContextMenu (FixMenuDescriptor entrySet)
		{
			var menu = new ContextMenu ();
			foreach (var item in entrySet.Items) {
				if (item == FixMenuEntry.Separator) {
					menu.Items.Add (new SeparatorContextMenuItem ());
					continue;
				}

				var menuItem = new ContextMenuItem (item.Label);
				menuItem.Context = item.Action;
				var subMenu = item as FixMenuDescriptor;
				if (subMenu != null) {
					menuItem.SubMenu = CreateContextMenu (subMenu);
					menuItem.Selected += delegate (object sender, Xwt.Rectangle e) {
						HidePreviewTooltip ();
					};
					menuItem.Deselected += delegate { HidePreviewTooltip (); };
				} else {
					menuItem.Clicked += (object sender, ContextMenuItemClickedEventArgs e) => ((System.Action)((ContextMenuItem)sender).Context) ();
					menuItem.Selected += delegate (object sender, Xwt.Rectangle e) {
						HidePreviewTooltip ();
						if (item.ShowPreviewTooltip != null) {
							item.ShowPreviewTooltip (e);
						}
					};
					menuItem.Deselected += delegate { HidePreviewTooltip (); };
				}
				menu.Items.Add (menuItem);
			}
			menu.Closed += delegate { HidePreviewTooltip (); };
			return menu;
		}

		void HidePreviewTooltip ()
		{
			if (currentPreviewWindow != null) {
				currentPreviewWindow.Destroy ();
				currentPreviewWindow = null;
			}
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

		internal class FixAllDiagnosticProvider : FixAllContext.DiagnosticProvider
		{
			private readonly ImmutableHashSet<string> _diagnosticIds;

			/// <summary>
			/// Delegate to fetch diagnostics for any given document within the given fix all scope.
			/// This delegate is invoked by <see cref="GetDocumentDiagnosticsAsync(Document, CancellationToken)"/> with the given <see cref="_diagnosticIds"/> as arguments.
			/// </summary>
			private readonly Func<Microsoft.CodeAnalysis.Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> _getDocumentDiagnosticsAsync;

			/// <summary>
			/// Delegate to fetch diagnostics for any given project within the given fix all scope.
			/// This delegate is invoked by <see cref="GetProjectDiagnosticsAsync(Project, CancellationToken)"/> and <see cref="GetAllDiagnosticsAsync(Project, CancellationToken)"/>
			/// with the given <see cref="_diagnosticIds"/> as arguments.
			/// The boolean argument to the delegate indicates whether or not to return location-based diagnostics, i.e.
			/// (a) False => Return only diagnostics with <see cref="Location.None"/>.
			/// (b) True => Return all project diagnostics, regardless of whether or not they have a location.
			/// </summary>
			private readonly Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> _getProjectDiagnosticsAsync;

			public FixAllDiagnosticProvider (
				ImmutableHashSet<string> diagnosticIds,
				Func<Microsoft.CodeAnalysis.Document, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getDocumentDiagnosticsAsync,
				Func<Project, bool, ImmutableHashSet<string>, CancellationToken, Task<IEnumerable<Diagnostic>>> getProjectDiagnosticsAsync)
			{
				_diagnosticIds = diagnosticIds;
				_getDocumentDiagnosticsAsync = getDocumentDiagnosticsAsync;
				_getProjectDiagnosticsAsync = getProjectDiagnosticsAsync;
			}

			public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync (Microsoft.CodeAnalysis.Document document, CancellationToken cancellationToken)
			{
				return _getDocumentDiagnosticsAsync (document, _diagnosticIds, cancellationToken);
			}

			public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync (Project project, CancellationToken cancellationToken)
			{
				return _getProjectDiagnosticsAsync (project, true, _diagnosticIds, cancellationToken);
			}

			public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync (Project project, CancellationToken cancellationToken)
			{
				return _getProjectDiagnosticsAsync (project, false, _diagnosticIds, cancellationToken);
			}
		}

		static RefactoringPreviewTooltipWindow currentPreviewWindow;

		void PopulateFixes (FixMenuDescriptor menu, ref int items)
		{
			int mnemonic = 1;
			bool gotImportantFix = false, addedSeparator = false;
			foreach (var fix_ in GetCurrentFixes ().CodeFixActions.OrderByDescending (i => Tuple.Create (IsAnalysisOrErrorFix (i.CodeAction), (int)0, GetUsage (i.CodeAction.EquivalenceKey)))) {
				// filter out code actions that are already resolutions of a code issue
				if (IsAnalysisOrErrorFix (fix_.CodeAction))
					gotImportantFix = true;
				if (!addedSeparator && gotImportantFix && !IsAnalysisOrErrorFix (fix_.CodeAction)) {
					menu.Add (FixMenuEntry.Separator);
					addedSeparator = true;
				}

				var fix = fix_;
				var label = CreateLabel (fix.CodeAction.Title, ref mnemonic);
				var thisInstanceMenuItem = new FixMenuEntry (label,async delegate {
					await new ContextActionRunner (fix.CodeAction, Editor, DocumentContext).Run ();
					ConfirmUsage (fix.CodeAction.EquivalenceKey);
				});

				thisInstanceMenuItem.ShowPreviewTooltip = delegate (Xwt.Rectangle rect) {
					HidePreviewTooltip ();
					currentPreviewWindow = new RefactoringPreviewTooltipWindow (this.Editor, this.DocumentContext, fix.CodeAction);
					currentPreviewWindow.RequestPopup (rect);
				};

				menu.Add (thisInstanceMenuItem);
				items++;
			}

			bool first = true;
			foreach (var fix in GetCurrentFixes ().CodeRefactoringActions) {
				if (first) {
					if (items > 0)
						menu.Add (FixMenuEntry.Separator);
					first = false;
				}

				var label = CreateLabel (fix.CodeAction.Title, ref mnemonic);
				var thisInstanceMenuItem = new FixMenuEntry (label, async delegate {
					await new ContextActionRunner (fix.CodeAction, Editor, DocumentContext).Run ();
					ConfirmUsage (fix.CodeAction.EquivalenceKey);
				});

				thisInstanceMenuItem.ShowPreviewTooltip = delegate (Xwt.Rectangle rect) {
					HidePreviewTooltip ();
					currentPreviewWindow = new RefactoringPreviewTooltipWindow (this.Editor, this.DocumentContext, fix.CodeAction);
					currentPreviewWindow.RequestPopup (rect);
				};

				menu.Add (thisInstanceMenuItem);
				items++;
			}

			first = false;

			var warningsAtCaret = (DocumentContext.AnalysisDocument.GetSemanticModelAsync ().Result)
				.GetDiagnostics (new TextSpan (Editor.CaretOffset, 0))
				.Where (diag => diag.Severity == DiagnosticSeverity.Warning).ToList ();
			
			foreach (var warning in warningsAtCaret) {
				var label = GettextCatalog.GetString ("_Options for \u2018{0}\u2019", warning.Descriptor.Title);
				var subMenu = new FixMenuDescriptor (label);
				if (first) {
					menu.Add (FixMenuEntry.Separator);
					first = false;
				}
				var menuItem = new FixMenuEntry (GettextCatalog.GetString ("_Suppress with #pragma"),
				 	async delegate {
						var fixes = await CSharpSuppressionFixProvider.Instance.GetSuppressionsAsync (DocumentContext.AnalysisDocument, new TextSpan (Editor.CaretOffset, 0), new [] { warning }, default (CancellationToken)).ConfigureAwait (false);
					 	foreach (var f in fixes) {
							CodeDiagnosticDescriptor.RunAction (DocumentContext, f.Action, default (CancellationToken));
					 	}
				 	}
				);
				menuItem.ShowPreviewTooltip = async delegate (Xwt.Rectangle rect) {
					var fixes = await CSharpSuppressionFixProvider.Instance.GetSuppressionsAsync (DocumentContext.AnalysisDocument, new TextSpan (Editor.CaretOffset, 0), new [] { warning }, default (CancellationToken)).ConfigureAwait (false);
					HidePreviewTooltip ();
					var fix = fixes.FirstOrDefault ();
					if (fix == null)
						return;
					currentPreviewWindow = new RefactoringPreviewTooltipWindow (this.Editor, this.DocumentContext, fix.Action);
					currentPreviewWindow.RequestPopup (rect);
				};

				subMenu.Add (menuItem);
				menu.Add (subMenu);
				items++;
			}

			foreach (var fix_ in GetCurrentFixes ().DiagnosticsAtCaret) {
				var fix = fix_;
				var label = GettextCatalog.GetString ("_Options for \u2018{0}\u2019", fix.GetMessage ());
				var subMenu = new FixMenuDescriptor (label);

				CodeDiagnosticDescriptor descriptor = BuiltInCodeDiagnosticProvider.GetCodeDiagnosticDescriptor (fix.Id);
				if (descriptor == null)
					continue;
				if (first) {
					menu.Add (FixMenuEntry.Separator);
					first = false;
				}
				//				if (inspector.CanSuppressWithAttribute) {
				//					var menuItem = new FixMenuEntry (GettextCatalog.GetString ("_Suppress with attribute"),
				//						delegate {
				//							
				//							inspector.SuppressWithAttribute (Editor, DocumentContext, GetTextSpan (fix.Item2)); 
				//						});
				//					subMenu.Add (menuItem);
				//				}

				if (descriptor.CanDisableWithPragma) {
					var menuItem = new FixMenuEntry (GettextCatalog.GetString ("_Suppress with #pragma"),
													 delegate {
														 descriptor.DisableWithPragma (Editor, DocumentContext, fix);
													 });
					menuItem.ShowPreviewTooltip = async delegate (Xwt.Rectangle rect) {
						var fixes = await CSharpSuppressionFixProvider.Instance.GetSuppressionsAsync (DocumentContext.AnalysisDocument, new TextSpan (Editor.CaretOffset, 0), new [] { fix }, default (CancellationToken)).ConfigureAwait (false);
						HidePreviewTooltip ();
						var fix2 = fixes.FirstOrDefault ();
						if (fix2 == null)
							return;
						currentPreviewWindow = new RefactoringPreviewTooltipWindow (this.Editor, this.DocumentContext, fix2.Action);
						currentPreviewWindow.RequestPopup (rect);
					};
					subMenu.Add (menuItem);
					menuItem = new FixMenuEntry (GettextCatalog.GetString ("_Suppress with file"),
						delegate {
							descriptor.DisableWithFile (Editor, DocumentContext, fix);
						});
					subMenu.Add (menuItem);
				}
				var optionsMenuItem = new FixMenuEntry (GettextCatalog.GetString ("_Configure Rule"),
					delegate {
						IdeApp.Workbench.ShowGlobalPreferencesDialog (null, "C#", dialog => {
							var panel = dialog.GetPanel<CodeIssuePanel> ("C#");
							if (panel == null)
								return;
							panel.Widget.SelectCodeIssue (descriptor.IdString);
						});
					});
				subMenu.Add (optionsMenuItem);


				foreach (var fix2 in GetCurrentFixes ().CodeFixActions.OrderByDescending (i => Tuple.Create (IsAnalysisOrErrorFix (i.CodeAction), (int)0, GetUsage (i.CodeAction.EquivalenceKey)))) {

					var provider = fix2.Diagnostic.GetCodeFixProvider ().GetFixAllProvider ();
					if (provider == null)
						continue;
					if (!provider.GetSupportedFixAllScopes ().Contains (FixAllScope.Document))
						continue;
					var subMenu2 = new FixMenuDescriptor (GettextCatalog.GetString ("Fix all"));
					var diagnosticAnalyzer = fix2.Diagnostic.GetCodeDiagnosticDescriptor (LanguageNames.CSharp).GetProvider ();
					if (!diagnosticAnalyzer.SupportedDiagnostics.Contains (fix.Descriptor))
						continue;

					var menuItem = new FixMenuEntry (
						GettextCatalog.GetString ("In _Document"),
						async delegate {
							var fixAllDiagnosticProvider = new FixAllDiagnosticProvider (diagnosticAnalyzer.SupportedDiagnostics.Select (d => d.Id).ToImmutableHashSet (), async (Microsoft.CodeAnalysis.Document doc, ImmutableHashSet<string> diagnostics, CancellationToken token) => {

								var model = await doc.GetSemanticModelAsync (token);
								var compilationWithAnalyzer = model.Compilation.WithAnalyzers (new [] { diagnosticAnalyzer }.ToImmutableArray (), null, token);

								return await compilationWithAnalyzer.GetAnalyzerSemanticDiagnosticsAsync (model, null, token);
							}, (Project arg1, bool arg2, ImmutableHashSet<string> arg3, CancellationToken arg4) => {
								return Task.FromResult ((IEnumerable<Diagnostic>)new Diagnostic [] { });
							});
							var ctx = new FixAllContext (
								this.DocumentContext.AnalysisDocument,
								fix2.Diagnostic.GetCodeFixProvider (),
								FixAllScope.Document,
								fix2.CodeAction.EquivalenceKey,
								diagnosticAnalyzer.SupportedDiagnostics.Select (d => d.Id),
								fixAllDiagnosticProvider,
								default (CancellationToken)
							);
							var fixAll = await provider.GetFixAsync (ctx);
							using (var undo = Editor.OpenUndoGroup ()) {
								CodeDiagnosticDescriptor.RunAction (DocumentContext, fixAll, default (CancellationToken));
							}
						});
					subMenu2.Add (menuItem);
					subMenu.Add (FixMenuEntry.Separator);
					subMenu.Add (subMenu2);
				}

				menu.Add (subMenu);
				items++;
			}
		}

		internal class ContextActionRunner
		{
			readonly CodeAction act;
			TextEditor editor;
			DocumentContext documentContext;

			public ContextActionRunner (CodeAction act, TextEditor editor, DocumentContext documentContext)
			{
				this.editor = editor;
				this.act = act;
				this.documentContext = documentContext;
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
								var node = Formatter.Format (insertion.Node, TypeSystemService.Workspace, document.GetOptionSet (), token);
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
				if (RefactoringService.OptionSetCreation != null)
					documentContext.RoslynWorkspace.Options = RefactoringService.OptionSetCreation (editor, documentContext);
				using (var undo = editor.OpenUndoGroup ()) {
					foreach (var operation in await act.GetOperationsAsync (token)) {
						var applyChanges = operation as ApplyChangesOperation;
						if (applyChanges == null) {
							operation.Apply (documentContext.RoslynWorkspace, token);
							continue;
						}
						if (updatedSolution == oldSolution) {
							updatedSolution = applyChanges.ChangedSolution;
						}
						operation.Apply (documentContext.RoslynWorkspace, token);
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
									await new MonoDevelop.Refactoring.Rename.RenameRefactoring ().Rename (sym);
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
											foreach (var v in textChanges) {
												editor.ReplaceText (v.Offset, v.RemovalLength, v.InsertedText);
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

		ISmartTagMarker currentSmartTag;
		int currentSmartTagBegin;

		void CreateSmartTag (CodeActionContainer fixes, int offset)
		{
			if (!AnalysisOptions.EnableFancyFeatures || fixes.IsEmpty) {
				RemoveWidget ();
				return;
			}
			var editor = Editor;
			if (editor == null) {
				RemoveWidget ();
				return;
			}
			if (DocumentContext.ParsedDocument == null || DocumentContext.ParsedDocument.IsInvalid) {
				RemoveWidget ();
				return;
			}

			//			var container = editor.Parent;
			//			if (container == null) {
			//				RemoveWidget ();
			//				return;
			//			}
			bool first = true;
			var smartTagLocBegin = offset;
			foreach (var fix in fixes.CodeFixActions.Concat (fixes.CodeRefactoringActions)) {
				var textSpan = fix.ValidSegment;
				if (textSpan.IsEmpty)
					continue;
				if (first || offset < textSpan.Start) {
					smartTagLocBegin = textSpan.Start;
				}
				first = false;
			}
			//			if (smartTagLocBegin.Line != loc.Line)
			//				smartTagLocBegin = new DocumentLocation (loc.Line, 1);
			// got no fix location -> try to search word start
			//			if (first) {
			//				int offset = document.Editor.LocationToOffset (smartTagLocBegin);
			//				while (offset > 0) {
			//					char ch = document.Editor.GetCharAt (offset - 1);
			//					if (!char.IsLetterOrDigit (ch) && ch != '_')
			//						break;
			//					offset--;
			//				}
			//				smartTagLocBegin = document.Editor.OffsetToLocation (offset);
			//			}

			if (currentSmartTag != null && currentSmartTagBegin == smartTagLocBegin) {
				return;
			}
			RemoveWidget ();
			currentSmartTagBegin = smartTagLocBegin;
			var realLoc = Editor.OffsetToLocation (smartTagLocBegin);

			currentSmartTag = TextMarkerFactory.CreateSmartTagMarker (Editor, smartTagLocBegin, realLoc);
			currentSmartTag.CancelPopup += CurrentSmartTag_CancelPopup;
			currentSmartTag.ShowPopup += CurrentSmartTag_ShowPopup;
			currentSmartTag.Tag = fixes;
			currentSmartTag.IsVisible = fixes.CodeFixActions.Count > 0;
			editor.AddMarker (currentSmartTag);
		}

		void CurrentSmartTag_ShowPopup (object sender, EventArgs e)
		{
			CurrentSmartTagPopup ();
		}

		void CurrentSmartTag_CancelPopup (object sender, EventArgs e)
		{
			CancelSmartTagPopupTimeout ();
		}

		protected override void Initialize ()
		{
			base.Initialize ();
			DocumentContext.DocumentParsed += HandleDocumentDocumentParsed;
			Editor.SelectionChanged += HandleSelectionChanged;
			Editor.MouseMoved += HandleBeginHover;
			Editor.CaretPositionChanged += HandleCaretPositionChanged;
			Editor.TextChanged += Editor_TextChanged;
			Editor.EndAtomicUndoOperation += Editor_EndAtomicUndoOperation;
		}

		void Editor_EndAtomicUndoOperation (object sender, EventArgs e)
		{
			RemoveWidget ();
			HandleCaretPositionChanged (null, EventArgs.Empty);
		}

		void Editor_TextChanged (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			if (Editor.IsInAtomicUndo)
				return;
			RemoveWidget ();
			HandleCaretPositionChanged (null, EventArgs.Empty);
		}

		void HandleBeginHover (object sender, EventArgs e)
		{
			CancelSmartTagPopupTimeout ();
			CancelMenuCloseTimer ();
		}

		void StartMenuCloseTimer ()
		{
			CancelMenuCloseTimer ();
			menuCloseTimeoutId = GLib.Timeout.Add (menuTimeout, delegate {
				/*if (codeActionMenu != null) {
					codeActionMenu.Destroy ();
					codeActionMenu = null;
				}*/
				menuCloseTimeoutId = 0;
				return false;
			});
		}

		void HandleSelectionChanged (object sender, EventArgs e)
		{
			HandleCaretPositionChanged (null, EventArgs.Empty);
		}

		void HandleDocumentDocumentParsed (object sender, EventArgs e)
		{
			HandleCaretPositionChanged (null, EventArgs.Empty);
		}

		void CurrentSmartTagPopup ()
		{
			CancelSmartTagPopupTimeout ();
			smartTagPopupTimeoutId = GLib.Timeout.Add (menuTimeout, delegate {
				PopupQuickFixMenu (null, menu => { });
				smartTagPopupTimeoutId = 0;
				return false;
			});
		}

		[CommandHandler (RefactoryCommands.QuickFix)]
		void OnQuickFixCommand ()
		{
			if (!AnalysisOptions.EnableFancyFeatures || currentSmartTag == null) {
				//Fixes = RefactoringService.GetValidActions (Editor, DocumentContext, Editor.CaretLocation).Result;
				currentSmartTagBegin = Editor.CaretOffset;
				PopupQuickFixMenu (null, null);
				return;
			}

			CancelSmartTagPopupTimeout ();
			PopupQuickFixMenu (null, menu => { });
		}

		internal bool HasCurrentFixes {
			get {
				return smartTagTask != null && smartTagTask.IsCompleted;
			}
		}

		internal CodeActionContainer GetCurrentFixes ()
		{
			return smartTagTask == null ? CodeActionContainer.Empty : smartTagTask.Result;
		}
	}
}
