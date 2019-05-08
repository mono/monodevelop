//
// VBNetTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using System.Threading;
using System.Threading.Tasks;


using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.Editor.Implementation.Highlighting;
using Microsoft.CodeAnalysis.FindSymbols;
using Roslyn.Utilities;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor.Highlighting;
using Microsoft.CodeAnalysis.DocumentHighlighting;
using Microsoft.VisualStudio.Platform;
using MonoDevelop.Ide.Composition;

namespace MonoDevelop.VBNet
{
#pragma warning disable CS0618 // Type or member is obsolete
	class VBNetTextEditorExtension : TextEditorExtension
	{
		ISyntaxHighlighting fallbackHighlighting;

		protected override void Initialize ()
		{
			fallbackHighlighting = Editor.SyntaxHighlighting;
			UpdateHighlighting ();
			DocumentContext.AnalysisDocumentChanged += HandleAnalysisDocumentChanged;
		}

		void HandleAnalysisDocumentChanged (object sender, EventArgs args)
		{
			Runtime.RunInMainThread (delegate {
				UpdateHighlighting ();
			});
		}

		void UpdateHighlighting ()
		{
			if (DocumentContext?.AnalysisDocument == null) {
				if (Editor.SyntaxHighlighting != fallbackHighlighting)
					Editor.SyntaxHighlighting = fallbackHighlighting;
				return;
			}
			var old = Editor.SyntaxHighlighting as TagBasedSyntaxHighlighting;
			if (old == null) {
				Editor.SyntaxHighlighting = CompositionManager.Instance.GetExportedValue<ITagBasedSyntaxHighlightingFactory> ().CreateSyntaxHighlighting (Editor.TextView, "source.vb");
			}
		}

		public override void Dispose ()
		{
			DocumentContext.AnalysisDocumentChanged -= HandleAnalysisDocumentChanged;
			Editor.SyntaxHighlighting = fallbackHighlighting;
			base.Dispose ();
		}
	}
#pragma warning restore CS0618 // Type or member is obsolete

}
