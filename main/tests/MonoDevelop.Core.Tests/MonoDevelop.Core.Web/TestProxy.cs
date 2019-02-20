// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
//
// From: https://github.com/NuGet/NuGet.Client
// test/NuGet.Core.Tests/NuGet.Protocol.Tests/HttpSource/TestProxy.cs

using System;
using System.Net;

namespace MonoDevelop.Core.Web
{
	internal class TestProxy : IWebProxy
	{
		readonly Uri proxyAddress;

		public TestProxy (Uri proxyAddress)
		{
			this.proxyAddress = proxyAddress;
		}

		public ICredentials Credentials { get; set; }

		public Uri GetProxy (Uri destination) => proxyAddress;

		public bool IsBypassed (Uri host) => false;
	}
}