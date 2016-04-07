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
using MonoDevelop.Core.Text;

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

			var caretOffset = doc.Editor.CaretOffset;
			for (int i = 0; i < ext.Markers.Count; i++) {
				if (ext.Markers [i].Contains (caretOffset))
					MoveToNextUsageHandler.MoveToSegment (doc, ext.Markers [(i + ext.Markers.Count - 1) % ext.Markers.Count]);
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
			
			var caretOffset = doc.Editor.CaretOffset;
			for (int i = 0; i < ext.Markers.Count; i++) {
				if (ext.Markers [i].Contains (caretOffset))
					MoveToNextUsageHandler.MoveToSegment (doc, ext.Markers [(i + 1) % ext.Markers.Count]);
			}
		}
		
		public static void MoveToSegment (MonoDevelop.Ide.Gui.Document doc, ISegment segment)
		{
			if (segment == null || segment.Offset < 0 || segment.Length == 0)
				return;
			var data = doc.Editor;
			data.CaretOffset = segment.Offset;

//			var loc = data.OffsetToLocation (segment.EndOffset);
//			if (data.ColumnToX (data.GetLine (loc.Line), loc.Column) < data.HAdjustment.PageSize)
//				data.HAdjustment.Value = 0;
		}
	}
}

