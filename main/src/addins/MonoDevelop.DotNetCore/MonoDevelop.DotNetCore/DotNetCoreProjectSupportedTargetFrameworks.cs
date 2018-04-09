//
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

using System;
using System.Collections.Generic;
using System.Linq;
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

		public static IEnumerable<TargetFramework> GetNetStandardTargetFrameworks ()
		{
			if (DotNetCoreRuntime.IsNetCore2xInstalled ())
				yield return CreateTargetFramework (".NETStandard", "2.0");

			if (DotNetCoreRuntime.IsNetCore2xInstalled () || DotNetCoreRuntime.IsNetCore1xInstalled ()) {
				foreach (var targetFramework in GetTargetFrameworksVersion1x (".NETStandard", HighestNetStandard1xMinorVersionSupported).Reverse ())
					yield return targetFramework;
			}
		}

		/// <summary>
		/// These are the .NET Standard target frameworks that the sdks that ship with 
		/// Mono's MSBuild support.
		/// </summary>
		public static IEnumerable<TargetFramework> GetDefaultNetStandard1xTargetFrameworks ()
		{
			foreach (var targetFramework in GetTargetFrameworksVersion1x (".NETStandard", HighestNetStandard1xMinorVersionSupported).Reverse ())
				yield return targetFramework;
		}

		static IEnumerable<TargetFramework> GetTargetFrameworksVersion1x (string identifier, int maxMinorVersion)
		{
			for (int minorVersion = 0; minorVersion <= maxMinorVersion; ++minorVersion) {
				string version = string.Format ($"1.{minorVersion}");
				yield return CreateTargetFramework (identifier, version);
			}
		}

		public static IEnumerable<TargetFramework> GetNetCoreAppTargetFrameworks ()
		{
			foreach (Version runtimeVersion in GetMajorRuntimeVersions ()) {
				if (runtimeVersion.Major == 2 && runtimeVersion.Minor > 1) {
					// Skip version 2.2 since this is not currently supported.
					continue;
				}

				string version = runtimeVersion.ToString (2);
				yield return CreateTargetFramework (".NETCoreApp", version);
			}
		}

		static IEnumerable<Version> GetMajorRuntimeVersions ()
		{
			return DotNetCoreRuntime.Versions
				.Select (version => new Version (version.Major, version.Minor))
				.Distinct ();
		}

		static TargetFramework CreateTargetFramework (string identifier, string version)
		{
			var moniker = new TargetFrameworkMoniker (identifier, version);
			return Runtime.SystemAssemblyService.GetTargetFramework (moniker);
		}

		IEnumerable<TargetFramework> GetNetFrameworkTargetFrameworks ()
		{
			foreach (var targetFramework in Runtime.SystemAssemblyService.GetTargetFrameworks ()) {
				if (targetFramework.IsNetFramework () &&
					project.TargetRuntime.IsInstalled (targetFramework))
					yield return targetFramework;
			}
		}
	}
}
