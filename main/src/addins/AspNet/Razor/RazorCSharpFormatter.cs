//
// RazorCSharpCodeFormatter.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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

using System.Collections.Generic;
using MonoDevelop.Ide.CodeFormatting;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.AspNet.Razor
{
	public class RazorCSharpFormatter : AbstractCodeFormatter
	{
		public override bool SupportsOnTheFlyFormatting { get { return true; } }
		public override bool SupportsCorrectingIndent { get { return true; } }
		public override bool SupportsPartialDocumentFormatting { get { return true; } }

		protected override void CorrectIndentingImplementation (PolicyContainer policyParent, TextEditor editor, int line)
		{
		}

		protected override Core.Text.ITextSource FormatImplementation (PolicyContainer policyParent, string mimeType, Core.Text.ITextSource input, int startOffset, int length)
		{
			return input.CreateSnapshot (startOffset, length);
        }

		protected override void OnTheFlyFormatImplementation (TextEditor editor, DocumentContext context, int startOffset, int length)
		{
		}

	}
}
