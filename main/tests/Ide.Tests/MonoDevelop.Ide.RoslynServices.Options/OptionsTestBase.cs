//
// OptionsTestHelper.cs
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.Core;
using NUnit.Framework;

namespace MonoDevelop.Ide.RoslynServices.Options
{
	public enum StorageLocationKind
	{
		None,
		Roaming,
		UserProfile
	}

	public abstract class OptionsTestBase
	{
		protected StorageLocationKind[] allKinds = {
			StorageLocationKind.None,
			StorageLocationKind.Roaming,
			StorageLocationKind.UserProfile
		};

		const string featureRoaming = "feature.test";
		const string featureRoamingLanguage = "feature.%LANGUAGE%.test";
		const string featureUserProfile = @"Some\Feature";

		[SetUp]
		public void BaseSetUp()
		{
			PropertyService.Set (GetExpectedPropertyName (StorageLocationKind.Roaming), null);
			PropertyService.Set (GetExpectedPropertyName (StorageLocationKind.UserProfile), null);

			foreach (var language in RoslynService.AllLanguages)
				PropertyService.Set (GetExpectedPropertyName (StorageLocationKind.Roaming, language), null);
		}

		OptionStorageLocation GetLocation (StorageLocationKind kind, bool isPerLanguage = false)
		{
			switch (kind)
			{
			case StorageLocationKind.None:
				return null;
			case StorageLocationKind.Roaming:
				return new RoamingProfileStorageLocation (isPerLanguage ? featureRoamingLanguage : featureRoaming);
			case StorageLocationKind.UserProfile:
				return new LocalUserProfileStorageLocation (featureUserProfile);
			}
			throw new NotImplementedException ();
		}

		protected Option<T> GetOption<T> (StorageLocationKind kind = StorageLocationKind.Roaming, T defaultValue = default (T))
		{
			var option = new Option<T> ("feature option", "name", defaultValue, GetLocation (kind));
			// We need a way to mock property service in tests
			var propName = new OptionKey (option).GetPropertyName ();
			if (propName != null)
				PropertyService.Set (propName, null);

			return option;
		}

		protected PerLanguageOption<T> GetPerLanguageOption<T> (StorageLocationKind kind = StorageLocationKind.Roaming, T defaultValue = default (T), string language = null)
		{
			var option = new PerLanguageOption<T> ("feature language option", "name", defaultValue, GetLocation (kind, true));

			var propName = new OptionKey (option, language).GetPropertyName ();
			if (propName != null)
				PropertyService.Set (propName, null);

			return option;
		}

		protected string GetExpectedPropertyName (StorageLocationKind kind, string language = null)
		{
			switch (kind)
			{
			case StorageLocationKind.None:
				return null;
			case StorageLocationKind.Roaming:
				if (language == null)
					return featureRoaming;

				string substituteLanguageName = language == LanguageNames.CSharp ? "CSharp" :
												language == LanguageNames.VisualBasic ? "VisualBasic" :
												language;

				return featureRoamingLanguage.Replace ("%LANGUAGE%", substituteLanguageName);
			case StorageLocationKind.UserProfile:
				return featureUserProfile;
			}
			throw new NotImplementedException ();
		}

		protected IEnumerable<OptionKey> GetOptionKeys<T> (StorageLocationKind kind = StorageLocationKind.Roaming, T defaultValue = default(T))
		{
			yield return GetOption (kind, defaultValue);

			if (kind != StorageLocationKind.UserProfile)
				yield return new OptionKey (GetPerLanguageOption (kind, defaultValue, LanguageNames.CSharp), LanguageNames.CSharp);
		}

		internal (RoslynPreferences, MonoDevelopGlobalOptionPersister) Setup ()
		{
			var (preferences, persister, _) = SetupOptions ();
			return (preferences, persister);
		}

		internal (RoslynPreferences, MonoDevelopGlobalOptionPersister, MockOptionService) SetupOptions ()
		{
			var preferences = new RoslynPreferences ();
			var optionsService = new MockOptionService ();
			var persister = new MonoDevelopGlobalOptionPersister (optionsService, preferences);
			optionsService.RegisterPersister (persister);

			return (preferences, persister, optionsService);
		}
	}
}
