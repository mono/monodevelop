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
	sealed class DotNetCoreSystemInformation : ProductInformationProvider
	{
		public override string Title => GettextCatalog.GetString (".NET Core SDK");

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
				return new UpdateInfo (ApplicationId, GenerateVersionId(latestSdkVersion));
			}

			// .NET Core not installed, so don't offer as new install
			return null;
		}


		/// <summary>
		/// Slightly modified version of GenerateVersionId in UpdateInfo.cs
		/// to use 6 significant digits for revision.
		/// </summary>
		internal static long GenerateVersionId(DotNetCoreVersion version)
		{
			// For stable releases (that have no revision number),
			// we need a version Id that is higher then the previews
			// (which do have a revision number) to enable the comparison
			// to work on the feed.
			var revision = "999999";

			if(version.IsPrerelease) {
				revision = FixVersionNumber (version.Revision).ToString ("000000");
			}

			var s = FixVersionNumber (version.Major) +
					FixVersionNumber (version.Minor).ToString ("00") +
					FixVersionNumber (version.Patch).ToString ("00") +
					revision;
			return long.Parse (s);
		}

		/// <summary>
		/// Unspecified version numbers can be -1 so map this to 0 instead.
		/// </summary>
		static int FixVersionNumber (int number)
		{
			if (number == -1)
				return 0;

			return number;
		}

		static bool IsSierraOrHigher () => Platform.IsMac && (Platform.OSVersion >= MacSystemInformation.Sierra);

		string GetDescription ()
		{
			var description = new StringBuilder ();

			description.AppendLine (GettextCatalog.GetString ("SDK: {0}", GetDotNetSdkLocation ()));
			AppendDotNetCoreSdkVersions (description);
			description.AppendLine (GettextCatalog.GetString ("MSBuild SDKs: {0}", GetMSBuildSdksLocation ()));

			return description.ToString ();
		}

		internal static string GetNotInstalledString ()
		{
			return GettextCatalog.GetString ("Not installed");
		}

		static string GetDotNetSdkLocation ()
		{
			var dotNetCoreSdkPaths = new DotNetCoreSdkPaths ();
			if (dotNetCoreSdkPaths.GetInstalledSdkVersions ().Any ()) {
				dotNetCoreSdkPaths.ResolveSDK ();
				return dotNetCoreSdkPaths.MSBuildSDKsPath;
			}

			return GetNotInstalledString ();
		}

		static string GetMSBuildSdksLocation ()
		{
			if (MSBuildSdks.Installed)
				return MSBuildSdks.MSBuildSDKsPath;

			return GetNotInstalledString ();
		}

		static void AppendDotNetCoreSdkVersions (StringBuilder description)
		{
			AppendVersions (
				description,
				DotNetCoreSdk.Versions,
				version => GettextCatalog.GetString ("SDK Version: {0}", version),
				() => GettextCatalog.GetString ("SDK Versions:"));
		}

		internal static void AppendVersions (
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
