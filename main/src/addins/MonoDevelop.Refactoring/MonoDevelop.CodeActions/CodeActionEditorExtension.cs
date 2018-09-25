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

namespace MonoDevelop.CodeActions
{
	class CodeActionEditorExtension : TextEditorExtension
	{
		const int menuTimeout = 250;
		uint smartTagPopupTimeoutId;

		void CancelSmartTagPopupTimeout ()
		{

			if (smartTagPopupTimeoutId != 0) {
				GLib.Source.Remove (smartTagPopupTimeoutId);
				smartTagPopupTimeoutId = 0;
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
			CancelQuickFixTimer ();
			RefactoringPreviewTooltipWindow.HidePreviewTooltip ();
			Editor.CaretPositionChanged -= HandleCaretPositionChanged;
			DocumentContext.DocumentParsed -= HandleDocumentDocumentParsed;
			Editor.MouseMoved -= HandleBeginHover;
			Editor.TextChanged -= Editor_TextChanged;
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

				smartTagTask = GetCurrentFixesAsync (quickFixCancellationTokenSource.Token);
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

					var fixes = await codeFixService.GetFixesAsync (ad, span, true, cancellationToken);

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

		async void PopupQuickFixMenu (Gdk.EventButton evt, Action<CodeFixMenu> menuAction)
		{
			using (Counters.FixesMenu.BeginTiming ("Show quick fixes menu")) {
				var token = quickFixCancellationTokenSource.Token;

				var fixes = await GetCurrentFixesAsync (token);
				if (token.IsCancellationRequested)
					return;

				var menu = CodeFixMenuService.CreateFixMenu (Editor, fixes, token);
				if (token.IsCancellationRequested)
					return;

				if (menu.Items.Count == 0) {
					return;
				}

				Editor.SuppressTooltips = true;
				if (menuAction != null)
					menuAction (menu);

				var p = Editor.LocationToPoint (Editor.OffsetToLocation (currentSmartTagBegin));
				Widget widget = Editor;
				var rect = new Gdk.Rectangle (
					(int)p.X + widget.Allocation.X,
					(int)p.Y + widget.Allocation.Y, 0, 0);

				ShowFixesMenu (widget, rect, menu);
			}
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
				Gdk.Pointer.Ungrab (Global.CurrentEventTime);
				var menu = CreateContextMenu (entrySet);
				RefactoringPreviewTooltipWindow.HidePreviewTooltip ();
				menu.Show (parent, x, y, () => {
					Editor.SuppressTooltips = false;
					RefactoringPreviewTooltipWindow.HidePreviewTooltip ();
				}, true);
			} catch (Exception ex) {
				LoggingService.LogError ("Error while context menu popup.", ex);
			}

			return true;
		}

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

			bool first = true;
			var smartTagLocBegin = offset;
			foreach (var fix in fixes.CodeFixActions) {
				var textSpan = fix.TextSpan;
				if (textSpan.IsEmpty)
					continue;
				if (first || offset < textSpan.Start) {
					smartTagLocBegin = textSpan.Start;
				}
				first = false;
			}

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
			currentSmartTag.IsVisible = fixes.CodeFixActions.Sum (x => x.Fixes.Length) > 0;
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
			Editor.MouseMoved += HandleBeginHover;
			Editor.CaretPositionChanged += HandleCaretPositionChanged;
			Editor.TextChanged += Editor_TextChanged;
			Editor.EndAtomicUndoOperation += Editor_EndAtomicUndoOperation;
		}

		void Editor_EndAtomicUndoOperation (object sender, EventArgs e)
		{
			RemoveWidget ();
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

	}
}
