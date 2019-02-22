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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Gtk;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.AnalysisCore;
using MonoDevelop.CodeIssues;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Refactoring;
using RefactoringEssentials;
using MonoDevelop.AnalysisCore.Gui;
using MonoDevelop.SourceEditor;
using Gdk;

namespace MonoDevelop.CodeActions
{
	[Obsolete ("Old editor")]
	class CodeActionEditorExtension : TextEditorExtension
	{
		const int menuTimeout = 150;
		internal uint smartTagPopupTimeoutId { get; set; }

		internal void CancelSmartTagPopupTimeout ()
		{
			if (smartTagPopupTimeoutId != 0) {
				GLib.Source.Remove (smartTagPopupTimeoutId);
				smartTagPopupTimeoutId = 0;
			}
		}

		void RemoveWidget ()
		{
			if (smartTagMarginMarker != null) {
				Editor.RemoveMarker (smartTagMarginMarker);
				smartTagMarginMarker.ShowPopup -= SmartTagMarginMarker_ShowPopup;
				smartTagMarginMarker = null;
			}
			CancelSmartTagPopupTimeout ();
		}

		public override void Dispose ()
		{
			CancelQuickFixTimer ();
			RefactoringPreviewTooltipWindow.HidePreviewTooltip ();
			Editor.CaretPositionChanged -= HandleCaretPositionChanged;
			DocumentContext.DocumentParsed -= HandleDocumentDocumentParsed;
			Editor.TextChanged -= Editor_TextChanged;
			Editor.BeginAtomicUndoOperation -= Editor_BeginAtomicUndoOperation;
			Editor.EndAtomicUndoOperation -= Editor_EndAtomicUndoOperation;
			RemoveWidget ();
			base.Dispose ();
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
			var token = quickFixCancellationTokenSource.Token;
			if (AnalysisOptions.EnableFancyFeatures && DocumentContext.ParsedDocument != null) {
				if (HasCurrentFixes) {
					var curOffset = Editor.CaretOffset;
					foreach (var fix in smartTagTask.Result.CodeFixActions) {
						if (!fix.TextSpan.Contains (curOffset)) {
							RemoveWidget ();
							break;
						}
					}
				}

				smartTagTask = GetCurrentFixesAsync (token);
			} else {
				RemoveWidget ();
			}
		}

		ICodeFixService codeFixService = Ide.Composition.CompositionManager.GetExportedValue<ICodeFixService> ();
		ICodeRefactoringService codeRefactoringService = Ide.Composition.CompositionManager.GetExportedValue<ICodeRefactoringService> ();
		internal Task<CodeActionContainer> GetCurrentFixesAsync (CancellationToken cancellationToken)
		{
			var loc = Editor.CaretOffset;
			var ad = DocumentContext.AnalysisDocument;
			var line = Editor.GetLine (Editor.CaretLine);

			if (ad == null) {
				return Task.FromResult (CodeActionContainer.Empty);
			}
			TextSpan span;
			if (Editor.IsSomethingSelected) {
				var selectionRange = Editor.SelectionRange;
				span = selectionRange.Offset >= 0 ? TextSpan.FromBounds (selectionRange.Offset, selectionRange.EndOffset) : TextSpan.FromBounds (loc, loc);
			} else {
				span = TextSpan.FromBounds (loc, loc);
			}

			return Task.Run (async delegate {
				try {
					var root = await ad.GetSyntaxRootAsync (cancellationToken);
					if (root.Span.End < span.End) {
						LoggingService.LogError ($"Error in GetCurrentFixesAsync span {span.Start}/{span.Length} not inside syntax root {root.Span.End} document length {Editor.Length}.");
						return CodeActionContainer.Empty;
					}

					var lineSpan = new TextSpan (line.Offset, line.Length);
					var fixes = await codeFixService.GetFixesAsync (ad, lineSpan, true, cancellationToken);
					fixes = await Runtime.RunInMainThread(() => FilterOnUIThread (fixes, DocumentContext.RoslynWorkspace));

					var refactorings = await codeRefactoringService.GetRefactoringsAsync (ad, span, cancellationToken);
					var codeActionContainer = new CodeActionContainer (fixes, refactorings);
					Application.Invoke ((o, args) => {
						if (cancellationToken.IsCancellationRequested)
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

			}, cancellationToken);
		}

		ImmutableArray<CodeFixCollection> FilterOnUIThread (
			  ImmutableArray<CodeFixCollection> collections, Workspace workspace)
		{
			Runtime.AssertMainThread ();
			var caretOffset = Editor.CaretOffset;
			return collections.Select (c => FilterOnUIThread (c, workspace)).Where(x => x != null).OrderBy(x => GetDistance (x, caretOffset)).ToImmutableArray ();
		}

		static int GetDistance (CodeFixCollection fixCollection, int caretOffset)
		{
			return fixCollection.TextSpan.End < caretOffset ? caretOffset - fixCollection.TextSpan.End : fixCollection.TextSpan.Start - caretOffset;
		}

		static CodeFixCollection FilterOnUIThread (
			CodeFixCollection collection,
			Workspace workspace)
		{
			Runtime.AssertMainThread ();

			var applicableFixes = collection.Fixes.WhereAsArray (f => IsApplicable (f.Action, workspace));
			return applicableFixes.Length == 0
				? null
				: applicableFixes.Length == collection.Fixes.Length
					? collection
					: new CodeFixCollection (
						collection.Provider, collection.TextSpan, applicableFixes,
						collection.FixAllState, collection.SupportedScopes, collection.FirstDiagnostic);
		}

		static bool IsApplicable (Microsoft.CodeAnalysis.CodeActions.CodeAction action, Workspace workspace)
		{
			if (!action.PerformFinalApplicabilityCheck) {
				return true;
			}

			Runtime.AssertMainThread ();
			return action.IsApplicable (workspace);
		}

		internal async void PopupQuickFixMenu (Gdk.EventButton evt, Action<CodeFixMenu> menuAction, Xwt.Point? point = null)
		{
			using (Refactoring.Counters.FixesMenu.BeginTiming ("Show quick fixes menu")) {
				var token = quickFixCancellationTokenSource.Token;

				var fixes = await GetCurrentFixesAsync (token);
				if (token.IsCancellationRequested)
					return;
				Editor.SuppressTooltips = true;
				PopupQuickFixMenu (evt, fixes, menuAction, point);
			}
		}

		internal void PopupQuickFixMenu (Gdk.EventButton evt, CodeActionContainer fixes, Action<CodeFixMenu> menuAction, Xwt.Point? point = null)
		{
			var token = quickFixCancellationTokenSource.Token;

			if (token.IsCancellationRequested)
				return;

			var menu = CodeFixMenuService.CreateFixMenu (Editor, fixes, token);
			if (token.IsCancellationRequested)
				return;

			if (menu.Items.Count == 0) {
				return;
			}

			if (menuAction != null)
				menuAction (menu);
			Gdk.Rectangle rect;
			Widget widget = Editor;

			if (!point.HasValue) {
				var p = Editor.LocationToPoint (Editor.CaretLocation);
				rect = new Gdk.Rectangle (
					(int)p.X + widget.Allocation.X,
					(int)p.Y + widget.Allocation.Y, 0, 0);
			} else {
				rect = new Gdk.Rectangle ((int)point.Value.X, (int)point.Value.Y, 0, 0);
			}
			ShowFixesMenu (widget, rect, menu);
		}

		bool ShowFixesMenu (Widget parent, Gdk.Rectangle evt, CodeFixMenu entrySet)
		{
			if (parent == null || parent.GdkWindow == null) {
				Editor.SuppressTooltips = false;
				return true;
			}

			try {
				parent.GrabFocus ();
				int x, y;
				x = evt.X;
				y = evt.Y;

				// Explicitly release the grab because the menu is shown on the mouse position, and the widget doesn't get the mouse release event
				Gdk.Pointer.Ungrab (Gtk.Global.CurrentEventTime);
				var menu = CreateContextMenu (entrySet);
				RefactoringPreviewTooltipWindow.HidePreviewTooltip ();
				menu.Show (parent, x, y, () => {
					Editor.SuppressTooltips = false;
					RefactoringPreviewTooltipWindow.HidePreviewTooltip ();
					FixesMenuClosed?.Invoke (this, EventArgs.Empty);
				}, true);
			} catch (Exception ex) {
				LoggingService.LogError ("Error while context menu popup.", ex);
			}

			return true;
		}
		public event EventHandler FixesMenuClosed;

		ContextMenu CreateContextMenu (CodeFixMenu entrySet)
		{
			var menu = new ContextMenu ();
			foreach (var item in entrySet.Items) {
				if (item == CodeFixMenuEntry.Separator) {
					menu.Items.Add (new SeparatorContextMenuItem ());
					continue;
				}

				var menuItem = new ContextMenuItem (item.Label);
				menuItem.Context = item.Action;
				if (item.Action == null) {
					if (!(item is CodeFixMenu itemAsMenu) || itemAsMenu.Items.Count <= 0) {
						menuItem.Sensitive = false;
					}
				}
				var subMenu = item as CodeFixMenu;
				if (subMenu != null) {
					menuItem.SubMenu = CreateContextMenu (subMenu);
					menuItem.Selected += delegate {
						RefactoringPreviewTooltipWindow.HidePreviewTooltip ();
					};
					menuItem.Deselected += delegate { RefactoringPreviewTooltipWindow.HidePreviewTooltip (); };
				} else {
					menuItem.Clicked += (sender, e) => ((System.Action)((ContextMenuItem)sender).Context) ();
					menuItem.Selected += (sender, e) => {
						RefactoringPreviewTooltipWindow.HidePreviewTooltip ();
						if (item.ShowPreviewTooltip != null) {
							item.ShowPreviewTooltip (e);
						}
					};
					menuItem.Deselected += delegate { RefactoringPreviewTooltipWindow.HidePreviewTooltip (); };
				}
				menu.Items.Add (menuItem);
			}
			menu.Closed += delegate { RefactoringPreviewTooltipWindow.HidePreviewTooltip (); };
			return menu;
		}

		SourceEditor.SmartTagMarginMarker smartTagMarginMarker;
		private ITextSourceVersion beginVersion;

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

			var severity = fixes.GetSmartTagSeverity ();

			if (smartTagMarginMarker?.Line?.LineNumber != editor.CaretLine) {
				RemoveWidget ();
				smartTagMarginMarker = new SourceEditor.SmartTagMarginMarker () { SmartTagSeverity = severity };
				smartTagMarginMarker.ShowPopup += SmartTagMarginMarker_ShowPopup;
				editor.AddMarker (editor.GetLine (editor.CaretLine), smartTagMarginMarker);
			} else {
				smartTagMarginMarker.SmartTagSeverity = severity;
				var view = editor.GetContent<SourceEditorView> ();
				view.TextEditor.RedrawMarginLine (view.TextEditor.TextArea.QuickFixMargin, editor.CaretLine);
			}
		}

		void SmartTagMarginMarker_ShowPopup (object sender, EventArgs e)
		{
			var marker = (SourceEditor.SmartTagMarginMarker)sender;

			CancelSmartTagPopupTimeout ();
			smartTagPopupTimeoutId = GLib.Timeout.Add (menuTimeout, delegate {
				PopupQuickFixMenu (null, menu => { }, new Xwt.Point (marker.PopupPosition.X, marker.PopupPosition.Y + marker.Height));
				smartTagPopupTimeoutId = 0;
				return false;
			});
		}

		protected override void Initialize ()
		{
			base.Initialize ();
			DocumentContext.DocumentParsed += HandleDocumentDocumentParsed;
			Editor.CaretPositionChanged += HandleCaretPositionChanged;
			Editor.TextChanged += Editor_TextChanged;
			Editor.BeginAtomicUndoOperation += Editor_BeginAtomicUndoOperation;
			Editor.EndAtomicUndoOperation += Editor_EndAtomicUndoOperation;
		}

		void Editor_BeginAtomicUndoOperation (object sender, EventArgs e)
		{
			beginVersion = Editor.Version;
		}

		void Editor_EndAtomicUndoOperation (object sender, EventArgs e)
		{
			if (beginVersion != null && beginVersion.CompareAge (Editor.Version) != 0)
				RemoveWidget ();
			beginVersion = null;
		}

		void Editor_TextChanged (object sender, MonoDevelop.Core.Text.TextChangeEventArgs e)
		{
			if (Editor.IsInAtomicUndo)
				return;
			RemoveWidget ();
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
			if (!AnalysisOptions.EnableFancyFeatures
				|| smartTagMarginMarker == null
				) {
				// Fixes = RefactoringService.GetValidActions (Editor, DocumentContext, Editor.CaretLocation).Result;

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

	}
}
