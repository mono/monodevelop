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
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;
using MonoDevelop.CodeIssues;
using Mono.Addins;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using MonoDevelop.Core.Text;

namespace MonoDevelop.CodeActions
{
	static class CodeRefactoringService
	{
		readonly static List<CodeDiagnosticProvider> providers = new List<CodeDiagnosticProvider> ();

		static CodeRefactoringService ()
		{	
			providers.Add (new BuiltInCodeDiagnosticProvider ());

			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/CodeDiagnosticProvider", delegate(object sender, ExtensionNodeEventArgs args) {
				var node = (TypeExtensionNode)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					providers.Add ((CodeDiagnosticProvider)node.CreateInstance ());
					break;
				}
			});
		}

		public async static Task<IEnumerable<CodeDiagnosticDescriptor>> GetCodeIssuesAsync (DocumentContext documentContext, string language, CancellationToken cancellationToken = default (CancellationToken))
		{
			var result = new List<CodeDiagnosticDescriptor> ();
			foreach (var provider in providers) {
				result.AddRange (await provider.GetCodeIssuesAsync (documentContext, language, cancellationToken).ConfigureAwait (false));
			}

			return result;
		}

		public async static Task<IEnumerable<CodeDiagnosticFixDescriptor>> GetCodeFixDescriptorAsync (DocumentContext documentContext, string language, CancellationToken cancellationToken = default (CancellationToken))
		{
			var result = new List<CodeDiagnosticFixDescriptor> ();
			foreach (var provider in providers) {
				result.AddRange (await provider.GetCodeFixDescriptorAsync (documentContext, language, cancellationToken).ConfigureAwait (false));
			}
			return result;
		}

		public async static Task<IEnumerable<CodeRefactoringDescriptor>> GetCodeActionsAsync (DocumentContext documentContext, string language, CancellationToken cancellationToken = default (CancellationToken))
		{
			var result = new List<CodeRefactoringDescriptor> ();
			foreach (var provider in providers) {
				result.AddRange (await provider.GetCodeActionsAsync (documentContext, language, cancellationToken).ConfigureAwait (false));
			}
			return result;
		}

		public static async Task<IEnumerable<ValidCodeAction>> GetValidActionsAsync (TextEditor editor, DocumentContext doc, TextSpan span, CancellationToken cancellationToken = default (CancellationToken))
		{
			if (editor == null)
				throw new ArgumentNullException ("editor");
			if (doc == null)
				throw new ArgumentNullException ("doc");
			var parsedDocument = doc.ParsedDocument;
			var actions = new List<ValidCodeAction> ();
			if (parsedDocument == null)
				return actions;
			var model = parsedDocument.GetAst<SemanticModel> ();
			if (model == null)
				return actions;
			var root = await model.SyntaxTree.GetRootAsync(cancellationToken);
			if (span.End > root.Span.End)
				return actions;
			TextSpan tokenSegment = span;
			var token = root.FindToken (span.Start);
			if (!token.IsMissing)
				tokenSegment = token.Span;
			
			foreach (var descriptor in await GetCodeActionsAsync (doc, MimeTypeToLanguage(editor.MimeType), cancellationToken)) {
				try {
					await descriptor.GetProvider ().ComputeRefactoringsAsync (
						new CodeRefactoringContext (doc.AnalysisDocument, span, delegate (CodeAction ca) {
							var nrca = ca as NRefactoryCodeAction;
							var validSegment = tokenSegment;
							if (nrca != null)
								validSegment = nrca.TextSpan;
							actions.Add (new ValidCodeAction (ca, validSegment));
						}, cancellationToken)
					);
				} catch (OperationCanceledException) {
					break;
				} catch (Exception e) {
					LoggingService.LogError ("Error while getting refactorings from " + descriptor.IdString, e); 
					continue;
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
