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
	public enum MoveToUsagesCommand {
		PrevUsage,
		NextUsage
	}
	
	public class MoveToPrevUsageHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.TextEditorData == null) {
				info.Visible = info.Enabled = false;
				return;
			}
			
			info.Visible = info.Enabled = true;
		}
		
		internal static HighlightUsagesExtension GetHighlightUsageExtension (MonoDevelop.Ide.Gui.Document doc)
		{
			ITextEditorExtension ext = doc.EditorExtension;
			while (ext != null) {
				if (ext is HighlightUsagesExtension)
					return (HighlightUsagesExtension)ext;
				ext = ext.Next;
			}
			return null;
		}
		protected override void Run ()
		{
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			HighlightUsagesExtension ext = GetHighlightUsageExtension (doc);
			if (ext == null || ext.Markers.Count == 0)
				return;
			
			if (ext.Markers.ContainsKey (doc.TextEditorData.Caret.Line)) {
				var marker = ext.Markers[doc.TextEditorData.Caret.Line];
				ISegment segment = null;
				for (int i = 0; i < marker.Usages.Count; i++) {
					if (marker.Usages[i].EndOffset < doc.TextEditorData.Caret.Offset)
						segment = marker.Usages[i];
				}
				if (segment != null) {
					doc.TextEditorData.Caret.Offset = segment.Offset;
					return;
				}
			}
			
			int max = int.MinValue;
			foreach (var pair in ext.Markers) {
				if (pair.Key > max && pair.Key < doc.TextEditorData.Caret.Line)
					max = pair.Key;
			}
			if (max >= 0) {
				doc.TextEditorData.Caret.Offset = ext.Markers[max].Usages.Last ().Offset;
				return;
			}
			doc.TextEditorData.Caret.Offset = ext.Markers.Last ().Value.Usages.Last ().Offset;
		}
	}
	
	public class MoveToNextUsageHandler : CommandHandler
	{
		protected override void Update (CommandInfo info)
		{
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.TextEditorData == null) {
				info.Visible = info.Enabled = false;
				return;
			}
			
			info.Visible = info.Enabled = true;
		}
		
		protected override void Run ()
		{
			MonoDevelop.Ide.Gui.Document doc = IdeApp.Workbench.ActiveDocument;
			HighlightUsagesExtension ext = MoveToPrevUsageHandler.GetHighlightUsageExtension (doc);
			if (ext == null || ext.Markers.Count == 0)
				return;
			
			if (ext.Markers.ContainsKey (doc.TextEditorData.Caret.Line)) {
				var marker = ext.Markers[doc.TextEditorData.Caret.Line];
				ISegment segment = null;
				for (int i = 0; i < marker.Usages.Count; i++) {
					if (marker.Usages[i].Offset > doc.TextEditorData.Caret.Offset) {
						segment = marker.Usages[i];
						break;
					}
				}
				if (segment != null) {
					doc.TextEditorData.Caret.Offset = segment.Offset;
					return;
				}
			}
			
			int max = int.MinValue;
			foreach (var pair in ext.Markers) {
				if (pair.Key > doc.TextEditorData.Caret.Line) {
					max = pair.Key;
					break;
				}
			}
			
			if (max >= 0) {
				doc.TextEditorData.Caret.Offset = ext.Markers[max].Usages.First ().Offset;
				return;
			}
			doc.TextEditorData.Caret.Offset = ext.Markers.First ().Value.Usages.First ().Offset;
		}
	}
}

