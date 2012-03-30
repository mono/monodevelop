// 
// OnTheFlyFormatterTextEditorExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Core;
using Mono.TextEditor;
using ICSharpCode.NRefactory;

namespace MonoDevelop.CSharp.Formatting
{
	public class OnTheFlyFormatterTextEditorExtension : TextEditorExtension
	{
		TextEditorData textEditorData {
			get {
				return Document.Editor;
			}
		}
		
		public static bool OnTheFlyFormatting {
			get {
				return PropertyService.Get ("OnTheFlyFormatting", true);
			}
			set {
				PropertyService.Set ("OnTheFlyFormatting", value);
			}
		}
		
		void RunFormatter ()
		{
			if (OnTheFlyFormatting && textEditorData != null && !(textEditorData.CurrentMode is TextLinkEditMode) && !(textEditorData.CurrentMode is InsertionCursorEditMode)) {
				OnTheFlyFormatter.Format (Document, textEditorData.Caret.Location);
			}
		}

		public override void Initialize ()
		{
			base.Initialize ();
			Document.Editor.Paste += HandleTextPaste;
		}

		void HandleTextPaste (int insertionOffset, string text, int insertedChars)
		{
			// Trim blank spaces on text paste, see: Bug 511 - Trim blank spaces when copy-pasting
			if (OnTheFlyFormatting) {
				int i = insertionOffset + insertedChars;
				bool foundNonWsFollowUp = false;

				var line = Document.Editor.GetLineByOffset (i);
				if (line != null) {
					for (int j = 0; j < line.Offset + line.EditableLength; j++) {
						var ch = Document.Editor.GetCharAt (j);
						if (ch != ' ' && ch != '\t') {
							foundNonWsFollowUp = true;
							break;
						}
					}
				}

				if (!foundNonWsFollowUp) {
					while (i > insertionOffset) {
						char ch = Document.Editor.GetCharAt (i - 1);
						if (ch != ' ' && ch != '\t') 
							break;
						i--;
					}
					int delta = insertionOffset + insertedChars - i;
					if (delta > 0) {
						Editor.Caret.Offset -= delta;
						Editor.Remove (insertionOffset + insertedChars - delta, delta);
					}
				}
			}

			RunFormatter ();
		}

		public override void Dispose ()
		{
			base.Dispose ();
		}

		public override bool KeyPress (Gdk.Key key, char keyChar, Gdk.ModifierType modifier)
		{
			bool runBefore = key == Gdk.Key.Return || key == Gdk.Key.KP_Enter;
			if (runBefore)
				RunFormatter ();
			var result = base.KeyPress (key, keyChar, modifier);

			bool runAfter = keyChar == '}' || keyChar == ';';
			if (runAfter)
				RunFormatter ();
			return result;
		}
	}
}

