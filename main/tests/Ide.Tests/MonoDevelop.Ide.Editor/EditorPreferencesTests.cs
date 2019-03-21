//
// EditorPreferencesTests.cs
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
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text.Editor;
using MonoDevelop.Core;
using MonoDevelop.Ide.Composition;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.Editor
{
	[TestFixture]
	[RequireService (typeof (CompositionManager))]
	public class EditorPreferencesTests : IdeTestBase
	{
		const string mdPropertyKey = "MD_EDITOR_FAKE_KEY";
		// Use a supported key so we don't bother creating a new MEF assembly to add that's not going to be surfaced in the preferences page.
		const string editorOptionKey = DefaultTextViewOptions.ViewProhibitUserInputName;

		static async Task<(EditorPreferences, IEditorOptions, ConfigurationProperty<bool>)> GetEditorPreferences (bool unset = true)
		{
			if (unset)
				PropertyService.Set (mdPropertyKey, null);

			var preferences = new EditorPreferences ();

			var factoryService = CompositionManager.Instance.GetExportedValue<IEditorOptionsFactoryService2> ();
			var preference = preferences.Wrap (mdPropertyKey, editorOptionKey, false);
			return (preferences, factoryService.GlobalOptions, preference);
		}

		[Test]
		public async Task OptionValuesAreInheritedWhenTheyExist()
		{
			PropertyService.Set (mdPropertyKey, true);

			var (preferences, options, preferencesValue) = await GetEditorPreferences (false);

			var optionsValue = options.GetOptionValue<bool> (editorOptionKey);

			Assert.AreEqual (true, optionsValue, "Option did not inherit default value override");
		}

		[Test]
		public async Task OptionDefaultValuesAreOverridden ()
		{
			var (preferences, options, preferencesValue) = await GetEditorPreferences ();

			var optionsValue = options.GetOptionValue<bool> (editorOptionKey);

			Assert.AreEqual (false, preferencesValue.Value, "Default value was not correctly overridden");
			Assert.AreEqual (preferencesValue.Value, optionsValue, "Option did not inherit default value override");
		}

		[Test]
		public async Task OptionsAreNotified ()
		{
			var (preferences, options, preferencesValue) = await GetEditorPreferences ();

			var optionsValue = options.GetOptionValue<bool> (editorOptionKey);

			int optionGotChangedFromConfigurationProperty = 0;
			options.OptionChanged += (sender, e) => {
				optionGotChangedFromConfigurationProperty++;
			};

			AssertValueToggled (
				ref optionGotChangedFromConfigurationProperty,
				preferencesValue.Value,
				newValue => preferencesValue.Value = newValue,
				shouldBe => Assert.AreEqual (shouldBe, options.GetOptionValue<bool> (editorOptionKey))
			);

			int optionGotChangedFromPropertyService = 0;
			options.OptionChanged += (sender, e) => {
				optionGotChangedFromPropertyService++;
			};

			AssertValueToggled (
				ref optionGotChangedFromPropertyService,
				preferencesValue.Value,
				newValue => PropertyService.Set (mdPropertyKey, newValue),
				shouldBe => Assert.AreEqual (shouldBe, options.GetOptionValue<bool> (editorOptionKey))
			);

			int propertyServiceWasChanged = 0;
			PropertyService.PropertyChanged += (sender, e) => {
				propertyServiceWasChanged++;
			};

			AssertValueToggled (
				ref propertyServiceWasChanged,
				preferencesValue.Value,
				newValue => options.SetOptionValue (editorOptionKey, newValue),
				shouldBe => Assert.AreEqual (shouldBe, preferencesValue.Value)
			);

			int configurationPropertyChanged = 0;
			preferencesValue.Changed += (sender, e) => {
				configurationPropertyChanged++;
			};

			AssertValueToggled (
				ref configurationPropertyChanged,
				preferencesValue.Value,
				newValue => options.SetOptionValue (editorOptionKey, newValue),
				shouldBe => Assert.AreEqual (shouldBe, preferencesValue.Value)
			);
		}

		void AssertValueToggled (ref int gotChanged, bool oldValue, Action<bool> set, Action<bool> assert)
		{
			var newValue = !oldValue;

			try {
				set (newValue);
				Assert.AreEqual (1, gotChanged);
				assert (newValue);
			} finally {
				set (oldValue); // PropertyService is a global service with mutable state, need to restore.
			}
		}
	}
}
