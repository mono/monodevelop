//
// RefactoringPreviewTooltipWindow.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis.CodeActions;
using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;
using System.Threading;
using MonoDevelop.Core.Text;
using Gtk;
using System.Threading.Tasks;
using System.Collections.Generic;
using MonoDevelop.Ide.Editor.Util;
using System.Linq;
using MonoDevelop.Core;
using Pango;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.Refactoring
{
	class RefactoringPreviewTooltipWindow : PopoverWindow
	{
		TextEditor editor;
		CodeAction codeAction;
		DocumentContext documentContext;
		CancellationTokenSource popupSrc = new CancellationTokenSource ();
		ITextDocument changedTextDocument;

		List<DiffHunk> diff;

		int lineHeight;
		int indentLength;
		FontDescription fontDescription;

		static RefactoringPreviewTooltipWindow currentPreviewWindow;

		RefactoringPreviewTooltipWindow (TextEditor editor, CodeAction codeAction)
		{
			this.editor = editor;
			this.documentContext = documentContext = editor.DocumentContext;
			this.codeAction = codeAction;
			TransientFor = IdeApp.Workbench.RootWindow;

			fontDescription = Pango.FontDescription.FromString (DefaultSourceEditorOptions.Instance.FontName);
			fontDescription.Size = (int)(fontDescription.Size * 0.8f);

			using (var metrics = PangoContext.GetMetrics (fontDescription, PangoContext.Language)) {
				lineHeight = (int)Math.Ceiling (0.5 + (metrics.Ascent + metrics.Descent) / Pango.Scale.PangoScale);
			}
		}

		public static void ShowPreviewTooltip (TextEditor editor, CodeAction fix, Xwt.Rectangle rect)
		{
			HidePreviewTooltip ();
			currentPreviewWindow = new RefactoringPreviewTooltipWindow (editor, fix);
			currentPreviewWindow.RequestPopup (rect);
		}

		public static void HidePreviewTooltip ()
		{
			if (currentPreviewWindow != null) {
				currentPreviewWindow.Destroy ();
				currentPreviewWindow = null;
			}
		}

		async void RequestPopup (Xwt.Rectangle rect)
		{
			var token = popupSrc.Token;

			diff = await Task.Run (async delegate {
				try {
					foreach (var op in await codeAction.GetPreviewOperationsAsync (token)) {
						var ac = op as ApplyChangesOperation;
						if (ac == null) {
							continue;
						}
						var changedDocument = ac.ChangedSolution.GetDocument (documentContext.AnalysisDocument.Id);

						changedTextDocument = TextEditorFactory.CreateNewDocument (new StringTextSource ((await changedDocument.GetTextAsync (token)).ToString ()), editor.FileName);
						try {
							var list = new List<DiffHunk> (editor.GetDiff (changedTextDocument, new DiffOptions (false, true)));
							if (list.Count > 0)
								return list;
						} catch (Exception e) {
							LoggingService.LogError ("Error while getting preview list diff.", e);
						}

					}
				} catch (OperationCanceledException) {}
				return new List<DiffHunk> ();
			});
			if (diff.Count > 0 && !token.IsCancellationRequested)
				ShowPopup (rect, PopupPosition.Left);
		}

		protected override void OnDestroyed ()
		{
			popupSrc.Cancel ();
			base.OnDestroyed ();
		}

		// Layout constants
		const int verticalTextBorder = 10;
		const int verticalTextSpace = 7;

		const int textBorder = 12;

		protected override void OnSizeRequested (ref Gtk.Requisition requisition)
		{
			base.OnSizeRequested (ref requisition);
			int y = verticalTextBorder * 2 - verticalTextSpace + (Core.Platform.IsWindows ? 10 : 2);

			var qh = new Queue<DiffHunk> ();
			var he = diff.GetEnumerator ();
			he.MoveNext ();
			var current = he.Current;
			DiffHunk next;
			qh.Enqueue (current);
			int x = 0;
			indentLength = -1;
			while (he.MoveNext ()) {
				next = he.Current;

				if (current.Overlaps (next)) {
					// Change upper bounds.
				} else {
					MeasureHunks (qh, editor, changedTextDocument, ref x, ref y);
				}
				qh.Enqueue (next);

				current = next;
			}
			if (qh.Count != 0) {
				MeasureHunks (qh, editor, changedTextDocument, ref x, ref y);
			}
			requisition.Height = y;
			requisition.Width = x + textBorder * 2;
		}

		void MeasureHunks (Queue<DiffHunk> qh, IReadonlyTextDocument baseDocument, IReadonlyTextDocument changedDocument, ref int x, ref int y)
		{
			DiffHunk item;
			int remStart;
			int insStart;
			int distance = 0;
			do {
				item = qh.Dequeue ();
				remStart = System.Math.Max (1, item.RemoveStart - (distance != 0 ? distance : item.Context));
				insStart = System.Math.Max (1, item.InsertStart - (distance != 0 ? distance : item.Context));

				for (int i = System.Math.Min (remStart, insStart); i < item.RemoveStart; i++) {
					MeasureLine (editor, i, ref x, ref y);
				}

				for (int i = item.RemoveStart; i < item.RemoveStart + item.Removed; i++) {
					MeasureLine (editor, i, ref x, ref y);
				}

				for (int i = item.InsertStart; i < item.InsertStart + item.Inserted; i++) {
					MeasureLine ( changedDocument, i, ref x, ref y);
				}

				if (qh.Count != 0)
					distance = item.DistanceTo (qh.Peek ());
			} while (qh.Count != 0);

			int remEnd = System.Math.Min (baseDocument.LineCount, item.RemoveStart + item.Removed + item.Context);
			for (int i = item.RemoveStart + item.Removed; i < remEnd; i++) {
				MeasureLine (editor, i, ref x, ref y);
			}
		}

		void MeasureLine (IReadonlyTextDocument document, int lineNumber, ref int x, ref int y)
		{
			using (var drawingLayout = new Pango.Layout (this.PangoContext)) {
				drawingLayout.FontDescription = fontDescription;
				var line = document.GetLine (lineNumber);
				var indent = line.GetIndentation (document);
				var curLineIndent = CalcIndentLength(indent);
				if (line.Length == curLineIndent) {
					y += lineHeight;
					return;
				}
				if (this.indentLength < 0 || this.indentLength > curLineIndent)
					this.indentLength = curLineIndent;
				drawingLayout.SetText (document.GetTextAt (line));
				int w, h;
				drawingLayout.GetPixelSize (out w, out h);
				x = Math.Max (x, w);
				y += lineHeight;
			}
		}

		int CalcIndentLength (string indent)
		{
			int result = 0;
			foreach (var ch in indent) {
				if (ch == '\t') {
					result = result - result % DefaultSourceEditorOptions.Instance.TabSize + DefaultSourceEditorOptions.Instance.TabSize;
				} else {
					result++;
				}
			}
			return result;
		}

		protected override void OnDrawContent (Gdk.EventExpose evnt, Cairo.Context g)
		{
			var style = editor.Options.GetEditorTheme ();
			g.Rectangle (0, 0, Allocation.Width, Allocation.Height);

			g.SetSourceColor (SyntaxHighlightingService.GetColor (style, EditorThemeColors.Background));
			g.Fill ();

			int y = verticalTextSpace / 2;

			var qh = new Queue<DiffHunk> ();
			var he = diff.GetEnumerator ();
			he.MoveNext ();
			var current = he.Current;
			DiffHunk next;
			qh.Enqueue (current);
			while (he.MoveNext ()) {
				next = he.Current;

				if (current.Overlaps (next)) {
					// Change upper bounds.
				} else {
					WriteHunks (qh, editor, changedTextDocument, g, ref y);
				}
				qh.Enqueue (next);

				current = next;
			}
			if (qh.Count != 0) {
				WriteHunks (qh, editor, changedTextDocument, g, ref y);
			}
		}

		void WriteHunks (Queue<DiffHunk> qh, IReadonlyTextDocument baseDocument, IReadonlyTextDocument changedDocument, Cairo.Context g, ref int y)
		{
			DiffHunk item;
			int remStart;
			int insStart;
			int distance = 0;

			do {
				item = qh.Dequeue ();
				remStart = System.Math.Max (1, item.RemoveStart - (distance != 0 ? distance : item.Context));
				insStart = System.Math.Max (1, item.InsertStart - (distance != 0 ? distance : item.Context));

				for (int i = System.Math.Min (remStart, insStart); i < item.RemoveStart; i++) {
					DrawLine (g, editor, i, ref y);
				}

				for (int i = item.RemoveStart; i < item.RemoveStart + item.Removed; i++) {
					g.Rectangle (0, y, Allocation.Width, lineHeight);

					g.SetSourceColor (SyntaxHighlightingService.GetColor (editor.Options.GetEditorTheme (), EditorThemeColors.PreviewDiffRemovedBackground));
					g.Fill ();
					g.SetSourceColor (SyntaxHighlightingService.GetColor (editor.Options.GetEditorTheme (), EditorThemeColors.PreviewDiffRemoved));
					DrawTextLine (g, editor, i, ref y);
				}

				for (int i = item.InsertStart; i < item.InsertStart + item.Inserted; i++) {
					g.Rectangle (0, y, Allocation.Width, lineHeight);
					g.SetSourceColor (SyntaxHighlightingService.GetColor (editor.Options.GetEditorTheme (), EditorThemeColors.PreviewDiffAddedBackground));
					g.Fill ();
					g.SetSourceColor (SyntaxHighlightingService.GetColor (editor.Options.GetEditorTheme (), EditorThemeColors.PreviewDiffAdded));
					DrawTextLine (g, changedDocument, i, ref y);
				}

				if (qh.Count != 0)
					distance = item.DistanceTo (qh.Peek ());
			} while (qh.Count != 0);

			int remEnd = System.Math.Min (baseDocument.LineCount, item.RemoveStart + item.Removed + item.Context);
			for (int i = item.RemoveStart + item.Removed; i < remEnd; i++) {
				DrawLine (g, editor, i, ref y);
			}
		}

		int CorrectIndent (IReadonlyTextDocument document, IDocumentLine line, int indentLength)
		{
			int result = 0;
			int o = line.Offset;
			while (indentLength > 0) {
				char ch = document [o + result];
				if (ch == '\t') {
					indentLength = indentLength + indentLength % DefaultSourceEditorOptions.Instance.TabSize - DefaultSourceEditorOptions.Instance.TabSize;
				} else if (ch == ' '){
					indentLength --;
				} else {
					break;
				}
				result++;
			}
			return result;
		}

		void DrawLine (Cairo.Context g, TextEditor editor, int lineNumber, ref int y)
		{
			using (var drawingLayout = new Pango.Layout (this.PangoContext)) {
				drawingLayout.FontDescription = fontDescription;
				var line = editor.GetLine (lineNumber);
				var correctedIndentLength = CorrectIndent (editor, line, indentLength);
				drawingLayout.SetMarkup (editor.GetPangoMarkup (line.Offset + Math.Min (correctedIndentLength, line.Length), Math.Max (0, line.Length - correctedIndentLength)));
				g.Save ();
				g.Translate (textBorder, y);
				g.ShowLayout (drawingLayout);
				g.Restore ();
				y += lineHeight;
			}
		}

		void DrawTextLine (Cairo.Context g, IReadonlyTextDocument document, int lineNumber, ref int y)
		{
			using (var drawingLayout = new Pango.Layout (this.PangoContext)) {
				drawingLayout.FontDescription = fontDescription;
				var line = document.GetLine (lineNumber);
				var correctedIndentLength = CorrectIndent (document, line, indentLength);
				drawingLayout.SetText (document.GetTextAt (line.Offset + Math.Min (correctedIndentLength, line.Length), Math.Max (0, line.Length - correctedIndentLength)));
				g.Save ();
				g.Translate (textBorder, y);
				g.ShowLayout (drawingLayout);
				g.Restore ();
				y += lineHeight;
			}
		}
	}
}