//
// JSonIndentEngineTests.cs
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
using NUnit.Framework;
using System.Text;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.JSon;
using ICSharpCode.NRefactory6.CSharp;
using System;
using System.Threading.Tasks;
using UnitTests;
using MonoDevelop.Ide.TextEditing;
using MonoDevelop.Ide.Gui.Documents;

namespace MonoDevelop.SourceEditor
{
	[TestFixture]
	[RequireService(typeof(TextEditorService))]
	public class JSonIndentEngineTests : IdeTestBase
	{
		const string indentString = "\t";

		class TestCase : IDisposable
		{
			TextEditorExtensionTestCase testCase;

			public IDocumentIndentEngine Engine { get; set; }

			public static async Task<TestCase> Create (string text, bool tabsToSpaces = false)
			{
				var test = new TestCase ();
				var sb = new StringBuilder ();
				int offset = 0;
				for (int i = 0; i < text.Length; i++) {
					var ch = text [i];
					if (ch == '$') {
						offset = i;
						continue;
					}
					sb.Append (ch);
				}

				var content = new TestViewContent ();
				await content.Initialize (new FileDescriptor ("/a.json", null, null));
				content.Editor.MimeType = "application/json";

				content.Editor.Text = sb.ToString ();

				test.testCase = await TextEditorExtensionTestCase.Create (content, null, false);

				test.testCase.Document.Editor.Options = new CustomEditorOptions {
					TabsToSpaces = tabsToSpaces,
					TabSize = 4
				};

				var csi = new JSonIndentEngine (content.Editor);
				var result = new CacheIndentEngine (csi);
				result.Update (content.Editor, offset);
				test.Engine = result;

				return test;
			}

			public void Dispose ()
			{
				testCase.Dispose ();
			}
		}

		[Test]
		public async Task TestBracketIndentation ()
		{
			using (var testCase = await TestCase.Create (
				@"
{
$
")) {
				Assert.AreEqual (indentString, testCase.Engine.ThisLineIndent);
				Assert.AreEqual (indentString, testCase.Engine.NextLineIndent);
			}
		}

		[Test]
		public async Task TestBodyIndentation ()
		{
			using (var testCase = await TestCase.Create (
				@"
{
" + indentString + @"""foo"":""bar"",
$
")) {
				Assert.AreEqual (indentString, testCase.Engine.ThisLineIndent);
				Assert.AreEqual (indentString, testCase.Engine.NextLineIndent);
			}
		}

		[Test]
		public async Task TestArrayIndentation ()
		{
			using (var testCase = await TestCase.Create (
				@"
{
" + indentString + @"""test"":[
$
")) {
				Assert.AreEqual (indentString + indentString, testCase.Engine.ThisLineIndent);
				Assert.AreEqual (indentString + indentString, testCase.Engine.NextLineIndent);
			}
		}

		[Test]
		public async Task TestWindowsEOL ()
		{
			using (var testCase = await TestCase.Create ("\r\n{\r\n$\r\n")) {
				Assert.AreEqual (indentString, testCase.Engine.ThisLineIndent);
				Assert.AreEqual (indentString, testCase.Engine.NextLineIndent);
			}
		}

		/// <summary>
		/// Bug 40892 - json indenter should not indent multi-line strings
		/// </summary>
		[Test]
		public async Task TestBug40892 ()
		{
			using (var testCase = await TestCase.Create (
				@"
{
" + indentString + @"""test"":""
$
")) {
				Assert.AreEqual ("", testCase.Engine.ThisLineIndent);
				Assert.AreEqual ("", testCase.Engine.NextLineIndent);
			}
		}


		[Test]
		public async Task TestSpaceIndentation ()
		{
			using (var testCase = await TestCase.Create (
				@"
{
$
", true)) {
				Assert.AreEqual ("    ", testCase.Engine.ThisLineIndent);
				Assert.AreEqual (testCase.Engine.ThisLineIndent, testCase.Engine.NextLineIndent);
			}
		}

	}
}

