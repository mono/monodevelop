//
// FoldingTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Highlighting;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide.Editor.Extension
{
	class FoldingTextEditorExtension : TextEditorExtension
	{
		CancellationTokenSource src = new CancellationTokenSource ();
		bool isDisposed;

		protected override void Initialize ()
		{
			DocumentContext.DocumentParsed += DocumentContext_DocumentParsed;
		}

		public override void Dispose ()
		{
			if (isDisposed)
				return;
			isDisposed = true;
			CancelDocumentParsedUpdate ();
			DocumentContext.DocumentParsed -= DocumentContext_DocumentParsed;
			base.Dispose ();
		}

		void CancelDocumentParsedUpdate ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
		}

		void DocumentContext_DocumentParsed (object sender, EventArgs e)
		{
			CancelDocumentParsedUpdate ();
			var token = src.Token;
			var caretLocation = Editor.CaretLocation;
			var parsedDocument = DocumentContext.ParsedDocument;
			Task.Run (async () => {
				try {
					if (!isDisposed)
						await UpdateFoldings (Editor, parsedDocument, caretLocation, false, token);
				} catch (OperationCanceledException) {}
			}, token);
		}

		internal static async Task UpdateFoldings (TextEditor Editor, ParsedDocument parsedDocument, DocumentLocation caretLocation, bool firstTime = false, CancellationToken token = default (CancellationToken))
		{
			if (parsedDocument == null || !Editor.Options.ShowFoldMargin || parsedDocument.Flags.HasFlag (ParsedDocumentFlags.SkipFoldings))
				return;
			// don't update parsed documents that contain errors - the foldings from there may be invalid.
			if (await parsedDocument.HasErrorsAsync (token))
				return;

			try {
				var foldSegments = new List<IFoldSegment> ();

				foreach (FoldingRegion region in await parsedDocument.GetFoldingsAsync (token)) {
					if (token.IsCancellationRequested)
						return;
					var type = FoldingType.Unknown;
					bool setFolded = false;
					bool folded = false;
					//decide whether the regions should be folded by default
					switch (region.Type) {
					case FoldType.Member:
						type = FoldingType.TypeMember;
						break;
					case FoldType.Type:
						type = FoldingType.TypeDefinition;
						break;
					case FoldType.UserRegion:
						type = FoldingType.Region;
						setFolded = DefaultSourceEditorOptions.Instance.DefaultRegionsFolding;
						folded = true;
						break;
					case FoldType.Comment:
						type = FoldingType.Comment;
						setFolded = DefaultSourceEditorOptions.Instance.DefaultCommentFolding;
						folded = true;
						break;
					case FoldType.CommentInsideMember:
						type = FoldingType.Comment;
						setFolded = DefaultSourceEditorOptions.Instance.DefaultCommentFolding;
						folded = false;
						break;
					case FoldType.Undefined:
						setFolded = true;
						folded = region.IsFoldedByDefault;
						break;
					}
					var start = Editor.LocationToOffset (region.Region.Begin);
					var end = Editor.LocationToOffset (region.Region.End);
					var marker = Editor.CreateFoldSegment (start, end - start);
					foldSegments.Add (marker);
					marker.CollapsedText = region.Name;
					marker.FoldingType = type;
					//and, if necessary, set its fold state
					if (marker != null && setFolded && firstTime) {
						// only fold on document open, later added folds are NOT folded by default.
						marker.IsCollapsed = folded;
						continue;
					}
					if (marker != null && region.Region.Contains (caretLocation.Line, caretLocation.Column))
						marker.IsCollapsed = false;
				}
				if (firstTime) {
					Editor.SetFoldings (foldSegments);
				} else {
					Application.Invoke (delegate {
						if (!token.IsCancellationRequested)
							Editor.SetFoldings (foldSegments);
					});
				}
			} catch (OperationCanceledException) {
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled exception in ParseInformationUpdaterWorkerThread", ex);
			}
		}

	}
}