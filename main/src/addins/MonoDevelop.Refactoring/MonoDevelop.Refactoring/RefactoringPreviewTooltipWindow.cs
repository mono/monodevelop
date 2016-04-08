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

		public RefactoringPreviewTooltipWindow (TextEditor editor, DocumentContext documentContext, CodeAction codeAction)
		{
			this.editor = editor;
			this.documentContext = documentContext;
			this.codeAction = codeAction;
			TransientFor = IdeApp.Workbench.RootWindow;

			using (var metrics = PangoContext.GetMetrics (PangoContext.FontDescription, PangoContext.Language)) {
				lineHeight = (int)Math.Ceiling (0.5 + (metrics.Ascent + metrics.Descent) / Pango.Scale.PangoScale);
			}
		}

		internal async void RequestPopup (Widget parent)
		{
			var token = popupSrc.Token;

			diff = await Task.Run (async delegate {
				Console.WriteLine ("action : " + codeAction);
				foreach (var op in await codeAction.GetOperationsAsync (token)) {
					Console.WriteLine ("operation : " + op);
					var ac = op as ApplyChangesOperation;
					if (ac == null) {
						continue;
					}
					var changedDocument = ac.ChangedSolution.GetDocument (documentContext.AnalysisDocument.Id);

					changedTextDocument = TextEditorFactory.CreateNewDocument (new StringTextSource ((await changedDocument.GetTextAsync (token)).ToString ()), editor.FileName);
					try {
						var list = new List<DiffHunk> (editor.GetDiff (changedTextDocument));
						if (list.Count > 0)
							return list;
					} catch (Exception e) {
						LoggingService.LogError ("Error while getting preview list diff.", e);
					}

				}
				return new List<DiffHunk> ();
			});
			if (diff.Count > 0 && !token.IsCancellationRequested)
				ShowPopup (parent, PopupPosition.Left);
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
				drawingLayout.SetText (document.GetLineText (lineNumber));
				int w, h;
				drawingLayout.GetPixelSize (out w, out h);
				x = Math.Max (x, w);
				y += lineHeight;
			}
		}

		protected override void OnDrawContent (Gdk.EventExpose evnt, Cairo.Context g)
		{
			var style = editor.Options.GetColorStyle ();
			g.Rectangle (0, 0, Allocation.Width, Allocation.Height);
			g.SetSourceColor (style.PlainText.Background);
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
					g.SetSourceColor (editor.Options.GetColorStyle ().PreviewDiffRemoved.Background);
					g.Fill ();
					g.SetSourceColor (editor.Options.GetColorStyle ().PreviewDiffRemoved.Foreground);
					DrawTextLine (g, editor, i, ref y);
				}

				for (int i = item.InsertStart; i < item.InsertStart + item.Inserted; i++) {
					g.Rectangle (0, y, Allocation.Width, lineHeight);
					g.SetSourceColor (editor.Options.GetColorStyle ().PreviewDiffAddedd.Background);
					g.Fill ();
					g.SetSourceColor (editor.Options.GetColorStyle ().PreviewDiffAddedd.Foreground);
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

		void DrawLine (Cairo.Context g, IReadonlyTextDocument document, int lineNumber, ref int y)
		{
			using (var drawingLayout = new Pango.Layout (this.PangoContext)) {
				var line = document.GetLine (lineNumber);
				drawingLayout.SetMarkup (editor.GetPangoMarkup (line.Offset, line.Length));
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
				drawingLayout.SetText (document.GetLineText (lineNumber));
				g.Save ();
				g.Translate (textBorder, y);
				g.ShowLayout (drawingLayout);
				g.Restore ();
				y += lineHeight;
			}
		}
	}
}