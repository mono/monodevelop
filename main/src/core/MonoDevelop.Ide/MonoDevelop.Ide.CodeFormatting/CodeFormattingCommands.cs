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
				Formatter formatter = TextFileService.GetFormatter (mt);
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
			Formatter formatter = TextFileService.GetFormatter (mt);
			if (formatter == null)
				return;
			doc.Editor.Document.BeginAtomicUndo ();
			var loc = doc.Editor.Caret.Location;
			doc.Editor.Replace (0, doc.Editor.Length, formatter.FormatText (doc.Project != null ? doc.Project.Policies : null, doc.Editor.Text));
			doc.Editor.Caret.Location = loc;
			doc.Editor.Document.EndAtomicUndo ();
		}
	}
	
	public class FormatSelectionHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			if (IdeApp.Workbench.ActiveDocument != null && IdeApp.Workbench.ActiveDocument.IsFile) {
				string mt = DesktopService.GetMimeTypeForUri (IdeApp.Workbench.ActiveDocument.FileName);
				Formatter formatter = TextFileService.GetFormatter (mt);
				if (formatter != null) {
					info.Enabled = IdeApp.Workbench.ActiveDocument.Editor.IsSomethingSelected;
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
			Formatter formatter = TextFileService.GetFormatter (mt);
			if (formatter == null || !doc.Editor.IsSomethingSelected)
				return;
			var selection = doc.Editor.SelectionRange;
			
			doc.Editor.Document.BeginAtomicUndo ();
			formatter.OnTheFlyFormat (doc.Project != null ? doc.Project.Policies : null, doc.Editor, selection.Offset, selection.EndOffset);
			doc.Editor.Document.EndAtomicUndo ();
		}
	}
}
