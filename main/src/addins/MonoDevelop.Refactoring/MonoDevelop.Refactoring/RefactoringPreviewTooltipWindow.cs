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
using Gdk;

namespace MonoDevelop.Refactoring
{
	class RefactoringPreviewTooltipWindow : PopoverWindow
	{
		TextEditor editor;
		CodeAction codeAction;
		DocumentContext documentContext;
		CancellationTokenSource popupSrc = new CancellationTokenSource ();
		ITextDocument changedTextDocument;

		ProcessResult diff;
		readonly int lineHeight;
		readonly FontDescription fontDescription;

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

		void RequestPopup (Xwt.Rectangle rect)
		{
			var token = popupSrc.Token;
			Task.Run (async delegate {
				try {
					foreach (var op in await codeAction.GetPreviewOperationsAsync (token)) {
						if (!(op is ApplyChangesOperation ac)) {
							continue;
						}
						var changedDocument = ac.ChangedSolution.GetDocument (documentContext.AnalysisDocument.Id);

						var changedText = await changedDocument.GetTextAsync (token);
						var changedTextSource = new StringTextSource (changedText.ToString ());
						changedTextDocument = TextEditorFactory.CreateNewDocument (changedTextSource, editor.FileName);

						try {
							var processor = new DiffProcessor (editor, changedTextDocument);
							return processor.Process ();
						} catch (Exception e) {
							LoggingService.LogError ("Error while getting preview list diff.", e);
						}
					}
				} catch (OperationCanceledException) { }

				return new ProcessResult ();
			}).ContinueWith (t => {
				diff = t.Result;
				if (diff.LineResults.Count > 0 && !token.IsCancellationRequested) {
					var pos = PopupPosition.Left;
					if (Platform.IsMac) {
						var screenRect = GtkUtil.ToScreenCoordinates (IdeApp.Workbench.RootWindow, IdeApp.Workbench.RootWindow.GdkWindow, rect.ToGdkRectangle ());
						var geometry = Screen.GetUsableMonitorGeometry (Screen.GetMonitorAtPoint (screenRect.X, screenRect.Y));
						var request = SizeRequest ();
						if (screenRect.X - geometry.X < request.Width) {
							pos = PopupPosition.Top;
							if (geometry.Bottom - screenRect.Bottom < request.Height)
								pos = PopupPosition.Bottom;
						} else {
							pos = PopupPosition.Right;
						}
					}
					ShowPopup (rect, pos);
				}
			}, Runtime.MainTaskScheduler);
		}

		enum LineKind
		{
			Normal,
			Removed,
			Added
		}

		struct LineResult
		{
			public bool XNeedsMeasure;
			public string TextOrMarkup;
			public LineKind LineKind;
		}

		class ProcessResult
		{
			public List<LineResult> LineResults = new List<LineResult> ();
			public HslColor AddedForeground, AddedBackground, RemovedForeground, RemovedBackground;
		}

		class DiffProcessor
		{
			readonly TextEditor baseDocument;
			readonly IReadonlyTextDocument changedTextDocument;

			public int IndentLength { get; set; }

			public DiffProcessor (TextEditor baseDocument, IReadonlyTextDocument changedTextDocument)
			{
				this.baseDocument = baseDocument;
				this.changedTextDocument = changedTextDocument;

				IndentLength = -1;
			}

			public ProcessResult Process ()
			{
				var he = baseDocument.GetDiff (changedTextDocument, new DiffOptions (false, true)).GetEnumerator ();
				he.MoveNext ();
				var current = he.Current;

				var qh = new Queue<DiffHunk> ();
				qh.Enqueue (current);

				IndentLength = -1;

				var result = new ProcessResult {
					AddedBackground = SyntaxHighlightingService.GetColor (baseDocument.Options.GetEditorTheme (), EditorThemeColors.PreviewDiffAddedBackground),
					AddedForeground = SyntaxHighlightingService.GetColor (baseDocument.Options.GetEditorTheme (), EditorThemeColors.PreviewDiffAdded),
					RemovedBackground = SyntaxHighlightingService.GetColor (baseDocument.Options.GetEditorTheme (), EditorThemeColors.PreviewDiffRemovedBackground),
					RemovedForeground = SyntaxHighlightingService.GetColor (baseDocument.Options.GetEditorTheme (), EditorThemeColors.PreviewDiffRemoved),
				};

				while (he.MoveNext ()) {
					var next = he.Current;

					if (current.Overlaps (next)) {
						// Change upper bounds.
					} else {
						ProcessHunks (qh, result);
					}
					qh.Enqueue (next);

					current = next;
				}

				if (qh.Count != 0) {
					ProcessHunks (qh, result);
				}

				return result;
			}

			void ProcessHunks (Queue<DiffHunk> qh, ProcessResult result)
			{
				DiffHunk item;
				int remStart;
				int insStart;
				int distance = 0;

				do {
					item = qh.Dequeue ();
					remStart = Math.Max (1, item.RemoveStart - (distance != 0 ? distance : item.Context));
					insStart = Math.Max (1, item.InsertStart - (distance != 0 ? distance : item.Context));

					for (int i = Math.Min (remStart, insStart); i < item.RemoveStart; i++) {
						ProcessLine (baseDocument, i, LineKind.Normal, result);
					}

					for (int i = item.RemoveStart; i < item.RemoveStart + item.Removed; i++) {
						ProcessLine (baseDocument, i, LineKind.Removed, result);
					}

					for (int i = item.InsertStart; i < item.InsertStart + item.Inserted; i++) {
						ProcessLine (changedTextDocument, i, LineKind.Added, result);
					}

					if (qh.Count != 0)
						distance = item.DistanceTo (qh.Peek ());
				} while (qh.Count != 0);

				int remEnd = Math.Min (baseDocument.LineCount, item.RemoveStart + item.Removed + item.Context);
				for (int i = item.RemoveStart + item.Removed; i < remEnd; i++) {
					ProcessLine (baseDocument, i, LineKind.Normal, result);
				}
			}

			void ProcessLine (IReadonlyTextDocument document, int lineNumber, LineKind lineKind, ProcessResult result)
			{
				var line = document.GetLine (lineNumber);
				var curLineIndent = CalcIndentLength (line.GetIndentation (document));

				if (IndentLength < 0 || IndentLength > curLineIndent)
					IndentLength = curLineIndent;

				var correctedIndentLength = CorrectIndent (document, line, IndentLength);
				var offset = line.Offset + Math.Min (correctedIndentLength, line.Length);
				var length = Math.Max (0, line.Length - correctedIndentLength);

				string text;
				if (lineKind == LineKind.Normal && document is TextEditor editor) {
					text = editor.GetMarkup (offset, length, new MarkupOptions (MarkupFormat.Pango, false));
				} else {
					text = document.GetTextAt (offset, length);
				}

				var lineResult = new LineResult {
					XNeedsMeasure = line.Length != curLineIndent,
					TextOrMarkup = text,
					LineKind = lineKind,
				};

				result.LineResults.Add (lineResult);
			}

			static int CalcIndentLength (string indent)
			{
				int indentLen = 0;
				foreach (var ch in indent) {
					if (ch == '\t') {
						indentLen = indentLen - indentLen % DefaultSourceEditorOptions.Instance.TabSize + DefaultSourceEditorOptions.Instance.TabSize;
					} else {
						indentLen++;
					}
				}
				return indentLen;
			}

			static int CorrectIndent (IReadonlyTextDocument document, IDocumentLine line, int indentLength)
			{
				int result = 0;
				int o = line.Offset;
				while (indentLength > 0) {
					char ch = document [o + result];
					if (ch == '\t') {
						indentLength = indentLength + indentLength % DefaultSourceEditorOptions.Instance.TabSize - DefaultSourceEditorOptions.Instance.TabSize;
					} else if (ch == ' ') {
						indentLength--;
					} else {
						break;
					}
					result++;
				}
				return result;
			}
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

			int y = verticalTextBorder * 2 - verticalTextSpace + (Platform.IsWindows ? 10 : 2);
			int x = 0;

			foreach (var line in diff.LineResults) {
				MeasureLine (line, ref x, ref y);
			}

			requisition.Height = y;
			requisition.Width = x + textBorder * 2;
		}

		void MeasureLine (LineResult lineResult, ref int x, ref int y)
		{
			using (var drawingLayout = new Pango.Layout (PangoContext)) {
				drawingLayout.FontDescription = fontDescription;
				if (!lineResult.XNeedsMeasure) {
					y += lineHeight;
					return;
				}

				if (lineResult.LineKind == LineKind.Normal)
					drawingLayout.SetMarkup (lineResult.TextOrMarkup);
				else
					drawingLayout.SetText (lineResult.TextOrMarkup);

				drawingLayout.GetPixelSize (out int w, out int h);
				x = Math.Max (x, w);
				y += lineHeight;
			}
		}

		protected override void OnDrawContent (Gdk.EventExpose evnt, Cairo.Context g)
		{
			var style = editor.Options.GetEditorTheme ();
			g.Rectangle (0, 0, Allocation.Width, Allocation.Height);

			g.SetSourceColor (SyntaxHighlightingService.GetColor (style, EditorThemeColors.Background));
			g.Fill ();

			int y = verticalTextSpace / 2;

			foreach (var lineResult in diff.LineResults) {
				switch (lineResult.LineKind) {
				case LineKind.Normal:
					DrawLine (g, lineResult, ref y, isMarkup: true);
					break;
				case LineKind.Removed:
					g.Rectangle (0, y, Allocation.Width, lineHeight);

					g.SetSourceColor (diff.RemovedBackground);
					g.Fill ();
					g.SetSourceColor (diff.RemovedForeground);
					DrawLine (g, lineResult, ref y, isMarkup: false);
					break;
				case LineKind.Added:
					g.Rectangle (0, y, Allocation.Width, lineHeight);
					g.SetSourceColor (diff.AddedBackground);
					g.Fill ();
					g.SetSourceColor (diff.AddedForeground);
					DrawLine (g, lineResult, ref y, isMarkup: false);
					break;
				}
			}
		}

		void DrawLine (Cairo.Context g, LineResult lineResult, ref int y, bool isMarkup)
		{
			using (var drawingLayout = new Pango.Layout (PangoContext)) {
				drawingLayout.FontDescription = fontDescription;

				if (isMarkup)
					drawingLayout.SetMarkup (lineResult.TextOrMarkup);
				else
					drawingLayout.SetText (lineResult.TextOrMarkup);

				g.Save ();
				g.Translate (textBorder, y);
				g.ShowLayout (drawingLayout);
				g.Restore ();
				y += lineHeight;
			}
		}
	}
}