//
// WildcardVersion.cs
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
using System.Linq;
using NuGet;

namespace MonoDevelop.PackageManagement
{
	/// <summary>
	/// Implicit wildcard version specification.
	/// 
	/// 1 => 1.*
	/// 1.2 => 1.2.*
	/// 1.2.3 => 1.2.3.*
	/// 1.2.3.4 => 1.2.3.4
	/// </summary>
	public class WildcardVersionSpec
	{
		string[] parts;
		int wildcardPart = -1;

		bool HasWildcards {
			get { return wildcardPart != -1; }
		}

		public WildcardVersionSpec (string version)
		{
			Parse (version);
		}

		public VersionSpec VersionSpec { get; private set; }

		void Parse (string versionText)
		{
			SplitIntoParts (versionText);

			if (!ParseExactVersion (ConvertVersionIfSingleNumber (versionText))) {
				return;
			}

			ConfigureMaximumVersion ();
		}

		void SplitIntoParts (string versionText)
		{
			parts = versionText.Split ('.');
		}

		/// <summary>
		/// Version "1" will fail to be parsed by the SemanticVersion
		/// class so append ".0" to allow the conversion to succeed.
		/// </summary>
		string ConvertVersionIfSingleNumber (string versionText)
		{
			if (parts.Length == 1) {
				return versionText + ".0";
			}
			return versionText;
		}

		bool ParseExactVersion (string versionText)
		{
			SemanticVersion version = null;
			if (SemanticVersion.TryParse (versionText, out version)) {
				VersionSpec = new VersionSpec (version);
				return true;
			}
			return false;
		}

		void ConfigureMaximumVersion ()
		{
			if (!NeedsWildCard ())
				return;

			VersionSpec.IsMaxInclusive = false;
			VersionSpec.MaxVersion = GetMaximumWildcardVersion (VersionSpec.MinVersion);
		}

		bool NeedsWildCard ()
		{
			return parts.Length < 4;
		}

		string GetMinimumWildcardVersion (string wildcardVersion)
		{
			return wildcardVersion.Replace ('*', '0');
		}

		SemanticVersion GetMaximumWildcardVersion (SemanticVersion minVersion)
		{
			return new SemanticVersion (GetMaximumWildcardVersion (minVersion.Version));
		}

		Version GetMaximumWildcardVersion (Version minVersion)
		{
			switch (parts.Length) {
			case 1:
				return new Version (minVersion.Major + 1, 0, 0, 0);
			case 2:
				return new Version (minVersion.Major, minVersion.Minor + 1, 0, 0);
			default:
				return new Version (minVersion.Major, minVersion.Minor, minVersion.Build + 1, 0);
			}
		}

		public bool Satisfies (SemanticVersion version)
		{
			if (VersionSpec == null)
				return true;

			if (!IsSpecial (VersionSpec.MinVersion) && IsSpecial (version)) {
				version = RemoveSpecialPart (version);
			}
			return VersionSpec.Satisfies (version);
		}

		static bool IsSpecial (SemanticVersion version)
		{
			return !String.IsNullOrEmpty (version.SpecialVersion);
		}

		SemanticVersion RemoveSpecialPart (SemanticVersion version)
		{
			return new SemanticVersion (version.Version);
		}
	}
}

