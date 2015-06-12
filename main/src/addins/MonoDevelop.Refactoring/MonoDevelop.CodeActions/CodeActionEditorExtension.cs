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
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using MonoDevelop.AnalysisCore;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Components;
using MonoDevelop.Ide.Editor.Extension;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis;
using System.Reflection;

namespace MonoDevelop.CodeActions
{
	class CodeActionEditorExtension : TextEditorExtension
	{
		uint quickFixTimeout;

		const int menuTimeout = 250;
		uint smartTagPopupTimeoutId;
		uint menuCloseTimeoutId;
		FixMenuDescriptor codeActionMenu;

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
			Editor.CaretPositionChanged -= HandleCaretPositionChanged;
			Editor.SelectionChanged -= HandleSelectionChanged;
			DocumentContext.DocumentParsed -= HandleDocumentDocumentParsed;
			Editor.BeginMouseHover -= HandleBeginHover;
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

		void HandleCaretPositionChanged (object sender, EventArgs e)
		{
			if (Editor.IsInAtomicUndo)
				return;
			CancelQuickFixTimer ();
			if (AnalysisOptions.EnableFancyFeatures && DocumentContext.ParsedDocument != null && !Debugger.DebuggingService.IsDebugging) {
				var token = quickFixCancellationTokenSource.Token;
				var curOffset = Editor.CaretOffset;
				foreach (var fix in GetCurrentFixes ().AllValidCodeActions) {
					if (!fix.ValidSegment.Contains (curOffset)) {
						RemoveWidget ();
						break;
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
						var diagnosticIds = diagnosticsAtCaret.Select (diagnostic => diagnostic.Id).Concat (errorList.Select (rm => rm.Error.Id)).ToImmutableArray<string> ();
						foreach (var cfp in CodeRefactoringService.GetCodeFixesAsync (DocumentContext, CodeRefactoringService.MimeTypeToLanguage (Editor.MimeType)).Result) {
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
									await provider.RegisterCodeFixesAsync (new CodeFixContext (ad, diagnosticSpan, validDiagnostics, (ca, d) => codeIssueFixes.Add (new ValidCodeDiagnosticAction (cfp, ca, diagnosticSpan)), token));

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
						foreach (var action in await CodeRefactoringService.GetValidActionsAsync (Editor, DocumentContext, span, token)) {
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

			public FixMenuEntry (string label, System.Action action)
			{
				this.Label = label;
				this.Action = action;
			}
		}

		internal class FixMenuDescriptor : FixMenuEntry
		{
			readonly List<FixMenuEntry> items = new List<FixMenuEntry> ();

			public IReadOnlyList<FixMenuEntry> Items
			{
				get
				{
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

			public object MotionNotifyEvent
			{
				get;
				set;
			}
		}

		internal static Action<TextEditor, DocumentContext, FixMenuDescriptor> AddPossibleNamespace;

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
//			document.Editor.SuppressTooltips = true;
//			document.Editor.Parent.HideTooltip ();
			if (menuAction != null)
				menuAction (menu);

			var p = Editor.LocationToPoint (Editor.OffsetToLocation (currentSmartTagBegin));
			Gtk.Widget widget = Editor;
			var rect = new Gdk.Rectangle (
				(int)p.X + widget.Allocation.X,
				(int)p.Y + widget.Allocation.Y, 0, 0);

			ShowFixesMenu (widget, rect, menu);
		}

#if MAC
		class ClosingMenuDelegate : AppKit.NSMenuDelegate
		{
			readonly TextEditor data;

			public ClosingMenuDelegate (TextEditor editor_data)
			{
				data = editor_data;
			}

			public override void MenuWillHighlightItem (AppKit.NSMenu menu, AppKit.NSMenuItem item)
			{
			}

			public override void MenuDidClose (AppKit.NSMenu menu)
			{
				// TODO: roslyn port ? (seems to be unused anyways btw.)
				//data.SuppressTooltips = false;
			}
		}
#endif

		bool ShowFixesMenu (Gtk.Widget parent, Gdk.Rectangle evt, FixMenuDescriptor entrySet)
		{
			if (parent == null || parent.GdkWindow == null)
				return true;
			try {
				#if MAC
				Gtk.Application.Invoke (delegate {
				parent.GrabFocus ();
				int x, y;
				x = (int)evt.X;
				y = (int)evt.Y;
				// Explicitly release the grab because the menu is shown on the mouse position, and the widget doesn't get the mouse release event
				Gdk.Pointer.Ungrab (Gtk.Global.CurrentEventTime);
				var menu = CreateNSMenu (entrySet);
				menu.Delegate = new ClosingMenuDelegate (Editor);
				var nsview = MonoDevelop.Components.Mac.GtkMacInterop.GetNSView (parent);
				var toplevel = parent.Toplevel as Gtk.Window;
				int trans_x, trans_y;
				parent.TranslateCoordinates (toplevel, (int)x, (int)y, out trans_x, out trans_y);

				// Window coordinates in gtk are the same for cocoa, with the exception of the Y coordinate, that has to be flipped.
				var pt = new CoreGraphics.CGPoint ((float)trans_x, (float)trans_y);
				int w,h;
				toplevel.GetSize (out w, out h);
				pt.Y = h - pt.Y;

				var tmp_event = AppKit.NSEvent.MouseEvent (AppKit.NSEventType.LeftMouseDown,
				pt,
				0, 0,
				MonoDevelop.Components.Mac.GtkMacInterop.GetNSWindow (toplevel).WindowNumber,
				null, 0, 0, 0);

				AppKit.NSMenu.PopUpContextMenu (menu, tmp_event, nsview);
				});
				#else
				var menu = CreateGtkMenu (entrySet);
				menu.Events |= Gdk.EventMask.AllEventsMask;
				menu.SelectFirst (true);

				menu.Hidden += delegate {
					// document.Editor.SuppressTooltips = false;
				};
				menu.ShowAll ();
				menu.SelectFirst (true);
				menu.MotionNotifyEvent += (o, args) => {
					Gtk.Widget widget = Editor;
					if (args.Event.Window == widget.GdkWindow) {
						StartMenuCloseTimer ();
					} else {
						CancelMenuCloseTimer ();
					}
				};

				GtkWorkarounds.ShowContextMenu (menu, parent, null, evt);
				#endif
			} catch (Exception ex) {
				LoggingService.LogError ("Error while context menu popup.", ex);
			}
			return true;
		}

#if MAC

		AppKit.NSMenu CreateNSMenu (FixMenuDescriptor entrySet)
		{
			var menu = new AppKit.NSMenu ();
			foreach (var item in entrySet.Items) {
				if (item == FixMenuEntry.Separator) {
					menu.AddItem (AppKit.NSMenuItem.SeparatorItem);
					continue;
				}
				var subMenu = item as FixMenuDescriptor;
				if (subMenu != null) {
					var gtkSubMenu = new AppKit.NSMenuItem (item.Label.Replace ("_", ""));
					gtkSubMenu.Submenu = CreateNSMenu (subMenu);
					menu.AddItem (gtkSubMenu); 
					continue;
				}
				var menuItem = new AppKit.NSMenuItem (item.Label.Replace ("_", ""));
				menuItem.Activated += delegate {
					item.Action ();
				};
				menu.AddItem (menuItem); 
			}
			return menu;
		}
#endif

		static Menu CreateGtkMenu (FixMenuDescriptor entrySet)
		{
			var menu = new Menu ();
			foreach (var item in entrySet.Items) {
				if (item == FixMenuEntry.Separator) {
					menu.Add (new SeparatorMenuItem ());
					continue;
				}
				var subMenu = item as FixMenuDescriptor;
				if (subMenu != null) {
					var gtkSubMenu = new Gtk.MenuItem (item.Label);
					gtkSubMenu.Submenu = CreateGtkMenu (subMenu);
					menu.Add (gtkSubMenu);
					continue;
				}
				var menuItem = new Gtk.MenuItem (item.Label);
				menuItem.Activated += delegate {
					item.Action ();
				};
				menu.Add (menuItem);
			}
			return menu;
		}

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
				var escapedLabel = fix.CodeAction.Title.Replace ("_", "__");
				var label = (mnemonic <= 10)
					? "_" + (mnemonic++ % 10).ToString () + " " + escapedLabel
					: "  " + escapedLabel;
				var thisInstanceMenuItem = new FixMenuEntry (label, delegate {
					new ContextActionRunner (fix.CodeAction, Editor, DocumentContext).Run (null, EventArgs.Empty);
					ConfirmUsage (fix.CodeAction.EquivalenceKey);
				});
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

				var escapedLabel = fix.CodeAction.Title.Replace ("_", "__");
				var label = (mnemonic <= 10)
					? "_" + (mnemonic++ % 10).ToString () + " " + escapedLabel
					: "  " + escapedLabel;
				var thisInstanceMenuItem = new FixMenuEntry (label, delegate {
					new ContextActionRunner (fix.CodeAction, Editor, DocumentContext).Run (null, EventArgs.Empty);
					ConfirmUsage (fix.CodeAction.EquivalenceKey);
				});
				menu.Add (thisInstanceMenuItem);
				items++;
			}

			first = false;
			foreach (var fix_ in GetCurrentFixes ().DiagnosticsAtCaret) {
				var fix = fix_;
				var label = GettextCatalog.GetString ("_Options for \"{0}\"", fix.GetMessage ());
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
							descriptor.DisableWithPragma (Editor, DocumentContext, fix.Location.SourceSpan);
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

			public void Run (object sender, EventArgs e)
			{
				Run ();
			}

			internal async void Run ()
			{
				var token = default(CancellationToken);
				var insertionAction = act as InsertionAction;
				if (insertionAction != null) {
					var insertion = await insertionAction.CreateInsertion (token).ConfigureAwait (false);

					var document = IdeApp.Workbench.OpenDocument (insertion.Location.SourceTree.FilePath);
					var parsedDocument = document.UpdateParseDocument ();
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
							             async point => {
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
				foreach (var operation in act.GetOperationsAsync (token).Result) {
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
				TryStartRenameSession (documentContext.RoslynWorkspace, oldSolution, updatedSolution, token);
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

			async void TryStartRenameSession (Workspace workspace, Solution oldSolution, Solution newSolution, CancellationToken cancellationToken)
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
						Application.Invoke (delegate {
							try {
								var node = latestRoot.FindNode (renameTokenOpt.Value.Parent.Span, false, false);
								if (node == null)
									return;
								var info = latestModel.GetSymbolInfo (node);
								var sym = info.Symbol ?? latestModel.GetDeclaredSymbol (node);
								if (sym != null) 
									new MonoDevelop.Refactoring.Rename.RenameRefactoring ().Rename (sym);
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
			Editor.BeginMouseHover += HandleBeginHover;
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

		[CommandUpdateHandler(RefactoryCommands.QuickFix)]
		public void UpdateQuickFixCommand (CommandInfo ci)
		{
			if (AnalysisOptions.EnableFancyFeatures) {
				ci.Enabled = currentSmartTag != null;
			} else {
				ci.Enabled = true;
			}
		}

		void CurrentSmartTagPopup ()
		{
			CancelSmartTagPopupTimeout ();
			smartTagPopupTimeoutId = GLib.Timeout.Add (menuTimeout, delegate {
				PopupQuickFixMenu (null, menu => {
					codeActionMenu = menu;
				});
				smartTagPopupTimeoutId = 0;
				return false;
			});
		}

		[CommandHandler(RefactoryCommands.QuickFix)]
		void OnQuickFixCommand ()
		{
			if (!AnalysisOptions.EnableFancyFeatures) {
				//Fixes = RefactoringService.GetValidActions (Editor, DocumentContext, Editor.CaretLocation).Result;
				currentSmartTagBegin = Editor.CaretOffset;
				PopupQuickFixMenu (null, null);

				return;
			}
			if (currentSmartTag == null)
				return;
			CurrentSmartTagPopup ();
		}

		internal CodeActionContainer GetCurrentFixes ()
		{
			return smartTagTask == null ? CodeActionContainer.Empty : smartTagTask.Result;
		}
	}
}
