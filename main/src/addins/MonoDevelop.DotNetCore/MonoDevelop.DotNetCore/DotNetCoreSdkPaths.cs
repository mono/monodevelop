//
// DotNetCoreSdkPaths.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreSdkPaths
	{
		string msbuildSDKsPath;
		string sdkRootPath;

		public void FindMSBuildSDKsPath ()
		{
			var dotNetCorePath = new DotNetCorePath ();
			if (dotNetCorePath.IsMissing)
				return;

			string rootDirectory = Path.GetDirectoryName (dotNetCorePath.FileName);
			sdkRootPath = Path.Combine (rootDirectory, "sdk");
			if (!Directory.Exists (sdkRootPath))
				return;

			SdkVersions = GetInstalledSdkVersions (sdkRootPath)
				.OrderByDescending (version => version)
				.ToArray ();
			if (!SdkVersions.Any ())
				return;

			DotNetCoreVersion latestVersion = SdkVersions.FirstOrDefault ();
			SdksParentDirectory = Path.Combine (sdkRootPath, latestVersion.OriginalString);
			if (SdksParentDirectory == null)
				return;

			msbuildSDKsPath = Path.Combine (SdksParentDirectory, "Sdks");

			MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", MSBuildSDKsPath);
		}

		public void FindSdkPaths (string sdk)
		{
			if (string.IsNullOrEmpty (MSBuildSDKsPath))
				return;

			Exist = CheckSdksExist (sdk);

			if (Exist) {
				IsUnsupportedSdkVersion = !CheckIsSupportedSdkVersion (SdksParentDirectory);
				Exist = !IsUnsupportedSdkVersion;
			} else {
				IsUnsupportedSdkVersion = true;
			}
		}

		public bool IsUnsupportedSdkVersion { get; private set; }
		public bool Exist { get; private set; }

		public string MSBuildSDKsPath {
			get { return msbuildSDKsPath; }
			internal set {
				msbuildSDKsPath = value;
				if (!string.IsNullOrEmpty (msbuildSDKsPath)) {
					SdksParentDirectory = Path.GetDirectoryName (msbuildSDKsPath);
				}
			}
		}

		public string SdkRootPath {
			get { return sdkRootPath; }
		}

		public DotNetCoreVersion[] SdkVersions { get; private set; }

		string SdksParentDirectory { get; set; }

		static IEnumerable<string> SplitSdks (string sdk)
		{
			return sdk.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		}

		bool CheckSdksExist (string sdk)
		{
			if (sdk.Contains (';')) {
				foreach (string sdkItem in SplitSdks (sdk)) {
					if (!SdkPathExists (sdkItem))
						return false;
				}
				return true;
			}
			return SdkPathExists (sdk);
		}

		bool SdkPathExists (string sdk)
		{
			string sdkDirectory = Path.Combine (MSBuildSDKsPath, sdk);
			return Directory.Exists (sdkDirectory);
		}

		/// <summary>
		/// .NET Core SDK version needs to be at least 1.0.0
		/// </summary>
		bool CheckIsSupportedSdkVersion (string sdkDirectory)
		{
			try {
				string sdkVersion = Path.GetFileName (sdkDirectory);
				DotNetCoreVersion version = null;
				if (DotNetCoreVersion.TryParse (sdkVersion, out version)) {
					if (version < DotNetCoreVersion.MinimumSupportedVersion) {
						LoggingService.LogInfo ("Unsupported .NET Core SDK version installed '{0}'. Require at least 1.0.0. '{1}'", sdkVersion, sdkDirectory);
						return false;
					}
				} else {
					LoggingService.LogWarning ("Unable to get version information for .NET Core SDK. '{0}'", sdkDirectory);
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Error checking sdk version.", ex);
			}
			return true;
		}

		IEnumerable<DotNetCoreVersion> GetInstalledSdkVersions (string sdkRootPath)
		{
			return Directory.EnumerateDirectories (sdkRootPath)
				.Select (directory => DotNetCoreVersion.GetDotNetCoreVersionFromDirectory (directory))
				.Where (version => version != null);
		}
	}
}
