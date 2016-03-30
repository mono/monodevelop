// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Protocol.Core.Types;
using NuGet.Protocol.Core.v3;
using NuGet.Protocol.Core.v3.Data;

namespace NuGet.Protocol.VisualStudio
{
	internal class UISearchResourceV3Provider : ResourceProvider
	{
		private readonly DataClient _client;

		public UISearchResourceV3Provider()
			: this(new DataClient())
		{
		}

		public UISearchResourceV3Provider(DataClient client)
			: base(typeof(UISearchResource), nameof(UISearchResourceV3Provider), "UISearchResourceV2Provider")
		{
			_client = client;
		}

		public override async Task<Tuple<bool, INuGetResource>> TryCreate(SourceRepository source, CancellationToken token)
		{
			UISearchResourceV3 curResource = null;
			var serviceIndex = await source.GetResourceAsync<ServiceIndexResourceV3>(token);

			if (serviceIndex != null)
			{
				var rawSearch = await source.GetResourceAsync<RawSearchResourceV3>(token);
				var metadataResource = await source.GetResourceAsync<UIMetadataResource>(token);

				curResource = new UISearchResourceV3(rawSearch, metadataResource);
			}

			return new Tuple<bool, INuGetResource>(curResource != null, curResource);
		}
	}
}

