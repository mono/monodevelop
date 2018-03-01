//
// TextMateCompletionTextEditorExtension.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2016 Microsoft Corporation
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
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.CodeTemplates;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.TextMate
{
	class TextMateCompletionTextEditorExtension : CompletionTextEditorExtension
	{
		bool inactive;

		public override bool CanRunCompletionCommand ()
		{
			return true;
		}

		protected override void Initialize ()
		{
			inactive = false;

			// check if there is any other active completion extension 
			// -> if so that one is disabled automatically
			var ext = Editor.TextEditorExtensionChain;
			while (ext != null) {
				var completionTextEditorExtension = ext as CompletionTextEditorExtension;
				if (completionTextEditorExtension != null && ext != this) {
					if (completionTextEditorExtension.IsValidInContext (DocumentContext))
						inactive = true;
				}
				ext = ext.Next;
			}
			DocumentContext.DocumentParsed += DocumentContext_DocumentParsed;
			base.Initialize ();
		}

		public override void Dispose ()
		{
			DocumentContext.DocumentParsed -= DocumentContext_DocumentParsed;
			base.Dispose ();
		}

		void DocumentContext_DocumentParsed (object sender, EventArgs e)
		{
			if (!inactive && DocumentContext.ParsedDocument != null)
				inactive = (DocumentContext.ParsedDocument.Flags & ParsedDocumentFlags.HasCustomCompletionExtension) == ParsedDocumentFlags.HasCustomCompletionExtension;
		}

		internal protected override bool IsActiveExtension ()
		{
			return !inactive;
		}

		static Task<IEnumerable<string>> GetAllWords(ITextSource source, int offset)
		{
			return Task.Run (delegate {
				var result = new HashSet<string> ();
				var sb = StringBuilderCache.Allocate ();
				int i = 0;
				while (i < source.Length) {
					char ch = source[i];
					if (char.IsLetter (ch) || ch == '_') {
						sb.Append (ch);
						i++;
						while (i < source.Length) {
							ch = source[i];
							if (char.IsLetterOrDigit (ch) || ch == '_') {
								sb.Append (ch);
							} else {
								break;
							}
							i++;
						}
						if (sb.Length > 1 && offset != i) {
							result.Add (sb.ToString ());
						}
						sb.Length = 0;
						continue;
					}

					i++;
				}
				StringBuilderCache.Free (sb);
				return (IEnumerable<string>)result;
			});
		}

		public override async Task<ICompletionDataList> HandleCodeCompletionAsync (CodeCompletionContext completionContext, CompletionTriggerInfo triggerInfo, CancellationToken token)
		{
			if (inactive)
				return await base.HandleCodeCompletionAsync (completionContext, triggerInfo, token);
			if (Editor.MimeType == "text/plain")
				return null;
			if (!IdeApp.Preferences.EnableAutoCodeCompletion)
					return null;
			if (triggerInfo.CompletionTriggerReason != CompletionTriggerReason.CharTyped)
				return null;
			char completionChar = triggerInfo.TriggerCharacter.Value;
			int triggerWordLength = 0;
			if (char.IsLetterOrDigit (completionChar) || completionChar == '_') {
				if (completionContext.TriggerOffset > 1 && char.IsLetterOrDigit (Editor.GetCharAt (completionContext.TriggerOffset - 2)))
					return null;
				triggerWordLength = 1;
			} else {
				return null;
			}

			var result = new CompletionDataList ();
			result.TriggerWordLength = triggerWordLength;
			result.AutoSelect = false;
			result.AutoCompleteEmptyMatch = false;
			result.AutoCompleteUniqueMatch = false;
			result.AutoCompleteEmptyMatchOnCurlyBrace = false;

			foreach (var ct in await CodeTemplateService.GetCodeTemplatesAsync (Editor, token)) {
				if (string.IsNullOrEmpty (ct.Shortcut) || ct.CodeTemplateContext != CodeTemplateContext.Standard)
					continue;
				result.Add (new CodeTemplateCompletionData (this, ct));
			}

			foreach (var word in await GetAllWords (Editor.CreateSnapshot (), Editor.CaretOffset)) {
				result.Add (new CompletionData (word));	
			}

			if (result.Count == 0)
				return null;
			return result;
		}
	}
}
