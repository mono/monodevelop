//
// JSonTextEditorExtension.cs
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
using ICSharpCode.NRefactory.CSharp;
using Mono.TextEditor;
using System.Text;
using ICSharpCode.NRefactory;

namespace MonoDevelop.SourceEditor.JSon
{
	class JSonIndentEngine : IStateMachineIndentEngine
	{
		TextEditorData data;
		int offset, line, column;
		internal Indent thisLineIndent, nextLineIndent;
		StringBuilder currentIndent;
		char previousNewline = '\0';
		bool isLineStart;
		bool isInString;

		public JSonIndentEngine (TextEditorData data)
		{
			this.data = data;
			Reset ();
		}

		#region IStateMachineIndentEngine implementation

		public IStateMachineIndentEngine Clone ()
		{
			return (IStateMachineIndentEngine)MemberwiseClone ();
		}

		public bool IsInsidePreprocessorDirective {
			get {
				return false;
			}
		}

		public bool IsInsidePreprocessorComment {
			get {
				return false;
			}
		}

		public bool IsInsideStringLiteral {
			get {
				return false;
			}
		}

		public bool IsInsideVerbatimString {
			get {
				return false;
			}
		}

		public bool IsInsideCharacter {
			get {
				return false;
			}
		}

		public bool IsInsideString {
			get {
				return isInString;
			}
		}

		public bool IsInsideLineComment {
			get {
				return false;
			}
		}

		public bool IsInsideMultiLineComment {
			get {
				return false;
			}
		}

		public bool IsInsideDocLineComment {
			get {
				return false;
			}
		}

		public bool IsInsideComment {
			get {
				return false;
			}
		}

		public bool IsInsideOrdinaryComment {
			get {
				return false;
			}
		}

		public bool IsInsideOrdinaryCommentOrString {
			get {
				return false;
			}
		}

		public bool LineBeganInsideVerbatimString {
			get {
				return false;
			}
		}

		public bool LineBeganInsideMultiLineComment {
			get {
				return false;
			}
		}

		#endregion

		public static ICSharpCode.NRefactory.CSharp.TextEditorOptions CreateNRefactoryTextEditorOptions (TextEditorData doc)
		{
			return new ICSharpCode.NRefactory.CSharp.TextEditorOptions {
				TabsToSpaces = doc.TabsToSpaces,
				TabSize = doc.Options.TabSize,
				IndentSize = doc.Options.IndentationSize,
				ContinuationIndent = doc.Options.IndentationSize,
				LabelIndent = -doc.Options.IndentationSize,
				EolMarker = doc.EolMarker,
				IndentBlankLines = doc.Options.IndentStyle != IndentStyle.Virtual,
				WrapLineLength = doc.Options.RulerColumn
			};
		}

		#region IDocumentIndentEngine implementation

		public void Push (char ch)
		{
			var isNewLine = NewLine.IsNewLine (ch);
			if (!isNewLine) {
				if (ch == '"')
					isInString = !IsInsideString;
				if (ch == '{' || ch == '[') {
					nextLineIndent.Push (IndentType.Block);
				} else if (ch == '}' || ch == ']') {
					thisLineIndent.Pop ();
					nextLineIndent.Pop ();
				} 
			} else {
				if (ch == NewLine.LF && previousNewline == NewLine.CR) {
					offset++;
					return;
				}
			}

			offset++;
			if (!isNewLine) {
				previousNewline = '\0';

				isLineStart &= char.IsWhiteSpace (ch);

				if (isLineStart)
					currentIndent.Append (ch);

				if (ch == '\t') {
					var nextTabStop = (column - 1 + data.Options.IndentationSize) / data.Options.IndentationSize;
					column = 1 + nextTabStop * data.Options.IndentationSize;
				} else {
					column++;
				}
			} else {
				previousNewline = ch;
				// there can be more than one chars that determine the EOL,
				// the engine uses only one of them defined with newLineChar
				if (ch != '\n')
					return;
				currentIndent.Length = 0;
				isLineStart = true;
				column = 1;
				line++;
				thisLineIndent = nextLineIndent.Clone ();
			}
		}

		public void Reset ()
		{
			offset = 0;
			line = column = 1;
			thisLineIndent = new Indent (CreateNRefactoryTextEditorOptions (data));
			nextLineIndent = new Indent (CreateNRefactoryTextEditorOptions (data));
			currentIndent = new StringBuilder ();
			previousNewline = '\0';
			isLineStart = true;
			isInString = false;
		}

		public void Update (int offset)
		{
			if (Offset > offset)
				Reset ();

			while (Offset < offset) {
				Push (Document.GetCharAt (Offset));
			}
		}

		IDocumentIndentEngine IDocumentIndentEngine.Clone ()
		{
			return Clone ();
		}

		public ICSharpCode.NRefactory.Editor.IDocument Document {
			get {
				return data.Document;
			}
		}

		public string ThisLineIndent {
			get {
				return thisLineIndent.IndentString;
			}
		}

		public string NextLineIndent {
			get {
				return nextLineIndent.IndentString;
			}
		}

		public string CurrentIndent {
			get {
				return currentIndent.ToString ();
			}
		}

		public bool NeedsReindent {
			get {
				return ThisLineIndent != CurrentIndent;
			}
		}

		public int Offset {
			get {
				return offset;
			}
		}

		public TextLocation Location {
			get {
				return new TextLocation (line, column);
			}
		}

		public bool EnableCustomIndentLevels {
			get;
			set;
		}

		#endregion

		#region ICloneable implementation

		object ICloneable.Clone ()
		{
			return Clone ();
		}

		#endregion
	}
}

