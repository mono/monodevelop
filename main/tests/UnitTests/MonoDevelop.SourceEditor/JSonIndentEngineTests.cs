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
using System;
using NUnit.Framework;
using MonoDevelop.Ide.CodeCompletion;
using ICSharpCode.NRefactory.CSharp;
using System.Text;
using ICSharpCode.NRefactory.Editor;
using MonoDevelop.SourceEditor.JSon;
using Mono.TextEditor;

namespace MonoDevelop.SourceEditor
{
	[TestFixture]
	public class JSonIndentEngineTests
	{
		public static IDocumentIndentEngine CreateEngine (string text)
		{
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

			var data = new TextEditorData ();
			data.Text = sb.ToString ();
			var csi = new JSonIndentEngine (data);
			var result = new CacheIndentEngine (csi);
			result.Update (offset);
			return result;
		}

		[Test]
		public void TestBracketIndentation ()
		{
			var engine = CreateEngine (
				@"
{
$
");
			Assert.AreEqual ("\t", engine.ThisLineIndent);
			Assert.AreEqual ("\t", engine.NextLineIndent);
		}

		[Test]
		public void TestBodyIndentation ()
		{
			var engine = CreateEngine (
				@"
{
	""foo"":""bar"",
$
");
			Assert.AreEqual ("\t", engine.ThisLineIndent);
			Assert.AreEqual ("\t", engine.NextLineIndent);
		}

		[Test]
		public void TestArrayIndentation ()
		{
			var engine = CreateEngine (
				@"
{
	""test"":[
$
");
			Assert.AreEqual ("\t\t", engine.ThisLineIndent);
			Assert.AreEqual ("\t\t", engine.NextLineIndent);
		}
	}
}

