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

using System.Text;
using MonoDevelop.Core;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreSystemInformation : ISystemInformationProvider
	{
		public string Title {
			get { return GettextCatalog.GetString (".NET Core"); }
		}

		public string Description {
			get {
				return GetDescription ();
			}
		}

		string GetDescription ()
		{
			var description = new StringBuilder ();

			description.AppendLine (GettextCatalog.GetString ("Runtime: {0}", GetDotNetRuntimeLocation ()));
			description.AppendLine (GettextCatalog.GetString ("SDK: {0}", GetDotNetSdkLocation ()));
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
	}
}
