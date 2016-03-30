// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using NuGet.Protocol.VisualStudio;

namespace NuGet.PackageManagement.UI
{
	internal class SearchResult
	{
		public IReadOnlyList<UISearchMetadata> Items { get; set; }

		public bool HasMoreItems { get; set; }

		public static SearchResult Empty
		{
			get
			{
				return new SearchResult
				{
					Items = new List<UISearchMetadata>(),
					HasMoreItems = false,
				};
			}
		}
	}
}

