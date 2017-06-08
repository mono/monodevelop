//
// MSBuildSdksPathGlobalPropertyProvider.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Projects.MSBuild;
using System.Linq;

namespace MonoDevelop.DotNetCore
{
	/// <summary>
	/// Defines the MSBuildSDKsPath global property which is used by the Microsoft.NET.Sdk.Web
	/// MSBuild targets when importing the core Sdk.props. This property is not needed for
	/// .NET Core 1.0.4 SDK or above. The Microsoft.NET.Sdk.Web MSBuild targets in .NET Core
	/// 1.0.4 SDK do not use the MSBuildSDKsPath property when importing other targets but
	/// older SDKs do.
	///
	/// If the installed .NET Core SDK is older than 1.0.4 then the MSBuildSDKsPath global
	/// property is set. If the .NET Core SDK is not installed then the MSBuildSDKsPath
	/// global property is set if the .NET Core SDKs are shipped with Mono. The
	/// Microsoft.NET.Sdk.Web MSBuild targets that ship with Mono are from an older .NET
	/// Core SDK version which uses the MSBuildSDKsPath property to import other targets.
	/// </summary>
	class MSBuildSdksPathGlobalPropertyProvider : IMSBuildGlobalPropertyProvider
	{
		static DotNetCoreVersion minimumVersion = DotNetCoreVersion.Parse ("1.0.4");
		Dictionary<string, string> properties;
		readonly object propertiesLock = new object ();

		#pragma warning disable 67
		public event EventHandler GlobalPropertiesChanged;
		#pragma warning restore 67

		public IDictionary<string, string> GetGlobalProperties ()
		{
			if (properties == null) {
				lock (propertiesLock) {
					if (properties == null)
						properties = CreateProperties ();
				}
			}
			return properties;
		}

		static Dictionary<string, string> CreateProperties ()
		{
			var properties = new Dictionary<string, string> ();

			string sdksPath = GetMSBuildSDKsPath ();
			if (sdksPath != null)
				properties.Add ("MSBuildSDKsPath", sdksPath);

			return properties;
		}

		static string GetMSBuildSDKsPath ()
		{
			if (DotNetCoreSdk.IsInstalled) {
				var latestVersion = DotNetCoreSdk.Versions.FirstOrDefault ();
				if (latestVersion != null && latestVersion < minimumVersion) {
					return DotNetCoreSdk.MSBuildSDKsPath;
				}
			} else if (MSBuildSdks.Installed) {
				return MSBuildSdks.MSBuildSDKsPath;
			}

			return null;
		}
	}
}
