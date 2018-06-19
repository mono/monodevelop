//
// ChangeableEditorOptions.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.Ide.Editor
{
	public sealed class CustomEditorOptions : ITextEditorOptions
	{
		#region ITextEditorOptions implementation
		public WordFindStrategy WordFindStrategy {
			get;
			set;
		}

		public bool TabsToSpaces {
			get;
			set;
		}

		public int IndentationSize {
			get;
			set;
		}

		public int TabSize {
			get;
			set;
		}

		public bool ShowIconMargin {
			get;
			set;
		}

		public bool ShowLineNumberMargin {
			get;
			set;
		}

		public bool ShowFoldMargin {
			get;
			set;
		}

		public bool HighlightCaretLine {
			get;
			set;
		}

		public int RulerColumn {
			get;
			set;
		}

		public bool ShowRuler {
			get;
			set;
		}

		public IndentStyle IndentStyle {
			get;
			set;
		}

		public bool OverrideDocumentEolMarker {
			get;
			set;
		}

		public bool EnableSyntaxHighlighting {
			get;
			set;
		}

		public bool RemoveTrailingWhitespaces {
			get;
			set;
		}

		public bool WrapLines {
			get;
			set;
		}

		public string FontName {
			get;
			set;
		}

		public string GutterFontName {
			get;
			set;
		}

		public string EditorTheme {
			get;
			set;
		}

		public string DefaultEolMarker {
			get;
			set;
		}

		public bool GenerateFormattingUndoStep {
			get;
			set;
		}

		public bool EnableSelectionWrappingKeys {
			get;
			set;
		}

		public ShowWhitespaces ShowWhitespaces {
			get;
			set;
		}

		public IncludeWhitespaces IncludeWhitespaces {
			get;
			set;
		}
		
		public bool SmartBackspace {
			get;
			set;
		}

		public bool EnableQuickDiff {
			get;
			set;
		}
		#endregion

		public CustomEditorOptions ()
		{
			this.EditorTheme = MonoDevelop.Ide.Editor.Highlighting.EditorTheme.DefaultThemeName;
			this.TabSize = this.IndentationSize = 4;
			this.DefaultEolMarker = "\n";
		}

		public CustomEditorOptions (ITextEditorOptions initializeFrom)
		{
			if (initializeFrom == null)
				throw new ArgumentNullException (nameof (initializeFrom));
			WordFindStrategy = initializeFrom.WordFindStrategy;
			TabsToSpaces = initializeFrom.TabsToSpaces;
			IndentationSize = initializeFrom.IndentationSize;
			TabSize = initializeFrom.TabSize;
			ShowIconMargin = initializeFrom.ShowIconMargin;
			ShowLineNumberMargin = initializeFrom.ShowLineNumberMargin;
			ShowFoldMargin = initializeFrom.ShowFoldMargin;
			HighlightCaretLine = initializeFrom.HighlightCaretLine;
			RulerColumn = initializeFrom.RulerColumn;
			ShowRuler = initializeFrom.ShowRuler;
			IndentStyle = initializeFrom.IndentStyle;
			OverrideDocumentEolMarker = initializeFrom.OverrideDocumentEolMarker;
			EnableSyntaxHighlighting = initializeFrom.EnableSyntaxHighlighting;
			RemoveTrailingWhitespaces = initializeFrom.RemoveTrailingWhitespaces;
			WrapLines = initializeFrom.WrapLines;
			FontName = initializeFrom.FontName;
			GutterFontName = initializeFrom.GutterFontName;
			EditorTheme = initializeFrom.EditorTheme;
			DefaultEolMarker = initializeFrom.DefaultEolMarker;
			GenerateFormattingUndoStep = initializeFrom.GenerateFormattingUndoStep;
			EnableSelectionWrappingKeys = initializeFrom.EnableSelectionWrappingKeys;
			ShowWhitespaces = initializeFrom.ShowWhitespaces;
			IncludeWhitespaces = initializeFrom.IncludeWhitespaces;
			SmartBackspace = initializeFrom.SmartBackspace;
			EnableQuickDiff = initializeFrom.EnableQuickDiff;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
		}
		#endregion
	}
}

