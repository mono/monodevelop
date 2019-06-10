//
// ICompletionKeyHandler.cs
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
using System;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.Ide.CodeCompletion
{
	[Obsolete ("Use the Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion APIs")]
	public interface ICompletionDataKeyHandler
	{
		/// <summary>
		/// Returns true if the character typed should be used to filter the specified completion
		/// item.  A character will be checked to see if it should filter an item.  If not, it will be
		/// checked to see if it should commit that item.  If it does neither, then completion will
		/// be dismissed.
		/// </summary>
		bool IsFilterCharacter(CompletionData completionItem, char ch, string textTypedSoFar);

		/// <summary>
		/// Returns true if the character is one that can commit the specified completion item. A
		/// character will be checked to see if it should filter an item.  If not, it will be checked
		/// to see if it should commit that item.  If it does neither, then completion will be
		/// dismissed.
		/// </summary>
		bool IsCommitCharacter(CompletionData completionItem, char ch, string textTypedSoFar);

		/// <summary>
		/// Returns true if the enter key that was typed should also be sent through to the editor
		/// after committing the provided completion item.
		/// </summary>
		bool SendEnterThroughToEditor(CompletionData completionItem, string textTypedSoFar);
	}	
}