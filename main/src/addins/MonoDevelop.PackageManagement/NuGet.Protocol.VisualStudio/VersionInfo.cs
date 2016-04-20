// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGet.Versioning;

namespace NuGet.Protocol.VisualStudio
{
	internal class VersionInfo
	{
		public VersionInfo(NuGetVersion version, long? downloadCount)
		{
			Version = version;
			DownloadCount = downloadCount;
		}

		public NuGetVersion Version { get; private set; }

		public long? DownloadCount { get; private set; }
	}
}

