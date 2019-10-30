//
// DebuggerAsyncCompletionSource.cs
//
// Author:
//       David Karlas <david.karlas@xamarin.com>
//
// Copyright (c) 2019 Microsoft Corp.
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;

using ObjectValueFlags = Mono.Debugging.Client.ObjectValueFlags;

namespace MonoDevelop.Debugger
{
	static class DebuggerCompletion
	{
		public const string ContentType = "DebuggerCompletion";
	}

	[Export (typeof (IAsyncCompletionSourceProvider))]
	[Name ("Debugger Completion Source Provider")]
	[ContentType (DebuggerCompletion.ContentType)]
	sealed class DebuggerAsyncCompletionSourceProvider : IAsyncCompletionSourceProvider
	{
		public IAsyncCompletionSource GetOrCreate (ITextView textView)
		{
			return new DebuggerAsyncCompletionSource ();
		}
	}

	sealed class DebuggerAsyncCompletionSource : IAsyncCompletionSource
	{
		static readonly Task<object> EmptyDescription = Task.FromResult<object> (null);

		public async Task<CompletionContext> GetCompletionContextAsync (IAsyncCompletionSession session, CompletionTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
		{
			var text = triggerLocation.Snapshot.GetText (0, triggerLocation.Position);
			var data = await DebuggingService.GetCompletionDataAsync (DebuggingService.CurrentFrame, text, token);

			if (data == null)
				return new CompletionContext (ImmutableArray<CompletionItem>.Empty);

			var builder = ImmutableArray.CreateBuilder<CompletionItem> (data.Items.Count);

			foreach (var item in data.Items) {
				var image = new ImageElement (ObjectValueTreeViewController.GetImageId (item.Flags));

				builder.Add (new CompletionItem (item.Name, this, image));
			}

			return new CompletionContext (builder.MoveToImmutable ());
		}

		public Task<object> GetDescriptionAsync (IAsyncCompletionSession session, CompletionItem item, CancellationToken token)
		{
			return EmptyDescription;
		}

		public CompletionStartData InitializeCompletion (CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
		{
			var text = triggerLocation.Snapshot.GetText (0, triggerLocation.Position);
			var span = GetWordSpan (text, triggerLocation.Position);

			return new CompletionStartData (CompletionParticipation.ProvidesItems, new SnapshotSpan (triggerLocation.Snapshot, span));
		}

		public static Span GetWordSpan (string text, int position)
		{
			var start = position;
			while (start > 0 && char.IsLetterOrDigit (text[start - 1]))
				start--;

			// If we're brought up in the middle of a word, extend to the end of the word as well.
			// This means that if a user brings up the completion list at the start of the word they
			// will "insert" the text before what's already there (useful for qualifying existing
			// text).  However, if they bring up completion in the "middle" of a word, then they will
			// "overwrite" the text. Useful for correcting misspellings or just replacing unwanted
			// code with new code.
			var end = position;
			if (start != position) {
				while (end < text.Length && char.IsLetterOrDigit (text[end]))
					end++;
			}

			return Span.FromBounds (start, end);
		}
	}

	[Export (typeof (IAsyncCompletionCommitManagerProvider))]
	[Name ("Debugger Completion Commit Manager")]
	[ContentType (DebuggerCompletion.ContentType)]
	sealed class DebuggerAsyncCompletionCommitManagerProvider : IAsyncCompletionCommitManagerProvider
	{
		public IAsyncCompletionCommitManager GetOrCreate (ITextView textView)
		{
			return new DebuggerAsyncCompletionCommitManager ();
		}
	}

	sealed class DebuggerAsyncCompletionCommitManager : IAsyncCompletionCommitManager
	{
		static readonly char[] CommitCharacters = new char[] { ' ', '\t', '\n', '.', ',', '<', '>', '(', ')', '[', ']' };

		public IEnumerable<char> PotentialCommitCharacters {
			get { return CommitCharacters; }
		}

		public bool ShouldCommitCompletion (IAsyncCompletionSession session, SnapshotPoint location, char typedChar, CancellationToken token)
		{
			return true;
		}

		public CommitResult TryCommit (IAsyncCompletionSession session, ITextBuffer buffer, CompletionItem item, char typedChar, CancellationToken token)
		{
			// Note: Hitting Return should *always* complete the current selection, but other typed chars require examining context...
			if (typedChar != '\0' && typedChar != '\n') {
				var text = buffer.CurrentSnapshot.GetText ();
				var span = DebuggerAsyncCompletionSource.GetWordSpan (text, text.Length);

				if (span.Length == 0)
					return new CommitResult (true, CommitBehavior.None);

				var typedWord = text.AsSpan (span.Start, span.Length);

				if (!item.InsertText.AsSpan ().Contains (typedWord, StringComparison.Ordinal))
					return new CommitResult (true, CommitBehavior.None);
			}

			return CommitResult.Unhandled;
		}
	}
}
