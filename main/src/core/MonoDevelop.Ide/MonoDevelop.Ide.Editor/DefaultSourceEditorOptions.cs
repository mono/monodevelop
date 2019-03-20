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
using Microsoft.VisualStudio.CodingConventions;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Components.Extensions;

namespace MonoDevelop.Ide.Editor
{
	public enum WordNavigationStyle
	{
		Unix,
		Windows
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
	public sealed class DefaultSourceEditorOptions : ITextEditorOptions
	{
		static DefaultSourceEditorOptions instance;
		//static TextStylePolicy defaultPolicy;
		static bool inited;
		ICodingConventionContext context;

		public static DefaultSourceEditorOptions Instance {
			get { return instance; }
		}

		public static ITextEditorOptions PlainEditor {
			get;
			private set;
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

			PlainEditor = new PlainEditorOptions ();
		}

		internal void FireChange ()
		{
			OnChanged (EventArgs.Empty);
		}

		class PlainEditorOptions : ITextEditorOptions
		{
			#region IDisposable implementation

			void IDisposable.Dispose ()
			{
				// nothing
			}

			#endregion

			#region ITextEditorOptions implementation

			WordFindStrategy ITextEditorOptions.WordFindStrategy {
				get {
					return DefaultSourceEditorOptions.Instance.WordFindStrategy;
				}
			}

			bool ITextEditorOptions.TabsToSpaces {
				get {
					return DefaultSourceEditorOptions.Instance.TabsToSpaces;
				}
			}

			int ITextEditorOptions.IndentationSize {
				get {
					return DefaultSourceEditorOptions.Instance.IndentationSize;
				}
			}

			int ITextEditorOptions.TabSize {
				get {
					return DefaultSourceEditorOptions.Instance.TabSize;
				}
			}

			bool ITextEditorOptions.ShowIconMargin {
				get {
					return false;
				}
			}

			bool ITextEditorOptions.ShowLineNumberMargin {
				get {
					return false;
				}
			}

			bool ITextEditorOptions.ShowFoldMargin {
				get {
					return false;
				}
			}

			bool ITextEditorOptions.HighlightCaretLine {
				get {
					return false;
				}
			}

			int ITextEditorOptions.RulerColumn {
				get {
					return DefaultSourceEditorOptions.Instance.RulerColumn;
				}
			}

			bool ITextEditorOptions.ShowRuler {
				get {
					return false;
				}
			}

			IndentStyle ITextEditorOptions.IndentStyle {
				get {
					return DefaultSourceEditorOptions.Instance.IndentStyle;
				}
			}

			bool ITextEditorOptions.OverrideDocumentEolMarker {
				get {
					return false;
				}
			}

			bool ITextEditorOptions.EnableSyntaxHighlighting {
				get {
					return DefaultSourceEditorOptions.Instance.EnableSyntaxHighlighting;
				}
			}

			bool ITextEditorOptions.RemoveTrailingWhitespaces {
				get {
					return DefaultSourceEditorOptions.Instance.RemoveTrailingWhitespaces;
				}
			}

			[Obsolete ("Old editor")]
			bool ITextEditorOptions.WrapLines {
				get {
					return DefaultSourceEditorOptions.Instance.WrapLines;
				}
			}

			string ITextEditorOptions.FontName {
				get {
					return DefaultSourceEditorOptions.Instance.FontName;
				}
			}

			string ITextEditorOptions.GutterFontName {
				get {
					return DefaultSourceEditorOptions.Instance.GutterFontName;
				}
			}

			string ITextEditorOptions.EditorTheme {
				get {
					return DefaultSourceEditorOptions.Instance.EditorTheme;
				}
			}

			string ITextEditorOptions.DefaultEolMarker {
				get {
					return DefaultSourceEditorOptions.Instance.DefaultEolMarker;
				}
			}

			bool ITextEditorOptions.GenerateFormattingUndoStep {
				get {
					return DefaultSourceEditorOptions.Instance.GenerateFormattingUndoStep;
				}
			}

			bool ITextEditorOptions.EnableSelectionWrappingKeys {
				get {
					return DefaultSourceEditorOptions.Instance.EnableSelectionWrappingKeys;
				}
			}

			ShowWhitespaces ITextEditorOptions.ShowWhitespaces {
				get {
					return ShowWhitespaces.Never;
				}
			}

			IncludeWhitespaces ITextEditorOptions.IncludeWhitespaces {
				get {
					return DefaultSourceEditorOptions.Instance.IncludeWhitespaces;
				}
			}
			
			bool ITextEditorOptions.SmartBackspace {
				get {
					return DefaultSourceEditorOptions.Instance.SmartBackspace;
				}
			}

			bool ITextEditorOptions.EnableQuickDiff {
				get {
					return false;
				}
			}
			#endregion


		}

		void HandlePolicyChanged (object sender, MonoDevelop.Projects.Policies.PolicyChangedEventArgs args)
		{
			TextStylePolicy pol = MonoDevelop.Projects.Policies.PolicyService.GetDefaultPolicy<TextStylePolicy> ("text/plain");
			UpdateStylePolicy (pol);
		}

		DefaultSourceEditorOptions (TextStylePolicy currentPolicy)
		{
			wordNavigationStyle = ConfigurationProperty.Create ("WordNavigationStyle", WordNavigationStyle.Windows);
			
			UpdateStylePolicy (currentPolicy);
			FontService.RegisterFontChangedCallback ("Editor", UpdateFont);
			FontService.RegisterFontChangedCallback ("MessageBubbles", UpdateFont);

			IdeApp.Preferences.ColorScheme.Changed += OnColorSchemeChanged;
			IdeApp.Preferences.Editor.FollowCodingConventions.Changed += OnFollowCodingConventionsChanged;
		}

		void OnFollowCodingConventionsChanged (object sender, EventArgs e)
		{
			UpdateContextOptions (null, null).Ignore ();
		}


		void UpdateFont ()
		{
			this.OnChanged (EventArgs.Empty);
		}

		TextStylePolicy currentPolicy;
		internal void UpdateStylePolicy (MonoDevelop.Ide.Gui.Content.TextStylePolicy currentPolicy)
		{
			if (currentPolicy == this.currentPolicy)
				return;
			this.currentPolicy = currentPolicy;
			rulerColumn = currentPolicy.FileWidth; //PropertyService.Get ("RulerColumn", 80);
			allowTabsAfterNonTabs = !currentPolicy.NoTabsAfterNonTabs; //PropertyService.Get ("AllowTabsAfterNonTabs", true);
			OnChanged (EventArgs.Empty);
		}

		internal DefaultSourceEditorOptions Create ()
		{
			var result = (DefaultSourceEditorOptions)MemberwiseClone ();
			result.Changed = null;
			return result;
		}

		public DefaultSourceEditorOptions WithTextStyle (TextStylePolicy policy)
		{
			if (policy == null)
				throw new ArgumentNullException (nameof (policy));
			var result = (DefaultSourceEditorOptions)MemberwiseClone ();
			result.UpdateStylePolicy (policy);
			result.Changed = null;
			return result;
		}

		internal void SetContext (ICodingConventionContext context)
		{
			if (this.context == context)
				return;
			if (this.context != null)
				this.context.CodingConventionsChangedAsync -= UpdateContextOptions;
			this.context = context;
			context.CodingConventionsChangedAsync += UpdateContextOptions;
			UpdateContextOptions (null, null).Ignore ();
		}

		private Task UpdateContextOptions (object sender, CodingConventionsChangedEventArgs arg)
		{
			if (context == null)
				return Task.FromResult (false);

			bool followCodingConventions = IdeApp.Preferences.Editor.FollowCodingConventions;

			defaultEolMarkerFromContext = null;
			if (followCodingConventions && context.CurrentConventions.UniversalConventions.TryGetLineEnding (out string eolMarker))
				defaultEolMarkerFromContext = eolMarker;

			tabsToSpacesFromContext = null;
			if (followCodingConventions && context.CurrentConventions.UniversalConventions.TryGetIndentStyle (out Microsoft.VisualStudio.CodingConventions.IndentStyle result))
				tabsToSpacesFromContext = result == Microsoft.VisualStudio.CodingConventions.IndentStyle.Spaces;

			indentationSizeFromContext = null;
			if (followCodingConventions && context.CurrentConventions.UniversalConventions.TryGetIndentSize (out int indentSize)) 
				indentationSizeFromContext = indentSize;

			removeTrailingWhitespacesFromContext = null;
			if (followCodingConventions && context.CurrentConventions.UniversalConventions.TryGetAllowTrailingWhitespace (out bool allowTrailing))
				removeTrailingWhitespacesFromContext = !allowTrailing;

			tabSizeFromContext = null;
			if (followCodingConventions && context.CurrentConventions.UniversalConventions.TryGetTabWidth (out int tSize))
				tabSizeFromContext = tSize;

			rulerColumnFromContext = null;
			showRulerFromContext = null;
			if (followCodingConventions && context.CurrentConventions.TryGetConventionValue<string> (EditorConfigService.MaxLineLengthConvention, out string maxLineLength)) {
				if (maxLineLength != "off" && int.TryParse (maxLineLength, out int i)) {
					rulerColumnFromContext = i;
					showRulerFromContext = true;
				} else {
					showRulerFromContext = false;
				}
			}

			return Task.FromResult (true);
		}

		#region new options

		public bool EnableAutoCodeCompletion {
			get { return IdeApp.Preferences.EnableAutoCodeCompletion; }
			set { IdeApp.Preferences.EnableAutoCodeCompletion.Set (value); }
		}

		// TODO: Windows equivalent?
		ConfigurationProperty<bool> defaultRegionsFolding = ConfigurationProperty.Create ("DefaultRegionsFolding", false);
		public bool DefaultRegionsFolding {
			get {
				return defaultRegionsFolding;
			}
			set {
				if (defaultRegionsFolding.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		// TODO: Windows equivalent?
		ConfigurationProperty<bool> defaultCommentFolding = ConfigurationProperty.Create ("DefaultCommentFolding", true);
		public bool DefaultCommentFolding {
			get {
				return defaultCommentFolding;
			}
			set {
				if (defaultCommentFolding.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		ConfigurationProperty<bool> enableNewEditor = ConfigurationProperty.Create ("EnableNewEditor", false);
		public bool EnableNewEditor {
			get {
				return enableNewEditor;
			}
			set {
				if (!enableNewEditor.Set (value))
					return;

				string messageText;

				if (value) {
					messageText = GettextCatalog.GetString (
						"The New Editor Preview has been enabled, but already opened files " +
						"will need to be closed and re-opened for the change to take effect.");
					Counters.NewEditorEnabled.Inc ();
				} else {
					messageText = GettextCatalog.GetString (
						"The New Editor Preview has been disabled, but already opened files " +
						"will need to be closed and re-opened for the change to take effect.");
					Counters.NewEditorDisabled.Inc ();
				}

				if (IdeApp.Workbench?.Documents?.Count > 0) {
					Gtk.Application.Invoke ((o, e) => {
						var closeAllFilesButton = new AlertButton (GettextCatalog.GetString ("Close All Files"));

						var message = new MessageDescription {
							Text = messageText
						};

						message.Buttons.Add (closeAllFilesButton);
						message.Buttons.Add (AlertButton.Ok);
						message.DefaultButton = 1;

						if (new AlertDialog (message).Run () == closeAllFilesButton)
							IdeApp.Workbench.CloseAllDocumentsAsync (false);
					});
				}

				OnChanged (EventArgs.Empty);
			}
		}


		// TODO: Windows equivalent?
		ConfigurationProperty<bool> enableSemanticHighlighting = ConfigurationProperty.Create ("EnableSemanticHighlighting", true);
		public bool EnableSemanticHighlighting {
			get {
				return enableSemanticHighlighting;
			}
			set {
				if (enableSemanticHighlighting.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}


		// TODO: Windows equivalent?
		ConfigurationProperty<bool> tabIsReindent = ConfigurationProperty.Create ("TabIsReindent", false);
		public bool TabIsReindent {
			get {
				return tabIsReindent;
			}
			set {
				if (tabIsReindent.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		ConfigurationProperty<bool> autoInsertMatchingBracket = IdeApp.Preferences.Editor.EnableBraceCompletion;
		public bool AutoInsertMatchingBracket {
			get {
				return autoInsertMatchingBracket;
			}
			set {
				if (autoInsertMatchingBracket.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		// TODO: Windows equivalent?
		ConfigurationProperty<bool> smartSemicolonPlacement = ConfigurationProperty.Create ("SmartSemicolonPlacement", false);
		public bool SmartSemicolonPlacement {
			get {
				return smartSemicolonPlacement;
			}
			set {
				if (smartSemicolonPlacement.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		// TODO: Windows equivalent?
		ConfigurationProperty<IndentStyle> indentStyle = ConfigurationProperty.Create ("IndentStyle", IndentStyle.Smart);
		public IndentStyle IndentStyle {
			get {
				return indentStyle;
			}
			set {
				if (indentStyle.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		// TODO: Windows equivalent?
		ConfigurationProperty<bool> enableHighlightUsages = ConfigurationProperty.Create ("EnableHighlightUsages", true);
		public bool EnableHighlightUsages {
			get {
				return enableHighlightUsages;
			}
			set {
				if (enableHighlightUsages.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		// TODO: Windows equivalent?
		ConfigurationProperty<LineEndingConversion> lineEndingConversion = ConfigurationProperty.Create ("LineEndingConversion", LineEndingConversion.LeaveAsIs);
		public LineEndingConversion LineEndingConversion {
			get {
				return lineEndingConversion;
			}
			set {
				if (lineEndingConversion.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		// TODO: Windows equivalent?
		ConfigurationProperty<bool> showProcedureLineSeparators = ConfigurationProperty.Create ("ShowProcedureLineSeparators", false);
		public bool ShowProcedureLineSeparators {
			get {
				return showProcedureLineSeparators;
			}
			set {
				if (showProcedureLineSeparators.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		#endregion

		[Obsolete ("Deprecated - use the roslyn FeatureOnOffOptions.FormatXXX per document options.")]
		public bool OnTheFlyFormatting {
			get {
				return true;
			}
			set {
				// unused
			}
		}

		#region ITextEditorOptions
		ConfigurationProperty<string> defaultEolMarker = IdeApp.Preferences.Editor.NewLineCharacter;
		string defaultEolMarkerFromContext = null;
		string overrridenDefaultEolMarker = null;

		// TODO: This isn't surfaced in properties, only policies. We have no UI for it.
		public string DefaultEolMarker {
			get {
				if (overrridenDefaultEolMarker != null)
					return overrridenDefaultEolMarker;
				if (defaultEolMarkerFromContext != null)
					return defaultEolMarkerFromContext;
				if (currentPolicy != null)
					return TextStylePolicy.GetEolMarker (currentPolicy.EolMarker);
				return defaultEolMarker;
			}
			set {
				overrridenDefaultEolMarker = value;
				if (defaultEolMarker.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		// TODO: Windows equivalent?
		ConfigurationProperty<WordNavigationStyle> wordNavigationStyle;
		public WordNavigationStyle WordNavigationStyle {
			get {
				return wordNavigationStyle;
			}
			set {
				if (wordNavigationStyle.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		public WordFindStrategy WordFindStrategy {
			get {
				switch (WordNavigationStyle) {
				case WordNavigationStyle.Windows:
					return WordFindStrategy.SharpDevelop;
				default:
					return WordFindStrategy.Emacs;
				}
			}
			set {
				throw new System.NotImplementedException ();
			}
		}

		// TODO: Windows equivalent?
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
		
		ConfigurationProperty<bool> tabsToSpaces = IdeApp.Preferences.Editor.ConvertTabsToSpaces;
		bool? tabsToSpacesFromContext;
		bool? overriddenTabsToSpaces;
		public bool TabsToSpaces {
			get {
				if (overriddenTabsToSpaces.HasValue)
					return overriddenTabsToSpaces.Value;
				if (tabsToSpacesFromContext.HasValue)
					return tabsToSpacesFromContext.Value;
				if (currentPolicy != null)
					return currentPolicy.TabsToSpaces; 
				return tabsToSpaces;
			}
			set {
				overriddenTabsToSpaces = value;
				if (tabsToSpaces.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		ConfigurationProperty<int> indentationSize = IdeApp.Preferences.Editor.IndentSize;
		int? indentationSizeFromContext;
		int? overriddenIndentationSize;
		public int IndentationSize {
			get {
				if (overriddenIndentationSize.HasValue)
					return overriddenIndentationSize.Value;
				if (indentationSizeFromContext.HasValue)
					return indentationSizeFromContext.Value;
				if (currentPolicy != null)
					return currentPolicy.IndentWidth; //PropertyService.Get ("IndentWidth", 4);
				return indentationSizeFromContext ?? indentationSize;
			}
			set {
				overriddenIndentationSize = value;
				if (indentationSize.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		public string IndentationString {
			get {
				return TabsToSpaces ? new string (' ', this.TabSize) : "\t";
			}
		}

		ConfigurationProperty<int> tabSize = IdeApp.Preferences.Editor.TabSize;
		int? tabSizeFromContext;
		int? overriddenTabSize;
		public int TabSize {
			get {
				if (overriddenTabSize.HasValue)
					return overriddenTabSize.Value;
				if (tabSizeFromContext.HasValue)
					return tabSizeFromContext.Value;
				if (currentPolicy != null)
					return currentPolicy.TabWidth; //PropertyService.Get ("IndentWidth", 4);
				return tabSize;
			}
			set {
				overriddenTabSize = value;
				if (tabSize.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		ConfigurationProperty<bool> trimTrailingWhitespace = IdeApp.Preferences.Editor.TrimTrailingWhitespace;
		bool? removeTrailingWhitespacesFromContext;
		bool? overriddenRemoveTrailingWhitespacesFromContext;

		public bool RemoveTrailingWhitespaces {
			get {
				if (overriddenRemoveTrailingWhitespacesFromContext.HasValue)
					return overriddenRemoveTrailingWhitespacesFromContext.Value;
				if (removeTrailingWhitespacesFromContext.HasValue)
					return removeTrailingWhitespacesFromContext.Value;
				if (currentPolicy != null)
					return currentPolicy.RemoveTrailingWhitespace;
				return trimTrailingWhitespace;
			}
			set {
				overriddenRemoveTrailingWhitespacesFromContext = value;
				if (trimTrailingWhitespace.Set(value))
					OnChanged (EventArgs.Empty);
			}
		}

		ConfigurationProperty<bool> showLineNumberMargin = IdeApp.Preferences.Editor.ShowLineNumberMargin;
		public bool ShowLineNumberMargin {
			get {
				return showLineNumberMargin;
			}
			set {
				if (showLineNumberMargin.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		ConfigurationProperty<bool> showFoldMargin = IdeApp.Preferences.Editor.ShowOutliningMargin;
		public bool ShowFoldMargin {
			get {
				return showFoldMargin;
			}
			set {
				if (showFoldMargin.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		ConfigurationProperty<bool> showIconMargin = IdeApp.Preferences.Editor.ShowGlyphMargin;
		public bool ShowIconMargin {
			get {
				return showIconMargin;
			}
			set {
				if (showIconMargin.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		ConfigurationProperty<bool> highlightCaretLine = IdeApp.Preferences.Editor.EnableHighlightCurrentLine;
		public bool HighlightCaretLine {
			get {
				return highlightCaretLine;
			}
			set {
				if (highlightCaretLine.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		// TODO: Windows equivalent?
		ConfigurationProperty<bool> enableSyntaxHighlighting = ConfigurationProperty.Create ("EnableSyntaxHighlighting", true);
		public bool EnableSyntaxHighlighting {
			get {
				return enableSyntaxHighlighting;
			}
			set {
				if (enableSyntaxHighlighting.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		internal ConfigurationProperty<bool> highlightMatchingBracket = IdeApp.Preferences.Editor.EnableHighlightDelimiter;
		public bool HighlightMatchingBracket {
			get {
				return highlightMatchingBracket;
			}
			set {
				if (highlightMatchingBracket.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		int  rulerColumn = 120;
		int? rulerColumnFromContext;


		// TODO: VS equivalent?
		public int RulerColumn {
			get {
				return rulerColumnFromContext ?? rulerColumn;
			}
			set {
				if (rulerColumn != value) {
					PropertyService.Set ("RulerColumn", value);
					rulerColumn = value;
					OnChanged (EventArgs.Empty);
				}
			}
		}

		// TODO: VS equivalent?
		ConfigurationProperty<bool> showRuler = ConfigurationProperty.Create ("ShowRuler", true);
		bool? showRulerFromContext;
		public bool ShowRuler {
			get {
				return showRulerFromContext ?? showRuler;
			}
			set {
				if (showRuler.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		// TODO: ???
		ConfigurationProperty<bool> enableAnimations = ConfigurationProperty.Create ("EnableAnimations", true);
		public bool EnableAnimations {
			get { 
				return enableAnimations; 
			}
			set {
				if (enableAnimations.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		ConfigurationProperty<bool> drawIndentationMarkers = IdeApp.Preferences.Editor.ShowBlockStructure;
		public bool DrawIndentationMarkers {
			get {
				return drawIndentationMarkers;
			}
			set {
				if (drawIndentationMarkers.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		[Obsolete ("Deprecated - use WordWrapStyle")]
		public bool WrapLines => false;

		ConfigurationProperty<WordWrapStyles> wordWrapStyle = IdeApp.Preferences.Editor.WordWrapStyle;
		public WordWrapStyles WordWrapStyle {
			get => wordWrapStyle;
			set {
				if (wordWrapStyle.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		ConfigurationProperty<bool> enableQuickDiff = IdeApp.Preferences.Editor.ShowChangeTrackingMargin;
		public bool EnableQuickDiff {
			get {
				return enableQuickDiff;
			}
			set {
				if (enableQuickDiff.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}


#if !WINDOWS
		readonly ConfigurationProperty<bool> shouldMoveCaretOnSelectAll = IdeApp.Preferences.Editor.ShouldMoveCaretOnSelectAll;
#else
		readonly ConfigurationProperty<bool> shouldMoveCaretOnSelectAll = ConfigurationProperty.Create (nameof (ShouldMoveCaretOnSelectAll), false);
#endif
		public bool ShouldMoveCaretOnSelectAll {
			get => shouldMoveCaretOnSelectAll;
			set {
				if (shouldMoveCaretOnSelectAll.Set (value))
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
				return FontService.FilterFontName (FontService.GetUnderlyingFontName ("Editor"));
			}
			set {
				throw new InvalidOperationException ("Set font through font service");
			}
		}
		
		ConfigurationProperty<string> colorScheme = IdeApp.Preferences.ColorScheme;
		public string EditorTheme {
			get {
				return colorScheme;
			}
			set {
				colorScheme.Set (value);
			}
		}

		void OnColorSchemeChanged (object sender, EventArgs e)
		{
			OnChanged (EventArgs.Empty);
		}

		ConfigurationProperty<bool> generateFormattingUndoStep = IdeApp.Preferences.Editor.OutliningUndoStep;
		public bool GenerateFormattingUndoStep {
			get {
				return generateFormattingUndoStep;
			}
			set {
				if (generateFormattingUndoStep.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		ConfigurationProperty<bool> enableSelectionWrappingKeys = ConfigurationProperty.Create ("EnableSelectionWrappingKeys", false);
		public bool EnableSelectionWrappingKeys {
			get {
				return enableSelectionWrappingKeys;
			}
			set {
				if (enableSelectionWrappingKeys.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		bool overrideDocumentEolMarker = false;
		public bool OverrideDocumentEolMarker {
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

#if !WINDOWS
		ConfigurationProperty<ShowWhitespaces> showWhitespaces = IdeApp.Preferences.Editor.ShowWhitespaces;
		ConfigurationProperty<IncludeWhitespaces> includeWhitespaces = IdeApp.Preferences.Editor.IncludeWhitespaces;
#else
		ConfigurationProperty<ShowWhitespaces> showWhitespaces = ConfigurationProperty.Create ("ShowWhitespaces", ShowWhitespaces.Never);
		ConfigurationProperty<IncludeWhitespaces> includeWhitespaces = ConfigurationProperty.Create ("IncludeWhitespaces", IncludeWhitespaces.All);
#endif

		public ShowWhitespaces ShowWhitespaces {
			get {
				return showWhitespaces;
			}
			set {
				if (showWhitespaces.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		public IncludeWhitespaces IncludeWhitespaces {
			get {
				return includeWhitespaces;
			}
			set {
				if (includeWhitespaces.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}

		// TODO: Windows equivalent?
		ConfigurationProperty<bool> smartBackspace = ConfigurationProperty.Create ("SmartBackspace", true);
		public bool SmartBackspace{
			get {
				return smartBackspace;

			}
			set {
				if (smartBackspace.Set (value))
					OnChanged (EventArgs.Empty);
			}
		}
		#endregion
		
		public void Dispose ()
		{
			FontService.RemoveCallback (UpdateFont);
			IdeApp.Preferences.ColorScheme.Changed -= OnColorSchemeChanged;
			IdeApp.Preferences.Editor.FollowCodingConventions.Changed -= OnFollowCodingConventionsChanged;
			if (context != null)
				context.CodingConventionsChangedAsync -= UpdateContextOptions;
		}

		void OnChanged (EventArgs args)
		{
			Changed?.Invoke (null, args);
		}

		public event EventHandler Changed;

		/// <summary>
		/// This is to allow setting UseAsyncCompletion to true for the Cocoa editor and to false for the Gtk editor.
		/// Currently Roslyn doesn't allow setting this option per view, so we have to work around.
		/// See here for details: https://github.com/dotnet/roslyn/issues/33807
		/// </summary>
		internal static void SetUseAsyncCompletion(bool useAsyncCompletion)
		{
			var asyncCompletionService = Composition.CompositionManager.GetExportedValue<Microsoft.CodeAnalysis.Editor.IAsyncCompletionService> ();
			var field = asyncCompletionService.GetType ().GetField (
				"_newCompletionAPIEnabled",
				System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
			field.SetValue (asyncCompletionService, useAsyncCompletion);
		}
	}
}

