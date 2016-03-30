// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;

namespace NuGet.Protocol.VisualStudio
{
	/// <summary>
	/// Retrieves search metadata in the from used by the VS UI
	/// </summary>
	internal abstract class UISearchResource : INuGetResource
	{
		/// <summary>
		/// Retrieves search results
		/// </summary>
		public abstract Task<IEnumerable<UISearchMetadata>> Search(
			string searchTerm,
			SearchFilter filters,
			int skip,
			int take,
			CancellationToken cancellationToken);
	}
}

