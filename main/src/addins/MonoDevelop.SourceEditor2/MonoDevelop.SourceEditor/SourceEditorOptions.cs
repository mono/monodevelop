// SourceEditorOptions.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

using Mono.TextEditor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.SourceEditor
{
	public enum ControlLeftRightMode {
		MonoDevelop,
		Emacs,
		SharpDevelop
		
	}
	
	public class DefaultSourceEditorOptions : TextEditorOptions, ISourceEditorOptions
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
			
			TextStylePolicy policy = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> ("text/plain");
			instance = new DefaultSourceEditorOptions (policy);
			MonoDevelop.Projects.Policies.PolicyService.DefaultPolicies.PolicyChanged += instance.HandlePolicyChanged;
		}

		void HandlePolicyChanged (object sender, MonoDevelop.Projects.Policies.PolicyChangedEventArgs args)
		{
			TextStylePolicy pol = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> ("text/plain");
			UpdateStylePolicy (pol);
		}
		
		DefaultSourceEditorOptions (MonoDevelop.Ide.Gui.Content.TextStylePolicy currentPolicy)
		{
			LoadAllPrefs ();
			UpdateStylePolicy (currentPolicy);
			PropertyService.PropertyChanged += UpdatePreferences;
			FontService.RegisterFontChangedCallback ("Editor", UpdateFont);
			FontService.RegisterFontChangedCallback ("MessageBubbles", UpdateFont);
			
		}
		
		public override void Dispose()
		{
			PropertyService.PropertyChanged -= UpdatePreferences;
			FontService.RemoveCallback (UpdateFont);
		}
		
		void UpdateFont ()
		{
			base.FontName = FontName;
			this.OnChanged (EventArgs.Empty);
		
		}
		
		void UpdateStylePolicy (MonoDevelop.Ide.Gui.Content.TextStylePolicy currentPolicy)
		{
			this.defaultEolMarker = TextStylePolicy.GetEolMarker (currentPolicy.EolMarker);
			
			base.TabsToSpaces          = currentPolicy.TabsToSpaces; // PropertyService.Get ("TabsToSpaces", false);
			base.IndentationSize       = currentPolicy.TabWidth; //PropertyService.Get ("TabIndent", 4);
			base.RulerColumn           = currentPolicy.FileWidth; //PropertyService.Get ("RulerColumn", 80);
			base.AllowTabsAfterNonTabs = !currentPolicy.NoTabsAfterNonTabs; //PropertyService.Get ("AllowTabsAfterNonTabs", true);
			base.RemoveTrailingWhitespaces = currentPolicy.RemoveTrailingWhitespace; //PropertyService.Get ("RemoveTrailingWhitespaces", true);
		}
		
		// Need to be picky about only updating individual properties when they change.
		// The old approach called LoadAllPrefs on any prefs event, which sometimes caused 
		// massive change event storms.
		void UpdatePreferences (object sender, PropertyChangedEventArgs args)
		{
			try {
			switch (args.Key) {
			case "TabIsReindent": 
				this.TabIsReindent = (bool) args.NewValue;
				break;
			case "EnableSemanticHighlighting":
				this.EnableSemanticHighlighting = (bool) args.NewValue;
				break;
			case "AutoInsertMatchingBracket":
				this.AutoInsertMatchingBracket = (bool) args.NewValue;
				break;
			case "EnableCodeCompletion":
				this.EnableCodeCompletion = (bool) args.NewValue;
				break;
			case "EnableParameterInsight":
				this.EnableParameterInsight = (bool) args.NewValue;
				break;
			case "UnderlineErrors":
				this.UnderlineErrors = (bool) args.NewValue;
				break;
			case "IndentStyle":
				if (args.NewValue == null) {
					LoggingService.LogWarning ("tried to set indent style == null");
				} else if (!(args.NewValue is MonoDevelop.Ide.Gui.Content.IndentStyle)) {
					LoggingService.LogWarning ("tried to set indent style to " + args.NewValue + " which isn't from type IndentStyle instead it is from:" +  args.NewValue.GetType ());
					this.IndentStyle = (MonoDevelop.Ide.Gui.Content.IndentStyle)Enum.Parse (typeof (MonoDevelop.Ide.Gui.Content.IndentStyle), args.NewValue.ToString ());
				} else 
					this.IndentStyle = (MonoDevelop.Ide.Gui.Content.IndentStyle) args.NewValue;
				break;
			case "ShowLineNumberMargin":
				base.ShowLineNumberMargin = (bool) args.NewValue;
				break;
			case "ShowFoldMargin":
				base.ShowFoldMargin = (bool) args.NewValue;
				break;
			case "ShowInvalidLines":
				base.ShowInvalidLines = (bool) args.NewValue;
				break;
			case "ShowTabs":
				base.ShowTabs = (bool) args.NewValue;
				break;
			case "ShowEolMarkers":
				base.ShowEolMarkers = (bool) args.NewValue;
				break;
			case "HighlightCaretLine":
				base.HighlightCaretLine = (bool) args.NewValue;
				break;
			case "ShowSpaces":
				base.ShowSpaces = (bool) args.NewValue;
				break;
			case "EnableSyntaxHighlighting":
				base.EnableSyntaxHighlighting = (bool) args.NewValue;
				break;
			case "HighlightMatchingBracket":
				base.HighlightMatchingBracket = (bool) args.NewValue;
				break;
			case "ShowRuler":
				base.ShowRuler = (bool) args.NewValue;
				break;
			case "FontName":
				base.FontName = (string) args.NewValue;
				break;
			case "ColorScheme":
				base.ColorScheme = (string) args.NewValue;
				break;
			case "DefaultRegionsFolding":
				this.DefaultRegionsFolding = (bool) args.NewValue;
				break;
			case "DefaultCommentFolding":
				this.DefaultCommentFolding = (bool) args.NewValue;
				break;
			case "UseViModes":
				this.UseViModes = (bool) args.NewValue;
				break;
			case "OnTheFlyFormatting":
				this.OnTheFlyFormatting = (bool) args.NewValue;
				break;
			case "EnableAutoCodeCompletion":
				this.EnableAutoCodeCompletion = (bool) args.NewValue;
				break;
			case "CompleteWithSpaceOrPunctuation":
				this.CompleteWithSpaceOrPunctuation = (bool) args.NewValue;
				break;
			case "ControlLeftRightMode":
				this.ControlLeftRightMode = (ControlLeftRightMode) args.NewValue;
				break;
			case "EnableAnimations":
				base.EnableAnimations =  (bool) args.NewValue;
				break;
			}
			} catch (Exception ex) {
				LoggingService.LogError ("SourceEditorOptions error with property value for '" + (args.Key ?? "") + "'", ex);
			}
		}
		
		void LoadAllPrefs ()
		{
			this.tabIsReindent = PropertyService.Get ("TabIsReindent", false);
			this.enableSemanticHighlighting = PropertyService.Get ("EnableSemanticHighlighting", false);
			//			this.autoInsertTemplates        = PropertyService.Get ("AutoInsertTemplates", false);
			this.autoInsertMatchingBracket = PropertyService.Get ("AutoInsertMatchingBracket", false);
			this.smartSemicolonPlacement = PropertyService.Get ("SmartSemicolonPlacement", false);
			this.enableCodeCompletion = PropertyService.Get ("EnableCodeCompletion", true);
			this.enableParameterInsight = PropertyService.Get ("EnableParameterInsight", true);
			this.underlineErrors = PropertyService.Get ("UnderlineErrors", true);
			this.indentStyle = PropertyService.Get ("IndentStyle", MonoDevelop.Ide.Gui.Content.IndentStyle.Smart);
			base.ShowLineNumberMargin = PropertyService.Get ("ShowLineNumberMargin", true);
			base.ShowFoldMargin = PropertyService.Get ("ShowFoldMargin", true);
			base.ShowInvalidLines = PropertyService.Get ("ShowInvalidLines", true);
			base.ShowTabs = PropertyService.Get ("ShowTabs", false);
			base.ShowEolMarkers = PropertyService.Get ("ShowEolMarkers", false);
			base.HighlightCaretLine = PropertyService.Get ("HighlightCaretLine", false);
			base.ShowSpaces = PropertyService.Get ("ShowSpaces", false);
			base.EnableSyntaxHighlighting = PropertyService.Get ("EnableSyntaxHighlighting", true);
			base.HighlightMatchingBracket = PropertyService.Get ("HighlightMatchingBracket", true);
			base.ShowRuler = PropertyService.Get ("ShowRuler", false);
			base.FontName = PropertyService.Get ("FontName", "Mono 10");
			base.ColorScheme = PropertyService.Get ("ColorScheme", "Default");
			this.defaultRegionsFolding = PropertyService.Get ("DefaultRegionsFolding", false);
			this.defaultCommentFolding = PropertyService.Get ("DefaultCommentFolding", true);
			this.useViModes = PropertyService.Get ("UseViModes", false);
			this.onTheFlyFormatting = PropertyService.Get ("OnTheFlyFormatting", false);
			this.enableAutoCodeCompletion = PropertyService.Get ("EnableAutoCodeCompletion", true);
			this.completeWithSpaceOrPunctuation = PropertyService.Get ("CompleteWithSpaceOrPunctuation", true);
			var defaultControlMode = (ControlLeftRightMode)Enum.Parse (typeof(ControlLeftRightMode), DesktopService.DefaultControlLeftRightBehavior);
			this.ControlLeftRightMode = PropertyService.Get ("ControlLeftRightMode", defaultControlMode);
			base.EnableAnimations = PropertyService.Get ("EnableAnimations", true);
			
		}
		
		#region new options
		
		
		bool enableAutoCodeCompletion;
		public bool EnableAutoCodeCompletion {
			get {
				return enableAutoCodeCompletion;
			}
			set {
				if (value != enableAutoCodeCompletion) {
					enableAutoCodeCompletion = value;
					PropertyService.Set ("EnableAutoCodeCompletion", value);
				}
			}
		}
		
		bool completeWithSpaceOrPunctuation;
		public bool CompleteWithSpaceOrPunctuation {
			get {
				return completeWithSpaceOrPunctuation;
			}
			set {
				if (value != completeWithSpaceOrPunctuation) {
					completeWithSpaceOrPunctuation = value;
					PropertyService.Set ("CompleteWithSpaceOrPunctuation", value);
				}
			}
		}
		
		bool defaultRegionsFolding;
		public bool DefaultRegionsFolding {
			get {
				return defaultRegionsFolding;
			}
			set {
				if (value != this.defaultRegionsFolding) {
					this.defaultRegionsFolding = value;
					PropertyService.Set ("DefaultRegionsFolding", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool defaultCommentFolding;
		public bool DefaultCommentFolding {
			get {
				return defaultCommentFolding;
			}
			set {
				if (value != this.defaultCommentFolding) {
					this.defaultCommentFolding = value;
					PropertyService.Set ("DefaultCommentFolding", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool enableSemanticHighlighting;
		public bool EnableSemanticHighlighting {
			get {
				return enableSemanticHighlighting;
			}
			set {
				if (value != this.enableSemanticHighlighting) {
					this.enableSemanticHighlighting = value;
					PropertyService.Set ("EnableSemanticHighlighting", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		/*
		bool autoInsertTemplates;
		public bool AutoInsertTemplates {
			get {
				return autoInsertTemplates;
			}
			set {
				if (value != this.autoInsertTemplates) {
					this.autoInsertTemplates = value;
					PropertyService.Set ("AutoInsertTemplates", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}*/
		
		bool tabIsReindent;
		public bool TabIsReindent {
			get {
				return tabIsReindent;
			}
			set {
				if (value != this.tabIsReindent) {
					this.tabIsReindent = value;
					PropertyService.Set ("TabIsReindent", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool autoInsertMatchingBracket;
		public bool AutoInsertMatchingBracket {
			get {
				return autoInsertMatchingBracket;
			}
			set {
				if (value != this.autoInsertMatchingBracket) {
					this.autoInsertMatchingBracket = value;
					PropertyService.Set ("AutoInsertMatchingBracket", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool smartSemicolonPlacement;
		public bool SmartSemicolonPlacement {
			get {
				return smartSemicolonPlacement;
			}
			set {
				if (value != this.smartSemicolonPlacement) {
					this.smartSemicolonPlacement= value;
					PropertyService.Set ("SmartSemicolonPlacement", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool enableCodeCompletion;
		public bool EnableCodeCompletion {
			get { return enableCodeCompletion; }
			set {
				if (value != this.enableCodeCompletion) {
					this.enableCodeCompletion = value;
					PropertyService.Set ("EnableCodeCompletion", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool enableParameterInsight;
		public bool EnableParameterInsight {
			get { return enableParameterInsight; }
			set {
				if (value != this.enableParameterInsight) {
					this.enableParameterInsight = value;
					PropertyService.Set ("EnableParameterInsight", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		bool underlineErrors;
		public bool UnderlineErrors {
			get {
				return underlineErrors; 
			}
			set {
				if (value != this.underlineErrors) {
					this.underlineErrors = value;
					PropertyService.Set ("UnderlineErrors", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		IndentStyle indentStyle;
		public IndentStyle IndentStyle {
			get {
				return indentStyle;
			}
			set {
				if (value != this.indentStyle) {
					this.indentStyle = value;
					PropertyService.Set ("IndentStyle", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		EditorFontType editorFontType;
		public EditorFontType EditorFontType {
			get {
				return editorFontType;
			}
			set {
				if (value != this.editorFontType) {
					this.editorFontType = value;
					PropertyService.Set ("EditorFontType", value);
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		#endregion
		
		bool useViModes = false;
		public bool UseViModes {
			get {
				return useViModes;
			}
			set {
				if (useViModes == value)
					return;
				useViModes = value;
				PropertyService.Set ("UseViModes", value);
				OnChanged (EventArgs.Empty);
			}
		}
		
		bool onTheFlyFormatting = false;
		public bool OnTheFlyFormatting {
			get {
				return onTheFlyFormatting;
			}
			set {
				if (onTheFlyFormatting == value)
					return;
				onTheFlyFormatting = value;
				PropertyService.Set ("OnTheFlyFormatting", value);
				OnChanged (EventArgs.Empty);
			}
		}
		
		#region old options
		string defaultEolMarker;
		public override string DefaultEolMarker {
			get { return defaultEolMarker; }
		}

		ControlLeftRightMode controlLeftRightMode = Platform.IsWindows
			? ControlLeftRightMode.SharpDevelop
			: ControlLeftRightMode.MonoDevelop;
		
		public ControlLeftRightMode ControlLeftRightMode {
			get {
				return controlLeftRightMode;
			}
			set {
				if (controlLeftRightMode != value) {
					controlLeftRightMode = value;
					PropertyService.Set ("ControlLeftRightMode", value);
					SetWordFindStrategy ();
					OnChanged (EventArgs.Empty);
				}
			}
		}
		
		IWordFindStrategy wordFindStrategy = null;
		public override IWordFindStrategy WordFindStrategy {
			get {
				if (wordFindStrategy == null)
					SetWordFindStrategy ();
				return this.wordFindStrategy;
			}
			set {
				throw new System.NotImplementedException ();
			}
		}
		
		void SetWordFindStrategy ()
		{
			if (useViModes) {
				this.wordFindStrategy = new Mono.TextEditor.Vi.ViWordFindStrategy ();
				return;
			}
			
			switch (ControlLeftRightMode) {
			case ControlLeftRightMode.MonoDevelop:
				this.wordFindStrategy = new EmacsWordFindStrategy (true);
				break;
			case ControlLeftRightMode.Emacs:
				this.wordFindStrategy = new EmacsWordFindStrategy (false);
				break;
			case ControlLeftRightMode.SharpDevelop:
				this.wordFindStrategy = new SharpDevelopWordFindStrategy ();
				break;
			}
		}

		public override bool AllowTabsAfterNonTabs {
			set {
				if (value != AllowTabsAfterNonTabs) {
					PropertyService.Set ("AllowTabsAfterNonTabs", value);
					base.AllowTabsAfterNonTabs = value;
				}
			}
		}
				
		public override bool TabsToSpaces {
			set {
				PropertyService.Set ("TabsToSpaces", value);
				base.TabsToSpaces = value;
			}
		}
		
		public override int IndentationSize {
			set {
				PropertyService.Set ("TabIndent", value);
				base.IndentationSize = value;
			}
		}
		
		public override int TabSize {
			get {
				return IndentationSize;
			}
			set {
				IndentationSize = value;
			}
		}
		
		public override bool RemoveTrailingWhitespaces {
			set {
				PropertyService.Set ("RemoveTrailingWhitespaces", value);
				base.RemoveTrailingWhitespaces = value;
			}
		}

		
		public override bool ShowLineNumberMargin {
			set {
				PropertyService.Set ("ShowLineNumberMargin", value);
				base.ShowLineNumberMargin = value;
			}
		}
		
		public override bool ShowFoldMargin {
			set {
				PropertyService.Set ("ShowFoldMargin", value);
				base.ShowFoldMargin = value;
			}
		}
		
		public override bool ShowInvalidLines {
			set {
				PropertyService.Set ("ShowInvalidLines", value);
				base.ShowInvalidLines = value;
			}
		}
		
		public override bool ShowTabs {
			set {
				PropertyService.Set ("ShowTabs", value);
				base.ShowTabs = value;
			}
		}
		
		public override bool ShowEolMarkers {
			set {
				PropertyService.Set ("ShowEolMarkers", value);
				base.ShowEolMarkers = value;
			}
		}
		
		public override bool HighlightCaretLine {
			set {
				PropertyService.Set ("HighlightCaretLine", value);
				base.HighlightCaretLine = value;
			}
		}
		
		public override bool ShowSpaces {
			set {
				PropertyService.Set ("ShowSpaces", value);
				base.ShowSpaces = value;
			}
		}
		
		public override bool EnableSyntaxHighlighting {
			set {
				PropertyService.Set ("EnableSyntaxHighlighting", value);
				base.EnableSyntaxHighlighting = value;
			}
		}
		
		public override bool HighlightMatchingBracket {
			set {
				PropertyService.Set ("HighlightMatchingBracket", value);
				base.HighlightMatchingBracket = value;
			}
		}
		
		public override int RulerColumn {
			set {
				PropertyService.Set ("RulerColumn", value);
				base.RulerColumn = value;
			}
		}
		
		public override bool ShowRuler {
			set {
				PropertyService.Set ("ShowRuler", value);
				base.ShowRuler = value;
			}
		}
		
		public override bool EnableAnimations {
			set {
				PropertyService.Set ("EnableAnimations", value);
				base.EnableAnimations = value;
			}
		}

		public override bool AutoIndent {
			get {
				return IndentStyle != MonoDevelop.Ide.Gui.Content.IndentStyle.None;
			}
			set {
				throw new NotSupportedException ("Use property 'IndentStyle' instead.");
			}
		}
		
		public override string FontName {
			get {
				return FontService.FilterFontName (FontService.GetUnderlyingFontName ("Editor"));
			}
			set {
				throw new InvalidOperationException ("Set font through font service");
			}
		}
		
		public override string ColorScheme {
			set {
				string newColorScheme = !String.IsNullOrEmpty (value) ? value : "Default";
				PropertyService.Set ("ColorScheme", newColorScheme);
				base.ColorScheme =  newColorScheme;
			}
		}
		
		#endregion
	}
}
