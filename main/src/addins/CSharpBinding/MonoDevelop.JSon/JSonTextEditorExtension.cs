//
// JSonTextEditorExtension.cs
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide.Editor.Extension;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.JSon
{
	class JSonTextEditorExtension : TextEditorExtension
	{
		CacheIndentEngine stateTracker;
		bool jsonExtensionInstalled;

		protected override void Initialize ()
		{
			base.Initialize ();

			// This extension needs to be turned off if the webtooling addin json extension is present.
			//   That addin defines a "text/x-json" mimeType that has multiple levels of inheritance.
			var mimeChain = DesktopService.GetMimeTypeInheritanceChain("text/x-json").ToList();
			jsonExtensionInstalled = (mimeChain.Count > 2);

			if (!jsonExtensionInstalled)
			{
				IStateMachineIndentEngine indentEngine;
				indentEngine = new JSonIndentEngine(Editor, DocumentContext);
				stateTracker = new CacheIndentEngine(indentEngine);
				Editor.IndentationTracker = new JSonIndentationTracker(Editor, stateTracker);
			}
		}
		public override bool KeyPress (KeyDescriptor descriptor)
		{
			var result = base.KeyPress (descriptor);

			if (!jsonExtensionInstalled)
			{
				if (descriptor.SpecialKey == SpecialKey.Return) {
					if (Editor.Options.IndentStyle == MonoDevelop.Ide.Editor.IndentStyle.Virtual) {
						if (Editor.GetLine (Editor.CaretLine).Length == 0)
							Editor.CaretColumn = Editor.GetVirtualIndentationColumn (Editor.CaretLine);
					} else {
						DoReSmartIndent ();
					}
				}
			}

			return result;
		}

		void DoReSmartIndent ()
		{
			DoReSmartIndent (Editor.CaretOffset);
		}

		void DoReSmartIndent (int cursor)
		{
			SafeUpdateIndentEngine (cursor);
			if (stateTracker.LineBeganInsideVerbatimString || stateTracker.LineBeganInsideMultiLineComment)
				return;
			var line = Editor.GetLineByOffset (cursor);

			// Get context to the end of the line w/o changing the main engine's state
			var curTracker = stateTracker.Clone ();
			try {
				for (int max = cursor; max < line.EndOffset; max++) {
					curTracker.Push (Editor.GetCharAt (max));
				}
			} catch (Exception e) {
				LoggingService.LogError ("Exception during indentation", e);
			}

			int pos = line.Offset;
			string curIndent = line.GetIndentation (Editor);
			int nlwsp = curIndent.Length;
			int offset = cursor > pos + nlwsp ? cursor - (pos + nlwsp) : 0;
			if (!stateTracker.LineBeganInsideMultiLineComment || (nlwsp < line.LengthIncludingDelimiter && Editor.GetCharAt (line.Offset + nlwsp) == '*')) {
				// Possibly replace the indent
				string newIndent = curTracker.ThisLineIndent;
				int newIndentLength = newIndent.Length;
				if (newIndent != curIndent) {
					if (CompletionWindowManager.IsVisible) {
						if (pos < CompletionWindowManager.CodeCompletionContext.TriggerOffset)
							CompletionWindowManager.CodeCompletionContext.TriggerOffset -= nlwsp;
					}

					Editor.ReplaceText (pos, nlwsp, newIndent);
					newIndentLength = newIndent.Length;
					CompletionWindowManager.HideWindow ();
				}
				pos += newIndentLength;
			} else {
				pos += curIndent.Length;
			}

			pos += offset;

			Editor.FixVirtualIndentation ();
		}

		internal void SafeUpdateIndentEngine (int offset)
		{
			try {
				stateTracker.Update (Editor, offset);
			} catch (Exception e) {
				LoggingService.LogError ("Error while updating the indentation engine", e);
			}
		}

	}
}