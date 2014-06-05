//
// CompletionHandler.cs
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
using MonoDevelop.Ide.CodeCompletion;
using ICSharpCode.NRefactory6.CSharp.Completion;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Gui.Content
{
	public abstract class CompletionProvider
	{
		public abstract bool CanHandle (string mimeType);
		public abstract CompletionHandler CreateHandler (string mimeType, Document document, CodeMapping mapping);
	}

	public class CompletionHandler
	{
		protected readonly Document document;
		protected readonly CodeMapping mapping;

		public CompletionHandler (Document document, CodeMapping mapping)
		{
			if (mapping == null)
				throw new ArgumentNullException ("mapping");
			if (document == null)
				throw new ArgumentNullException ("document");
			this.mapping = mapping;
			this.document = document;
		}

		public virtual ICompletionDataList GetAutomaticCompletion (CodeCompletionContext completionContext, char typedChar, ref int triggerWordLength)
		{
			return null;
		}

		public virtual ParameterHintingResult GetAutomaticParameterHinting (CodeCompletionContext completionContext, char typedChar)
		{
			return null;
		}

		public virtual ICompletionDataList RequestCodeCompletionComman (CodeCompletionContext completionContext)
		{
			// This default implementation of CodeCompletionCommand calls HandleCodeCompletion providing
			// the char at the cursor position. If it returns a provider, just return it.
			int pos = completionContext.TriggerOffset;
			if (pos > 0) {
				char ch = mapping.Text[pos - 1];
				int triggerWordLength = completionContext.TriggerWordLength;
				var completionList = GetAutomaticCompletion (completionContext, ch, ref triggerWordLength);
				if (completionList != null)
					return completionList;
			}
			return null;
		}

		public virtual ParameterHintingResult RequestParameterHinting (CodeCompletionContext completionContext)
		{
			// This default implementation of ParameterCompletionCommand calls HandleParameterCompletion providing
			// the char at the cursor position. If it returns a provider, just return it.

			int pos = completionContext.TriggerOffset;
			if (pos <= 0)
				return null;
			char ch = mapping.Text[pos - 1];
			var cp = GetAutomaticParameterHinting (completionContext, ch);
			if (cp != null)
				return cp;
			return null;
		}
	}
}