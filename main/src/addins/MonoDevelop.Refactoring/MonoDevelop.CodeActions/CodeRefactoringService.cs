//
// CodeActionService.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis.CodeRefactorings;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CodeActions
{
	static class CodeRefactoringService
	{
		static readonly List<CodeRefactoringDescriptor> codeActions = new List<CodeRefactoringDescriptor> ();

		static CodeRefactoringService ()
		{
			foreach (var type in typeof(ICSharpCode.NRefactory6.CSharp.Refactoring.IssueMarker).Assembly.GetTypes ()) {
				var exportAttr = type.GetCustomAttributes (typeof(ExportCodeRefactoringProviderAttribute), false).FirstOrDefault () as ExportCodeRefactoringProviderAttribute;
				if (exportAttr == null)
					continue;
				codeActions.Add (new CodeRefactoringDescriptor (type, exportAttr)); 
			}
		}

		public static IEnumerable<CodeRefactoringDescriptor> GetCodeActions (string language, bool includeDisabledNodes = false)
		{
			if (string.IsNullOrEmpty (language))
				return includeDisabledNodes ? codeActions : codeActions.Where (act => act.IsEnabled);
			return includeDisabledNodes ? codeActions.Where (ca => ca.Language == language) : codeActions.Where (ca => ca.Language == language && ca.IsEnabled);
		}

		public static async Task<IEnumerable<Tuple<CodeRefactoringDescriptor, CodeAction>>> GetValidActionsAsync (TextEditor editor, DocumentContext doc, TextSpan span, CancellationToken cancellationToken = default (CancellationToken))
		{
			if (editor == null)
				throw new ArgumentNullException ("editor");
			if (doc == null)
				throw new ArgumentNullException ("doc");
			var analysisDocument = doc.AnalysisDocument;
			var actions = new List<Tuple<CodeRefactoringDescriptor, CodeAction>> ();
			if (analysisDocument == null)
				return actions;
			var ctx = new CodeRefactoringContext (analysisDocument, span, cancellationToken);
			var root = await doc.AnalysisDocument.GetSyntaxRootAsync ();
			if (ctx.Span.End > root.Span.End)
				return actions;

			foreach (var descriptor in GetCodeActions (CodeRefactoringService.MimeTypeToLanguage(editor.MimeType))) {
				IEnumerable<CodeAction> refactorings;
				try {
					refactorings = await descriptor.GetProvider ().GetRefactoringsAsync (ctx);
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting refactorings from " + descriptor.IdString, e); 
					continue;
				}
				if (refactorings != null) {
					foreach (var action in refactorings)
						actions.Add (Tuple.Create (descriptor, action));
				}
			}
			return actions;
		}

		public static string MimeTypeToLanguage (string mimeType)
		{
			switch (mimeType) {
			case "text/x-csharp":
				return LanguageNames.CSharp;
			}
			return null;
		}
	}
}
