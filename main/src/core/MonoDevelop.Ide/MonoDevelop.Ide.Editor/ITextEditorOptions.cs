//
// ITextEditorOptions.cs
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
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.Ide.Editor
{
	public enum IndentStyle
	{
		/// <summary>
		/// No indentation occurs
		/// </summary>
		None,

		/// <summary>
		/// The indentation from the line above will be
		/// taken to indent the current line
		/// </summary>
		Auto, 

		/// <summary>
		/// Intelligent, context sensitive indentation will occur
		/// </summary>
		Smart,

		/// <summary>
		/// Intelligent, context sensitive indentation that minimizes whitespaces will occur
		/// </summary>
		Virtual
	}

	public enum ShowWhitespaces {
		Never,
		Selection,
		Always
	}

	[Flags]
	public enum IncludeWhitespaces {
		None        = 0,
		Space       = 1,
		Tab         = 2,
		LineEndings = 4,
		All         = Space | Tab | LineEndings
	}

	public interface ITextEditorOptions : IDisposable
	{
		WordFindStrategy WordFindStrategy { get; }

		bool TabsToSpaces { get; }
		int IndentationSize { get; }
		int TabSize { get; }
		bool ShowIconMargin { get; }
		bool ShowLineNumberMargin { get; }
		bool ShowFoldMargin { get; }
		bool HighlightCaretLine { get; }
		int RulerColumn { get; }
		bool ShowRuler { get; }
		IndentStyle IndentStyle { get; }
		bool OverrideDocumentEolMarker { get; }
		bool EnableSyntaxHighlighting { get; }
		bool RemoveTrailingWhitespaces { get; }
		
		bool WrapLines { get; }

		string FontName { get; }

		string GutterFontName { get; }

		string EditorTheme { get;  }

		string DefaultEolMarker { get; }

		bool GenerateFormattingUndoStep { get; }

		bool EnableSelectionWrappingKeys { get; }

		bool SmartBackspace { get; }

		ShowWhitespaces ShowWhitespaces { get; }

		IncludeWhitespaces IncludeWhitespaces { get; }
	}

	public static class TextEditorOptionsExtension
	{
		public static EditorTheme GetEditorTheme (this ITextEditorOptions options)
		{
			if (options == null)
				throw new ArgumentNullException ("options");
			return SyntaxHighlightingService.GetEditorTheme (options.EditorTheme);
		}

		/// <summary>
		/// Gets the indentation string for a single indent.
		/// </summary>
		public static string GetIndentationString (this ITextEditorOptions options)
		{
			if (options == null)
				throw new ArgumentNullException ("options");
			return options.TabsToSpaces ? new string (' ', options.IndentationSize) : "\t";
		}
	}
}