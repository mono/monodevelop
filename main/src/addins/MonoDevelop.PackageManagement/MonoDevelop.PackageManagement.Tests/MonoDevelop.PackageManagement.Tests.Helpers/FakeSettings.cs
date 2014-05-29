//
// FakeSettings.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
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
using System.Collections.Generic;
using ICSharpCode.PackageManagement;
using NuGet;
using System.Linq;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	/// <summary>
	/// TODO: Remove this class. Replace it with the actual NuGet.Settings class but
	/// use a fake IFileSystem instead. There is too much implementation in this test
	/// helper class and it makes the tests brittle now that we are using the
	/// NuGet.PackageSourceProvider instead of directly accessing the ISettings.
	/// </summary>
	public class FakeSettings : ISettings
	{
		public List<KeyValuePair<string, string>> PackageSources 
			= new List<KeyValuePair<string, string>> ();

		public List<KeyValuePair<string, string>> DisabledPackageSources
			= new List<KeyValuePair<string, string>> ();

		public List<KeyValuePair<string, string>> ActivePackageSourceSettings =
			new List<KeyValuePair<string, string>> ();

		public Dictionary<string, IList<KeyValuePair<string, string>>> Sections
			= new Dictionary<string, IList<KeyValuePair<string, string>>> ();

		public const string PackageSourcesSectionName = "packageSources";
		public const string DisabledPackageSourcesSectionName = "disabledPackageSources";

		public FakeSettings ()
		{
			Sections.Add (PackageSourcesSectionName, PackageSources);
			Sections.Add (RegisteredPackageSourceSettings.ActivePackageSourceSectionName, ActivePackageSourceSettings);
			Sections.Add (DisabledPackageSourcesSectionName, DisabledPackageSources);
		}

		public string GetValue (string section, string key)
		{
			if (!Sections.ContainsKey (section))
				return null;

			IList<KeyValuePair<string, string>> values = Sections [section];
			foreach (KeyValuePair<string, string> keyPair in values) {
				if (keyPair.Key == key) {
					return keyPair.Value;
				}
			}
			return null;
		}

		public IList<KeyValuePair<string, string>> GetValues (string section)
		{
			return Sections [section];
		}

		public void AddFakePackageSource (PackageSource packageSource)
		{
			var valuePair = new KeyValuePair<string, string> (packageSource.Name, packageSource.Source);
			PackageSources.Add (valuePair);
		}

		public Dictionary<string, KeyValuePair<string, string>> SavedSectionValues =
			new Dictionary<string, KeyValuePair<string, string>> ();

		public void SetValue (string section, string key, string value)
		{
			SavedSectionValues.Remove (section);
			SavedSectionValues.Add (section, new KeyValuePair<string, string> (key, value));
		}

		public KeyValuePair<string, string> GetValuePassedToSetValueForActivePackageSourceSection ()
		{
			return SavedSectionValues [RegisteredPackageSourceSettings.ActivePackageSourceSectionName];
		}

		public void SetValues (string section, IList<KeyValuePair<string, string>> values)
		{
			SavedSectionValueLists.Remove (section);
			SavedSectionValueLists.Add (section, values);
		}

		public Dictionary<string, IList<KeyValuePair<string, string>>> SavedSectionValueLists
			= new Dictionary<string, IList<KeyValuePair<string, string>>> ();

		public IList<KeyValuePair<string, string>> GetValuesPassedToSetValuesForPackageSourcesSection ()
		{
			return SavedSectionValueLists [PackageSourcesSectionName];
		}

		public bool DeleteValue (string section, string key)
		{
			throw new NotImplementedException ();
		}

		public List<string> SectionsDeleted = new List<string> ();

		public bool DeleteSection (string section)
		{
			SectionsDeleted.Add (section);
			return true;
		}

		public bool IsPackageSourcesSectionDeleted {
			get {
				return SectionsDeleted.Contains (PackageSourcesSectionName);
			}
		}

		public bool IsDisabledPackageSourcesSectionDeleted {
			get {
				return SectionsDeleted.Contains (DisabledPackageSourcesSectionName);
			}
		}

		public bool IsActivePackageSourceSectionDeleted {
			get {
				return SectionsDeleted.Contains (RegisteredPackageSourceSettings.ActivePackageSourceSectionName);
			}
		}

		public void SetFakeActivePackageSource (PackageSource packageSource)
		{
			ActivePackageSourceSettings.Clear ();
			var valuePair = new KeyValuePair<string, string> (packageSource.Name, packageSource.Source);
			ActivePackageSourceSettings.Add (valuePair);
		}

		public void MakeActivePackageSourceSectionNull ()
		{
			Sections.Remove (RegisteredPackageSourceSettings.ActivePackageSourceSectionName);
			Sections.Add (RegisteredPackageSourceSettings.ActivePackageSourceSectionName, null);
		}

		public void MakePackageSourceSectionsNull ()
		{
			Sections.Remove (PackageSourcesSectionName);
			Sections.Add (PackageSourcesSectionName, null);
		}

		public void AddFakePackageSources (IEnumerable<PackageSource> packageSources)
		{
			foreach (PackageSource packageSource in packageSources) {
				AddFakePackageSource (packageSource);
			}
		}

		public IList<KeyValuePair<string, string>> GetNestedValues (string section, string key)
		{
			return new List<KeyValuePair<string, string>> ();
		}

		public void SetNestedValues (string section, string key, IList<KeyValuePair<string, string>> values)
		{
			throw new NotImplementedException ();
		}

		public void AddDisabledPackageSource (PackageSource packageSource)
		{
			var valuePair = new KeyValuePair<string, string> (packageSource.Name, packageSource.Source);
			DisabledPackageSources.Add (valuePair);
		}

		public IList<KeyValuePair<string, string>> GetValuesPassedToSetValuesForDisabledPackageSourcesSection ()
		{
			return SavedSectionValueLists [DisabledPackageSourcesSectionName];
		}

		public bool AnyValuesPassedToSetValuesForDisabledPackageSourcesSection {
			get {
				return SavedSectionValueLists.ContainsKey (DisabledPackageSourcesSectionName);
			}
		}

		public void SetPackageRestoreSetting (bool enabled)
		{
			var items = new List<KeyValuePair<string, string>> ();
			items.Add (new KeyValuePair<string, string> ("enabled", enabled.ToString ()));
			Sections.Add ("packageRestore", items);
		}

		public KeyValuePair<string, string> GetValuePassedToSetValueForPackageRestoreSection ()
		{
			return SavedSectionValues ["packageRestore"];
		}

		public bool IsPackageRestoreSectionDeleted {
			get {
				return SectionsDeleted.Contains ("packageRestore");
			}
		}

		public string GetValue (string section, string key, bool isPath)
		{
			throw new NotImplementedException ();
		}

		public IList<KeyValuePair<string, string>> GetValues (string section, bool isPath)
		{
			throw new NotImplementedException ();
		}

		public IList<SettingValue> GetSettingValues (string section, bool isPath)
		{
			return Sections [section]
				.Select (item => new SettingValue (item.Key, item.Value, false))
				.ToList ();
		}
	}
}

