//
// DotNetCoreVersion.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//       Lluis Sanchez <lluis@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

namespace MonoDevelop.Projects.MSBuild
{
	/// <summary>
	/// The representation of a version, with the format: major.minor.patch-releaseLabel+buildLabel
	/// </summary>
	public class SdkVersion : IEquatable<SdkVersion>, IComparable<SdkVersion>
	{
		SdkVersion (string originalString, Version version, string releaseLabel, string buildLabel)
		{
			this.originalString = originalString;
			Version = version;
			ReleaseLabel = releaseLabel;
			BuildLabel = buildLabel;
		}

		readonly string originalString;

		/// <summary>
		/// Gets the major, minor and patch components as a System.Version instance
		/// </summary>
		/// <value>The version.</value>
		public Version Version { get; private set; }

		/// <summary>
		/// Gets the major component of the version
		/// </summary>
		/// <value>The major.</value>
		public int Major => Version.Major;

		/// <summary>
		/// Gets the minor component of the version
		/// </summary>
		/// <value>The minor.</value>
		public int Minor => Version.Minor;

		/// <summary>
		/// Gets the patch component of the version
		/// </summary>
		/// <value>The patch.</value>
		public int Patch => Version.Build;

		/// <summary>
		/// Gets a value indicating whether this <see cref="T:MonoDevelop.Projects.MSBuild.SdkVersion"/> is a pre-release version.
		/// </summary>
		/// <value><c>true</c> if is prerelease; otherwise, <c>false</c>.</value>
		public bool IsPrerelease => ReleaseLabel.Length > 0;

		/// <summary>
		/// Gets the release label.
		/// </summary>
		/// <value>The release label.</value>
		public string ReleaseLabel { get; private set; }

		/// <summary>
		/// Gets the build label.
		/// </summary>
		/// <value>The build label.</value>
		public string BuildLabel { get; private set; }

		public override string ToString () => originalString;

		/// <summary>
		/// Parses a version from a string
		/// </summary>
		public static SdkVersion Parse (string input)
		{
			if (string.IsNullOrEmpty (input))
				throw new ArgumentException ("Version cannot be null or an empty string.", nameof (input));

			SdkVersion version = null;

			if (TryParse (input, out version))
				return version;

			throw new FormatException (string.Format ("Invalid version: '{0}'", input));
		}

		/// <summary>
		/// Tries to parse a version from a string
		/// </summary>
		/// <returns><c>true</c>, if version could be parsed, <c>false</c> otherwise.</returns>
		/// <param name="input">Input string</param>
		/// <param name="result">Parsed version</param>
		public static bool TryParse (string input, out SdkVersion result)
		{
			result = null;

			if (string.IsNullOrEmpty (input))
				return false;

			string versionString = input;
			string releaseLabel = string.Empty;
			string buildLabel = string.Empty;

			int prereleaseLabelStart = input.IndexOf ('-');
			if (prereleaseLabelStart >= 0) {
				versionString = input.Substring (0, prereleaseLabelStart++);
				int buildLabelStart = input.IndexOf ('+', prereleaseLabelStart);
				if (buildLabelStart >= 0) {
					releaseLabel = input.Substring (prereleaseLabelStart, buildLabelStart - prereleaseLabelStart);
					buildLabel = input.Substring (buildLabelStart + 1);
				} else
					releaseLabel = input.Substring (prereleaseLabelStart);
			}

			Version version = null;
			if (!Version.TryParse (versionString, out version))
				return false;

			result = new SdkVersion (input, version, releaseLabel, buildLabel);
			return true;
		}

		public override int GetHashCode ()
		{
			return originalString.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			return Equals (obj as SdkVersion);
		}

		public bool Equals (SdkVersion other)
		{
			return CompareTo (other) == 0;
		}

		public int CompareTo (SdkVersion other)
		{
			if ((object)other == null)
				return 1;

			int result = Version.CompareTo (other.Version);
			if (result != 0)
				return result;

			if (!IsPrerelease && !other.IsPrerelease)
				return result;

			// Pre-release versions are lower than stable versions.
			if (IsPrerelease && !other.IsPrerelease)
				return -1;

			if (!IsPrerelease && other.IsPrerelease)
				return 1;

			result = StringComparer.OrdinalIgnoreCase.Compare (ReleaseLabel, other.ReleaseLabel);
			if (result != 0)
				return result;

			return StringComparer.OrdinalIgnoreCase.Compare (BuildLabel, other.BuildLabel);
		}

		public static bool operator > (SdkVersion a, SdkVersion b)
		{
			if ((object)a == null)
				throw new ArgumentNullException (nameof (a));
			if ((object)b == null)
				throw new ArgumentNullException (nameof (b));
			return a.CompareTo (b) > 0;
		}

		public static bool operator < (SdkVersion a, SdkVersion b)
		{
			if ((object)a == null)
				throw new ArgumentNullException (nameof (a));
			if ((object)b == null)
				throw new ArgumentNullException (nameof (b));
			return a.CompareTo (b) < 0;
		}

		public static bool operator == (SdkVersion a, SdkVersion b)
		{
			if ((object)a == null && (object)b == null)
				return true;
			if ((object)a == null || (object)b == null)
				return false;
			return a.CompareTo(b) == 0;
		}

		public static bool operator != (SdkVersion a, SdkVersion b)
		{
			if ((object)a == null && (object)b == null)
				return false;
			if ((object)a == null || (object)b == null)
				return true;
			return a.CompareTo (b) != 0;
		}
	}
}