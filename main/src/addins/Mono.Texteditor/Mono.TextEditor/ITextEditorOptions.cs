//
// ITextEditorOptions.cs
// 
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
//
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	
	
	public interface ITextEditorOptions : IDisposable
	{
		double Zoom { get; }
		bool CanZoomIn { get; }
		bool CanZoomOut { get; }
		bool CanResetZoom { get; }
		void ZoomIn ();
		void ZoomOut ();
		void ZoomReset ();
		string IndentationString { get; }
		IWordFindStrategy WordFindStrategy { get; }
		bool AllowTabsAfterNonTabs { get; }
		bool HighlightMatchingBracket { get; }
		bool RemoveTrailingWhitespaces { get; }
		bool TabsToSpaces { get; }
		int IndentationSize { get; }
		int TabSize { get; }
		bool ShowIconMargin { get; }
		bool ShowLineNumberMargin { get; }
		bool ShowFoldMargin { get; }
		bool ShowInvalidLines { get; }
		bool ShowTabs { get; }
		bool ShowEolMarkers { get; }
		bool HighlightCaretLine { get; }
		bool ShowSpaces { get; }
		int RulerColumn { get; }
		bool ShowRuler { get; }
		bool AutoIndent { get; }
		string FontName { get; }
		bool EnableSyntaxHighlighting { get; }
		Pango.FontDescription Font { get; }
		string ColorScheme { get; }
		Style GetColorStyle (Gtk.Widget widget);
		event EventHandler Changed;
	}
}
