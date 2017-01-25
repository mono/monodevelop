//
// DotNetCoreFrameworkCompatibility.cs
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
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;
using NuGet.Frameworks;

namespace MonoDevelop.PackageManagement
{
	static class DotNetCoreFrameworkCompatibility
	{
		public static bool CanReferenceNetStandardProject (TargetFrameworkMoniker targetFramework, DotNetProject netStandardProject)
		{
			var netStandardVersion = new Version (netStandardProject.TargetFramework.Id.Version);
			netStandardVersion = new Version (netStandardVersion.Major, netStandardVersion.Minor, 0, 0);

			var coreAppVersion = new Version (targetFramework.Version);
			coreAppVersion = new Version (coreAppVersion.Major, coreAppVersion.Minor, 0, 0);

			if (coreAppVersion == FrameworkConstants.CommonFrameworks.NetCoreApp10.Version)
				return netStandardVersion < FrameworkConstants.CommonFrameworks.NetStandard17.Version;
			else if (coreAppVersion == FrameworkConstants.CommonFrameworks.NetCoreApp11.Version)
				return netStandardVersion <= FrameworkConstants.CommonFrameworks.NetStandard17.Version;
			else // Assume compatible.
				return true;
		}

		public static bool CanReferenceNetStandardProject (DotNetProject project, DotNetProject netStandardProject)
		{
			var netStandardFramework = NuGetFramework.Parse (netStandardProject.TargetFramework.Id.ToString ());
			var projectFramework = NuGetFramework.Parse (project.TargetFramework.Id.ToString ());

			return DefaultCompatibilityProvider.Instance.IsCompatible (projectFramework, netStandardFramework);
		}
	}
}
