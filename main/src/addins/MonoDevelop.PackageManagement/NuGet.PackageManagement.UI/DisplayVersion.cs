// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Versioning;

namespace NuGet.PackageManagement.UI
{
	/// <summary>
	/// Represents a version that will be displayed on the UI.
	/// </summary>
	internal class DisplayVersion
	{
		private readonly string _additionalInfo;

		private readonly string _toString;

		public DisplayVersion(
			NuGetVersion version,
			string additionalInfo)
			: this(GetRange(version), additionalInfo)
		{
		}

		public DisplayVersion(
			VersionRange range,
			string additionalInfo)
		{
			Range = range;
			_additionalInfo = additionalInfo;

			Version = range.MinVersion;

			// Display a single version if the range is locked
			if (range.HasLowerAndUpperBounds && range.MinVersion == range.MaxVersion)
			{
				_toString = string.IsNullOrEmpty(_additionalInfo) ?
				                  Version.ToNormalizedString() :
				                  _additionalInfo + " " + Version.ToNormalizedString();
			}
			else
			{
				// Display the range, use the original value for floating ranges
				_toString = string.IsNullOrEmpty(_additionalInfo) ?
				                  Range.OriginalString :
				                  _additionalInfo + " " + Range.OriginalString;
			}
		}

		public NuGetVersion Version { get; }

		public VersionRange Range { get; }

		public override string ToString()
		{
			return _toString;
		}

		public override bool Equals(object obj)
		{
			var other = obj as DisplayVersion;
			return other != null && other.Version == Version;
		}

		public override int GetHashCode()
		{
			return Version.GetHashCode();
		}

		private static VersionRange GetRange(NuGetVersion version)
		{
			return new VersionRange(minVersion: version, includeMinVersion: true, maxVersion: version, includeMaxVersion: true);
		}
	}
}
