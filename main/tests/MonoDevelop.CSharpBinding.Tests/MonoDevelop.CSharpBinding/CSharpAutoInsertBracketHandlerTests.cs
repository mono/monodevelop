//
// CSharpAutoInsertBracketHandlerTests.cs
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
using MonoDevelop.Core;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.CSharp.Features.AutoInsertBracket;
using MonoDevelop.CSharpBinding.Tests;
using MonoDevelop.Ide;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using NUnit.Framework;

namespace MonoDevelop.CSharpBinding
{
	[TestFixture]
	class CSharpAutoInsertBracketHandlerTests : TextEditorExtensionTestBase
	{
		protected override EditorExtensionTestData GetContentData () => EditorExtensionTestData.CSharpWithReferences;
		protected override IEnumerable<TextEditorExtension> GetEditorExtensions ()
		{
			foreach (var ext in base.GetEditorExtensions ())
				yield return ext;
			yield return new CSharpCompletionTextEditorExtension ();
		}

		[Test]
		public async Task TestBug59627 ()
		{
			await CheckAutoBracket (@"$""", "$\"\"");
		}
		 
		[Test]
		public async Task TestBrackets ()
		{
			await CheckAutoBracket (@"(", "()");
			await CheckAutoBracket (@"[", "[]");
			await CheckAutoBracket (@"{", "{}");
		}

		[Test]
		public async Task TestString ()
		{
			await CheckAutoBracket (@"""", "\"\"");
			await CheckAutoBracket (@"'", "''");
		}

		private async Task CheckAutoBracket (string mid, string expected)
		{
			var prefix = @"
class FooBar
{
	public static void Main (string[] args)
	{
		Console.WriteLine (";


			var suffix = ");\n\t}\n}\n";

			var text = prefix + mid + "@" + suffix;

			int endPos = text.IndexOf ('@');
			if (endPos >= 0)
				text = text.Substring (0, endPos) + text.Substring (endPos + 1);

			using (var testCase = await SetupTestCase (text, cursorPosition: Math.Max (0, endPos))) {
				var doc = testCase.Document;
				await doc.UpdateParseDocument ();

				var ctx = new CodeCompletionContext ();

				var handler = new CSharpAutoInsertBracketHandler ();
				char ch = mid [mid.Length - 1];
				handler.Handle (doc.Editor, doc, KeyDescriptor.FromGtk ((Gdk.Key)ch, ch, Gdk.ModifierType.None));
				var newText = doc.Editor.GetTextAt (prefix.Length, doc.Editor.Length - prefix.Length - suffix.Length);
				Assert.AreEqual (expected, newText);
			}
		}
	}
}
