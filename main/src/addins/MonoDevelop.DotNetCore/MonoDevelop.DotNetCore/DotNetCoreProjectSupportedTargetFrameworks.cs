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
				return GetNetCoreAppTargetFrameworksWithSdkSupport ();
			} else if (framework.IsNetFramework ()) {
				return GetNetFrameworkTargetFrameworks ();
			}

			return new TargetFramework [0];
		}

		static string [] supportedNetStandardVersions = {
			"2.1", "2.0", "1.6", "1.5", "1.4", "1.3", "1.2", "1.1", "1.0"
		};
		static string [] supportedNetCoreAppVersions = {
			"3.1", "3.0", "2.2", "2.1", "2.0", "1.1", "1.0"
		};

		public IEnumerable<TargetFramework> GetKnownFrameworks ()
		{
			static IEnumerable<TargetFramework> GetKnownNetStandardFrameworks ()
			{
				foreach (var v in supportedNetStandardVersions) {
					yield return CreateTargetFramework (".NETStandard", v);
				}
			}

			static IEnumerable<TargetFramework> GetKnownNetCoreAppFrameworks ()
			{
				foreach (var v in supportedNetCoreAppVersions) {
					yield return CreateTargetFramework (".NETCoreApp", v);
				}
			}

			if (framework.IsNetStandard ()) {
				return GetKnownNetStandardFrameworks ();
			} else if (framework.IsNetCoreApp ()) {
				return GetKnownNetCoreAppFrameworks ();
			} else if (framework.IsNetFramework ()) {
				return GetNetFrameworkTargetFrameworks ();
			}

			return new TargetFramework [0];
		}

		public static IEnumerable<TargetFramework> GetNetStandardTargetFrameworks ()
		{
			if (DotNetCoreRuntime.IsNetCore30Installed () || MonoRuntimeInfoExtensions.CurrentRuntimeVersion.SupportsNetStandard21 ())
				yield return CreateTargetFramework (".NETStandard", "2.1");
				
			if (DotNetCoreRuntime.IsNetCore2xInstalled () || MonoRuntimeInfoExtensions.CurrentRuntimeVersion.SupportsNetStandard20 ())
				yield return CreateTargetFramework (".NETStandard", "2.0");

			foreach (var targetFramework in GetTargetFrameworksVersion1x (".NETStandard", HighestNetStandard1xMinorVersionSupported).Reverse ())
				yield return targetFramework;
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
			foreach (var runtimeVersion in GetMajorRuntimeVersions ()) {
				yield return CreateTargetFramework (".NETCoreApp", runtimeVersion.ToString (2));
			}
		}

		public static IEnumerable<TargetFramework> GetNetCoreAppTargetFrameworksWithSdkSupport ()
		{
			foreach (var runtimeVersion in GetMajorRuntimeVersions ()) {
				// In DotNetCore version 2.1 and above the Runtime always ships in an Sdk with the same Major.Minor version. For older versions, this 
				// rule does not apply, but as these versions have been deprecated we will not worry about explicit filtering support here as this
				// may cause regressions.
				if ((runtimeVersion.Major == 2 && runtimeVersion.Minor >= 1) || runtimeVersion.Major >= 3) {
					if (DotNetCoreSdk.Versions.Any (sdkVersion => runtimeVersion.Major == sdkVersion.Major && runtimeVersion.Minor == sdkVersion.Minor))
						yield return CreateTargetFramework (".NETCoreApp", runtimeVersion.ToString (2));
				} else {
					yield return CreateTargetFramework (".NETCoreApp", runtimeVersion.ToString (2));
				}
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
