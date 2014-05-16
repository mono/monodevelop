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

namespace MonoDevelop.CodeActions
{
	static class CodeActionService
	{
		static readonly List<CodeActionDescriptor> codeActions = new List<CodeActionDescriptor> ();

		static CodeActionService ()
		{
			foreach (var type in typeof(ICSharpCode.NRefactory6.CSharp.Refactoring.IssueMarker).Assembly.GetTypes ()) {
				var exportAttr = type.GetCustomAttributes (typeof(ExportCodeRefactoringProviderAttribute), false).FirstOrDefault () as ExportCodeRefactoringProviderAttribute;
				if (exportAttr == null)
					continue;
				codeActions.Add (new CodeActionDescriptor (exportAttr.Name, exportAttr.Language, type)); 
			}
		}

		public static IEnumerable<CodeActionDescriptor> GetCodeActions (string language = null)
		{
			if (string.IsNullOrEmpty (language))
				return codeActions;
			return codeActions.Where (ca => ca.Language == language);
		}

		public static async Task<IEnumerable<CodeAction>> GetValidActions (MonoDevelop.Ide.Gui.Document doc, TextSpan span, CancellationToken cancellationToken = default (CancellationToken))
		{
			var analysisDocument = doc.AnalysisDocument;
			var actions = new List<CodeAction> ();

			foreach (var provider in GetCodeActions (LanguageNames.CSharp).Select (desc => desc.GetProvider ())) {
				var refactorings = await provider.GetRefactoringsAsync (analysisDocument, span, cancellationToken);
				if (refactorings != null)
					actions.AddRange (refactorings);
			}
			return actions;
		}
//
//		Task<IEnumerable<CodeAction>> GetRefactoringsAsync (Document document, TextSpan span, CancellationToken cancellationToken);
//
//
//		Task<IEnumerable<CodeAction>> GetRefactoringsAsync (Document document, TextSpan span, CancellationToken cancellationToken);
	}
}
