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
		// TODO: Maybe per language preferences?
		#endregion

		internal EditorPreferences ()
		{
			factoryService = CompositionManager.GetExportedValue<IEditorOptionsFactoryService2> ();
			globalOptions = factoryService.GlobalOptions;

			PropertyService.PropertyChanged += PropertyService_PropertyChanged;
			globalOptions.OptionChanged += GlobalOptions_OptionChanged;

			Migrate ("HideLineNumberMargin", "ShowLineNumberMargin", Flip);

			// Prefered to write in code rather than extension to use actual string constants
			ConvertTabsToSpaces = Wrap<bool> ("TabsToSpaces", DefaultOptions.ConvertTabsToSpacesOptionName);
			EnableBraceCompletion = Wrap<bool> ("AutoInsertMatchingBracket", DefaultTextViewOptions.BraceCompletionEnabledOptionName);
			EnableCompletionSuggestionMode = Map<bool> ("ForceCompletionSuggestionMode", PredefinedCompletionNames.SuggestionModeInCompletionOptionName, IdeApp.Preferences.ForceSuggestionMode);
			EnableHighlightCurrentLine = Wrap ("HighlightCaretLine", DefaultTextViewOptions.EnableHighlightCurrentLineName, false);
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

			LogNonMappedOptions ();
		}

		static Func<bool, bool> Flip = value => !value;

		void Migrate<T> (string oldKey, string newKey, Func<T, T> transform = null)
		{
			// in 8.0
			if (PropertyService.HasValue (oldKey) && !PropertyService.HasValue (newKey)) {
				var value = PropertyService.Get<T> (oldKey);
				if (transform != null)
					value = transform (value);
				PropertyService.Set (newKey, value);
			}
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
			// Override the default value first.
			globalOptions.SetOptionValue (editorOptionId, (object)defaultValue);

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

			return property;
		}
		#endregion
	}
}
