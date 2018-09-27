#if DEBUG_COMPLETION
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.Intellisense;
using System.Collections.Immutable;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.PatternMatching;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Core.Imaging;
using System;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.Text.Editor;
using System.Threading;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
	[Export (typeof (IAsyncCompletionItemSourceProvider))]
	[Name ("Debug completion item source")]
	[Order (After = "default")]
	[ContentType ("any")]
	public class DebugCompletionItemSourceProvider : IAsyncCompletionItemSourceProvider
	{
		DebugCompletionItemSource _instance;

		IAsyncCompletionItemSource IAsyncCompletionItemSourceProvider.GetOrCreate (ITextView textView)
		{
			if (_instance == null)
				_instance = new DebugCompletionItemSource ();
			return _instance;
		}
	}

	public class DebugCompletionItemSource : IAsyncCompletionItemSource
	{
		private static readonly AccessibleImageId Icon1 = new AccessibleImageId (new Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 666, "TODO: remove", "Icon description");
		private static readonly CompletionFilter Filter1 = new CompletionFilter ("Diagnostic", "d", Icon1);
		private static readonly AccessibleImageId Icon2 = new AccessibleImageId (new Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 2852, "TODO: remove", "Icon description");
		private static readonly CompletionFilter Filter2 = new CompletionFilter ("Snippets", "s", Icon2);
		private static readonly AccessibleImageId Icon3 = new AccessibleImageId (new Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 473, "TODO: remove", "Icon description");
		private static readonly CompletionFilter Filter3 = new CompletionFilter ("Class", "c", Icon3);
		private static readonly ImmutableArray<CompletionFilter> FilterCollection1 = ImmutableArray.Create (Filter1);
		private static readonly ImmutableArray<CompletionFilter> FilterCollection2 = ImmutableArray.Create (Filter2);
		private static readonly ImmutableArray<CompletionFilter> FilterCollection3 = ImmutableArray.Create (Filter3);
		private static readonly ImmutableArray<char> commitCharacters = ImmutableArray.Create (' ', ';', '\t', '.', '<', '(', '[');

		void IAsyncCompletionItemSource.CustomCommit (ITextView view, ITextBuffer buffer, CompletionItem item, ITrackingSpan applicableSpan, char typeChar, CancellationToken token)
		{
			throw new System.NotImplementedException ();
		}

		async Task<CompletionContext> IAsyncCompletionItemSource.GetCompletionContextAsync (CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
		{
			var charBeforeCaret = triggerLocation.Subtract (1).GetChar ();
			SnapshotSpan applicableSpan;
			if (commitCharacters.Contains (charBeforeCaret)) {
				// skip this character. the applicable span starts later
				applicableSpan = new SnapshotSpan (triggerLocation, 0);
			} else {
				// include this character. the applicable span starts here
				applicableSpan = new SnapshotSpan (triggerLocation - 1, 1);
			}
			return await Task.FromResult (new CompletionContext (
				ImmutableArray.Create (
					new CompletionItem ("SampleItem<>", this, Icon3, FilterCollection3, string.Empty, false, "SampleItem", "SampleItem<>", "SampleItem", ImmutableArray<AccessibleImageId>.Empty),
					new CompletionItem ("AnotherItem🐱‍👤", this, Icon3, FilterCollection3, string.Empty, false, "AnotherItem", "AnotherItem", "AnotherItem", ImmutableArray.Create (Icon3)),
					new CompletionItem ("Sampling", this, Icon1, FilterCollection1),
					new CompletionItem ("Sampler", this, Icon1, FilterCollection1),
					new CompletionItem ("Sapling", this, Icon2, FilterCollection2, "Sapling is a young tree"),
					new CompletionItem ("OverSampling", this, Icon1, FilterCollection1, "overload"),
					new CompletionItem ("AnotherSample", this, Icon2, FilterCollection2),
					new CompletionItem ("AnotherSampling", this, Icon2, FilterCollection2),
					new CompletionItem ("Simple", this, Icon3, FilterCollection3, "KISS"),
					new CompletionItem ("Simpler", this, Icon3, FilterCollection3, "KISS")
				), applicableSpan));//, true, true, "Suggestion mode description!"));
		}

		async Task<object> IAsyncCompletionItemSource.GetDescriptionAsync (CompletionItem item, CancellationToken token)
		{
			return await Task.FromResult ("This is a tooltip for " + item.DisplayText);
		}

		ImmutableArray<char> IAsyncCompletionItemSource.GetPotentialCommitCharacters () => commitCharacters;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		async Task IAsyncCompletionItemSource.HandleViewClosedAsync (Text.Editor.ITextView view)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			return;
		}

		bool IAsyncCompletionItemSource.ShouldCommitCompletion (char typeChar, SnapshotPoint location)
		{
			return true;
		}

		bool IAsyncCompletionItemSource.ShouldTriggerCompletion (char typeChar, SnapshotPoint location)
		{
			return true;
		}
	}

	[Export (typeof (IAsyncCompletionItemSourceProvider))]
	[Name ("Debug HTML completion item source")]
	[Order (After = "default")]
	[ContentType ("RazorCSharp")]
	public class DebugHtmlCompletionItemSourceProvider : IAsyncCompletionItemSourceProvider
	{
		DebugHtmlCompletionItemSource _instance;

		IAsyncCompletionItemSource IAsyncCompletionItemSourceProvider.GetOrCreate (ITextView textView)
		{
			if (_instance == null)
				_instance = new DebugHtmlCompletionItemSource ();
			return _instance;
		}
	}

	public class DebugHtmlCompletionItemSource : IAsyncCompletionItemSource
	{
		void IAsyncCompletionItemSource.CustomCommit (Text.Editor.ITextView view, ITextBuffer buffer, CompletionItem item, ITrackingSpan applicableSpan, char typeChar, CancellationToken token)
		{
			throw new System.NotImplementedException ();
		}

		async Task<CompletionContext> IAsyncCompletionItemSource.GetCompletionContextAsync (CompletionTrigger trigger, SnapshotPoint triggerLocation, CancellationToken token)
		{
			return await Task.FromResult (new CompletionContext (ImmutableArray.Create (new CompletionItem ("html", this), new CompletionItem ("head", this), new CompletionItem ("body", this), new CompletionItem ("header", this)), new SnapshotSpan (triggerLocation, 0)));
		}

		async Task<object> IAsyncCompletionItemSource.GetDescriptionAsync (CompletionItem item, CancellationToken token)
		{
			return await Task.FromResult (item.DisplayText);
		}

		ImmutableArray<char> IAsyncCompletionItemSource.GetPotentialCommitCharacters ()
		{
			return ImmutableArray.Create (' ', '>', '=', '\t');
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		async Task IAsyncCompletionItemSource.HandleViewClosedAsync (Text.Editor.ITextView view)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			return;
		}

		bool IAsyncCompletionItemSource.ShouldCommitCompletion (char typeChar, SnapshotPoint location)
		{
			return true;
		}

		bool IAsyncCompletionItemSource.ShouldTriggerCompletion (char typeChar, SnapshotPoint location)
		{
			return true;
		}
	}
}
#endif
