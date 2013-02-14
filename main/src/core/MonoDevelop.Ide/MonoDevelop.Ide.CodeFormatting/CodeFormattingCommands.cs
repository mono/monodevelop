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
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;


namespace MonoDevelop.Ide.CodeFormatting
{
	public enum CodeFormattingCommands {
		FormatBuffer,
		FormatSelection
	}
	
	public class FormatBufferHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsFile) {
				string mt = DesktopService.GetMimeTypeForUri (IdeApp.Workbench.ActiveDocument.FileName);
				var formatter = CodeFormatterService.GetFormatter (mt);
				if (formatter != null)
					return;
			}
			info.Enabled = false;
		}
		
		protected override void Run (object tool)
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			string mt = DesktopService.GetMimeTypeForUri (doc.FileName);
			var formatter = CodeFormatterService.GetFormatter (mt);
			if (formatter == null)
				return;
			using (var undo = doc.Editor.OpenUndoGroup ()) {
				var loc = doc.Editor.Caret.Location;
				var text = formatter.FormatText (doc.Project != null ? doc.Project.Policies : null, doc.Editor.Text);
				if (text != null) {
					doc.Editor.Replace (0, doc.Editor.Length, text);
					doc.Editor.Caret.Location = loc;
				}
			}
		}
	}
	
	public class FormatSelectionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsFile) {
				string mt = DesktopService.GetMimeTypeForUri (IdeApp.Workbench.ActiveDocument.FileName);
				var formatter = CodeFormatterService.GetFormatter (mt);
				if (formatter != null && !formatter.IsDefault) {
					info.Enabled = true;
					return;
				}
			}
			info.Enabled = false;
		}
		
		protected override void Run (object tool)
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;
			string mt = DesktopService.GetMimeTypeForUri (doc.FileName);
			var formatter = CodeFormatterService.GetFormatter (mt);
			if (formatter == null)
				return;
			Mono.TextEditor.TextSegment selection;
			var editor = doc.Editor;
			if (editor.IsSomethingSelected) {
				selection = editor.SelectionRange;
			} else {
				selection = editor.GetLine (editor.Caret.Line).Segment;
			}
			
			using (var undo = editor.OpenUndoGroup ()) {
				var version = editor.Version;

				if (formatter.SupportsOnTheFlyFormatting) {
					formatter.OnTheFlyFormat (doc, selection.Offset, selection.EndOffset);
				} else {
					var pol = doc.Project != null ? doc.Project.Policies : null;
					string text = formatter.FormatText (pol, editor.Text, selection.Offset, selection.EndOffset);
					if (text != null) {
						editor.Replace (selection.Offset, selection.Length, text);
					}
				}

				int newOffset = version.MoveOffsetTo (editor.Version, selection.Offset);
				int newEndOffset = version.MoveOffsetTo (editor.Version, selection.EndOffset);
				
				editor.SetSelection (newOffset, newEndOffset);

			}
		}
	}
}
