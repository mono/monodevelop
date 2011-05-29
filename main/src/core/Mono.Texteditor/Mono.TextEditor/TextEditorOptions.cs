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
	public class TextEditorOptions : ITextEditorOptions
	{
		public const string DEFAULT_FONT = "Mono 10";
		static TextEditorOptions options = new TextEditorOptions ();
		
		public static TextEditorOptions DefaultOptions {
			get {
				return options;
			}
		}
		
		bool overrideDocumentEolMarker = false;
		string defaultEolMarker = Environment.NewLine;
		
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
		bool allowTabsAfterNonTabs = true;
		string fontName = DEFAULT_FONT;
		string colorStyle = "text";
		Pango.FontDescription font;
		
		int zoomPow = 0;
		double zoom = 1;
		IWordFindStrategy wordFindStrategy = new EmacsWordFindStrategy (true);
		
		
		public double Zoom {
			get {
				 return zoom;
			}
			set {
				ZoomPow = (int) System.Math.Round (System.Math.Log (value) / System.Math.Log (ZOOM_FACTOR));
			}
		}
		
		int ZoomPow {
			get {
				return zoomPow;
			}
			set {
				value = System.Math.Min (ZOOM_MAX, System.Math.Max (ZOOM_MIN, value));
				if (zoomPow != value) {
					zoomPow = value;
					zoom = System.Math.Pow (ZOOM_FACTOR, zoomPow);
					DisposeFont ();
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		const int ZOOM_MIN = -4;
		const int ZOOM_MAX = 8;
		const double ZOOM_FACTOR = 1.1f;
		
		public bool CanZoomIn {
			get {
				return ZoomPow <= ZOOM_MAX;
			}
		}
		
		public bool CanZoomOut {
			get {
				return ZoomPow >= ZOOM_MIN;
			}
		}
		
		public bool CanResetZoom {
			get {
				return ZoomPow != 0;
			}
		}
		
		public void ZoomIn ()
		{
			ZoomPow++;
		}
		
		public void ZoomOut ()
		{
			ZoomPow--;
		}
		
		public void ZoomReset ()
		{
			ZoomPow = 0;
		}
		
		public string IndentationString {
			get {
				return this.tabsToSpaces ? new string (' ', this.TabSize) : "\t";
			}
		}
		
		public virtual bool OverrideDocumentEolMarker {
			get {
				return overrideDocumentEolMarker;
			}
			set {
				if (overrideDocumentEolMarker != value) {
					overrideDocumentEolMarker = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public virtual string DefaultEolMarker {
			get {
				return defaultEolMarker;
			}
			set {
				if (defaultEolMarker != value) {
					defaultEolMarker = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public virtual IWordFindStrategy WordFindStrategy {
			get {
				return wordFindStrategy;
			}
			set {
				if (wordFindStrategy != value) {
					wordFindStrategy = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public virtual bool AllowTabsAfterNonTabs {
			get {
				return allowTabsAfterNonTabs;
			}
			set {
				if (allowTabsAfterNonTabs != value) {
					allowTabsAfterNonTabs = value;
					OnChanged (EventArgs.Empty);
				}
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
				if (removeTrailingWhitespaces != value) {
					removeTrailingWhitespaces = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public virtual bool TabsToSpaces {
			get {
				return tabsToSpaces;
			}
			set {
				if (tabsToSpaces != value) {
					tabsToSpaces = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual int IndentationSize {
			get {
				return indentationSize;
			}
			set {
				if (indentationSize != value) {
					indentationSize = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual int TabSize {
			get {
				return tabSize;
			}
			set {
				if (tabSize != value) {
					tabSize = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual bool ShowIconMargin {
			get {
				return showIconMargin;
			}
			set {
				if (showIconMargin != value) {
					showIconMargin = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual bool ShowLineNumberMargin {
			get {
				return showLineNumberMargin;
			}
			set {
				if (showLineNumberMargin != value) {
					showLineNumberMargin = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual bool ShowFoldMargin {
			get {
				return showFoldMargin;
			}
			set {
				if (showFoldMargin != value) {
					showFoldMargin = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual bool ShowInvalidLines {
			get {
				return showInvalidLines;
			}
			set {
				if (showInvalidLines != value) {
					showInvalidLines = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual bool ShowTabs {
			get {
				return showTabs;
			}
			set {
				if (showTabs != value) {
					showTabs = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual bool ShowEolMarkers {
			get {
				return showEolMarkers;
			}
			set {
				if (showEolMarkers != value) {
					showEolMarkers = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual bool HighlightCaretLine {
			get {
				return highlightCaretLine;
			}
			set {
				if (highlightCaretLine != value) {
					highlightCaretLine = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual bool ShowSpaces {
			get {
				return showSpaces;
			}
			set {
				if (showSpaces != value) {
					showSpaces = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual int RulerColumn {
			get {
				return rulerColumn;
			}
			set {
				if (rulerColumn != value) {
					rulerColumn = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual bool ShowRuler {
			get {
				return showRuler;
			}
			set {
				if (showRuler != value) {
					showRuler = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual bool AutoIndent {
			get {
				return autoIndent;
			}
			set {
				if (autoIndent != value) {
					autoIndent = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public virtual string FontName {
			get {
				return fontName;
			}
			set {
				if (fontName != value) {
					DisposeFont ();
					fontName = !String.IsNullOrEmpty (value) ? value : DEFAULT_FONT;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		void DisposeFont ()
		{
			if (font != null) {
				font.Dispose ();
				font = null;
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
				if (font == null) {
					try {
						font = Pango.FontDescription.FromString (FontName);
					} catch {
						Console.WriteLine ("Could not load font: {0}", FontName);
					}
					if (font == null || String.IsNullOrEmpty (font.Family))
						font = Pango.FontDescription.FromString (DEFAULT_FONT);
					if (font != null)
						font.Size = (int)(font.Size * Zoom);
				}
				return font;
			}
		}
		
		public virtual string ColorScheme {
			get {
				return colorStyle;
			}
			set {
				if (colorStyle != value) {
					colorStyle = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool enableAnimations = true;
		public virtual bool EnableAnimations {
			get { 
				return enableAnimations; 
			}
			set {
				if (enableAnimations != value) {
					enableAnimations = value; 
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		public virtual ColorSheme GetColorStyle (Gtk.Style widgetStyle)
		{
			return SyntaxModeService.GetColorStyle (widgetStyle, ColorScheme);
		}
		
		public virtual void CopyFrom (TextEditorOptions other)
		{
			zoom = other.zoom;
			highlightMatchingBracket = other.highlightMatchingBracket;
			tabsToSpaces = other.tabsToSpaces;
			indentationSize = other.indentationSize;
			tabSize = other.tabSize;
			showIconMargin = other.showIconMargin;
			showLineNumberMargin = other.showLineNumberMargin;
			showFoldMargin = other.showFoldMargin;
			showInvalidLines = other.showInvalidLines;
			showTabs = other.showTabs;
			showEolMarkers = other.showEolMarkers;
			highlightCaretLine = other.highlightCaretLine;
			showSpaces = other.showSpaces;
			rulerColumn = other.rulerColumn;
			showRuler = other.showRuler;
			autoIndent = other.autoIndent;
			fontName = other.fontName;
			enableSyntaxHighlighting = other.enableSyntaxHighlighting;
			colorStyle = other.colorStyle;
			overrideDocumentEolMarker = other.overrideDocumentEolMarker;
			defaultEolMarker = other.defaultEolMarker;
			enableAnimations = other.enableAnimations;
			DisposeFont ();
			OnChanged (EventArgs.Empty);
		}
		
		public virtual void Dispose ()
		{
		}
		
		protected void OnChanged (EventArgs args)
		{
			if (Changed != null)
				Changed (null, args);
		}
		
		public event EventHandler Changed;
	}
}
