//
// PreProcessorCompletionProvider.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corporation. All rights reserved.
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

using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.CSharp.Completion.Provider
{
	[ExportCompletionProvider ("PreProcessorCompletionProvider", LanguageNames.CSharp)]
	class PreProcessorCompletionProvider : CommonCompletionProvider
	{
		public override bool ShouldTriggerCompletion (SourceText text, int position, CompletionTrigger trigger, Microsoft.CodeAnalysis.Options.OptionSet options)
		{
			return trigger.Character == ' ';
		}

		public override async Task ProvideCompletionsAsync (CompletionContext context)
		{
			var document = context.Document;
			var position = context.Position;
			var cancellationToken = context.CancellationToken;

			var model = await document.GetSemanticModelForSpanAsync (new TextSpan (position, 0), cancellationToken).ConfigureAwait (false);

			var workspace = document.Project.Solution.Workspace;
			var ctx = CSharpSyntaxContext.CreateContext (workspace, model, position, cancellationToken);

			if (ctx.IsPreProcessorExpressionContext) {
				var parseOptions = model.SyntaxTree.Options as CSharpParseOptions;
				foreach (var define in parseOptions.PreprocessorSymbolNames) {
					context.AddItem (CompletionItem.Create (define));
				}
			}
		}
	}
}