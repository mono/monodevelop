//
// DefaultSourceEditorOptions.cs
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Ide.Editor.Extension;

namespace MonoDevelop.Ide.Editor
{
	public enum ControlLeftRightMode {
		MonoDevelop,
		Emacs,
		SharpDevelop
	}

	public enum LineEndingConversion {
		Ask,
		LeaveAsIs,
		ConvertAlways
	}
	
	/// <summary>
	/// This class contains all text editor options from ITextEditorOptions and additional options
	/// the text editor frontend may use.  
	/// </summary>
	public class DefaultSourceEditorOptions : ITextEditorOptions
	{
		static DefaultSourceEditorOptions instance;
		//static TextStylePolicy defaultPolicy;
		static bool inited;

		public static DefaultSourceEditorOptions Instance {
			get { return instance; }
		}

		static DefaultSourceEditorOptions ()
		{
			Init ();
		}

		public static void Init ()
		{
			if (inited)
				return;
			inited = true;

			var policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> ("text/plain");
			instance = new DefaultSourceEditorOptions (policy);
			MonoDevelop.Projects.Policies.PolicyService.DefaultPolicies.PolicyChanged += instance.HandlePolicyChanged;
		}

		void HandlePolicyChanged (object sender, MonoDevelop.Projects.Policies.PolicyChangedEventArgs args)
		{
			TextStylePolicy pol = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> ("text/plain");
			UpdateStylePolicy (pol);
		}

		DefaultSourceEditorOptions (TextStylePolicy currentPolicy)
		{
			var defaultControlMode = (ControlLeftRightMode)Enum.Parse (typeof(ControlLeftRightMode), DesktopService.DefaultControlLeftRightBehavior);
			controlLeftRightMode = new PropertyWrapper<ControlLeftRightMode> ("ControlLeftRightMode", defaultControlMode);
			UpdateStylePolicy (currentPolicy);
			FontService.RegisterFontChangedCallback ("Editor", UpdateFont);
			FontService.RegisterFontChangedCallback ("Editor(Gutter)", UpdateFont);
			FontService.RegisterFontChangedCallback ("MessageBubbles", UpdateFont);
		}

		void UpdateFont ()
		{
			this.OnChanged (EventArgs.Empty);
		}

		void UpdateStylePolicy (MonoDevelop.Ide.Gui.Content.TextStylePolicy currentPolicy)
		{
			defaultEolMarker = TextStylePolicy.GetEolMarker (currentPolicy.EolMarker);
			tabsToSpaces          = currentPolicy.TabsToSpaces; // PropertyService.Get ("TabsToSpaces", false);
			indentationSize       = currentPolicy.TabWidth; //PropertyService.Get ("TabIndent", 4);
			rulerColumn           = currentPolicy.FileWidth; //PropertyService.Get ("RulerColumn", 80);
			allowTabsAfterNonTabs = !currentPolicy.NoTabsAfterNonTabs; //PropertyService.Get ("AllowTabsAfterNonTabs", true);
			removeTrailingWhitespaces = currentPolicy.RemoveTrailingWhitespace; //PropertyService.Get ("RemoveTrailingWhitespaces", true);
			OnChanged (EventArgs.Empty);
		}


		#region new options

		public bool EnableAutoCodeCompletion {
			get { return CompletionTextEditorExtension.EnableAutoCodeCompletion; }
			set { CompletionTextEditorExtension.EnableAutoCodeCompletion.Set (value); }
		}

		PropertyWrapper<bool> defaultRegionsFolding = new PropertyWrapper<bool> ("DefaultRegionsFolding", false);
		public bool DefaultRegionsFolding {
			get {
				return defaultRegionsFolding;
			}
			set {
				if (defaultRegionsFolding.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}
		
		PropertyWrapper<bool> defaultCommentFolding = new PropertyWrapper<bool> ("DefaultCommentFolding", true);
		public bool DefaultCommentFolding {
			get {
				return defaultCommentFolding;
			}
			set {
				if (defaultCommentFolding.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<bool> enableSemanticHighlighting = new PropertyWrapper<bool> ("EnableSemanticHighlighting", true);
		public bool EnableSemanticHighlighting {
			get {
				return enableSemanticHighlighting;
			}
			set {
				if (enableSemanticHighlighting.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<bool> tabIsReindent = new PropertyWrapper<bool> ("TabIsReindent", false);
		public bool TabIsReindent {
			get {
				return tabIsReindent;
			}
			set {
				if (tabIsReindent.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<bool> autoInsertMatchingBracket = new PropertyWrapper<bool> ("AutoInsertMatchingBracket", false);
		public bool AutoInsertMatchingBracket {
			get {
				return autoInsertMatchingBracket;
			}
			set {
				if (autoInsertMatchingBracket.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<bool> smartSemicolonPlacement = new PropertyWrapper<bool> ("SmartSemicolonPlacement", false);
		public bool SmartSemicolonPlacement {
			get {
				return smartSemicolonPlacement;
			}
			set {
				if (smartSemicolonPlacement.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}
		
		PropertyWrapper<bool> underlineErrors = new PropertyWrapper<bool> ("UnderlineErrors", true);
		public bool UnderlineErrors {
			get {
				return underlineErrors; 
			}
			set {
				if (underlineErrors.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<IndentStyle> indentStyle = new PropertyWrapper<IndentStyle> ("IndentStyle", IndentStyle.Smart);
		public IndentStyle IndentStyle {
			get {
				return indentStyle;
			}
			set {
				if (indentStyle.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<bool> enableHighlightUsages = new PropertyWrapper<bool> ("EnableHighlightUsages", false);
		public bool EnableHighlightUsages {
			get {
				return enableHighlightUsages;
			}
			set {
				if (enableHighlightUsages.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}
		
		PropertyWrapper<LineEndingConversion> lineEndingConversion = new PropertyWrapper<LineEndingConversion> ("LineEndingConversion", LineEndingConversion.Ask);
		public LineEndingConversion LineEndingConversion {
			get {
				return lineEndingConversion;
			}
			set {
				if (lineEndingConversion.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		#endregion

		PropertyWrapper<bool> useViModes = new PropertyWrapper<bool> ("UseViModes", true);
		public bool UseViModes {
			get {
				return useViModes;
			}
			set {
				if (useViModes.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<bool> onTheFlyFormatting = new PropertyWrapper<bool> ("OnTheFlyFormatting", true);
		public bool OnTheFlyFormatting {
			get {
				return onTheFlyFormatting;
			}
			set {
				if (onTheFlyFormatting.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		#region ITextEditorOptions
		
		double zoom = 1d;
		const double ZOOM_FACTOR = 1.1f;
		const int ZOOM_MIN_POW = -4;
		const int ZOOM_MAX_POW = 8;
		static readonly double ZOOM_MIN = System.Math.Pow (ZOOM_FACTOR, ZOOM_MIN_POW);
		static readonly double ZOOM_MAX = System.Math.Pow (ZOOM_FACTOR, ZOOM_MAX_POW);

		public double Zoom {
			get {
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
				if (zoom != value) {
					zoom = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		string defaultEolMarker = Environment.NewLine;
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

		PropertyWrapper<ControlLeftRightMode> controlLeftRightMode;
		public ControlLeftRightMode ControlLeftRightMode {
			get {
				return controlLeftRightMode;
			}
			set {
				if (controlLeftRightMode.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		public WordFindStrategy WordFindStrategy {
			get {
				if (useViModes) {
					return WordFindStrategy.Vim;
				}
				switch (ControlLeftRightMode) {
				case ControlLeftRightMode.Emacs:
					return WordFindStrategy.Emacs;
				case ControlLeftRightMode.SharpDevelop:
					return WordFindStrategy.SharpDevelop;
				}
				return WordFindStrategy.MonoDevelop;
			}
			set {
				throw new System.NotImplementedException ();
			}
		}
		
		bool allowTabsAfterNonTabs = true;
		public bool AllowTabsAfterNonTabs {
			get {
				return allowTabsAfterNonTabs;
			}
			set {
				if (allowTabsAfterNonTabs != value) {
					PropertyService.Set ("AllowTabsAfterNonTabs", value);
					allowTabsAfterNonTabs = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool tabsToSpaces = false;
		public bool TabsToSpaces {
			get {
				return tabsToSpaces;
			}
			set {
				if (tabsToSpaces != value) {
					PropertyService.Set ("TabsToSpaces", value);
					tabsToSpaces = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		int indentationSize = 4;
		public virtual int IndentationSize {
			get {
				return indentationSize;
			}
			set {
				if (indentationSize != value) {
					PropertyService.Set ("TabIndent", value);
					indentationSize = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		
		public string IndentationString {
			get {
				return TabsToSpaces ? new string (' ', this.TabSize) : "\t";
			}
		}
		
		public int TabSize {
			get {
				return IndentationSize;
			}
			set {
				IndentationSize = value;
			}
		}

		
		bool removeTrailingWhitespaces = true;
		public bool RemoveTrailingWhitespaces {
			get {
				return removeTrailingWhitespaces;
			}
			set {
				if (removeTrailingWhitespaces != value) {
					PropertyService.Set ("RemoveTrailingWhitespaces", value);
					OnChanged (EventArgs.Empty);
					removeTrailingWhitespaces = value;
				}
			}
		}
		
		PropertyWrapper<bool> showLineNumberMargin = new PropertyWrapper<bool> ("ShowLineNumberMargin", true);
		public bool ShowLineNumberMargin {
			get {
				return showLineNumberMargin;
			}
			set {
				if (showLineNumberMargin.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}
		
		PropertyWrapper<bool> showFoldMargin = new PropertyWrapper<bool> ("ShowFoldMargin", false);
		public bool ShowFoldMargin {
			get {
				return showFoldMargin;
			}
			set {
				if (showFoldMargin.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}
		
		bool showIconMargin = true;
		public virtual bool ShowIconMargin {
			get {
				return showIconMargin;
			}
			set {
				if (showIconMargin != value) {
					PropertyService.Set ("ShowIconMargin", value);
					showIconMargin = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		PropertyWrapper<bool> highlightCaretLine = new PropertyWrapper<bool> ("HighlightCaretLine", false);
		public bool HighlightCaretLine {
			get {
				return highlightCaretLine;
			}
			set {
				if (highlightCaretLine.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<bool> enableSyntaxHighlighting = new PropertyWrapper<bool> ("EnableSyntaxHighlighting", true);
		public bool EnableSyntaxHighlighting {
			get {
				return enableSyntaxHighlighting;
			}
			set {
				if (enableSyntaxHighlighting.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<bool> highlightMatchingBracket = new PropertyWrapper<bool> ("HighlightMatchingBracket", true);
		public bool HighlightMatchingBracket {
			get {
				return highlightMatchingBracket;
			}
			set {
				if (highlightMatchingBracket.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		int  rulerColumn = 80;

		public virtual int RulerColumn {
			get {
				return rulerColumn;
			}
			set {
				if (rulerColumn != value) {
					PropertyService.Set ("RulerColumn", value);
					rulerColumn = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		PropertyWrapper<bool> showRuler = new PropertyWrapper<bool> ("ShowRuler", true);
		public bool ShowRuler {
			get {
				return showRuler;
			}
			set {
				if (showRuler.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<bool> enableAnimations = new PropertyWrapper<bool> ("EnableAnimations", true);
		public bool EnableAnimations {
			get { 
				return enableAnimations; 
			}
			set {
				if (enableAnimations.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}
		
		PropertyWrapper<bool> drawIndentationMarkers = new PropertyWrapper<bool> ("DrawIndentationMarkers", false);
		public bool DrawIndentationMarkers {
			get {
				return drawIndentationMarkers;
			}
			set {
				if (drawIndentationMarkers.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<bool> wrapLines = new PropertyWrapper<bool> ("WrapLines", false);
		public bool WrapLines {
			get {
				return wrapLines;
			}
			set {
				if (wrapLines.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		PropertyWrapper<bool> enableQuickDiff = new PropertyWrapper<bool> ("EnableQuickDiff", false);
		public bool EnableQuickDiff {
			get {
				return enableQuickDiff;
			}
			set {
				if (enableQuickDiff.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		public string FontName {
			get {
				return FontService.FilterFontName (FontService.GetUnderlyingFontName ("Editor"));
			}
			set {
				throw new InvalidOperationException ("Set font through font service");
			}
		}

		public string GutterFontName {
			get {
				return FontService.FilterFontName (FontService.GetUnderlyingFontName ("Editor(Gutter)"));
			}
			set {
				throw new InvalidOperationException ("Set font through font service");
			}
		}
		
		PropertyWrapper<string> colorScheme = new PropertyWrapper<string> ("ColorScheme", "Default");
		public string ColorScheme {
			get {
				return colorScheme;
			}
			set {
				if (colorScheme.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}
		
		PropertyWrapper<bool> generateFormattingUndoStep = new PropertyWrapper<bool> ("GenerateFormattingUndoStep", false);
		public bool GenerateFormattingUndoStep {
			get {
				return generateFormattingUndoStep;
			}
			set {
				if (generateFormattingUndoStep.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}
		
		bool overrideDocumentEolMarker = false;
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
		#endregion
		
		public virtual void Dispose ()
		{
			FontService.RemoveCallback (UpdateFont);
		}

		protected void OnChanged (EventArgs args)
		{
			if (Changed != null)
				Changed (null, args);
		}

		public event EventHandler Changed;
	}
}

