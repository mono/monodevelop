//
// VsCompletionTextEditorExtension.cs
//
// Author:
//       David Karlaš <david.karlas@microsoft.com>
//
// Copyright (c) 2018 Microsoft Corp
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
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Utilities;
using Microsoft.VisualStudio.Utilities;
using MonoDevelop.Ide.Composition;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Ide.Editor.Extension
{
	public class VsCompletionTextEditorExtension : TextEditorExtension
	{
		private ITextView view;
		IAsyncCompletionBroker host;

		protected override void Initialize()
		{
			base.Initialize();
			host = CompositionManager.GetExportedValue<IAsyncCompletionBroker> ();
			view = Editor.TextView;
		}

		private IEnumerable<Lazy<IAsyncCompletionItemSource, IOrderableContentTypeMetadata>> completionItemSources;

		private ImmutableDictionary<IContentType, bool> _cachedCompletionItemSources = ImmutableDictionary<IContentType, bool>.Empty;

		private bool AnyCompletionItemSources (IContentType contentType)
		{
			if (_cachedCompletionItemSources.TryGetValue (contentType, out var cachedSources)) {
				return cachedSources;
			}
			if (completionItemSources == null)
				completionItemSources = CompositionManager.Instance.ExportProvider.GetExports<IAsyncCompletionItemSource, IOrderableContentTypeMetadata> ();
			bool result = false;
			foreach (var item in completionItemSources) {
				if (item.Metadata.ContentTypes.Any (n => contentType.IsOfType (n))) {
					result = true;
					break;
				}
			}
			_cachedCompletionItemSources = _cachedCompletionItemSources.Add (contentType, result);
			return result;
		}

		public override bool IsValidInContext (DocumentContext context)
		{
			var filePathRegistryService = CompositionManager.GetExportedValue<IFilePathRegistryService> ();
			var contentType = filePathRegistryService.GetContentTypeForPath (context.Name);
			return AnyCompletionItemSources (contentType);
		}

		public override bool KeyPress (KeyDescriptor descriptor)
		{
			bool ExecThisAndIfFailedExecNext (Func<bool> func)
			{
				var funcResult = func ();
				if (funcResult) {
					funcResult = base.KeyPress (descriptor);
				}
				return funcResult;
			}
			bool result = true;
			try {
				switch (descriptor.SpecialKey) {
				case SpecialKey.BackSpace:
					// TODO: get deleted character
					result = base.KeyPress (descriptor);
					result &= OpenCompletionFromDeletion ();
					break;
				case SpecialKey.Escape:
					result = ExecThisAndIfFailedExecNext (Dismiss);
					break;
				//case (uint)VSConstants.VSStd2KCmdID.COMPLETEWORD:
				//result = OpenCompletionFromCommand ();
				//break;
				case SpecialKey.Delete:
					result = ExecThisAndIfFailedExecNext (Dismiss);
					break;
				//case (uint)VSConstants.VSStd2KCmdID.DELETEWORDLEFT:
				//	result = ExecThisAndIfFailedExecNext (Dismiss);
				//	break;
				//case (uint)VSConstants.VSStd2KCmdID.DELETEWORDRIGHT:
				//result = ExecThisAndIfFailedExecNext (Dismiss);
				//break;
				case SpecialKey.Return:
					result = ExecThisAndIfFailedExecNext (Commit);
					break;
				//case (uint)VSConstants.VSStd2KCmdID.SHOWMEMBERLIST:
				//result = OpenCompletionFromCommand ();
				//break;
				case SpecialKey.Tab:
					result = ExecThisAndIfFailedExecNext (Commit);
					break;
				case SpecialKey.Down:
					result = ExecThisAndIfFailedExecNext (ExecuteDown);
					break;
				case SpecialKey.PageDown:
					result = ExecThisAndIfFailedExecNext (ExecutePageDown);
					break;
				case SpecialKey.Up:
					result = ExecThisAndIfFailedExecNext (ExecuteUp);
					break;
				case SpecialKey.PageUp:
					result = ExecThisAndIfFailedExecNext (ExecutePageUp);
					break;
				default:
					if (descriptor.KeyChar != '\0') {
						// Before we insert and edit, see if we should commit now.
						// Some language services wish to process this type char themselves
						// for their undo requirements

						var trackedEdit = view.Caret.Position.BufferPosition.Snapshot.CreateTrackingSpan (new Span (view.Caret.Position.BufferPosition.Position, 0), SpanTrackingMode.EdgeInclusive);
						result = base.KeyPress (descriptor);
						result &= ReactToEdit (trackedEdit);
						break;
					} else {
						return base.KeyPress (descriptor);
					}
				}
			} catch (Exception ex) {
				Debug.WriteLine (ex);
				Debugger.Break ();
			}
			return result;
		}

		private bool ExecutePageUp ()
		{
			if (!host.IsCompletionActive (view))
				return true;

			host.SelectPageUp (view);
			return false;
		}

		private bool ExecutePageDown ()
		{
			if (!host.IsCompletionActive (view))
				return true;

			host.SelectPageDown (view);
			return false;
		}

		private bool ExecuteDown ()
		{
			if (!host.IsCompletionActive (view))
				return true;

			host.SelectDown (view);
			return false;
		}

		private bool ExecuteUp ()
		{
			if (!host.IsCompletionActive (view))
				return true;

			host.SelectUp (view);
			return false;
		}

		private bool ReactToEdit (ITrackingSpan trackedEdit)
		{
			var currentSnapshot = view.Caret.Position.BufferPosition.Snapshot;
			var edit = trackedEdit.GetText (currentSnapshot); // TODO: just get the difference between current snapshot and previous snapshot
			var needToCommit = host.ShouldCommitCompletion (view, edit, view.Caret.Position.BufferPosition);
			if (needToCommit) {
				host.Commit (view, edit);
				return false;
			} else {
				if (view.Caret.Position.VirtualBufferPosition.IsInVirtualSpace) {
					// TODO: Convert any virtual whitespace to real whitespace by doing an empty edit at the caret position.
				}

				var trigger = new CompletionTrigger (CompletionTriggerReason.Insertion, edit);
				var location = view.Caret.Position.BufferPosition;
				if (host.IsCompletionActive (view)) {
					host.OpenOrUpdate (view, trackedEdit, trigger, location);
					return false;
				} else {
					var triggered = host.ShouldTriggerCompletion (view, edit, location);
					if (triggered) {
						host.TriggerCompletion (view, location);
						host.OpenOrUpdate (view, trackedEdit, trigger, location);
						return false;
					} else {
						return true;
					}
				}
			}
		}

		private bool OpenCompletionFromCommand ()
		{
			var trigger = new CompletionTrigger (CompletionTriggerReason.Command);
			var location = view.Caret.Position.BufferPosition;
			var trackingAnchor = view.TextSnapshot.CreateTrackingSpan (new Span (view.Caret.Position.BufferPosition.Position, 0), SpanTrackingMode.EdgeInclusive);

			host.TriggerCompletion (view, location);
			host.OpenOrUpdate (view, trackingAnchor, trigger, location);
			return false;
		}
		private bool OpenCompletionFromDeletion ()
		{
			if (!host.IsCompletionActive (view)) {
				// don't start new completion from deleting characters
				return false;
			}

			var trigger = new CompletionTrigger (CompletionTriggerReason.Deletion);
			var location = view.Caret.Position.BufferPosition;
			var trackingAnchor = view.TextSnapshot.CreateTrackingSpan (new Span (view.Caret.Position.BufferPosition.Position, 0), SpanTrackingMode.EdgeInclusive);

			host.TriggerCompletion (view, location);
			host.OpenOrUpdate (view, trackingAnchor, trigger, location);
			return false;
		}

		private bool Commit ()
		{
			if (!host.IsCompletionActive (view))
				return true;

			host.Commit (view);
			return false;
		}

		private bool Dismiss ()
		{
			if (!host.IsCompletionActive (view))
				return true;

			host.Dismiss (view);
			return false;
		}
	}
}
