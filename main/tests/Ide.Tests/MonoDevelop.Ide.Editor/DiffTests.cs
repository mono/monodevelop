//
// DiffTests.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using UnitTests;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	class DiffTests : IdeTestBase
	{
		[Test]
		public void EmptyTreeList ()
		{
			var editor = TextEditorFactory.CreateNewEditor ();
			editor.Text = "1\n2\n3\n4\n5\n";

			var editor2 = TextEditorFactory.CreateNewEditor ();
			editor2.Text = "4\n1\n5\n2\n3\n";

			var diff = editor.GetDiffAsString (editor2);

			Assert.AreEqual ("--- \n+++ \n@@ -1,5 +1,5 @@\n+4\n 1\n+5\n 2\n 3\n-4\n-5\n", diff.Replace ("\r", ""));

		}

	}
}
