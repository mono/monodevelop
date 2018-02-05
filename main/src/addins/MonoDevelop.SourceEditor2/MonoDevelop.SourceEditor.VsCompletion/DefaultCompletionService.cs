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


namespace Microsoft.VisualStudio.Language.Intellisense.Implementation
{
	[Export (typeof (IAsyncCompletionItemSource))]
	[Name ("Debug completion item source")]
	[Order (After = "default")]
	[ContentType ("any")]
	public class DebugCompletionItemSource : IAsyncCompletionItemSource
	{
		private static readonly ImageId Icon1 = new ImageId (new Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 666);
		private static readonly CompletionFilter Filter1 = new CompletionFilter ("Diagnostic", "d", Icon1);
		private static readonly ImageId Icon2 = new ImageId (new Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 2852);
		private static readonly CompletionFilter Filter2 = new CompletionFilter ("Snippets", "s", Icon2);
		private static readonly ImageId Icon3 = new ImageId (new Guid ("{ae27a6b0-e345-4288-96df-5eaf394ee369}"), 473);
		private static readonly CompletionFilter Filter3 = new CompletionFilter ("Class", "c", Icon3);
		private static readonly ImmutableArray<CompletionFilter> FilterCollection1 = ImmutableArray.Create (Filter1);
		private static readonly ImmutableArray<CompletionFilter> FilterCollection2 = ImmutableArray.Create (Filter2);
		private static readonly ImmutableArray<CompletionFilter> FilterCollection3 = ImmutableArray.Create (Filter3);
		private static readonly ImmutableArray<string> commitCharacters = ImmutableArray.Create (" ", ";", "\t", ".", "<", "(", "[");

		void IAsyncCompletionItemSource.CustomCommit (ITextView view, ITextBuffer buffer, CompletionItem item, ITrackingSpan applicableSpan, string commitCharacter)
		{
			throw new System.NotImplementedException ();
		}

		async Task<CompletionContext> IAsyncCompletionItemSource.GetCompletionContextAsync (CompletionTrigger trigger, SnapshotPoint triggerLocation)
		{
			var charBeforeCaret = triggerLocation.Subtract (1).GetChar ();
			SnapshotSpan applicableSpan;
			if (commitCharacters.Contains (charBeforeCaret.ToString ())) {
				// skip this character. the applicable span starts later
				applicableSpan = new SnapshotSpan (triggerLocation, 0);
			} else {
				// include this character. the applicable span starts here
				applicableSpan = new SnapshotSpan (triggerLocation - 1, 1);
			}
			return await Task.FromResult (new CompletionContext (
				ImmutableArray.Create (
					new CompletionItem ("SampleItem<>", "SampleItem", "SampleItem<>", "SampleItem", this, FilterCollection1, false, Icon1),
					new CompletionItem ("AnotherItem🐱‍👤", "AnotherItem", "AnotherItem", "AnotherItem", this, FilterCollection1, false, Icon1),
					new CompletionItem ("Aaaaa", "Aaaaa", "Aaaaa", "Aaaaa", this, FilterCollection2, false, Icon2),
					new CompletionItem ("Bbbbb", "Bbbbb", "Bbbbb", "Bbbbb", this, FilterCollection2, false, Icon2),
					new CompletionItem ("Ccccc", "Ccccc", "Ccccc", "Ccccc", this, FilterCollection2, false, Icon2),
					new CompletionItem ("Ddddd", "Ddddd", "Ddddd", "Ddddd", this, FilterCollection2, false, Icon2),
					new CompletionItem ("Eeee", "Eeee", "Eeee", "Eeee", this, FilterCollection2, false, Icon2),
					new CompletionItem ("Ffffff", "Ffffff", "Ffffff", "Ffffff", this, FilterCollection2, false, Icon2),
					new CompletionItem ("Ggggggg", "Ggggggg", "Ggggggg", "Ggggggg", this, FilterCollection2, false, Icon2),
					new CompletionItem ("Hhhhh", "Hhhhh", "Hhhhh", "Hhhhh", this, FilterCollection2, false, Icon2),
					new CompletionItem ("Iiiii", "Iiiii", "Iiiii", "Iiiii", this, FilterCollection2, false, Icon2),
					new CompletionItem ("Jjjjj", "Jjjjj", "Jjjjj", "Jjjjj", this, FilterCollection3, false, Icon3),
					new CompletionItem ("kkkkk", "kkkkk", "kkkkk", "kkkkk", this, FilterCollection3, false, Icon3),
					new CompletionItem ("llllol", "llllol", "llllol", "llllol", this, FilterCollection3, false, Icon3),
					new CompletionItem ("mmmmm", "mmmmm", "mmmmm", "mmmmm", this, FilterCollection3, false, Icon3),
					new CompletionItem ("nnNnnn", "nnNnnn", "nnNnnn", "nnNnnn", this, FilterCollection3, false, Icon3),
					new CompletionItem ("oOoOOO", "oOoOOO", "oOoOOO", "oOoOOO", this, FilterCollection3, false, Icon3)
				), applicableSpan));//, true, true, "Suggestion mode description!"));
		}

		async Task<object> IAsyncCompletionItemSource.GetDescriptionAsync (CompletionItem item)
		{
			return await Task.FromResult ("This is a tooltip for " + item.DisplayText);
		}

		ImmutableArray<string> IAsyncCompletionItemSource.GetPotentialCommitCharacters () => commitCharacters;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		async Task IAsyncCompletionItemSource.HandleViewClosedAsync (ITextView view)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			return;
		}

		bool IAsyncCompletionItemSource.ShouldCommitCompletion (string typedChar, SnapshotPoint location)
		{
			return true;
		}

		bool IAsyncCompletionItemSource.ShouldTriggerCompletion (string typedChar, SnapshotPoint location)
		{
			return true;
		}
	}

	[Export (typeof (IAsyncCompletionItemSource))]
	[Name ("Debug HTML completion item source")]
	[Order (After = "default")]
	[ContentType ("RazorCSharp")]
	public class DebugHtmlCompletionItemSource : IAsyncCompletionItemSource
	{
		void IAsyncCompletionItemSource.CustomCommit (ITextView view, ITextBuffer buffer, CompletionItem item, ITrackingSpan applicableSpan, string commitCharacter)
		{
			throw new System.NotImplementedException ();
		}

		async Task<CompletionContext> IAsyncCompletionItemSource.GetCompletionContextAsync (CompletionTrigger trigger, SnapshotPoint triggerLocation)
		{
			return await Task.FromResult (new CompletionContext (ImmutableArray.Create (new CompletionItem ("html", this), new CompletionItem ("head", this), new CompletionItem ("body", this), new CompletionItem ("header", this)), new SnapshotSpan (triggerLocation, 0)));
		}

		async Task<object> IAsyncCompletionItemSource.GetDescriptionAsync (CompletionItem item)
		{
			return await Task.FromResult (item.DisplayText);
		}

		ImmutableArray<string> IAsyncCompletionItemSource.GetPotentialCommitCharacters ()
		{
			return ImmutableArray.Create (" ", ">", "=", "\t");
		}

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
		async Task IAsyncCompletionItemSource.HandleViewClosedAsync (ITextView view)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
		{
			return;
		}

		bool IAsyncCompletionItemSource.ShouldCommitCompletion (string typedChar, SnapshotPoint location)
		{
			return true;
		}

		bool IAsyncCompletionItemSource.ShouldTriggerCompletion (string typedChar, SnapshotPoint location)
		{
			return true;
		}
	}
}
#endif
