//
// DefaultPackageSourceSettingsProvider.cs
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
using System.IO;
using MonoDevelop.Core;
using NuGet.Configuration;

namespace MonoDevelop.PackageManagement
{
	internal static class DefaultPackageSourceSettingsProvider
	{
		public static void CreateDefaultPackageSourceSettingsIfMissing ()
		{
			try {
				if (!NuGetConfigFileExists ()) {
					CreateDefaultNuGetConfigFile ();
				}
			} catch (Exception ex) {
				LoggingService.LogError ("Unable to create NuGet.Config file with default package sources.", ex);
			}
		}

		static bool NuGetConfigFileExists ()
		{
			string configFilePath = GetNuGetConfigFilePath ();
			if (configFilePath != null) {
				return File.Exists (configFilePath);
			}
			return false;
		}

		static string GetNuGetConfigFilePath ()
		{
			string appDataPath = Environment.GetFolderPath (Environment.SpecialFolder.ApplicationData);
			if (!String.IsNullOrEmpty (appDataPath)) {
				return Path.Combine (appDataPath, "NuGet", Settings.DefaultSettingsFileName);
			}
			return null;
		}

		static void CreateDefaultNuGetConfigFile ()
		{
			ISettings settings = Settings.LoadDefaultSettings (null, null, null);
			var packageSourceProvider = new PackageSourceProvider (settings);
			packageSourceProvider.SavePackageSources (new [] { GetDefaultPackageSource () });
		}

		static PackageSource GetDefaultPackageSource ()
		{
			return new PackageSource (
				NuGetConstants.V3FeedUrl,
				GettextCatalog.GetString ("Official NuGet Gallery")
			) {
				ProtocolVersion = 3
			};
		}
	}
}

