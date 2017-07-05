//
// FakeNuGetSettings.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using NuGet.Configuration;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class FakeNuGetSettings : ISettings
	{
		public string FileName { get; set; } = "NuGet.Config";

		public IEnumerable<ISettings> Priority {
			get { yield return this; }
		}

		public string Root { get; set; } = string.Empty;

		public event EventHandler SettingsChanged;

		void OnSettingsChanged (object sender, EventArgs e)
		{
			SettingsChanged?.Invoke (sender, e);
		}

		public bool DeleteSection (string section)
		{
			throw new NotImplementedException ();
		}

		public bool DeleteValue (string section, string key)
		{
			throw new NotImplementedException ();
		}

		public IList<KeyValuePair<string, string>> GetNestedValues (string section, string subSection)
		{
			return new List<KeyValuePair<string, string>> ();
		}

		public Dictionary<string, List<SettingValue>> SettingValues = new Dictionary<string, List<SettingValue>> ();

		public IList<SettingValue> GetSettingValues (string section, bool isPath = false)
		{
			List<SettingValue> settings = null;
			if (SettingValues.TryGetValue (section, out settings))
				return settings;
			return new List<SettingValue> ();
		}

		public Dictionary<string, string> Values = new Dictionary<string, string> ();

		public string GetValue (string section, string key, bool isPath = false)
		{
			string value = null;
			if (Values.TryGetValue (GetKey (section, key), out value))
				return value;
			return null;
		}

		public void SetNestedValues (string section, string subSection, IList<KeyValuePair<string, string>> values)
		{
			throw new NotImplementedException ();
		}

		public void SetValue (string section, string key, string value)
		{
			Values [GetKey (section, key)] = value;
		}

		public void SetValues (string section, List<SettingValue> values)
		{
			SettingValues [section] = values;
		}

		public void SetValues (string section, IReadOnlyList<SettingValue> values)
		{
			throw new NotImplementedException ();
		}

		public void UpdateSections (string section, IReadOnlyList<SettingValue> values)
		{
			throw new NotImplementedException ();
		}

		static string GetKey (string section, string key)
		{
			return $"{section}-{key}";
		}
	}
}

