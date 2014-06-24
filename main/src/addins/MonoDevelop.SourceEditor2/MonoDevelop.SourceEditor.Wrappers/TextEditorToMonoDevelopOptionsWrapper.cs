//
// TextEditorToMonoDevelopOptionsWrapper.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.SourceEditor.Wrappers
{
	class TextEditorToMonoDevelopOptionsWrapper : ITextEditorOptions
	{
		readonly ISourceEditorOptions options;

		public TextEditorToMonoDevelopOptionsWrapper (ISourceEditorOptions options)
		{
			if (options == null)
				throw new ArgumentNullException ("options");
			this.options = options;
		}

		#region ITextEditorOptions implementation
		event EventHandler ITextEditorOptions.Changed {
			add {
				options.Changed += value;
			}
			remove {
				options.Changed -= value;
			}
		}

		void ITextEditorOptions.ZoomIn ()
		{
			options.ZoomIn ();
		}

		void ITextEditorOptions.ZoomOut ()
		{
			options.ZoomOut ();
		}

		void ITextEditorOptions.ZoomReset ()
		{
			options.ZoomReset ();
		}

		double ITextEditorOptions.Zoom {
			get {
				return options.Zoom;
			}
			set {
				options.Zoom = value;
			}
		}

		bool ITextEditorOptions.CanZoomIn {
			get {
				return options.CanZoomIn;
			}
		}

		bool ITextEditorOptions.CanZoomOut {
			get {
				return options.CanZoomOut;
			}
		}

		bool ITextEditorOptions.CanResetZoom {
			get {
				return options.CanResetZoom;
			}
		}

		string ITextEditorOptions.IndentationString {
			get {
				return options.IndentationString;
			}
		}

		IWordFindStrategy ITextEditorOptions.WordFindStrategy {
			get {
				return null;
			}
			set {
				throw new NotImplementedException ();
			}
		}

		bool ITextEditorOptions.AllowTabsAfterNonTabs {
			get {
				return options.AllowTabsAfterNonTabs;
			}
			set {
				options.AllowTabsAfterNonTabs = value;
			}
		}

		bool ITextEditorOptions.HighlightMatchingBracket {
			get {
				return options.HighlightMatchingBracket;
			}
			set {
				options.HighlightMatchingBracket = value;
			}
		}

		bool ITextEditorOptions.TabsToSpaces {
			get {
				return options.TabsToSpaces;
			}
			set {
				options.TabsToSpaces = value;
			}
		}

		int ITextEditorOptions.IndentationSize {
			get {
				return options.IndentationSize;
			}
			set {
				options.IndentationSize = value;
			}
		}

		int ITextEditorOptions.TabSize {
			get {
				return options.TabSize;
			}
			set {
				options.TabSize = value;
			}
		}

		bool ITextEditorOptions.ShowIconMargin {
			get {
				return options.ShowIconMargin;
			}
			set {
				options.ShowIconMargin = value;
			}
		}

		bool ITextEditorOptions.ShowLineNumberMargin {
			get {
				return options.ShowLineNumberMargin;
			}
			set {
				options.ShowLineNumberMargin = value;
			}
		}

		bool ITextEditorOptions.ShowFoldMargin {
			get {
				return options.ShowFoldMargin;
			}
			set {
				options.ShowFoldMargin = value;
			}
		}

		bool ITextEditorOptions.HighlightCaretLine {
			get {
				return options.HighlightCaretLine;
			}
			set {
				options.HighlightCaretLine = value;
			}
		}

		int ITextEditorOptions.RulerColumn {
			get {
				return options.RulerColumn;
			}
			set {
				options.RulerColumn = value;
			}
		}

		bool ITextEditorOptions.ShowRuler {
			get {
				return options.ShowRuler;
			}
			set {
				options.ShowRuler = value;
			}
		}

		IndentStyle ITextEditorOptions.IndentStyle {
			get {
				return (IndentStyle)options.IndentStyle;
			}
			set {
				options.IndentStyle = (Mono.TextEditor.IndentStyle)value;
			}
		}

		bool ITextEditorOptions.OverrideDocumentEolMarker {
			get {
				return options.OverrideDocumentEolMarker;
			}
			set {
				options.OverrideDocumentEolMarker = value;
			}
		}

		bool ITextEditorOptions.EnableSyntaxHighlighting {
			get {
				return options.EnableSyntaxHighlighting;
			}
			set {
				options.EnableSyntaxHighlighting = value;
			}
		}

		bool ITextEditorOptions.EnableAnimations {
			get {
				return options.EnableAnimations;
			}
		}

		bool ITextEditorOptions.EnableSelectionWrappingKeys {
			get {
				return options.EnableSelectionWrappingKeys;
			}
		}

		bool ITextEditorOptions.EnableQuickDiff {
			get {
				return options.EnableQuickDiff;
			}
			set {
				options.EnableQuickDiff = value;
			}
		}

		bool ITextEditorOptions.DrawIndentationMarkers {
			get {
				return options.DrawIndentationMarkers;
			}
			set {
				options.DrawIndentationMarkers = value;
			}
		}

		bool ITextEditorOptions.WrapLines {
			get {
				return options.WrapLines;
			}
			set {
				options.WrapLines = value;
			}
		}

		string ITextEditorOptions.FontName {
			get {
				return options.FontName;
			}
			set {
				options.FontName = value;
			}
		}

		Pango.FontDescription ITextEditorOptions.Font {
			get {
				return options.Font;
			}
		}
	
		string ITextEditorOptions.GutterFontName {
			get {
				return options.GutterFontName;
			}
			set {
				options.GutterFontName = value;
			}
		}

		Pango.FontDescription ITextEditorOptions.GutterFont {
			get {
				return options.GutterFont;
			}
		}

		string ITextEditorOptions.ColorScheme {
			get {
				return options.ColorScheme;
			}
			set {
				options.ColorScheme = value;
			}
		}

		string ITextEditorOptions.DefaultEolMarker {
			get {
				return options.DefaultEolMarker;
			}
			set {
				options.DefaultEolMarker = value;
			}
		}

		ShowWhitespaces ITextEditorOptions.ShowWhitespaces {
			get {
				return (ShowWhitespaces)options.ShowWhitespaces;
			}
			set {
				options.ShowWhitespaces = (Mono.TextEditor.ShowWhitespaces)value;
			}
		}

		IncludeWhitespaces ITextEditorOptions.IncludeWhitespaces {
			get {
				return (IncludeWhitespaces)options.IncludeWhitespaces;
			}
			set {
				options.IncludeWhitespaces = (Mono.TextEditor.IncludeWhitespaces)value;
			}
		}

		bool ITextEditorOptions.GenerateFormattingUndoStep {
			get {
				return options.GenerateFormattingUndoStep;
			}
			set {
				options.GenerateFormattingUndoStep = value;
			}
		}
		#endregion

		#region IDisposable implementation
		void IDisposable.Dispose ()
		{
			options.Dispose ();
		}
		#endregion
	}

}
