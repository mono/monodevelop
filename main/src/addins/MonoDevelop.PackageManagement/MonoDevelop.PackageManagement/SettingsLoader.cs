//
// SettingsLoader.cs
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
using System.Security.Cryptography;
using MonoDevelop.Core;
using NuGet.Configuration;

namespace MonoDevelop.PackageManagement
{
	internal static class SettingsLoader
	{
		public static ISettings LoadDefaultSettings (bool reportError = false, bool throwError = false)
		{
			return LoadDefaultSettings (null, reportError, throwError);
		}

		public static ISettings LoadDefaultSettings (string rootDirectory, bool reportError = false, bool throwError = false)
		{
			try {
				return Settings.LoadDefaultSettings (rootDirectory, null, null);
			} catch (Exception ex) {
				if (reportError) {
					ShowReadNuGetConfigFileError (ex);
				} else if (throwError) {
					throw;
				} else {
					LoggingService.LogError ("Unable to load global NuGet.Config.", ex);
				}
			}

			return NullSettings.Instance;
		}

		static void ShowReadNuGetConfigFileError (Exception ex)
		{
			Ide.MessageService.ShowError (
				GettextCatalog.GetString ("Unable to read the NuGet.Config file"),
				String.Format (GetReadNuGetConfigFileErrorMessage (ex),
					ex.Message),
				ex);
		}

		static string GetReadNuGetConfigFileErrorMessage (Exception ex)
		{
			if (ex is CryptographicException) {
				return GettextCatalog.GetString ("Unable to decrypt passwords stored in the NuGet.Config file. The NuGet.Config file will be treated as read-only.");
			}

			return GettextCatalog.GetString ("An error occurred when trying to read the NuGet.Config file. The NuGet.Config file will be treated as read-only.\n\n{0}");
		}
	}
}

