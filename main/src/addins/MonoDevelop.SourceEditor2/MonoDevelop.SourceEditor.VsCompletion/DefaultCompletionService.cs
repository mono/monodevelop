#if DEBUG_COMPLETION
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Core.Imaging;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion.Data;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
	[Export (typeof (IAsyncCompletionSourceProvider))]
	[Name ("Debug completion item source")]
	[Order (After = "default")]
	[ContentType ("any")]
	class DebugCompletionItemSourceProvider : IAsyncCompletionSourceProvider
	{
		DebugCompletionItemSource _instance;

		IAsyncCompletionSource IAsyncCompletionSourceProvider.GetOrCreate (ITextView textView)
		{
			if (_instance == null)
				_instance = new DebugCompletionItemSource ();
			return _instance;
		}
	}

	class DebugCompletionItemSource : IAsyncCompletionSource
	{
		private static readonly ImageElement Icon1 = new ImageElement (new ImageId (new Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 666), "Icon description");
		private static readonly CompletionFilter Filter1 = new CompletionFilter ("Diagnostic", "d", Icon1);
		private static readonly ImageElement Icon2 = new ImageElement (new ImageId (new Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 2852), "Icon description");
		private static readonly CompletionFilter Filter2 = new CompletionFilter ("Snippets", "s", Icon2);
		private static readonly ImageElement Icon3 = new ImageElement (new ImageId (new Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 473), "Icon description");
		private static readonly CompletionFilter Filter3 = new CompletionFilter ("Class", "c", Icon3);
		private static readonly ImmutableArray<CompletionFilter> FilterCollection1 = ImmutableArray.Create (Filter1);
		private static readonly ImmutableArray<CompletionFilter> FilterCollection2 = ImmutableArray.Create (Filter2);
		private static readonly ImmutableArray<CompletionFilter> FilterCollection3 = ImmutableArray.Create (Filter3);
		private static readonly ImmutableArray<char> commitCharacters = ImmutableArray.Create (' ', ';', '\t', '.', '<', '(', '[');

		Task<CompletionContext> IAsyncCompletionSource.GetCompletionContextAsync (InitialTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
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
			return Task.FromResult (new CompletionContext (
				ImmutableArray.Create (
					new CompletionItem ("SampleItem<>", this, Icon3, FilterCollection3, string.Empty, "SampleItem", "SampleItem<>", "SampleItem", ImmutableArray<ImageElement>.Empty),
					new CompletionItem ("AnotherItem🐱‍👤", this, Icon3, FilterCollection3, string.Empty, "AnotherItem", "AnotherItem", "AnotherItem", ImmutableArray.Create (Icon3)),
					new CompletionItem ("Sampling", this, Icon1, FilterCollection1),
					new CompletionItem ("Sampler", this, Icon1, FilterCollection1),
					new CompletionItem ("Sapling", this, Icon2, FilterCollection2, "Sapling is a young tree"),
					new CompletionItem ("OverSampling", this, Icon1, FilterCollection1, "overload"),
					new CompletionItem ("AnotherSample", this, Icon2, FilterCollection2),
					new CompletionItem ("AnotherSampling", this, Icon2, FilterCollection2),
					new CompletionItem ("Simple", this, Icon3, FilterCollection3, "KISS"),
					new CompletionItem ("Simpler", this, Icon3, FilterCollection3, "KISS")
				)));
		}

		Task<object> IAsyncCompletionSource.GetDescriptionAsync (CompletionItem item, CancellationToken token)
		{
			return Task.FromResult<object> ("This is a tooltip for " + item.DisplayText);
		}

		bool IAsyncCompletionSource.TryGetApplicableToSpan (char typedChar, SnapshotPoint triggerLocation, out SnapshotSpan applicableToSpan, CancellationToken token)
		{
			applicableToSpan = new SnapshotSpan (triggerLocation, 0);
			return true;
		}
	}

	[Export (typeof (IAsyncCompletionSourceProvider))]
	[Name ("Debug HTML completion item source")]
	[Order (After = "default")]
	[ContentType ("RazorCSharp")]
	class DebugHtmlCompletionItemSourceProvider : IAsyncCompletionSourceProvider
	{
		DebugHtmlCompletionItemSource _instance;

		IAsyncCompletionSource IAsyncCompletionSourceProvider.GetOrCreate (ITextView textView)
		{
			if (_instance == null)
				_instance = new DebugHtmlCompletionItemSource ();
			return _instance;
		}
	}

	class DebugHtmlCompletionItemSource : IAsyncCompletionSource
	{
		public Task<CompletionContext> GetCompletionContextAsync (InitialTrigger trigger, SnapshotPoint triggerLocation, SnapshotSpan applicableToSpan, CancellationToken token)
		{
			var items = ImmutableArray.Create (
				new CompletionItem ("html", this),
				new CompletionItem ("head", this),
				new CompletionItem ("body", this),
				new CompletionItem ("header", this));
			return Task.FromResult (new CompletionContext (items));
		}

		public Task<object> GetDescriptionAsync (CompletionItem item, CancellationToken token)
		{
			return Task.FromResult<object> ("Description");
		}

		public bool TryGetApplicableToSpan (char typedChar, SnapshotPoint triggerLocation, out SnapshotSpan applicableToSpan, CancellationToken token)
		{
			applicableToSpan = new SnapshotSpan (triggerLocation, 0);
			return true;
		}
	}
}
#endif
