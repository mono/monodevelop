//
// DotNetCoreSystemInformation.cs
//
// Author:
//       matt <matt.ward@xamarin.com>
//
// Copyright (c) 2017 (c) Matt Ward
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
using System.Text;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Updater;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreSystemInformation : ProductInformationProvider
	{
		public override string Title => GettextCatalog.GetString (".NET Core");

		public override string Description => GetDescription ();

		public override string Version => DotNetCoreSdk.Versions.FirstOrDefault()?.ToString ();

		public override string ApplicationId => "c07628e8-5521-4c1a-aa3a-f860e664f0a9";

		public override UpdateInfo GetUpdateInfo ()
		{
			if (IsSierraOrHigher ())
				return GenerateDotNetCoreUpdateInfo ();
			return null;
		}

		UpdateInfo GenerateDotNetCoreUpdateInfo ()
		{
			var latestSdkVersion = DotNetCoreSdk.Versions.FirstOrDefault ();
			if (latestSdkVersion != null) {
				return new UpdateInfo (ApplicationId, latestSdkVersion.Version);
			}

			// .NET Core not installed, so don't offer as new install
			return null;
		}

		static bool IsSierraOrHigher ()
		{
			return Platform.IsMac && (Platform.OSVersion >= MacSystemInformation.Sierra);
		}

		string GetDescription ()
		{
			var description = new StringBuilder ();

			description.AppendLine (GettextCatalog.GetString ("Runtime: {0}", GetDotNetRuntimeLocation ()));
			AppendDotNetCoreRuntimeVersions (description);
			description.AppendLine (GettextCatalog.GetString ("SDK: {0}", GetDotNetSdkLocation ()));
			AppendDotNetCoreSdkVersions (description);
			description.AppendLine (GettextCatalog.GetString ("MSBuild SDKs: {0}", GetMSBuildSdksLocation ()));

			return description.ToString ();
		}

		static string GetDotNetRuntimeLocation ()
		{
			if (DotNetCoreRuntime.IsInstalled)
				return DotNetCoreRuntime.FileName;

			return GetNotInstalledString ();
		}

		static string GetNotInstalledString ()
		{
			return GettextCatalog.GetString ("Not installed");
		}

		static string GetDotNetSdkLocation ()
		{
			if (DotNetCoreSdk.IsInstalled)
				return DotNetCoreSdk.MSBuildSDKsPath;

			return GetNotInstalledString ();
		}

		static string GetMSBuildSdksLocation ()
		{
			if (MSBuildSdks.Installed)
				return MSBuildSdks.MSBuildSDKsPath;

			return GetNotInstalledString ();
		}

		static void AppendDotNetCoreRuntimeVersions (StringBuilder description)
		{
			AppendVersions (
				description,
				DotNetCoreRuntime.Versions,
				version => GettextCatalog.GetString ("Runtime Version: {0}", version),
				() => GettextCatalog.GetString ("Runtime Versions:"));
		}

		static void AppendDotNetCoreSdkVersions (StringBuilder description)
		{
			AppendVersions (
				description,
				DotNetCoreSdk.Versions,
				version => GettextCatalog.GetString ("SDK Version: {0}", version),
				() => GettextCatalog.GetString ("SDK Versions:"));
		}

		static void AppendVersions (
			StringBuilder description,
			DotNetCoreVersion[] versions,
			Func<DotNetCoreVersion, string> getSingleVersionString,
			Func<string> getMultipleVersionsString)
		{
			if (!versions.Any ())
				return;

			if (versions.Count () == 1) {
				description.AppendLine (getSingleVersionString (versions[0]));
			} else {
				description.AppendLine (getMultipleVersionsString ());

				foreach (var version in versions) {
					description.Append ('\t');
					description.AppendLine (version.OriginalString);
				}
			}
		}
	}
}
