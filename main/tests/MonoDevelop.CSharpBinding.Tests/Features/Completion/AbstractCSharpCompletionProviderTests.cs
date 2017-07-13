//
// AbstractCSharpCompletionProviderTests.cs
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
using System.Linq;
using System.Collections.Immutable;
using System.Threading.Tasks;
using ICSharpCode.NRefactory6;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Ide.CodeCompletion;
using NUnit.Framework;
using Microsoft.CodeAnalysis.CSharp.Completion;
using System.Collections.Generic;
using System.Threading;

namespace MonoDevelop.CSharpBinding.Tests.Features.Completion
{
	abstract class AbstractCSharpCompletionProviderTests : TestBase
	{
		protected abstract IEnumerable<CompletionProvider> CreateCompletionProvider ();

		protected void VerifyItemsExist (string input, params string [] items)
		{
			foreach (var item in items) {
				VerifyItemExists (input, item);
			}
		}

		protected void VerifyItemExists (string input, string expectedItem, string expectedDescriptionOrNull = null, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, bool experimental = false, int? glyph = null)
		{
			var provider = CreateProvider (input, sourceCodeKind, usePreviousCharAsTrigger);
			if (provider == null)
				Assert.Fail ("No provider created!");
			if (!provider.Items.Any (i => i.DisplayText == expectedItem)) {
				Console.WriteLine ("Found items:");
				foreach (var item in provider.Items)
					Console.WriteLine (item.DisplayText);
				Console.WriteLine ("----- Expected: " + expectedItem);
				Assert.Fail ("item '" + expectedItem + "' not found.");
			}
		}

		protected void VerifyItemIsAbsent (string input, string expectedItem, string expectedDescriptionOrNull = null, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, bool experimental = false)
		{
			var provider = CreateProvider (input, sourceCodeKind, usePreviousCharAsTrigger);

			if (provider.Items.Any (i => i.DisplayText == expectedItem)) {
				Console.WriteLine ("Found items:");
				foreach (var item in provider.Items)
					Console.WriteLine (item.DisplayText);
				Console.WriteLine ("----- Should be absent: " + expectedItem);
				Assert.Fail ("item '" + expectedItem + "' found but shouldn't.");
			}
		}

		protected void VerifyItemsAbsent (string input, params string [] items)
		{
			foreach (var item in items) {
				VerifyItemIsAbsent (input, item);
			}
		}

		protected void VerifyNoItemsExist (string input, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, bool experimental = false)
		{
			var provider = CreateProvider (input, sourceCodeKind, usePreviousCharAsTrigger);
			if (provider != null && provider.Items.Length > 0) {
				foreach (var data in provider.Items)
					Console.WriteLine (data.DisplayText);
			}
			Assert.IsTrue (provider == null || provider.Items.Length == 0, "provider should be empty");
		}



		protected string AddUsingDirectives (string usingDirectives, string text)
		{
			return
				usingDirectives +
				@"
" +
			text;
		}

		protected string AddInsideMethod (string text)
		{
			return
@"class C
{
  void F()
  {
    " + text +
@"  }
}";
		}


		internal CompletionList CreateProvider (string input, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false)
		{

			string editorText;
			int cursorPosition = input.IndexOf ("$$", StringComparison.Ordinal);
			var parsedText = input.Substring (0, cursorPosition) + input.Substring (cursorPosition + 2);

			var workspace = new InspectionActionTestBase.TestWorkspace ();

			var projectId = ProjectId.CreateNewId ();
			//var solutionId = SolutionId.CreateNewId();
			var documentId = DocumentId.CreateNewId (projectId);

			workspace.Open (ProjectInfo.Create (
						projectId,
						VersionStamp.Create (),
						"TestProject",
						"TestProject",
						LanguageNames.CSharp,
						null,
						null,
						new CSharpCompilationOptions (
							OutputKind.DynamicallyLinkedLibrary,
							false,
							"",
							"",
							"Script",
							null,
							OptimizationLevel.Debug,
							false,
							false
						),
						new CSharpParseOptions (
							LanguageVersion.CSharp6,
							DocumentationMode.None,
							SourceCodeKind.Regular,
							ImmutableArray.Create ("DEBUG", "TEST")
						),
						new [] {
							DocumentInfo.Create(
								documentId,
								"a.cs",
								null,
								SourceCodeKind.Regular,
								TextLoader.From(TextAndVersion.Create(SourceText.From(parsedText), VersionStamp.Create()))
							)
						},
						null,
						InspectionActionTestBase.DefaultMetadataReferences
					)
			);
			var engine = new MonoDevelop.CSharp.Completion.RoslynParameterHintingEngine ();

			var compilation = workspace.CurrentSolution.GetProject (projectId).GetCompilationAsync ().Result;

			var document = workspace.CurrentSolution.GetDocument (documentId);
			var semanticModel = document.GetSemanticModelAsync ().Result;

			char triggerChar = cursorPosition > 0 ? document.GetTextAsync ().Result [cursorPosition - 1] : '\0';

			var cs = (CSharpCompletionService)workspace.Services.GetLanguageServices (LanguageNames.CSharp).GetService<CompletionService> ();
			cs.SetTestProviders (CreateCompletionProvider ());

			var trigger = CompletionTrigger.Invoke;
			if (usePreviousCharAsTrigger)
				trigger = new CompletionTrigger (CompletionTriggerKind.Insertion, triggerChar);
			return cs.GetCompletionsAsync (document, cursorPosition, trigger, null, null, default (CancellationToken)).Result;
		}
	}
}
