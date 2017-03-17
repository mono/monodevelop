﻿//
// DotNetCoreProjectSupportedTargetFrameworks.cs
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

using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreProjectSupportedTargetFrameworks
	{
		const int HighestNetStandard1xMinorVersionSupported = 6;
		const int HighestNetCoreApp1xMinorVersionSupported = 1;

		DotNetProject project;
		TargetFramework framework;

		public DotNetCoreProjectSupportedTargetFrameworks (DotNetProject project)
		{
			this.project = project;
			framework = project.TargetFramework;
		}

		public IEnumerable<TargetFramework> GetFrameworks ()
		{
			if (framework.IsNetStandard ()) {
				return GetNetStandardTargetFrameworks ();
			} else if (framework.IsNetCoreApp ()) {
				return GetNetCoreAppTargetFrameworks ();
			} else if (framework.IsNetFramework ()) {
				return GetNetFrameworkTargetFrameworks ();
			}

			return new TargetFramework [0];
		}

		IEnumerable<TargetFramework> GetNetStandardTargetFrameworks ()
		{
			return GetTargetFrameworksVersion1x (".NETStandard", HighestNetStandard1xMinorVersionSupported);
		}

		IEnumerable<TargetFramework> GetTargetFrameworksVersion1x (string identifier, int maxMinorVersion)
		{
			for (int minorVersion = 0; minorVersion <= maxMinorVersion; ++minorVersion) {
				string version = string.Format ($"1.{minorVersion}");
				var moniker = new TargetFrameworkMoniker (identifier, version);
				yield return Runtime.SystemAssemblyService.GetTargetFramework (moniker);
			}
		}

		IEnumerable<TargetFramework> GetNetCoreAppTargetFrameworks ()
		{
			return GetTargetFrameworksVersion1x (".NETCoreApp", HighestNetCoreApp1xMinorVersionSupported);
		}

		IEnumerable<TargetFramework> GetNetFrameworkTargetFrameworks ()
		{
			foreach (var targetFramework in Runtime.SystemAssemblyService.GetTargetFrameworks ()) {
				if (!targetFramework.Hidden &&
					targetFramework.IsNetFramework () &&
					project.TargetRuntime.IsInstalled (targetFramework))
					yield return targetFramework;
			}
		}
	}
}
