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
using RefactoringEssentials;
using System.Linq;

namespace MonoDevelop.CodeActions
{
	static class CodeRefactoringService
	{
		readonly static List<CodeDiagnosticProvider> providers = new List<CodeDiagnosticProvider> ();

		static CodeRefactoringService ()
		{
			providers.Add (new BuiltInCodeDiagnosticProvider ());

			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/CodeDiagnosticProvider", delegate (object sender, ExtensionNodeEventArgs args) {
				var node = (TypeExtensionNode)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					providers.Add ((CodeDiagnosticProvider)node.CreateInstance ());
					break;
				}
			});
		}

		public async static Task<IEnumerable<CodeDiagnosticDescriptor>> GetCodeDiagnosticsAsync (DocumentContext documentContext, string language, CancellationToken cancellationToken = default (CancellationToken))
		{
			var result = new List<CodeDiagnosticDescriptor> ();

			foreach (var provider in providers) {
				if (cancellationToken.IsCancellationRequested)
					return Enumerable.Empty<CodeDiagnosticDescriptor> ();
				result.AddRange (await provider.GetCodeDiagnosticDescriptorsAsync (documentContext, language, cancellationToken).ConfigureAwait (false));
			}
			return result;
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
