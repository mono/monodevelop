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
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	public class TextEditorOptions
	{
		public const string DEFAULT_FONT = "Mono 10";
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
		bool enableSyntaxHighlighting = true;
		bool highlightMatchingBracket = true;
		bool highlightCaretLine = false;
		bool removeTrailingWhitespaces = true;
		
		string fontName = DEFAULT_FONT;
		string colorStyle = "Default";
		
		double zoom = 1.0;
		
		public double Zoom {
			get {
				return zoom;
			}
			set {
				zoom = value;
				OnChanged (EventArgs.Empty);
			}
		}
		public bool CanZoomIn {
			get {
				return zoom < 8.0;
			}
		}
		public bool CanZoomOut {
			get {
				return zoom > 0.7;
			}
		}
		public bool CanResetZoom {
			get {
				return zoom != 1.0;
			}
		}
		public void ZoomIn ()
		{
			zoom *= 1.1;
			Zoom = System.Math.Min (8.0, System.Math.Max (0.7, zoom));
		}
		
		public void ZoomOut ()
		{
			zoom *= 0.9;
			Zoom = System.Math.Min (8.0, System.Math.Max (0.7, zoom));
		}
		
		public void ZoomReset ()
		{
			Zoom = 1.0;
		}
		
		public string IndentationString {
			get {
				return this.tabsToSpaces ? new string (' ', this.TabSize) : "\t";
			}
		}
		
		public virtual bool HighlightMatchingBracket {
			get {
				return highlightMatchingBracket;
			}
			set {
				if (value != HighlightMatchingBracket) {
					highlightMatchingBracket = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public virtual bool RemoveTrailingWhitespaces {
			get {
				return removeTrailingWhitespaces;
			}
			set {
				removeTrailingWhitespaces = value;
			}
		}
		
		public virtual bool TabsToSpaces {
			get {
				return tabsToSpaces;
			}
			set {
				tabsToSpaces = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual int IndentationSize {
			get {
				return indentationSize;
			}
			set {
				indentationSize = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual int TabSize {
			get {
				return tabSize;
			}
			set {
				tabSize = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual bool ShowIconMargin {
			get {
				return showIconMargin;
			}
			set {
				showIconMargin = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual bool ShowLineNumberMargin {
			get {
				return showLineNumberMargin;
			}
			set {
				showLineNumberMargin = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual bool ShowFoldMargin {
			get {
				return showFoldMargin;
			}
			set {
				showFoldMargin = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual bool ShowInvalidLines {
			get {
				return showInvalidLines;
			}
			set {
				showInvalidLines = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual bool ShowTabs {
			get {
				return showTabs;
			}
			set {
				showTabs = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual bool ShowEolMarkers {
			get {
				return showEolMarkers;
			}
			set {
				showEolMarkers = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual bool HighlightCaretLine {
			get {
				return highlightCaretLine;
			}
			set {
				highlightCaretLine = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual bool ShowSpaces {
			get {
				return showSpaces;
			}
			set {
				showSpaces = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual int RulerColumn {
			get {
				return rulerColumn;
			}
			set {
				rulerColumn = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual bool ShowRuler {
			get {
				return showRuler;
			}
			set {
				showRuler = value;
				OnChanged (EventArgs.Empty);
			}
		}

		public virtual bool AutoIndent {
			get {
				return autoIndent;
			}
			set {
				autoIndent = value;
				OnChanged (EventArgs.Empty);
			}
		}
		
		public virtual string FontName {
			get {
				return fontName;
			}
			set {
				if (fontName != value) {
					fontName = !String.IsNullOrEmpty (value) ? value : DEFAULT_FONT;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public virtual bool EnableSyntaxHighlighting {
			get {
				return enableSyntaxHighlighting;
			}
			set {
				if (value != EnableSyntaxHighlighting) {
					enableSyntaxHighlighting = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public Pango.FontDescription Font {
			get {
				Pango.FontDescription result = null;
				try {
					result = Pango.FontDescription.FromString (FontName);
				} catch {
					Console.WriteLine ("Could not load font: {0}", FontName);
				}
				if (result == null || String.IsNullOrEmpty (result.Family))
					result = Pango.FontDescription.FromString (DEFAULT_FONT);
				if (result != null)
					result.Size = (int)(result.Size * Zoom);
				return result;
			}
		}
		
		public virtual string ColorSheme {
			get {
				return colorStyle;
			}
			set {
				colorStyle = value;
				OnChanged (EventArgs.Empty);
			}
		}
		public virtual Style GetColorStyle (Gtk.Widget widget)
		{
			return SyntaxModeService.GetColorStyle (widget, ColorSheme);
		}
		
		public virtual void CopyFrom (TextEditorOptions other)
		{
			Zoom = other.Zoom;
			HighlightMatchingBracket = other.HighlightMatchingBracket;
			TabsToSpaces = other.TabsToSpaces;
			IndentationSize = other.IndentationSize;
			TabSize = other.TabSize;
			ShowIconMargin = other.ShowIconMargin;
			ShowLineNumberMargin = other.ShowLineNumberMargin;
			ShowFoldMargin = other.ShowFoldMargin;
			ShowInvalidLines = other.ShowInvalidLines;
			ShowTabs = other.ShowTabs;
			ShowEolMarkers = other.ShowEolMarkers;
			HighlightCaretLine = other.HighlightCaretLine;
			ShowSpaces = other.ShowSpaces;
			RulerColumn = other.RulerColumn;
			ShowRuler = other.ShowRuler;
			AutoIndent = other.AutoIndent;
			FontName = other.FontName;
			EnableSyntaxHighlighting = other.EnableSyntaxHighlighting;
			ColorSheme = other.ColorSheme;
		}
		
		protected void OnChanged (EventArgs args)
		{
			if (Changed != null)
				Changed (null, args);
		}
		
		public event EventHandler Changed;
	}
}
