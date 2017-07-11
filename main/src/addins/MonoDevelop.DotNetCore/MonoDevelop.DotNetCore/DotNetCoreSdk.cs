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
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.DotNetCore
{
	public static class DotNetCoreSdk
	{
		static DotNetCoreSdk ()
		{
			var sdkPaths = new DotNetCoreSdkPaths ();
			sdkPaths.FindMSBuildSDKsPath ();

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
	}
}
