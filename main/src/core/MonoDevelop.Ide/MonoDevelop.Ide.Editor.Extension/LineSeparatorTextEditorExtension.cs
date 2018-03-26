//
// LineSeparatorTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Editor;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.Extension
{
	class LineSeparatorTextEditorExtension : TextEditorExtension
	{
		CancellationTokenSource src = new CancellationTokenSource ();
		List<ITextLineMarker> markers = new List<ITextLineMarker> ();

		bool enabled;
		protected override void Initialize ()
		{
			DocumentContext.DocumentParsed += DocumentContext_DocumentParsed;
			DefaultSourceEditorOptions.Instance.Changed += OptionsChanged;
			OptionsChanged (this, EventArgs.Empty);

		}

		void EnableExtension ()
		{
			enabled = true;
			DocumentContext.ReparseDocument ();
		}

		void DisableExtension ()
		{
			RemoveMarkers ();
			enabled = false;
		}

		public override void Dispose ()
		{
			RemoveMarkers ();
			DocumentContext.DocumentParsed -= DocumentContext_DocumentParsed;
			DefaultSourceEditorOptions.Instance.Changed -= OptionsChanged;
		}

		void OptionsChanged (object sender, EventArgs e)
		{
			if (DefaultSourceEditorOptions.Instance.ShowProcedureLineSeparators && !enabled) {
				EnableExtension ();
			}

			if (!DefaultSourceEditorOptions.Instance.ShowProcedureLineSeparators && enabled) {
				DisableExtension ();
			}
		}

		async void DocumentContext_DocumentParsed (object sender, EventArgs e)
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
			if (!enabled)
				return;
			var analysisDocument = DocumentContext?.AnalysisDocument;
			if (analysisDocument == null)
				return;
			var lineSeparatorService = DocumentContext?.RoslynWorkspace?.Services.GetLanguageServices (analysisDocument.Project.Language).GetService<ILineSeparatorService> ();
			if (lineSeparatorService == null)
				return;
			var token = src.Token;
			var separators = await lineSeparatorService.GetLineSeparatorsAsync (analysisDocument, new TextSpan (0, Editor.Length), token);
			if (token.IsCancellationRequested)
				return;
			RemoveMarkers ();
			foreach (var s in separators) {
				var line = Editor.GetLineByOffset (s.Start);
				var marker = Editor.TextMarkerFactory.CreateLineSeparatorMarker (Editor);
				Editor.AddMarker (line, marker);
				markers.Add (marker);
			}
		}

		void RemoveMarkers ()
		{
			foreach (var m in markers)
				Editor.RemoveMarker (m);
			markers.Clear ();
		}
	}
}
