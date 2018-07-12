//
// ExpandSelectionHandlerTests.cs
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
using System.Threading.Tasks;
using MonoDevelop.CSharp;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using NUnit.Framework;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	class ExpandSelectionHandlerTests : TextEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharpWithReferences;
		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			yield return new CSharpCompletionTextEditorExtension ();
		}

		[Test]
		public async Task TestExpandSelection ()
		{
			await CheckAutoBracket (@"
			
class FooBar
{
	public static void Main (string[] args)
	{
		var i = 5 +@ 6;
	}
}", doc => {
				ExpandSelectionHandler.Run (doc);
				Assert.AreEqual (74, doc.Editor.SelectionRange.Offset);
				Assert.AreEqual (5, doc.Editor.SelectionRange.Length);
				return Task.CompletedTask;
			});
		}


		[Test]
		public async Task TestShrinkselection ()
		{
			await CheckAutoBracket (@"
			
class FooBar
{
	public static void Main (string[] args)
	{
		var i = 5 +@ 6;
	}
}", async doc => {
				ExpandSelectionHandler.Run (doc);
				var selection = doc.Editor.SelectionRange;
				ExpandSelectionHandler.Run (doc);
				await ShrinkSelectionHandler.Run (doc);
				Assert.AreEqual (selection, doc.Editor.SelectionRange);

			});
		}
		 
		// Bug 59918 - Unhandled exception because we pop an empty Queue
		[Test]
		public async Task TestBug59918 ()
		{
			await CheckAutoBracket (@"
			
class FooBar
{
	public static void Main (string[] args)
	{
		var i = 5 +@ 6;
	}
}", async doc => {
				ExpandSelectionHandler.Run (doc);
				await ShrinkSelectionHandler.Run (doc);
				await ShrinkSelectionHandler.Run (doc);

			});
		}

		async Task CheckAutoBracket (string text, Func<Document, Task> action)
		{
			int endPos = text.IndexOf ('@');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			using (var testCase = await SetupTestCase (text, Math.Max (0, endPos))) {
				var doc = testCase.Document;
				await doc.UpdateParseDocument ();

				var ctx = new CodeCompletionContext ();

				await action (doc);
			}
		}
	}
}
	