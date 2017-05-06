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
		List<string> projectImportProps = new List<string> ();
		List<string> projectImportTargets = new List<string> ();
		string msbuildSDKsPath;

		public void FindMSBuildSDKsPath ()
		{
			var dotNetCorePath = new DotNetCorePath ();
			if (dotNetCorePath.IsMissing)
				return;

			string rootDirectory = Path.GetDirectoryName (dotNetCorePath.FileName);
			string sdkRootPath = Path.Combine (rootDirectory, "sdk");
			if (!Directory.Exists (sdkRootPath))
				return;

			string[] directories = Directory.GetDirectories (sdkRootPath);
			SdksParentDirectory = directories.OrderBy (directory => directory).LastOrDefault ();
			if (SdksParentDirectory == null)
				return;

			LatestSdkFullVersion = Path.GetFileName (SdksParentDirectory);

			msbuildSDKsPath = Path.Combine (SdksParentDirectory, "Sdks");

			MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", MSBuildSDKsPath);
		}

		public void FindSdkPaths (string sdk)
		{
			if (string.IsNullOrEmpty (MSBuildSDKsPath))
				return;

			if (sdk.Contains (';')) {
				foreach (string sdkItem in SplitSdks (sdk)) {
					AddSdkImports (sdkItem);
				}
			} else {
				AddSdkImports (sdk);
			}

			Exist = CheckImportsExist ();

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

		public string LatestSdkFullVersion { get; private set; }

		string SdksParentDirectory { get; set; }

		public IEnumerable<string> ProjectImportProps {
			get { return projectImportProps; }
		}

		public IEnumerable<string> ProjectImportTargets {
			get { return projectImportTargets; }
		}

		static IEnumerable<string> SplitSdks (string sdk)
		{
			return sdk.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
		}

		void AddSdkImports (string sdk)
		{
			string sdkMSBuildTargetsDirectory = Path.Combine (MSBuildSDKsPath, sdk, "Sdk");
			projectImportProps.Add (Path.Combine (sdkMSBuildTargetsDirectory, "Sdk.props"));
			projectImportTargets.Add (Path.Combine (sdkMSBuildTargetsDirectory, "Sdk.targets"));
		}

		bool CheckImportsExist ()
		{
			foreach (string prop in ProjectImportProps) {
				if (!File.Exists (prop)) {
					LoggingService.LogError ("Sdk.props not found. '{0}'", prop);
					return false;
				}
			}

			foreach (string target in ProjectImportTargets) {
				if (!File.Exists (target)) {
					LoggingService.LogError ("Sdk.targets not found. '{0}'", target);
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// .NET Core SDK version needs to be at least 1.0.0-preview5-004460
		/// </summary>
		bool CheckIsSupportedSdkVersion (string sdkDirectory)
		{
			try {
				string sdkVersion = Path.GetFileName (sdkDirectory);
				int buildVersion = -1;
				if (DotNetCoreSdkVersion.TryGetBuildVersion (sdkVersion, out buildVersion)) {
					if (buildVersion < DotNetCoreSdkVersion.MinimumSupportedBuildVersion) {
						LoggingService.LogInfo ("Unsupported .NET Core SDK version installed '{0}'. Require at least 1.0.0-preview5-004460. '{1}'", sdkVersion, sdkDirectory);
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
	}
}
