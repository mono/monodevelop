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
using MonoDevelop.Ide.Editor.Highlighting;

namespace Mono.TextEditor
{
	class TextEditorOptions : ITextEditorOptions
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
		IndentStyle indentStyle = IndentStyle.Virtual;
		
		int  rulerColumn = 80;
		bool showRuler = false;
		
		bool enableSyntaxHighlighting = true;
		bool highlightMatchingBracket = true;
		bool highlightCaretLine = false;
		bool removeTrailingWhitespaces = true;
		bool allowTabsAfterNonTabs = true;
		string fontName = DEFAULT_FONT;
		public static string DefaultColorStyle = "Light";
		string colorStyle = DefaultColorStyle;
		Pango.FontDescription font, gutterFont;
		
		IWordFindStrategy wordFindStrategy = new EmacsWordFindStrategy (true);

		#region Zoom
		static double zoom = 1d;
		static event EventHandler StaticZoomChanged;

		const double ZOOM_FACTOR = 1.1f;
		const int ZOOM_MIN_POW = -4;
		const int ZOOM_MAX_POW = 8;
		static readonly double ZOOM_MIN = System.Math.Pow (ZOOM_FACTOR, ZOOM_MIN_POW);
		static readonly double ZOOM_MAX = System.Math.Pow (ZOOM_FACTOR, ZOOM_MAX_POW);


		double myZoom = 1d;
		public bool ZoomOverride { get; private set; }


		public double Zoom {
			get {
				if (ZoomOverride)
					return myZoom;
				return zoom;
			}
			set {
				value = System.Math.Min (ZOOM_MAX, System.Math.Max (ZOOM_MIN, value));
				if (value > ZOOM_MAX || value < ZOOM_MIN)
					return;
				
				//snap to one, if within 0.001d
				if ((System.Math.Abs (value - 1d)) < 0.001d) {
					value = 1d;
				}
				if (ZoomOverride) {
					if (myZoom != value) {
						myZoom = value;
						DisposeFont ();
						ZoomChanged?.Invoke (this, EventArgs.Empty);
						OnChanged (EventArgs.Empty);
					}
					return;
				}
				if (zoom != value) {
					zoom = value;
					StaticZoomChanged?.Invoke (this, EventArgs.Empty);
				}
			}
		}
		public event EventHandler ZoomChanged;

		public bool CanZoomIn {
			get {
				return Zoom < ZOOM_MAX - 0.000001d;
			}
		}
		
		public bool CanZoomOut {
			get {
				return Zoom > ZOOM_MIN + 0.000001d;
			}
		}
		
		public bool CanResetZoom {
			get {
				return Zoom != 1d;
			}
		}
		
		public void ZoomIn ()
		{
			int oldPow = (int)System.Math.Round (System.Math.Log (Zoom) / System.Math.Log (ZOOM_FACTOR));
			Zoom = System.Math.Pow (ZOOM_FACTOR, oldPow + 1);
		}
		
		public void ZoomOut ()
		{
			int oldPow = (int)System.Math.Round (System.Math.Log (Zoom) / System.Math.Log (ZOOM_FACTOR));
			Zoom = System.Math.Pow (ZOOM_FACTOR, oldPow - 1);
		}
		
		public void ZoomReset ()
		{
			Zoom = 1d;
		}

		#endregion Zoom
		
		public string IndentationString {
			get {
				return this.TabsToSpaces ? new string (' ', this.TabSize) : "\t";
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

		public virtual IndentStyle IndentStyle {
			get {
				return indentStyle;
			}
			set {
				if (indentStyle != value) {
					indentStyle = value;
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

		protected void DisposeFont ()
		{
			if (font != null) {
				font.Dispose ();
				font = null;
			}

			if (gutterFont != null) {
				gutterFont.Dispose ();
				gutterFont = null;
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
		string gutterFontName;
		public virtual string GutterFontName {
			get {
				return gutterFontName;
			}
			set {
				if (gutterFontName != value) {
					DisposeFont ();
					gutterFontName = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public Pango.FontDescription GutterFont {
			get {
				if (gutterFont == null) {
					try {
						if (!string.IsNullOrEmpty (GutterFontName))
							gutterFont = Pango.FontDescription.FromString (GutterFontName);
					} catch {
						Console.WriteLine ("Could not load gutter font: {0}", GutterFontName);
					}
					if (gutterFont == null || String.IsNullOrEmpty (gutterFont.Family))
						gutterFont = Gtk.Widget.DefaultStyle.FontDescription.Copy ();
					if (gutterFont != null)
						gutterFont.Size = (int)(gutterFont.Size * Zoom);
				}
				return gutterFont;
			}
		}

		public virtual string EditorThemeName {
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
		
		bool drawIndentationMarkers = false;
		public virtual bool DrawIndentationMarkers {
			get {
				return drawIndentationMarkers;
			}
			set {
				if (drawIndentationMarkers != value) {
					drawIndentationMarkers = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		ShowWhitespaces showWhitespaces = ShowWhitespaces.Never;
		public virtual ShowWhitespaces ShowWhitespaces {
			get {
				return showWhitespaces;
			}
			set {
				if (showWhitespaces != value) {
					showWhitespaces = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		IncludeWhitespaces includeWhitespaces = IncludeWhitespaces.All;
		public virtual IncludeWhitespaces IncludeWhitespaces {
			get {
				return includeWhitespaces;
			}
			set {
				if (includeWhitespaces != value) {
					includeWhitespaces = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		bool wrapLines = false;
		public virtual bool WrapLines {
			get {
				return false;
//				return wrapLines;
			}
			set {
				if (wrapLines != value) {
					wrapLines = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		bool enableQuickDiff = true;
		public virtual bool EnableQuickDiff {
			get {
				return enableQuickDiff;
			}
			set {
				if (enableQuickDiff != value) {
					enableQuickDiff = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		bool enableSelectionWrappingKeys = true;
		public virtual bool EnableSelectionWrappingKeys {
			get {
				return enableSelectionWrappingKeys;
			}
			set {
				if (enableSelectionWrappingKeys != value) {
					enableSelectionWrappingKeys = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		bool generateFormattingUndoStep;
		public virtual bool GenerateFormattingUndoStep {
			get {
				return generateFormattingUndoStep;
			}
			set {
				if (generateFormattingUndoStep != value) {
					generateFormattingUndoStep = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		bool smartBackspace = true;
		public virtual bool SmartBackspace {
			get {
				return smartBackspace;
			}
			set {
				if (smartBackspace != value) {
					smartBackspace = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		public virtual MonoDevelop.Ide.Editor.Highlighting.EditorTheme GetEditorTheme ()
		{
			return SyntaxHighlightingService.GetEditorTheme (EditorThemeName);
		}
		
		public virtual void CopyFrom (TextEditorOptions other)
		{
			Zoom = other.Zoom;
			highlightMatchingBracket = other.HighlightMatchingBracket;
			tabsToSpaces = other.TabsToSpaces;
			indentationSize = other.IndentationSize;
			tabSize = other.TabSize;
			showIconMargin = other.ShowIconMargin;
			showLineNumberMargin = other.ShowLineNumberMargin;
			showFoldMargin = other.ShowFoldMargin;
			highlightCaretLine = other.HighlightCaretLine;
			rulerColumn = other.RulerColumn;
			showRuler = other.ShowRuler;
			indentStyle = other.IndentStyle;
			fontName = other.FontName;
			enableSyntaxHighlighting = other.EnableSyntaxHighlighting;
			colorStyle = other.colorStyle;
			overrideDocumentEolMarker = other.OverrideDocumentEolMarker;
			defaultEolMarker = other.DefaultEolMarker;
			enableAnimations = other.EnableAnimations;
			drawIndentationMarkers = other.DrawIndentationMarkers;
			showWhitespaces = other.ShowWhitespaces;
			includeWhitespaces = other.IncludeWhitespaces;
			generateFormattingUndoStep = other.GenerateFormattingUndoStep;
			smartBackspace = other.SmartBackspace;
			DisposeFont ();
			OnChanged (EventArgs.Empty);
		}

		public TextEditorOptions (bool zoomOverride = false)
		{
			ZoomOverride = zoomOverride;
			if (!ZoomOverride)
				StaticZoomChanged += HandleStaticZoomChanged;
		}

		public virtual void Dispose ()
		{
			if (!ZoomOverride)
				StaticZoomChanged -= HandleStaticZoomChanged;
		}

		void HandleStaticZoomChanged (object sender, EventArgs e)
		{
			DisposeFont ();
			ZoomChanged?.Invoke (this, EventArgs.Empty);
			OnChanged (EventArgs.Empty);
		}

		protected void OnChanged (EventArgs args)
		{
			if (Changed != null)
				Changed (null, args);
		}
		
		public event EventHandler Changed;
	}
}
