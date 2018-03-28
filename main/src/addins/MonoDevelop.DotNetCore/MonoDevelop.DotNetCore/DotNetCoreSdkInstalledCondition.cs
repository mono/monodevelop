//
// DotNetCoreSdkInstalledCondition.cs
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
using Mono.Addins;

namespace MonoDevelop.DotNetCore
{
	internal class DotNetCoreSdkInstalledCondition : ConditionType
	{
		/// <summary>
		/// The SDK version check is not quite correct. It currently only checks the
		/// latest installed version when it should check all versions installed.
		/// The runtime check also needs improving. Currently it only checks that dotnet
		/// is available. It should check the runtimes installed so it is possible
		/// to create a .NET Core 2.0 project if only the 2.0 runtime is installed.
		///
		/// Note that the .NET Core SDK version is the logical version. Currently
		/// .NET Core SDK 2.1.4 supports .NET Core 2.0 projects but it is considered
		/// here to be version 2.0. .NET Core 2.1.300 supports .NET Core 2.1 projects
		/// so it is considered to be version 2.1
		/// </summary>
		public override bool Evaluate (NodeElement conditionNode)
		{
			if (DotNetCoreSdk.IsInstalled && SdkVersionSupported (conditionNode, DotNetCoreSdk.Versions))
				return true;

			// Mono's MSBuild SDKs currently includes .NET Core SDK 1.0.
			if (MSBuildSdks.Installed && SdkVersionSupported (conditionNode, DotNetCoreVersion.MinimumSupportedVersion))
				return DotNetCoreRuntime.IsInstalled || !RequiresRuntime (conditionNode);

			return false;
		}

		/// <summary>
		/// Supports simple wildcards. 1.* => 1.0, 1.2, up to but not including 2.0.
		/// Wildcards such as 1.*.3 are not supported.
		/// </summary>
		static bool SdkVersionSupported (NodeElement conditionNode, params DotNetCoreVersion[] versions)
		{
			string requiredSdkversion = conditionNode.GetAttribute ("sdkVersion");
			if (string.IsNullOrEmpty (requiredSdkversion))
				return true;

			// Special case '2.1' and '2.0'.
			if (requiredSdkversion == "2.1") {
				return versions.Any (IsNetCoreSdk21);
			} else if (requiredSdkversion == "2.0") {
				return versions.Any (IsNetCoreSdk20);
			}

			requiredSdkversion = requiredSdkversion.Replace ("*", string.Empty);
			return versions.Any (version => version.ToString ().StartsWith (requiredSdkversion, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// 2.1.300 is the lowest version that supports .NET Core 2.1 projects.
		/// </summary>
		static bool IsNetCoreSdk21 (DotNetCoreVersion version)
		{
			return version.Major == 2 && version.Minor == 1 && version.Patch >= 300;
		}

		/// <summary>
		/// 2.1.300 is the lowest version that supports .NET Core 2.1 projects.
		/// </summary>
		static bool IsNetCoreSdk20 (DotNetCoreVersion version)
		{
			return version.Major == 2 && version.Minor <= 1 && version.Patch < 300;
		}

		/// <summary>
		/// .NET Standard library projects do not require the .NET Core runtime.
		/// </summary>
		static bool RequiresRuntime (NodeElement conditionNode)
		{
			string value = conditionNode.GetAttribute ("requiresRuntime");
			if (string.IsNullOrEmpty (value))
				return true;

			bool result = true;
			if (bool.TryParse (value, out result))
				return result;

			return true;
		}
	}
}
