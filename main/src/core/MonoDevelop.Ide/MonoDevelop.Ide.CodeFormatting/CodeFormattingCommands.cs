// 
// CodeFormattingCommands.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Projects.Policies;
using System.Linq;

namespace MonoDevelop.Ide.CodeFormatting
{
	public enum CodeFormattingCommands {
		FormatBuffer
	}

	[Obsolete ("Use the Microsoft.VisualStudio.Text APIs")]
	public class FormatBufferHandler : CommandHandler
	{
		internal static CodeFormatter GetFormatter (out Document doc)
		{
			doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return null;
			var editor = doc.Editor;
			if (editor == null)
				return null;
			return CodeFormatterService.GetFormatter (editor.MimeType);
		}

		protected override void Update (CommandInfo info)
		{
			Document doc;
			var formatter = GetFormatter (out doc);
			info.Enabled = formatter != null;

			if (formatter != null && formatter.SupportsPartialDocumentFormatting && doc.Editor.IsSomethingSelected) {
				info.Text = GettextCatalog.GetString ("_Format Selection");
			}
		}
		
		protected override void Run (object tool)
		{
			Document doc;
			var formatter = GetFormatter (out doc);
			if (formatter == null)
				return;
			var editor = doc.Editor;

			if (editor.IsSomethingSelected && formatter.SupportsPartialDocumentFormatting) {
				ISegment selection = editor.SelectionRange;

				using (var undo = editor.OpenUndoGroup ()) {
					var version = editor.Version;

					if (formatter.SupportsOnTheFlyFormatting) {
						formatter.OnTheFlyFormat (doc.Editor, doc.DocumentContext, selection);
					} else {
						var pol = (doc.Owner as IPolicyProvider)?.Policies;
						try {
							var editorText = editor.Text;
							string text = formatter.FormatText (pol, editorText, selection);
							if (text != null && editorText.Substring (selection.Offset, selection.Length) != text) {
								editor.ReplaceText (selection.Offset, selection.Length, text);
							}
						} catch (Exception e) {
							LoggingService.LogError ("Error during format.", e); 
						}
					}

					int newOffset = version.MoveOffsetTo (editor.Version, selection.Offset);
					int newEndOffset = version.MoveOffsetTo (editor.Version, selection.EndOffset);
					editor.SetSelection (newOffset, newEndOffset);
				}
				return;
			}

			if (formatter.SupportsOnTheFlyFormatting) {
				using (var undo = doc.Editor.OpenUndoGroup ()) {
					formatter.OnTheFlyFormat (doc.Editor, doc.DocumentContext, new TextSegment (0, doc.Editor.Length));
				}
			} else {
				var text = editor.Text;
				var oldOffsetWithoutWhitespaces = editor.GetTextBetween (0, editor.CaretOffset).Count (c => !char.IsWhiteSpace (c));
				var policies = (doc.Owner as IPolicyProvider)?.Policies ?? PolicyService.DefaultPolicies;
				string formattedText = formatter.FormatText (policies, text);
				if (formattedText == null || formattedText == text)
					return;
				
				editor.ReplaceText (0, text.Length, formattedText);
				text = editor.Text;
				var currentOffsetWithoutWhitepspaces = 0;
				int i = 0;
				for (; i < text.Length && currentOffsetWithoutWhitepspaces < oldOffsetWithoutWhitespaces; i++) {
					if (!char.IsWhiteSpace(text [i])) {
						currentOffsetWithoutWhitepspaces++;
					}
				}
				editor.SetCaretLocation (editor.OffsetToLocation (i));
			}
		}
	}
}
