//
// MonoDevelopNuGetResourceProviderFactoryTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
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
using System.Collections.Generic;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class MonoDevelopNuGetResourceProviderFactoryTests
	{
		[Test]
		public void CustomProviderFactory_HasSameProvidersAsDefault_ExceptHttpHandlerResourceProvider ()
		{
			bool customHttpHandlerResourceV3Provider = false;
			var customProviderItems = new List<string> ();
			var defaultProviderItems = new List<string> ();

			// Touch the MonoDevelopNuGetResourceProviderFactory so it initializes the NuGet Respository.Provider.
			// The NuGet addin uses MonoDevelopNuGetResourceProviderFactory directly some parts of NuGet create
			// new SourceRepository instances using Repository.Provider directory.
			var _ = MonoDevelopNuGetResourceProviderFactory.GetProviders ();

			foreach (Lazy<INuGetResourceProvider> item in Repository.Provider.GetCoreV3 ()) {
				Type type = item.Value.GetType ();
				if (type == typeof (MonoDevelopHttpHandlerResourceV3Provider)) {
					type = typeof (HttpHandlerResourceV3Provider);
					customHttpHandlerResourceV3Provider = true;
				}
				customProviderItems.Add (type.Name);
			}

			var defaultProviderFactory = new Repository.ProviderFactory ();

			foreach (Lazy<INuGetResourceProvider> item in defaultProviderFactory.GetCoreV3 ()) {
				Type type = item.Value.GetType ();
				defaultProviderItems.Add (type.Name);
			}

			Assert.AreEqual (defaultProviderItems, customProviderItems);
			Assert.IsTrue (customHttpHandlerResourceV3Provider, "Custom MonoDevelopHttpHandlerResourceV3Provider not used");
		}
	}
}
