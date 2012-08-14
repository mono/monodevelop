// ICompletionWidget.cs
//
// Author:
//   Peter Johanson  <latexer@gentoo.org>
//
// Copyright (c) 2007 Peter Johanson
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
//
//

using System;
using MonoDevelop.Projects;
using Gtk;

namespace MonoDevelop.Ide.CodeCompletion
{
	public interface ICompletionWidget
	{
		CodeCompletionContext CurrentCodeCompletionContext {
			get;
		}

		int CaretOffset { get;}
		int TextLength { get; }
		int SelectedLength { get; }
		string GetText (int startOffset, int endOffset);
		
		char GetChar (int offset);
		
		void Replace (int offset, int count, string text);
		
		Gtk.Style GtkStyle { get; }

		CodeCompletionContext CreateCodeCompletionContext (int triggerOffset);
		string GetCompletionText (CodeCompletionContext ctx);
		void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word);
		
		void SetCompletionText (CodeCompletionContext ctx, string partial_word, string complete_word, int completeWordOffset);
		
		event EventHandler CompletionContextChanged;
	}
}
