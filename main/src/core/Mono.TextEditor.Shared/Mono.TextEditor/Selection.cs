using System;
using Mono.TextEditor.Highlighting;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor;

namespace Mono.TextEditor
{
	static class SelectionExtension
	{
		public static ISegment GetSelectionRange (this Selection selection, TextEditorData data)
		{
			int anchorOffset = GetAnchorOffset (selection, data);
			int leadOffset = GetLeadOffset (selection, data);
			return new TextSegment (System.Math.Min (anchorOffset, leadOffset), System.Math.Abs (anchorOffset - leadOffset));
		}

		public static int GetAnchorOffset (this Selection selection, TextEditorData data)
		{
			return data.Document.LocationToOffset (selection.Anchor);
		}

		public static int GetLeadOffset (this Selection selection, TextEditorData data)
		{
			return data.Document.LocationToOffset (selection.Lead);
		}
	}
}
