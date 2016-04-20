// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol.VisualStudio
{
	internal abstract class UIMetadataResource : INuGetResource
	{
		/// <summary>
		/// Returns all versions of a package
		/// </summary>
		public abstract Task<IEnumerable<UIPackageMetadata>> GetMetadata(string packageId, bool includePrerelease, bool includeUnlisted, CancellationToken token);

		/// <summary>
		/// Returns Metadata for a single package
		/// </summary>
		public async Task<UIPackageMetadata> GetMetadata(PackageIdentity package, CancellationToken token)
		{
			var results = await GetMetadata(new PackageIdentity[] { package }, token);

			return results.SingleOrDefault();
		}

		/// <summary>
		/// Returns metadata for all packages
		/// </summary>
		public abstract Task<IEnumerable<UIPackageMetadata>> GetMetadata(IEnumerable<PackageIdentity> packages, CancellationToken token);
	}
}

