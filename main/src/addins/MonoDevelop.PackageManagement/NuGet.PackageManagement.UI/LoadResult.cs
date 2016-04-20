// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace NuGet.PackageManagement.UI
{
	internal class LoadResult
	{
		public IReadOnlyList<PackageItemListViewModel> Items { get; set; }

		public bool HasMoreItems { get; set; }

		public int NextStartIndex { get; set; }
	}
}

