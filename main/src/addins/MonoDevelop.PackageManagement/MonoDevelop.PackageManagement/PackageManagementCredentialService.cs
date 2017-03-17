﻿//
// PackageManagementCredentialService.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.
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
using MonoDevelop.Core;
using NuGet.CommandLine;
using NuGet.Credentials;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementCredentialService
	{
		public void Initialize ()
		{
			try {
				InitializeDefaultCredentialProvider ();
			} catch (Exception ex) {
				LoggingService.LogError ("Failed to initialize PackageManagementCredentialService.", ex);
			}
		}

		void InitializeDefaultCredentialProvider ()
		{
			var credentialService = new CredentialService (
				GetCredentialProviders (),
				nonInteractive: false);

			HttpHandlerResourceV3Extensions.InitializeHttpHandlerResourceV3 (credentialService);
		}

		IEnumerable<ICredentialProvider> GetCredentialProviders ()
		{
			var credentialProviders = new List<ICredentialProvider>();

			credentialProviders.Add (CreateSettingsCredentialProvider ());
			credentialProviders.Add (new MonoDevelopCredentialProvider ());

			return credentialProviders;
		}

		static SettingsCredentialProvider CreateSettingsCredentialProvider ()
		{
			var settings = SettingsLoader.LoadDefaultSettings ();
			var packageSourceProvider = new MonoDevelopPackageSourceProvider (settings);
			return new SettingsCredentialProvider (packageSourceProvider);
		}
	}
}

