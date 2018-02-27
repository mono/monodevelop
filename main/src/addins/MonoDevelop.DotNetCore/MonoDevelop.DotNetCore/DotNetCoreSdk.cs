//
// DotNetCoreSdk.cs
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

namespace MonoDevelop.DotNetCore
{
	public static class DotNetCoreSdk
	{
		static DotNetCoreSdk ()
		{
			var sdkPaths = new DotNetCoreSdkPaths ();
			sdkPaths.FindMSBuildSDKsPath ();

			SdkRootPath = sdkPaths.SdkRootPath;
			MSBuildSDKsPath = sdkPaths.MSBuildSDKsPath;
			IsInstalled = !string.IsNullOrEmpty (MSBuildSDKsPath);
			Versions = sdkPaths.SdkVersions ?? new DotNetCoreVersion [0];

			if (!IsInstalled)
				LoggingService.LogInfo (".NET Core SDK not found.");

			if (IsInstalled)
				SetFSharpShims ();
		}

		public static bool IsInstalled { get; private set; }
		public static string MSBuildSDKsPath { get; private set; }
		internal static string SdkRootPath { get; private set; }

		internal static DotNetCoreVersion[] Versions { get; private set; }

		internal static void EnsureInitialized ()
		{
		}

		internal static DotNetCoreSdkPaths FindSdkPaths (string sdk)
		{
			var sdkPaths = new DotNetCoreSdkPaths ();
			sdkPaths.MSBuildSDKsPath = MSBuildSDKsPath;
			sdkPaths.FindSdkPaths (sdk);

			return sdkPaths;
		}

		/// <summary>
		/// Checks that the target framework (e.g. .NETCoreApp1.1 or .NETStandard2.0) is supported
		/// by the installed SDKs. Takes into account Mono having .NET Core v1 SDKs installed.
		/// </summary>
		internal static bool IsSupported (TargetFramework framework)
		{
			return IsSupported (framework.Id, Versions, MSBuildSdks.Installed);
		}

		/// <summary>
		/// Used by unit tests.
		/// </summary>
		internal static bool IsSupported (
			TargetFrameworkMoniker projectFramework,
			DotNetCoreVersion[] versions,
			bool msbuildSdksInstalled)
		{
			if (!projectFramework.IsNetStandardOrNetCoreApp ()) {
				// Allow other frameworks to be supported such as .NET Framework.
				return true;
			}

			var projectFrameworkVersion = Version.Parse (projectFramework.Version);

			if (versions.Any (sdkVersion => IsSupported (projectFrameworkVersion, sdkVersion)))
				return true;

			// .NET Core 1.x is supported by the MSBuild .NET Core SDKs if they are installed with Mono.
			if (projectFrameworkVersion.Major == 1)
				return msbuildSdksInstalled;

			return false;
		}

		/// <summary>
		/// Project framework version is considered supported if the major version of the
		/// .NET Core SDK is greater or equal to the major version of the project framework.
		/// The fact that a .NET Core SDK is a preview version is ignored in this check.
		///
		/// .NET Core SDK 1.0.4 supports .NET Core 1.0 and 1.1
		/// .NET Core SDK 1.0.4 supports .NET Standard 1.0 to 1.6
		/// .NET Core SDK 2.0 supports 1.0, 1.1 and 2.0
		/// .NET Core SDK 2.0 supports .NET Standard 1.0 to 1.6 and 2.0
		/// .NET Core SDK 2.1 supports 1.0, 1.1 and 2.0
		/// .NET Core SDK 2.1 supports .NET Standard 1.0 to 1.6 and 2.0
		/// </summary>
		static bool IsSupported (Version projectFrameworkVersion, DotNetCoreVersion sdkVersion)
		{
			return sdkVersion.Major >= projectFrameworkVersion.Major;
		}

		/// <summary>
		/// This is a workaround to allow F# .NET Core 2.0 projects to be evaluated properly and compile
		/// without any errors. The better solution would be to ship the new Microsoft.FSharp.NetSdk.props
		/// and .targets files with Mono so this workaround can be removed. Setting the FSharpPropsShim
		/// and FSharpTargetsShim as environment variables allows the correct MSBuild imports to be used
		/// when building and evaluating. Just setting a global MSBuild property would fix the Build target
		/// not being found but the MSBuild project evaluation would not add the FSharp.Core PackageReference
		/// to the project.assets.json file, also all .fs files were treated as None items instead of
		/// Compile items.
		/// </summary>
		static void SetFSharpShims ()
		{
			var latestVersion = Versions.FirstOrDefault ();
			if (latestVersion != null && latestVersion.Major == 2) {
				FilePath directory = FilePath.Build (MSBuildSDKsPath, "..", "FSharp");

				Environment.SetEnvironmentVariable ("FSharpPropsShim", directory.Combine ("Microsoft.FSharp.NetSdk.props").FullPath);
				Environment.SetEnvironmentVariable ("FSharpTargetsShim", directory.Combine ("Microsoft.FSharp.NetSdk.targets").FullPath);
			}
		}

		/// <summary>
		/// Used by unit tests to fake having different .NET Core sdks installed.
		/// </summary>
		internal static void SetVersions (IEnumerable<DotNetCoreVersion> versions)
		{
			Versions = versions.ToArray ();
		}

		/// <summary>
		/// Used by unit tests to fake having the sdk installed.
		/// </summary>
		internal static void SetInstalled (bool installed)
		{
			IsInstalled = installed;
		}

		/// <summary>
		/// Used by unit tests to fake having the sdk installed.
		/// </summary>
		internal static void SetSdkRootPath (string path)
		{
			SdkRootPath = path;
		}
	}
}
