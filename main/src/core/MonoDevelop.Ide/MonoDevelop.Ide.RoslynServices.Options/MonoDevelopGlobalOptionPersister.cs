﻿//
// RoamingMonoDevelopProfileOptionPersister.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using MonoDevelop.Core;
using System.Runtime.Serialization;
using System.ComponentModel;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Policies;

namespace MonoDevelop.Ide.RoslynServices.Options
{
	/// <summary>
	/// Handles options persisting and bridging between roslyn and MonoDevelop.
	/// </summary>
	[Export (typeof (IOptionPersister))]
	sealed class MonoDevelopGlobalOptionPersister : IOptionPersister, IDisposable
	{
		readonly IGlobalOptionService globalOptionService;
		readonly RoslynPreferences preferences;

		Dictionary<IOption, Func<TextStylePolicy, object>> mapping = new Dictionary<IOption, Func<TextStylePolicy, object>> {
				{ FormattingOptions.UseTabs, policy => !policy.TabsToSpaces },
				{ FormattingOptions.TabSize, policy => policy.TabWidth },
				{ FormattingOptions.IndentationSize, policy => policy.IndentWidth },
				{ FormattingOptions.NewLine, policy => policy.GetEolMarker () },
		};

		[ImportingConstructor]
		public MonoDevelopGlobalOptionPersister (IGlobalOptionService globalOptionService) : this (globalOptionService, null)
		{
		}

		internal MonoDevelopGlobalOptionPersister (IGlobalOptionService globalOptionService, RoslynPreferences preferences)
		{
			Contract.ThrowIfNull (globalOptionService);
			this.globalOptionService = globalOptionService;

			this.preferences = preferences ?? IdeApp.Preferences.Roslyn;

			PropertyService.PropertyChanged += OnPropertyChanged;
		}

		void OnPropertyChanged (object sender, Core.PropertyChangedEventArgs args)
		{
			Refresh (args.Key, args.NewValue);
		}

		void IDisposable.Dispose ()
		{
			PropertyService.PropertyChanged -= OnPropertyChanged;
		}

		public bool TryFetch (OptionKey optionKey, out object value)
		{
			// Policy mapping to roslyn options
			if (mapping.TryGetValue (optionKey.Option, out var policyValueGetter)) {
				// TODO: Handle policy value changes?

				value = policyValueGetter (optionKey.GetTextStylePolicy ());
				return true;
			}

			// Property bindings
			if (preferences.TryGet (optionKey, out var storageKey, out var serializedValue)) {
				MonitorChanges (storageKey, optionKey);
				try {
					value = Deserialize (serializedValue, optionKey.Option.Type);
					return true;
				} catch (Exception ex) {
					LoggingService.LogError ($"Failed to deserialize type: {optionKey.Option.Type} value: {serializedValue}", ex);
				}
			}

			//use this for checking for options we could be handling
			//PrintOptionKey(optionKey);

			value = null;
			return false;
		}

		public bool TryPersist (OptionKey optionKey, object value)
		{
			// Property bindings
			if (preferences.TryGetUpdater (optionKey, out string storageKey, out var updater)) {
				MonitorChanges (storageKey, optionKey);
				var serializedValue = Serialize (value, optionKey.Option.Type);
				updater (serializedValue);
				return true;
			}

			return false;
		}

		readonly Dictionary<string, List<OptionKey>> _optionsToMonitorForChanges = new Dictionary<string, List<OptionKey>> ();

		public void MonitorChanges (string propertyName, OptionKey optionKey)
		{
			// We're about to fetch the value, so make sure that if it changes we'll know about it
			lock (_optionsToMonitorForChanges) {
				var optionKeysToMonitor = _optionsToMonitorForChanges.GetOrAdd (propertyName, _ => new List<OptionKey> ());

				if (!optionKeysToMonitor.Contains (optionKey))
					optionKeysToMonitor.Add (optionKey);
			}
		}

		public void Refresh (string propertyName, object newValue)
		{
			lock (_optionsToMonitorForChanges) {
				if (!_optionsToMonitorForChanges.TryGetValue (propertyName, out var optionsToRefresh))
					return;

				foreach (var optionToRefresh in optionsToRefresh) {
					globalOptionService.RefreshOption (optionToRefresh, Deserialize (newValue, optionToRefresh.Option.Type));
				}
			}
		}

		static void PrintOptionKey (OptionKey optionKey)
		{
			Console.WriteLine ($"Name '{optionKey.Option.Name}' Language '{optionKey.Language}' LanguageSpecific'{optionKey.Option.IsPerLanguage}'");

			var locations = optionKey.Option.StorageLocations;
			if (locations.IsDefault) {
				return;
			}

			foreach (var loc in locations) {
				switch (loc) {
				case RoamingProfileStorageLocation roaming:
					Console.WriteLine ($"    roaming: {roaming.GetKeyNameForLanguage (optionKey.Language)}");
					break;
				case LocalUserProfileStorageLocation local:
					Console.WriteLine ($"    local: {local.KeyName}");
					break;
				case EditorConfigStorageLocation<int> edconf:
					Console.WriteLine ($"    editorconfig: {edconf.KeyName}");
					break;
				case EditorConfigStorageLocation<string> edconf:
					Console.WriteLine ($"    editorconfig: {edconf.KeyName}");
					break;
				case EditorConfigStorageLocation<bool> edconf:
					Console.WriteLine ($"    editorconfig: {edconf.KeyName}");
					break;
				case EditorConfigStorageLocation<Microsoft.CodeAnalysis.CodeStyle.CodeStyleOption<bool>> edconf:
					Console.WriteLine ($"    editorconfig: {edconf.KeyName}");
					break;
				default:
					Console.WriteLine ($"    unknown: {loc.GetType ()}");
					break;
				}
			}
		}

		#region Serialization
		static object Serialize (object value, Type optionType)
		{
			// We store these as strings, so serialize
			if (value is ICodeStyleOption codeStyleOption)
				return codeStyleOption.ToXElement ().ToString ();

			if (optionType == typeof (NamingStylePreferences) && value is NamingStylePreferences valueToSerialize)
				return valueToSerialize.CreateXElement ().ToString ();

			return value;
		}

		static object Deserialize (object value, Type optionType)
		{
			if (optionType.IsEnum && value != null && optionType.IsEnumDefined (value))
				return Enum.ToObject (optionType, value);
				
			if (optionType == typeof (CodeStyleOption<bool>))
				return DeserializeCodeStyleOption (value, x => CodeStyleOption<bool>.FromXElement (x));

			if (optionType == typeof (CodeStyleOption<ExpressionBodyPreference>))
				return DeserializeCodeStyleOption (value, x => CodeStyleOption<ExpressionBodyPreference>.FromXElement (x));

			if (optionType == typeof (NamingStylePreferences))
				return DeserializeCodeStyleOption (value, x => NamingStylePreferences.FromXElement (x));

			if (optionType == typeof (bool)) {
				// TypeScript used to store some booleans as integers. We now handle them properly for legacy sync scenarios.
				if (value is int intValue)
					return intValue != 0;

				if (value is long longValue)
					return longValue != 0;
			}

			if (optionType == typeof (bool?)) {
				// code uses object to hold onto any value which will use boxing on value types.
				// see boxing on nullable types - https://msdn.microsoft.com/en-us/library/ms228597.aspx
				if (!(value is bool) && value != null)
					throw new SerializationException ();
			} else if (value != null && optionType != value.GetType ()) {
				// We got something back different than we expected, so fail to deserialize
				throw new SerializationException ();
			}

			return value;

			object DeserializeCodeStyleOption<T> (object v, Func<XElement, T> transform) => transform (XElement.Parse ((string)v));
		}
		#endregion
	}
}
