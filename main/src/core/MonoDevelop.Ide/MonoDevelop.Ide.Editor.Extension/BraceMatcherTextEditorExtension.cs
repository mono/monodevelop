//
// BraceMatcherTextEditorExtension.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using Mono.Addins;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide.Extensions;

namespace MonoDevelop.Ide.Editor.Extension
{
	sealed class BraceMatcherTextEditorExtension : TextEditorExtension
	{
		CancellationTokenSource src = new CancellationTokenSource();
		static List<AbstractBraceMatcher> braceMatcher = new List<AbstractBraceMatcher> ();

		BraceMatchingResult? currentResult;

		bool isSubscribed;
		static BraceMatcherTextEditorExtension()
		{
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Ide/BraceMatcher", delegate(object sender, ExtensionNodeEventArgs args) {
				var node = (MimeTypeExtensionNode)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					var matcher = (AbstractBraceMatcher)node.CreateInstance ();
					matcher.MimeType = node.MimeType;
					braceMatcher.Add (matcher);
					break;
				case ExtensionChange.Remove:
					var toRemove = braceMatcher.FirstOrDefault (m => m.MimeType == node.MimeType);
					if (toRemove != null)
						braceMatcher.Remove (toRemove);
					break;
				}
			});
			braceMatcher.Add (new DefaultBraceMatcher());
		}

		AbstractBraceMatcher GetBraceMatcher ()
		{
			return braceMatcher.First (m => m.CanHandle (Editor));
		}

		protected override void Initialize ()
		{
			if ((Editor.TextEditorType & TextEditorType.Invisible) != 0)
				return;
			DefaultSourceEditorOptions.Instance.highlightMatchingBracket.Changed += HighlightMatchingBracket_Changed;
			HighlightMatchingBracket_Changed (this, EventArgs.Empty);
		}

		void HighlightMatchingBracket_Changed (object sender, EventArgs e)
		{
			if (DefaultSourceEditorOptions.Instance.HighlightMatchingBracket) {
				if (isSubscribed)
					return;
				Editor.CaretPositionChanged += Editor_CaretPositionChanged;
				DocumentContext.DocumentParsed += HandleDocumentParsed;
				isSubscribed = true;
				Editor_CaretPositionChanged (null, null);
			} else {
				if (!isSubscribed)
					return;
				Editor.CaretPositionChanged -= Editor_CaretPositionChanged;
				DocumentContext.DocumentParsed -= HandleDocumentParsed;
				Editor.UpdateBraceMatchingResult (null);
				isSubscribed = false;
			}
		}

		public override void Dispose ()
		{
			src.Cancel ();
			DefaultSourceEditorOptions.Instance.highlightMatchingBracket.Changed -= HighlightMatchingBracket_Changed;
			if (isSubscribed) {
				Editor.CaretPositionChanged -= Editor_CaretPositionChanged;
				DocumentContext.DocumentParsed -= HandleDocumentParsed;
				isSubscribed = false;
			}

			base.Dispose ();
		}

		void HandleDocumentParsed (object sender, EventArgs e)
		{
			Editor_CaretPositionChanged (sender, e);
		}


		[CommandHandler (MonoDevelop.Ide.Commands.TextEditorCommands.GotoMatchingBrace)]
		internal void OnGotoMatchingBrace ()
		{
			if (currentResult != null && currentResult.HasValue) {
				Editor.CaretOffset = currentResult.Value.IsCaretInLeft ? currentResult.Value.RightSegment.Offset : currentResult.Value.LeftSegment.Offset;
			}
		}

		void Editor_CaretPositionChanged (object sender, EventArgs e)
		{
			Editor.UpdateBraceMatchingResult (null);
			currentResult = null;
			src.Cancel ();
			src = new CancellationTokenSource ();
			var token = src.Token;
			var matcher = GetBraceMatcher ();
			if (matcher == null)
				return;
			var caretOffset = Editor.CaretOffset;
			var ctx = DocumentContext;
			var snapshot = Editor.CreateDocumentSnapshot ();
			Task.Run (async delegate() {
				BraceMatchingResult? result = null;
				try {
					if (caretOffset > 0)
						result = await matcher.GetMatchingBracesAsync (snapshot, ctx, caretOffset - 1, token).ConfigureAwait (false);
					if (result == null)
						result = await matcher.GetMatchingBracesAsync (snapshot, ctx, caretOffset, token).ConfigureAwait (false);
					if (result == null)
						return;
					if (result.HasValue) {
						if (result.Value.LeftSegment.Offset < 0 || 
						    result.Value.LeftSegment.EndOffset > snapshot.Length) {
							LoggingService.LogError ("bracket matcher left segment invalid:" + result.Value.LeftSegment);
							return;
						}
						if (result.Value.RightSegment.Offset < 0 ||
						    result.Value.RightSegment.EndOffset > snapshot.Length) {
							LoggingService.LogError ("bracket matcher right segment invalid:" + result.Value.RightSegment);
							return;
						}
					}
				} catch (OperationCanceledException) {
					return;
				} catch (AggregateException ae) {
					ae.Flatten ().Handle (ex => ex is OperationCanceledException);
					return;
				}
				if (token.IsCancellationRequested)
					return;
				Application.Invoke ((o, args) => {
					if (token.IsCancellationRequested)
						return;
					Editor.UpdateBraceMatchingResult (result);
					currentResult = result;
				});
			});
		}
	}
}