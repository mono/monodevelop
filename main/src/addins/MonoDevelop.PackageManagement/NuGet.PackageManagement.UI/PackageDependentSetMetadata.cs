// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;
using NuGet.Packaging;

namespace NuGet.PackageManagement.UI
{
	public class PackageDependencySetMetadata
	{
		public PackageDependencySetMetadata(PackageDependencyGroup dependencyGroup)
		{
			TargetFramework = dependencyGroup.TargetFramework;
			Dependencies = dependencyGroup.Packages
				.Select(d => new PackageDependencyMetadata(d))
				.ToList()
				.AsReadOnly();
		}

		public NuGetFramework TargetFramework { get; private set; }
		public IReadOnlyCollection<PackageDependencyMetadata> Dependencies { get; private set; }
	}
}