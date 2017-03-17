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
	enum ShowWhitespaces
	{
		Never,
		Selection,
		Always
	}

	[Flags]
	enum IncludeWhitespaces
	{
		None = 0,
		Space = 1,
		Tab = 2,
		LineEndings = 4,
		All = Space | Tab | LineEndings
	}

	interface ITextEditorOptions : IDisposable
	{
		double Zoom { get; set; }
		bool CanZoomIn { get; }
		bool CanZoomOut { get; }
		bool CanResetZoom { get; }
		void ZoomIn ();
		void ZoomOut ();
		void ZoomReset ();

		string IndentationString { get; }

		IWordFindStrategy WordFindStrategy { get; set; }

		bool AllowTabsAfterNonTabs { get; set; }
		bool HighlightMatchingBracket { get; set; }
		bool TabsToSpaces { get; set; }
		int IndentationSize { get; set; }
		int TabSize { get; set; }
		bool ShowIconMargin { get; set; }
		bool ShowLineNumberMargin { get; set; }
		bool ShowFoldMargin { get; set; }
		bool HighlightCaretLine { get; set; }
		int RulerColumn { get; set; }
		bool ShowRuler { get; set; }
		IndentStyle IndentStyle { get; set; }
		bool OverrideDocumentEolMarker { get; set; }
		bool EnableSyntaxHighlighting { get; set; }
		bool EnableAnimations { get; }
		bool EnableSelectionWrappingKeys { get; }
		bool SmartBackspace { get; }

		bool EnableQuickDiff { get; set; }

		bool DrawIndentationMarkers { get; set; }

		bool WrapLines { get; set; }
		string FontName { get; set; }
		Pango.FontDescription Font { get; }

		string GutterFontName { get; set; }
		Pango.FontDescription GutterFont { get; }

		string EditorThemeName { get; set; }
		string DefaultEolMarker { get; set; }

		ShowWhitespaces ShowWhitespaces { get; set; }
		IncludeWhitespaces IncludeWhitespaces { get; set; }

		bool GenerateFormattingUndoStep { get; set; }
		event EventHandler ZoomChanged;

		MonoDevelop.Ide.Editor.Highlighting.EditorTheme GetEditorTheme ();

		event EventHandler Changed;
	}
}
