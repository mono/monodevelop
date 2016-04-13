/*
//
// GetCurrentParameterIndexTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory6.CSharp.Completion;
using System.Text;

namespace ICSharpCode.NRefactory6.CSharp.CodeCompletion
{
	[TestFixture]
	class GetCurrentParameterIndexTests
	{
		static int GetIndex(string text)
		{
			var editorText = new StringBuilder();
			int trigger = 0, end = 0;
			for (int i = 0; i < text.Length; i++) {
				if (text[i] == '@') {
					trigger = editorText.Length;
					continue;
				}
				if (text[i] == '$') {
					end = editorText.Length;
					continue;
				}
				editorText.Append(text [i]);
			}

			var doc = new ReadOnlyDocument(editorText.ToString ());
			var pctx = new CSharpProjectContent();
			var rctx = new CSharpTypeResolveContext(pctx.CreateCompilation().MainAssembly);
			var ctxProvider = new DefaultCompletionContextProvider(doc, new CSharpUnresolvedFile());
			var engine = new CSharpParameterCompletionEngine(doc, ctxProvider, new ParameterCompletionTests.TestFactory(pctx), pctx, rctx);

			return engine.GetCurrentParameterIndex(trigger, end);
		}

		[Test]
		public void TestFirstParameterStart ()
		{
			var index = GetIndex("@Test($");
			Assert.AreEqual(0, index);
		}

		[Test]
		public void TestFirstParameter ()
		{
			var index = GetIndex("@Test(foo$");
			Assert.AreEqual(1, index);
		}

		[Test]
		public void TestSecondParameter ()
		{
			var index = GetIndex("@Test(foo, $");
			Assert.AreEqual(2, index);
		}

		
		[Test]
		public void TestAfterMethod ()
		{
			var index = GetIndex("@Test(foo) $");
			Assert.AreEqual(-1, index);
		}

		[Test]
		public void TestJaggedMethod ()
		{
			var index = GetIndex("@Foo(Test($");
			Assert.AreEqual(-1, index);
		}
	}
}

*/