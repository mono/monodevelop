// 
// MoveToUsagesHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Content;
using Mono.TextEditor;

namespace MonoDevelop.CSharp.Highlighting
{
	enum MoveToUsagesCommand {
		PrevUsage,
		NextUsage
	}
	
	class MoveToPrevUsageHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.Editor == null) {
				info.Visible = info.Enabled = false;
				return;
			}
			
			info.Visible = info.Enabled = true;
		}
		
		internal static HighlightUsagesExtension GetHighlightUsageExtension (MonoDevelop.Ide.Gui.Document doc)
		{
			return doc.GetContent <HighlightUsagesExtension> ();
		}
		protected override void Run ()
		{
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			HighlightUsagesExtension ext = GetHighlightUsageExtension (doc);
			if (ext == null)
				return;
			if (ext.IsTimerOnQueue)
				ext.ForceUpdate ();

			var caretOffset = doc.Editor.Caret.Offset;
			for (int i = 0; i < ext.UsagesSegments.Count; i++) {
				if (ext.UsagesSegments [i].TextSegment.Contains (caretOffset))
					MoveToNextUsageHandler.MoveToSegment (doc, ext.UsagesSegments [(i + ext.UsagesSegments.Count - 1) % ext.UsagesSegments.Count]);
			}
		}
	}
	
	class MoveToNextUsageHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.Editor == null) {
				info.Visible = info.Enabled = false;
				return;
			}
			
			info.Visible = info.Enabled = true;
		}
		
		protected override void Run ()
		{
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			HighlightUsagesExtension ext = MoveToPrevUsageHandler.GetHighlightUsageExtension (doc);
			if (ext == null)
				return;
			if (ext.IsTimerOnQueue)
				ext.ForceUpdate ();
			if (ext == null || ext.Markers.Count == 0)
				return;
			
			var caretOffset = doc.Editor.Caret.Offset;
			for (int i = 0; i < ext.UsagesSegments.Count; i++) {
				if (ext.UsagesSegments [i].TextSegment.Contains (caretOffset))
					MoveToNextUsageHandler.MoveToSegment (doc, ext.UsagesSegments [(i + 1) % ext.UsagesSegments.Count]);
			}
		}
		
		public static void MoveToSegment (MonoDevelop.Ide.Gui.Document doc, TextSegment segment)
		{
			if (segment.IsInvalid || segment.IsEmpty)
				return;
			TextEditorData data = doc.Editor;
			data.Caret.Offset = segment.Offset;
			data.Parent.ScrollTo (segment.EndOffset);
			
			var loc = data.Document.OffsetToLocation (segment.EndOffset);
			if (data.Parent.TextViewMargin.ColumnToX (data.Document.GetLine (loc.Line), loc.Column) < data.HAdjustment.PageSize)
				data.HAdjustment.Value = 0;
		}
	}
}

