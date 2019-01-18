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
using Newtonsoft.Json.Linq;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreSdkPaths
	{
		string msbuildSDKsPath;

		public DotNetCoreVersion [] SdkVersions { get; internal set; } = Array.Empty<DotNetCoreVersion> ();
		public string SdkRootPath { get; internal set; }
		public string GlobalJsonPath { get; set; }

		public DotNetCoreSdkPaths (string dotNetCorePath = "")
		{
			if (string.IsNullOrEmpty (dotNetCorePath)) {
				if (DotNetCoreRuntime.IsInstalled)
					dotNetCorePath = DotNetCoreRuntime.FileName;
				else
					return;
			}

			string rootDirectory = Path.GetDirectoryName (dotNetCorePath);
			SdkRootPath = Path.Combine (rootDirectory, "sdk");

			if (!Directory.Exists (SdkRootPath))
				return;

			SdkVersions = GetInstalledSdkVersions ()
				.OrderByDescending (version => version)
				.ToArray ();
		}

		//https://docs.microsoft.com/en-us/dotnet/core/tools/global-json
		public void ResolveSDK (string workingDir = "", bool forceLookUpGlobalJson = false)
		{
			if (!SdkVersions.Any ())
				return;
					
			DotNetCoreVersion targetVersion = null;
			if (forceLookUpGlobalJson) {
				GlobalJsonPath = LookUpGlobalJson (workingDir);
			}
			var specificVersion = ReadGlobalJson ();

			//if !global.json, returns latest
			if (string.IsNullOrEmpty (specificVersion)) {
				msbuildSDKsPath = GetSdksParentDirectory (GetLatestSdk ());
				Exist = true;
				return;
			}

			DotNetCoreVersion requiredVersion;
			DotNetCoreVersion.TryParse (specificVersion, out requiredVersion);

			if (requiredVersion == null) {
				msbuildSDKsPath = string.Empty;
				IsUnsupportedSdkVersion = true;
				Exist = false;
				return;
			}

			//if global.json exists and matches returns it
			targetVersion = SdkVersions.FirstOrDefault (x => x.OriginalString.IndexOf (specificVersion, StringComparison.InvariantCulture) == 0);
			if (targetVersion == null) {
				//if global.json exists and !matches then:
				if (requiredVersion >= DotNetCoreVersion.Parse ("2.1")) {
					targetVersion = SdkVersions.Where (version => version.Major == requiredVersion.Major
																	&& version.Minor == requiredVersion.Minor)
												.OrderByDescending (version => version.Patch).FirstOrDefault (x => {
												return (x.Patch / 100 == requiredVersion.Patch / 100) &&
														(x.Patch % 100 >= requiredVersion.Patch % 100);
												});
				} else {
					targetVersion = SdkVersions.Where (version => version.Major == requiredVersion.Major && version.Minor == requiredVersion.Minor)
												.OrderByDescending (version => version.Patch).FirstOrDefault ();
				}

				if (targetVersion == null) {
					msbuildSDKsPath = string.Empty;
					IsUnsupportedSdkVersion = true;
					Exist = false;
					return;
				}
			}

			msbuildSDKsPath = GetSdksParentDirectory (targetVersion);
			Exist = true;
			IsUnsupportedSdkVersion = false;
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

		string SdksParentDirectory { get; set; }

		static IEnumerable<string> SplitSdks (string sdk) => sdk.Split (new [] { ';' }, StringSplitOptions.RemoveEmptyEntries);

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

		internal IEnumerable<DotNetCoreVersion> GetInstalledSdkVersions () => GetInstalledSdkVersions (SdkRootPath);

		string GetSdksParentDirectory (DotNetCoreVersion targetVersion)
		{
			SdksParentDirectory = Path.Combine (SdkRootPath, targetVersion.OriginalString);
			if (SdksParentDirectory == null)
				return string.Empty;

			return Path.Combine (SdksParentDirectory, "Sdks");
		}

		public string LookUpGlobalJson (string workingDir)
		{
			if (string.IsNullOrEmpty (workingDir))
				return string.Empty;

			var workingDirInfo = new DirectoryInfo (workingDir);
			var globalJsonPath = workingDirInfo.GetFiles ("global.json", SearchOption.TopDirectoryOnly).FirstOrDefault ();
			while (globalJsonPath == null) {
				if (workingDirInfo.Parent == null)
					break;

				workingDirInfo = workingDirInfo.Parent;
				globalJsonPath = workingDirInfo.GetFiles ("global.json", SearchOption.TopDirectoryOnly).FirstOrDefault ();
			}

			if (globalJsonPath == null)
				return string.Empty;

			return globalJsonPath.FullName;
		}

		string ReadGlobalJson ()
		{
			if (string.IsNullOrEmpty (GlobalJsonPath))
				return string.Empty;

			using (var r = new StreamReader (GlobalJsonPath)) {
				try {
					var token = JObject.Parse (r.ReadToEnd ());

					if (token != null && token.TryGetValue ("sdk", out var sdkToken)) {
						var version = sdkToken ["version"];
						if (version != null)
							return version.Value<string>();
					}

					return string.Empty;
				} catch (Exception e) {
					LoggingService.LogWarning ($"Unable to parse {GlobalJsonPath}.", e);
					return string.Empty;
				}
			}
		}

		internal DotNetCoreVersion GetLatestSdk () => SdkVersions.OrderByDescending (v => v).FirstOrDefault ();
	}
}
