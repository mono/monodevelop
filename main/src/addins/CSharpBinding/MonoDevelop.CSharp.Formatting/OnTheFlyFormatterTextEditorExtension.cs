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
			RunFormatter ();
			/*
			if (PropertyService.Get ("OnTheFlyFormatting", true)) {
				var prettyPrinter = CodeFormatterService.GetFormatter (Document.MimeType);
				if (prettyPrinter != null && Project != null && text != null) {
					try {
						var policies = Project.Policies;
						string newText = prettyPrinter.FormatText (policies, Document.Text, insertionOffset, insertionOffset + insertedChars);
						if (!string.IsNullOrEmpty (newText)) {
							int replaceResult = Replace (insertionOffset, insertedChars, newText);
							Caret.Offset = insertionOffset + replaceResult;
						}
					} catch (Exception e) {
						LoggingService.LogError ("Error formatting pasted text", e);
					}
				}
			}*/
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

