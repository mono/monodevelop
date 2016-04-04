//
// CompletionContextHandler.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;

using Microsoft.CodeAnalysis.Text;
using System.Threading.Tasks;
using System.Security.Policy;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.CodeCompletion;

namespace ICSharpCode.NRefactory6.CSharp.Completion
{
	abstract class CompletionContextHandler : ICompletionDataKeyHandler
	{
		public async Task<IEnumerable<CompletionData>> GetCompletionDataAsync (CompletionResult result, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken = default(CancellationToken))
		{
			// If we were triggered by typign a character, then do a semantic check to make sure
			// we're still applicable.  If not, then return immediately.
			if (info.CompletionTriggerReason == CompletionTriggerReason.CharTyped)
			{
				var isSemanticTriggerCharacter = await IsSemanticTriggerCharacterAsync(completionContext.Document, completionContext.Position - 1, cancellationToken).ConfigureAwait(false);
				if (!isSemanticTriggerCharacter)
					return null;
			}

			return await GetItemsWorkerAsync(result, engine, completionContext, info, ctx, cancellationToken).ConfigureAwait(false);

		}

		protected virtual Task<bool> IsSemanticTriggerCharacterAsync(Document document, int characterPosition, CancellationToken cancellationToken)
        {
			return Task.FromResult (true);
        }

		protected abstract Task<IEnumerable<CompletionData>> GetItemsWorkerAsync (CompletionResult result, CompletionEngine engine, CompletionContext completionContext, CompletionTriggerInfo info, SyntaxContext ctx, CancellationToken cancellationToken);

		static readonly char[] csharpCommitChars = {
			' ', '{', '}', '[', ']', '(', ')', '.', ',', ':',
			';', '+', '-', '*', '/', '%', '&', '|', '^', '!',
			'~', '=', '<', '>', '?', '@', '#', '\'', '\"', '\\'
		};

		public virtual bool IsCommitCharacter (CompletionData completionItem, char ch, string textTypedSoFar)
		{
			return csharpCommitChars.Contains (ch);
		}

		public virtual bool SendEnterThroughToEditor(CompletionData completionItem, string textTypedSoFar)
		{
			return string.Compare (completionItem.DisplayText, textTypedSoFar, StringComparison.OrdinalIgnoreCase) == 0;
		}

		public virtual bool IsFilterCharacter(CompletionData completionItem, char ch, string textTypedSoFar)
		{
			return false;
		}

		public virtual Task<bool> IsExclusiveAsync(CompletionContext completionContext, SyntaxContext syntaxContext, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
		{
			return Task.FromResult (false);
		}

		public virtual bool IsTriggerCharacter (SourceText text, int position)
		{
			var ch = text [position];
			return ch == '.' || // simple member access
				ch == '#' || // pre processor directives 
				ch == '>' && position >= 1 && text [position - 1] == '-' || // pointer member access
				ch == ':' && position >= 1 && text [position - 1] == ':' || // alias name
				IsStartingNewWord (text, position);
		}

		internal protected static bool IsTriggerAfterSpaceOrStartOfWordCharacter(SourceText text, int characterPosition)
		{
			var ch = text[characterPosition];
			if (ch == ' ') {
				ch = text[characterPosition - 1];
				return !char.IsWhiteSpace (ch);
			}
			return IsStartingNewWord(text, characterPosition);
		}

		internal protected static bool IsStartingNewWord (SourceText text, int position)
		{
			var ch = text [position];
			if (!SyntaxFacts.IsIdentifierStartCharacter (ch))
				return false;

			if (position > 0 && IsWordCharacter (text [position - 1]))
				return false;

			if (position < text.Length - 1 && IsWordCharacter (text [position + 1]))
				return false;

			return true;
		}

		protected static bool IsWordCharacter (char ch)
		{
			return SyntaxFacts.IsIdentifierStartCharacter (ch) || SyntaxFacts.IsIdentifierPartCharacter (ch);
		}

		protected static bool IsOnStartLine(int position, SourceText text, int startLine)
		{
			return text.Lines.IndexOf(position) == startLine;
		}

		protected static TextSpan GetTextChangeSpan(SourceText text, int position)
		{
			return GetTextChangeSpan(text, position, IsTextChangeSpanStartCharacter, IsWordCharacter);
		}

		public static bool IsTextChangeSpanStartCharacter(char ch)
		{
			return ch == '@' || IsWordCharacter(ch);
		}

		public static TextSpan GetTextChangeSpan(SourceText text, int position,
			Func<char, bool> isWordStartCharacter, Func<char, bool> isWordCharacter)
		{
			int start = position;
			while (start > 0 && isWordStartCharacter(text[start - 1]))
			{
				start--;
			}

			// If we're brought up in the middle of a word, extend to the end of the word as well.
			// This means that if a user brings up the completion list at the start of the word they
			// will "insert" the text before what's already there (useful for qualifying existing
			// text).  However, if they bring up completion in the "middle" of a word, then they will
			// "overwrite" the text. Useful for correcting misspellings or just replacing unwanted
			// code with new code.
			int end = position;
			if (start != position)
			{
				while (end < text.Length && isWordCharacter(text[end]))
				{
					end++;
				}
			}

			return TextSpan.FromBounds(start, end);
		}

		protected class UnionCompletionItemComparer : IEqualityComparer<CompletionData>
		{
			public static UnionCompletionItemComparer Instance = new UnionCompletionItemComparer();

			public bool Equals(CompletionData x, CompletionData y)
			{
				return x.DisplayText == y.DisplayText;
			}

			public int GetHashCode(CompletionData obj)
			{
				return obj.DisplayText.GetHashCode();
			}
		}
	}
}
