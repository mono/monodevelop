// TextEditorOptions.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Diagnostics;

namespace Mono.TextEditor
{
	public class TextEditorOptions
	{
		static TextEditorOptions options = new TextEditorOptions ();
		public static TextEditorOptions Options {
			get {
				return options;
			}
			set {
				Debug.Assert (value != null);
				options = value;
			}
		}
		
		int indentationSize = 4;
		int  tabSize = 4;
		bool tabsToSpaces = false;
		bool showIconMargin = true;
		bool showLineNumberMargin = true;
		bool showFoldMargin = true;
		bool showInvalidLines = true;
		bool autoIndent = true;

		int  rulerColumn = 80;
		bool showRuler = false;
		
		bool showTabs   = false;
		bool showSpaces = false;
		bool showEolMarkers = false;
		
		bool highlightCaretLine = false;
		
		public bool TabsToSpaces {
			get {
				return tabsToSpaces;
			}
			set {
				tabsToSpaces = value;
			}
		}

		public int IndentationSize {
			get {
				return indentationSize;
			}
			set {
				indentationSize = value;
			}
		}

		public int TabSize {
			get {
				return tabSize;
			}
			set {
				tabSize = value;
			}
		}

		public bool ShowIconMargin {
			get {
				return showIconMargin;
			}
			set {
				showIconMargin = value;
			}
		}

		public bool ShowLineNumberMargin {
			get {
				return showLineNumberMargin;
			}
			set {
				showLineNumberMargin = value;
			}
		}

		public bool ShowFoldMargin {
			get {
				return showFoldMargin;
			}
			set {
				showFoldMargin = value;
			}
		}

		public bool ShowInvalidLines {
			get {
				return showInvalidLines;
			}
			set {
				showInvalidLines = value;
			}
		}

		public bool ShowTabs {
			get {
				return showTabs;
			}
			set {
				showTabs = value;
			}
		}

		public bool ShowEolMarkers {
			get {
				return showEolMarkers;
			}
			set {
				showEolMarkers = value;
			}
		}

		public bool HighlightCaretLine {
			get {
				return highlightCaretLine;
			}
			set {
				highlightCaretLine = value;
			}
		}

		public bool ShowSpaces {
			get {
				return showSpaces;
			}
			set {
				showSpaces = value;
			}
		}

		public int RulerColumn {
			get {
				return rulerColumn;
			}
			set {
				rulerColumn = value;
			}
		}

		public bool ShowRuler {
			get {
				return showRuler;
			}
			set {
				showRuler = value;
			}
		}

		public bool AutoIndent {
			get {
				return autoIndent;
			}
			set {
				autoIndent = value;
			}
		}
		
	}
}
