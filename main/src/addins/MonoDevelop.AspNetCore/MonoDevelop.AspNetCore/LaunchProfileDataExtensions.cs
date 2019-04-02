//
// LaunchProfileDataExtensions.cs
//
// Author:
//       José Miguel Torres <jostor@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

namespace MonoDevelop.AspNetCore
{
	internal static class LaunchProfileDataExtensions
	{
		public static T TryGetOtherSettings<T> (this LaunchProfileData launchProfile, string otherSetting)
		{
			if (launchProfile.OtherSettings != null && launchProfile.OtherSettings.TryGetValue (otherSetting, out var value))
				return (T)value;

			return default;
		}

		public static string TryGetApplicationUrl (this LaunchProfileData launchProfile)
		{
			return TryGetOtherSettings <string> (launchProfile, "applicationUrl") ?? string.Empty;
		}

		public static IDictionary<string, Dictionary<string, object>> ToSerializableForm (this IDictionary<string, LaunchProfileData> profiles)
		{
			var profileData = new Dictionary<string, Dictionary<string, object>> (StringComparer.Ordinal);
			foreach (var profile in profiles) {
				var launch = new LaunchProfile (profile.Value);
				profileData.Add (profile.Key, LaunchProfileData.ToSerializableForm (launch));
			}

			return profileData;
		}
	}
}
