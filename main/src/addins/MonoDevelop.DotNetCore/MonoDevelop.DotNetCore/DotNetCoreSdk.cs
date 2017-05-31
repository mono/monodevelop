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
using System.IO;
using System.Xml;
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

			if (IsInstalled)
				GetPreviewNetStandard20LibraryVersion ();

			if (!IsInstalled)
				LoggingService.LogInfo (".NET Core SDK not found.");
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

		internal static string PreviewNetStandard20LibraryVersion { get; private set; }

		static void GetPreviewNetStandard20LibraryVersion ()
		{
			var latestInstalledVersion = Versions.FirstOrDefault ();
			if (latestInstalledVersion == null)
				return;

			if (latestInstalledVersion.Major == 2 &&
			    latestInstalledVersion.Minor == 0 &&
			    latestInstalledVersion.IsPrerelease) {
				PreviewNetStandard20LibraryVersion = ReadBundledNETStandardPackageVersion ();
			}
		}

		static string ReadBundledNETStandardPackageVersion ()
		{
			string sdkRoot = Path.GetDirectoryName (MSBuildSDKsPath);
			string fileName = Path.Combine (sdkRoot, "Microsoft.NETCoreSdk.BundledVersions.props");

			try {
				if (!File.Exists (fileName))
					return null;

				using (var reader = XmlReader.Create (fileName)) {
					while (reader.Read ()) {
						switch (reader.NodeType) {
						case XmlNodeType.Element:
							if (reader.LocalName == "BundledNETStandardPackageVersion")
								return reader.ReadElementContentAsString ();
							break;
						}
					}
				}
			} catch (Exception ex) {
				LoggingService.LogError (string.Format ("Unable to read '{0}'", fileName), ex);
			}
			return null;
		}
	}
}
