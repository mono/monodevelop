//
// NuGetConfigFileChangedMonitor.cs
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
using MonoDevelop.Core;
using System.Linq;
using NuGet.Configuration;

namespace MonoDevelop.PackageManagement
{
	class NuGetConfigFileChangedMonitor
	{
		public void MonitorFileChanges ()
		{
			FileService.FileChanged += FileChanged;
		}

		void FileChanged (object sender, FileEventArgs e)
		{
			try {
				if (IsNuGetConfigFileChanged (e)) {
					Runtime.RunInMainThread (() => {
						PackageManagementServices.Workspace.ReloadSettings ();
					});
				}
			} catch (Exception ex) {
				LoggingService.LogError ("NuGetConfigFileChangedMonitor error.", ex);
			}
		}

		bool IsNuGetConfigFileChanged (FileEventArgs e)
		{
			return e.Any (IsNuGetConfigFile);
		}

		bool IsNuGetConfigFile (FileEventInfo fileInfo)
		{
			if (fileInfo.FileName.IsNull)
				return false;

			return StringComparer.OrdinalIgnoreCase.Equals (Settings.DefaultSettingsFileName, fileInfo.FileName.FileName);
		}
	}
}
