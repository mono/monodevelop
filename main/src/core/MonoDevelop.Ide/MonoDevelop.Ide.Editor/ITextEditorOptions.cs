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

	public interface ITextEditorOptions : IDisposable
	{
		string IndentationString { get; }

		WordFindStrategy WordFindStrategy { get; set; }

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

		bool WrapLines { get; set; }

		string FontName { get;  set; }

		string GutterFontName { get; set; }

		string ColorScheme { get; set;  }

		string DefaultEolMarker { get; set; }

		bool GenerateFormattingUndoStep { get; set; }

		event EventHandler Changed;
	}


	public static class TextEditorOptionsExtension
	{
		public static ColorScheme GetColorStyle (this ITextEditorOptions options)
		{
			if (options == null)
				throw new ArgumentNullException ("options");
			return SyntaxModeService.GetColorStyle (options.ColorScheme);
		}
	}
}

