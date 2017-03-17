//
// OverrideCompletionHandlerTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using NUnit.Framework;
using ICSharpCode.NRefactory6.CSharp.Completion;
using Microsoft.CodeAnalysis;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion.Roslyn
{
	class CompletionTestBase : ICSharpCode.NRefactory6.TestBase
	{
		//[TestFixtureSetUp]
		//public void Setup ()
		//{
		//	Xwt.Application.Initialize ();
		//	Gtk.Application.Init ();
		//}

		internal virtual CompletionContextHandler CreateContextHandler()
		{
			return null;
		}

		protected void VerifyItemsExist(string input, params string[] items)
		{
			foreach (var item in items) {
				VerifyItemExists(input, item);
			}
		}

		internal CompletionResult CreateProvider(string input, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false)
		{
			int cursorPosition;
			SemanticModel semanticModel;
			Document document;
			var idx = input.IndexOf("$$", StringComparison.Ordinal);
			if (idx >= 0) {
				input = input.Substring(0, idx) + input.Substring(idx + 1);
			}
			var engine = CodeCompletionBugTests.CreateEngine(input, out cursorPosition, out semanticModel, out document, null, sourceCodeKind);
			char triggerChar = cursorPosition > 0 ? document.GetTextAsync().Result [cursorPosition - 1] : '\0';
			var completionContext = new CompletionContext (document, cursorPosition, semanticModel);
			var exclusiveHandler = CreateContextHandler ();
			if (exclusiveHandler != null) {
				completionContext.AdditionalContextHandlers = new [] { exclusiveHandler };
				completionContext.UseDefaultContextHandlers = false;
			}

			return engine.GetCompletionDataAsync (
				completionContext,
				new CompletionTriggerInfo (usePreviousCharAsTrigger ? CompletionTriggerReason.CharTyped : CompletionTriggerReason.CompletionCommand, triggerChar)).Result;
		}


		protected void VerifyItemExists(string input, string expectedItem, string expectedDescriptionOrNull = null, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, bool experimental = false, int? glyph = null)
		{
			var provider = CreateProvider (input, sourceCodeKind, usePreviousCharAsTrigger);

			if (provider.Find (expectedItem) == null) {
				Console.WriteLine ("Found items:");
				foreach (var item in provider)
					Console.WriteLine (item.DisplayText);
				Console.WriteLine ("----- Expected: " + expectedItem);
				Assert.Fail ("item '" + expectedItem + "' not found.");
			}

		}

		protected void VerifyItemIsAbsent(string input, string expectedItem, string expectedDescriptionOrNull = null, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, bool experimental = false)
		{
			var provider = CreateProvider (input, sourceCodeKind, usePreviousCharAsTrigger);

			if (provider.Find (expectedItem) != null) {
				Console.WriteLine ("Found items:");
				foreach (var item in provider)
					Console.WriteLine (item.DisplayText);
				Console.WriteLine ("----- Should be absent: " + expectedItem);
				Assert.Fail ("item '" + expectedItem + "' found but shouldn't.");
			}
		}


		protected void VerifyItemsAbsent(string input, params string[] items)
		{
			foreach (var item in items) {
				VerifyItemIsAbsent(input, item);
			}
		}

		protected void VerifyNoItemsExist(string input, SourceCodeKind? sourceCodeKind = null, bool usePreviousCharAsTrigger = false, bool experimental = false)
		{
			var provider = CreateProvider (input, sourceCodeKind, usePreviousCharAsTrigger);
			if (provider != null && provider.Count > 0) {
				foreach (var data in provider)
					Console.WriteLine(data.DisplayText);
			}
			Assert.IsTrue(provider == null || provider.Count == 0, "provider should be empty");
		}



		protected string AddUsingDirectives(string usingDirectives, string text)
		{
			return
				usingDirectives +
				@"


" +
			text;
		}

		protected string AddInsideMethod(string text)
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

	}

}