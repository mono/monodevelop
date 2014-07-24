//
// SourceEditorView_ITextEditorOptions.cs
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

namespace MonoDevelop.SourceEditor
{
	public partial class SourceEditorView
	{
		MonoDevelop.Ide.Editor.WordFindStrategy MonoDevelop.Ide.Editor.ITextEditorOptions.WordFindStrategy {
			get {
				return ((StyledSourceEditorOptions)TextEditor.Options).OptionsCore.WordFindStrategy;
			}
		}

		bool MonoDevelop.Ide.Editor.ITextEditorOptions.TabsToSpaces {
			get {
				return TextEditor.Options.TabsToSpaces;
			}
		}

		int MonoDevelop.Ide.Editor.ITextEditorOptions.IndentationSize {
			get {
				return TextEditor.Options.IndentationSize;
			}
		}

		int MonoDevelop.Ide.Editor.ITextEditorOptions.TabSize {
			get {
				return TextEditor.Options.TabSize;
			}
		}

		bool MonoDevelop.Ide.Editor.ITextEditorOptions.ShowIconMargin {
			get {
				return TextEditor.Options.ShowIconMargin;
			}
		}

		bool MonoDevelop.Ide.Editor.ITextEditorOptions.ShowLineNumberMargin {
			get {
				return TextEditor.Options.ShowLineNumberMargin;
			}
		}

		bool MonoDevelop.Ide.Editor.ITextEditorOptions.ShowFoldMargin {
			get {
				return TextEditor.Options.ShowFoldMargin;
			}
		}

		bool MonoDevelop.Ide.Editor.ITextEditorOptions.HighlightCaretLine {
			get {
				return TextEditor.Options.HighlightCaretLine;
			}
		}

		int MonoDevelop.Ide.Editor.ITextEditorOptions.RulerColumn {
			get {
				return TextEditor.Options.RulerColumn;
			}
		}

		bool MonoDevelop.Ide.Editor.ITextEditorOptions.ShowRuler {
			get {
				return TextEditor.Options.ShowRuler;
			}
		}

		MonoDevelop.Ide.Editor.IndentStyle MonoDevelop.Ide.Editor.ITextEditorOptions.IndentStyle {
			get {
				return (MonoDevelop.Ide.Editor.IndentStyle)TextEditor.Options.IndentStyle;
			}
		}

		bool MonoDevelop.Ide.Editor.ITextEditorOptions.OverrideDocumentEolMarker {
			get {
				return TextEditor.Options.OverrideDocumentEolMarker;
			}
		}

		bool MonoDevelop.Ide.Editor.ITextEditorOptions.EnableSyntaxHighlighting {
			get {
				return TextEditor.Options.EnableSyntaxHighlighting;
			}
		}

		bool MonoDevelop.Ide.Editor.ITextEditorOptions.WrapLines {
			get {
				return TextEditor.Options.WrapLines;
			}
		}

		string MonoDevelop.Ide.Editor.ITextEditorOptions.FontName {
			get {
				return TextEditor.Options.FontName;
			}
		}

		string MonoDevelop.Ide.Editor.ITextEditorOptions.GutterFontName {
			get {
				return TextEditor.Options.GutterFontName;
			}
		}

		string MonoDevelop.Ide.Editor.ITextEditorOptions.ColorScheme {
			get {
				return TextEditor.Options.ColorScheme;
			}
		}

		string MonoDevelop.Ide.Editor.ITextEditorOptions.DefaultEolMarker {
			get {
				return TextEditor.Options.DefaultEolMarker;
			}
		}

		bool MonoDevelop.Ide.Editor.ITextEditorOptions.GenerateFormattingUndoStep {
			get {
				return TextEditor.Options.GenerateFormattingUndoStep;
			}
		}

		ShowWhitespaces MonoDevelop.Ide.Editor.ITextEditorOptions.ShowWhitespaces {
			get {
				return (ShowWhitespaces)TextEditor.Options.ShowWhitespaces;
			}
		}

		IncludeWhitespaces MonoDevelop.Ide.Editor.ITextEditorOptions.IncludeWhitespaces {
			get {
				return (IncludeWhitespaces)TextEditor.Options.IncludeWhitespaces;
			}
		}
		
		bool MonoDevelop.Ide.Editor.ITextEditorOptions.RemoveTrailingWhitespaces {
			get {
				return ((StyledSourceEditorOptions)TextEditor.Options).OptionsCore.RemoveTrailingWhitespaces;
			}
		}
	}
}