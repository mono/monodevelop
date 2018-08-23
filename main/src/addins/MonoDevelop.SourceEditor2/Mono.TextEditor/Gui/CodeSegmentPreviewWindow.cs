//
// CodeSegmentPreviewWindow.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gdk;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Editor.Highlighting;

namespace Mono.TextEditor
{
	class CodeSegmentPreviewWindow : CodePreviewWindow
	{
		MonoTextEditor editor;

		static string CodeSegmentPreviewInformString { get; set; } = GettextCatalog.GetString ("Press F2 to focus");

		public ISegment Segment { get; private set; }

		public bool IsEmptyText => string.IsNullOrWhiteSpace (Text) && string.IsNullOrWhiteSpace (Markup);
		
		public CodeSegmentPreviewWindow (MonoTextEditor editor, bool hideFooter, ISegment segment, bool removeIndent = true)
			: base (editor.GdkWindow, editor.Options.FontName, editor.Options.GetEditorTheme ())
		{
			if (!hideFooter) {
				FooterText = CodeSegmentPreviewInformString;
			}
			this.editor = editor;

			SetSegment (segment, removeIndent);
		}
		
		public void SetSegment (ISegment segment, bool removeIndent)
		{
			this.Segment = segment;

			// no need to markup thousands of lines for a preview window
			int startLine = editor.Document.OffsetToLineNumber (segment.Offset);
			int endLine = editor.Document.OffsetToLineNumber (segment.EndOffset);
			
			bool pushedLineLimit = endLine - startLine > MaximumLineCount;
			if (pushedLineLimit)
				segment = new TextSegment (segment.Offset, editor.Document.GetLine (startLine + MaximumLineCount).Offset - segment.Offset);
			Markup = editor.GetTextEditorData ().GetMarkup (segment.Offset, segment.Length, removeIndent) + (pushedLineLimit ? Environment.NewLine + "..." : "");
		}
		
		protected override void OnDestroyed ()
		{
			editor = null;
			base.OnDestroyed ();
		}
	}
}
