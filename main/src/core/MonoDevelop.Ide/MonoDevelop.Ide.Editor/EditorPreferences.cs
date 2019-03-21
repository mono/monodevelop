//
// EditorPreferences.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Composition;

namespace MonoDevelop.Ide.Editor
{
	public class EditorPreferences
	{
		readonly Dictionary<string, Action<object>> EditorToIdeMapping = new Dictionary<string, Action<object>> ();
		readonly Dictionary<string, string> IdeToEditorMapping = new Dictionary<string, string> ();

		readonly IEditorOptionsFactoryService2 factoryService;
		readonly IEditorOptions globalOptions;

		#region Mapped Editor Options
		public readonly ConfigurationProperty<bool> ConvertTabsToSpaces;
		public readonly ConfigurationProperty<bool> EnableBraceCompletion;
		public readonly ConfigurationProperty<bool> EnableCompletionSuggestionMode;
		public readonly ConfigurationProperty<bool> EnableHighlightCurrentLine;
		public readonly ConfigurationProperty<bool> EnableHighlightDelimiter;
		public readonly ConfigurationProperty<bool> FollowCodingConventions; // TODO: Needs UI
		public readonly ConfigurationProperty<int> IndentSize;
		public readonly ConfigurationProperty<string> NewLineCharacter; // TODO: Needs UI
		public readonly ConfigurationProperty<bool> OutliningUndoStep;
		public readonly ConfigurationProperty<bool> ShowChangeTrackingMargin;
		public readonly ConfigurationProperty<bool> ShowGlyphMargin;
		public readonly ConfigurationProperty<bool> ShowLineNumberMargin;
		public readonly ConfigurationProperty<bool> ShowOutliningMargin;
		public readonly ConfigurationProperty<int> TabSize;
		public readonly ConfigurationProperty<bool> TrimTrailingWhitespace;
		public readonly ConfigurationProperty<WordWrapStyles> WordWrapStyle;
#if !WINDOWS
		public readonly ConfigurationProperty<ShowWhitespaces> ShowWhitespaces;
		public readonly ConfigurationProperty<IncludeWhitespaces> IncludeWhitespaces;
		public readonly ConfigurationProperty<bool> ShouldMoveCaretOnSelectAll;
#endif
		public readonly ConfigurationProperty<bool> ShowBlockStructure;
		// TODO: Maybe per language preferences?
		#endregion

		internal EditorPreferences ()
		{
			factoryService = CompositionManager.Instance.GetExportedValue<IEditorOptionsFactoryService2> ();
			globalOptions = factoryService.GlobalOptions;

			PropertyService.PropertyChanged += PropertyService_PropertyChanged;
			globalOptions.OptionChanged += GlobalOptions_OptionChanged;

			Migrate ("HideLineNumberMargin", "ShowLineNumberMargin", Flip);

			// Prefered to write in code rather than extension to use actual string constants
			ConvertTabsToSpaces = Wrap<bool> ("TabsToSpaces", DefaultOptions.ConvertTabsToSpacesOptionName);
			EnableBraceCompletion = Wrap<bool> ("AutoInsertMatchingBracket", DefaultTextViewOptions.BraceCompletionEnabledOptionName);
			EnableCompletionSuggestionMode = Map<bool> ("ForceCompletionSuggestionMode", PredefinedCompletionNames.SuggestionModeInCompletionOptionName, IdeApp.Preferences.ForceSuggestionMode);
			// have to use literal because it's in different types on different platforms (DefaultTextViewOptions vs. DefaultWpfViewOptions)
			EnableHighlightCurrentLine = Wrap ("HighlightCaretLine", "Adornments/HighlightCurrentLine/Enable", false);
			EnableHighlightDelimiter = Wrap<bool> ("HighlightMatchingBracket", DefaultOptions.AutomaticDelimiterHighlightingName);
			FollowCodingConventions = Wrap<bool> ("FollowCodingConventions", DefaultOptions.FollowCodingConventionsName);
			IndentSize = Wrap<int> ("TabIndent", DefaultOptions.IndentSizeOptionName);
			NewLineCharacter = Wrap<string> ("NewLineCharacter", DefaultOptions.NewLineCharacterOptionName, Environment.NewLine);
			OutliningUndoStep = Wrap<bool> ("GenerateFormattingUndoStep", DefaultTextViewOptions.OutliningUndoOptionName);
			ShowChangeTrackingMargin = Wrap ("EnableQuickDiff", DefaultTextViewHostOptions.ChangeTrackingName, false);
			ShowGlyphMargin = Wrap<bool> ("ShowGlyphMargin", DefaultTextViewHostOptions.GlyphMarginName);
			ShowLineNumberMargin = Wrap<bool> ("ShowLineNumberMargin", DefaultTextViewHostOptions.LineNumberMarginName);
			ShowOutliningMargin = Wrap<bool> ("ShowFoldMargin", DefaultTextViewHostOptions.OutliningMarginName);
			TrimTrailingWhitespace = Wrap ("RemoveTrailingWhitespaces", DefaultOptions.TrimTrailingWhiteSpaceOptionName, true);
			// UseVirtualSpace should be a combination of IndentStyle == MonoDevelop.Ide.Editor.IndentStyle.Smart && RemoveTrailingWhitespaces
			WordWrapStyle = Wrap<WordWrapStyles> ("WordWrapStyle", DefaultTextViewOptions.WordWrapStyleName);
			TabSize = Wrap<int> ("TabSize", DefaultOptions.TabSizeOptionName);
#if !WINDOWS
			ShowWhitespaces = new ShowWhitespacesProperty (this);
			IncludeWhitespaces = new IncludeWhitespacesProperty (this);
			ShouldMoveCaretOnSelectAll = Wrap<bool> ("ShouldMoveCaretOnSelectAll", DefaultTextViewOptions.ShouldMoveCaretOnSelectAllName, false);
#endif
			ShowBlockStructure = Wrap<bool> ("ShowBlockStructure", DefaultTextViewOptions.ShowBlockStructureName);

			LogNonMappedOptions ();
		}

		static Func<bool, bool> Flip = value => !value;

		void Migrate<T> (string oldKey, string newKey, Func<T, T> transform = null)
		{
			// Check the old key
			if (!PropertyService.HasValue (oldKey))
				return;

			// Migrate old to new if new doesn't exist and then unset the old one
			if (!PropertyService.HasValue (newKey)) {
				var value = PropertyService.Get<T> (oldKey);
				if (transform != null)
					value = transform (value);

				PropertyService.Set (newKey, value);
			}
			PropertyService.Set (oldKey, null);

		}


		[Conditional ("DEBUG")]
		void LogNonMappedOptions ()
		{
			foreach (var option in globalOptions.SupportedOptions) {
				if (!EditorToIdeMapping.ContainsKey (option.Name)) {
					LoggingService.LogDebug ("Unmapped editor command: {0}", option.Name);
				}
			}
		}

		#region Cross-service updating logic
		void PropertyService_PropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (IdeToEditorMapping.TryGetValue (e.Key, out var editorKey)) {
				globalOptions.SetOptionValue (editorKey, e.NewValue);
			}
		}

		void GlobalOptions_OptionChanged (object sender, EditorOptionChangedEventArgs e)
		{
			if (EditorToIdeMapping.TryGetValue (e.OptionId, out var updater)) {
				updater (globalOptions.GetOptionValue (e.OptionId));
			}
		}
		#endregion

		#region Property creation logic
		/// <summary>
		/// Wrap the specified editor option using the editor default value
		/// </summary>
		/// <returns>The property.</returns>
		/// <param name="name">The key in the IDE Properties.</param>
		/// <param name="editorOptionId">Editor option identifier.</param>
		/// <typeparam name="T">The option type.</typeparam>
		internal ConfigurationProperty<T> Wrap<T> (string name, string editorOptionId)
		{
			var definition = factoryService.GetOptionDefinition (editorOptionId);
			var defaultValue = (T)definition.DefaultValue;

			return Create (name, editorOptionId, defaultValue);
		}

		/// <summary>
		/// Wrap the specified editor option overwriting it's default value.
		/// </summary>
		/// <returns>The property.</returns>
		/// <param name="name">The key in the IDE Properties.</param>
		/// <param name="editorOptionId">Editor option identifier.</param>
		/// <param name="defaultValue">Default value override.</param>
		/// <typeparam name="T">The option type.</typeparam>
		internal ConfigurationProperty<T> Wrap<T> (string name, string editorOptionId, T defaultValue)
		{
			return Create (name, editorOptionId, defaultValue);
		}

		ConfigurationProperty<T> Create<T> (string name, string editorOptionId, T defaultValue)
		{
			return Map (name, editorOptionId, ConfigurationProperty.Create (name, defaultValue));
		}

		ConfigurationProperty<T> Map<T> (string name, string editorOptionId, ConfigurationProperty<T> property)
		{
			IdeToEditorMapping.Add (name, editorOptionId);
			EditorToIdeMapping.Add (editorOptionId, value => property.Value = (T)value);

			// Set the editor options to override any editor option that we use.
			globalOptions.SetOptionValue (editorOptionId, property.Value);

			return property;
		}

#if !WINDOWS
		class IncludeWhitespacesProperty : ConfigurationProperty<IncludeWhitespaces>
		{
			IncludeWhitespaces propertyValue;
			const string propertyName = "IncludeWhitespaces";
			readonly EditorPreferences editorPreferences;

			public IncludeWhitespacesProperty (EditorPreferences editorPreferences)
			{
				this.editorPreferences = editorPreferences;
				var definition = editorPreferences.factoryService.GetOptionDefinition (DefaultTextViewOptions.UseVisibleWhitespaceIncludeName);
				propertyValue = PropertyService.Get (propertyName, Convert ((DefaultTextViewOptions.IncludeWhitespaces)definition.DefaultValue));
				UpdateEditor (propertyValue);
				editorPreferences.globalOptions.OptionChanged += GlobalOptions_OptionChanged;
			}

			private IncludeWhitespaces Convert (DefaultTextViewOptions.IncludeWhitespaces includeWhitespaces)
			{
				return (IncludeWhitespaces)(int)includeWhitespaces;
			}

			void GlobalOptions_OptionChanged (object sender, EditorOptionChangedEventArgs e)
			{
				if (e.OptionId == DefaultTextViewOptions.UseVisibleWhitespaceName) {
					this.Set (Convert (editorPreferences.globalOptions.GetOptionValue<DefaultTextViewOptions.IncludeWhitespaces> (DefaultTextViewOptions.UseVisibleWhitespaceName)));
				}
			}

			protected override IncludeWhitespaces OnGetValue ()
			{
				return propertyValue;
			}

			protected override bool OnSetValue (IncludeWhitespaces value)
			{
				if (this.propertyValue == value)
					return false;
				this.propertyValue = value;
				PropertyService.Set (propertyName, value);
				OnChanged ();
				UpdateEditor (value);
				return true;
			}

			private void UpdateEditor (IncludeWhitespaces value)
			{
				var val = (DefaultTextViewOptions.IncludeWhitespaces)(int)value;
				if (val.HasFlag (DefaultTextViewOptions.IncludeWhitespaces.Spaces))
					val |= DefaultTextViewOptions.IncludeWhitespaces.Ideographics;
				editorPreferences.globalOptions.SetOptionValue (DefaultTextViewOptions.UseVisibleWhitespaceIncludeName, val);
			}
		}

		class ShowWhitespacesProperty : ConfigurationProperty<ShowWhitespaces>
		{
			ShowWhitespaces value;
			const string propertyName = "ShowWhitespaces";
			readonly EditorPreferences editorPreferences;

			public ShowWhitespacesProperty (EditorPreferences editorPreferences)
			{
				this.editorPreferences = editorPreferences;
				var definitionEnabled = editorPreferences.factoryService.GetOptionDefinition (DefaultTextViewOptions.UseVisibleWhitespaceName);
				var definitionSelection = editorPreferences.factoryService.GetOptionDefinition (DefaultTextViewOptions.UseVisibleWhitespaceOnlyWhenSelectedName);
				value = PropertyService.Get (propertyName, Convert ((bool)definitionEnabled.DefaultValue, (bool)definitionSelection.DefaultValue));
				UpdateEditor (value);
				editorPreferences.globalOptions.OptionChanged += GlobalOptions_OptionChanged;
			}

			private ShowWhitespaces Convert (bool enable, bool selection)
			{
				if (enable)
					if (selection)
						return Editor.ShowWhitespaces.Selection;
					else
						return Editor.ShowWhitespaces.Always;
				else
					return Editor.ShowWhitespaces.Never;
			}

			void GlobalOptions_OptionChanged (object sender, EditorOptionChangedEventArgs e)
			{
				if (e.OptionId == DefaultTextViewOptions.UseVisibleWhitespaceName ||
					e.OptionId == DefaultTextViewOptions.UseVisibleWhitespaceOnlyWhenSelectedName) {
					if (editorPreferences.globalOptions.GetOptionValue<bool> (DefaultTextViewOptions.UseVisibleWhitespaceName)) {
						if (editorPreferences.globalOptions.GetOptionValue<bool> (DefaultTextViewOptions.UseVisibleWhitespaceOnlyWhenSelectedName))
							Set (Editor.ShowWhitespaces.Selection);
						else
							Set (Editor.ShowWhitespaces.Always);
					} else {
						Set (Editor.ShowWhitespaces.Never);
					}
				}
			}

			protected override ShowWhitespaces OnGetValue ()
			{
				return value;
			}

			protected override bool OnSetValue (ShowWhitespaces value)
			{
				if (this.value == value)
					return false;
				this.value = value;
				PropertyService.Set (propertyName, value);
				OnChanged ();
				UpdateEditor (value);
				return true;
			}

			private void UpdateEditor (ShowWhitespaces value)
			{
				editorPreferences.globalOptions.SetOptionValue (DefaultTextViewOptions.UseVisibleWhitespaceName, value != Editor.ShowWhitespaces.Never);
				editorPreferences.globalOptions.SetOptionValue (DefaultTextViewOptions.UseVisibleWhitespaceOnlyWhenSelectedName, value == Editor.ShowWhitespaces.Selection);
			}
		}
#endif
		#endregion
	}
}
