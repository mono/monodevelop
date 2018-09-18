//
// AbstractCodeFormatterTests.cs
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
using System;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Projects.Policies;
using NUnit.Framework;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	public class AbstractCodeFormatterTests : IdeTestBase
	{
		class TestAbstractCodeFormatter : AbstractCodeFormatter
		{
			protected override ITextSource FormatImplementation (PolicyContainer policyParent, string mimeType, ITextSource input, int startOffset, int length)
			{
				return input;
			}

			protected override void CorrectIndentingImplementation (PolicyContainer policyParent, TextEditor editor, int line)
			{
				// nothing
			}
		}

		[Test]
		public void Test677780  ()
		{
			var formatter = new TestAbstractCodeFormatter ();
			var editor = TextEditorFactory.CreateNewEditor ();
			editor.Text = new string ('\n', 9);
			try {
				formatter.CorrectIndenting (PolicyService.InvariantPolicies, editor, 0);
				Assert.Fail ();
			} catch (ArgumentOutOfRangeException) { }
			formatter.CorrectIndenting (PolicyService.InvariantPolicies, editor, 1);
			formatter.CorrectIndenting (PolicyService.InvariantPolicies, editor, 10);
			try {
				formatter.CorrectIndenting (PolicyService.InvariantPolicies, editor, 11);
				Assert.Fail ();
			} catch (ArgumentOutOfRangeException) { }
		}
	}
}
