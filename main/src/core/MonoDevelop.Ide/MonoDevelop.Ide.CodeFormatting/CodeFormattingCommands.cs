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

namespace MonoDevelop.Ide.CodeFormatting
{
	public enum CodeFormattingCommands {
		FormatBuffer,
		FormatSelection
	}
	
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
			return editor == null ? null : CodeFormatterService.GetFormatter (editor.MimeType);
		}

		protected override void Update (CommandInfo info)
		{
			Document doc;
			var formatter = GetFormatter (out doc);
			info.Enabled = formatter != null;
		}
		
		protected override void Run (object tool)
		{
			Document doc;
			var formatter = GetFormatter (out doc);
			if (formatter == null)
				return;

			if (formatter.SupportsOnTheFlyFormatting) {
				using (var undo = doc.Editor.OpenUndoGroup ()) {
					formatter.OnTheFlyFormat (doc, 0, doc.Editor.Length);
				}
			} else {
				doc.Editor.Text = formatter.FormatText (doc.Project.Policies, doc.Editor.Text); 
			}
			doc.Editor.RequestRedraw ();
		}
	}
	
	public class FormatSelectionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			Document doc;
			var formatter = FormatBufferHandler.GetFormatter (out doc);
			info.Enabled = formatter != null && !formatter.IsDefault;
		}
		
		protected override void Run (object tool)
		{
			Document doc;
			var formatter = FormatBufferHandler.GetFormatter (out doc);
			if (formatter == null)
				return;

			ISegment selection;
			var editor = doc.Editor;
			if (editor.IsSomethingSelected) {
				selection = editor.SelectionRange;
			} else {
				selection = editor.GetLine (editor.CaretLocation.Line);
			}
			
			using (var undo = editor.OpenUndoGroup ()) {
				var version = editor.Version;

				if (formatter.SupportsOnTheFlyFormatting) {
					formatter.OnTheFlyFormat (doc, selection.Offset, selection.EndOffset);
				} else {
					var pol = doc.Project != null ? doc.Project.Policies : null;
					try {
						string text = formatter.FormatText (pol, editor.Text, selection.Offset, selection.EndOffset);
						if (text != null) {
							editor.Replace (selection.Offset, selection.Length, text);
						}
					} catch (Exception e) {
						LoggingService.LogError ("Error during format.", e); 
					}
				}

				if (editor.IsSomethingSelected) { 
					int newOffset = version.MoveOffsetTo (editor.Version, selection.Offset);
					int newEndOffset = version.MoveOffsetTo (editor.Version, selection.EndOffset);
					editor.SetSelection (newOffset, newEndOffset);
				}
			}
		}
	}
}
