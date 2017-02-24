//
// DotNetCoreCanReferenceProjectExtension.cs
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
using MonoDevelop.Core;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore
{
	[ExportProjectModelExtension]
	class DotNetCoreCanReferenceProjectExtension : DotNetProjectExtension
	{
		protected override bool OnGetCanReferenceProject (DotNetProject targetProject, out string reason)
		{
			if (base.OnGetCanReferenceProject (targetProject, out reason))
				return true;

			if (IsNetStandardProject (targetProject) &&
				CanReferenceNetStandardProject (Project, targetProject)) {

				if (Runtime.Preferences.BuildWithMSBuild)
					return true;

				reason = GettextCatalog.GetString ("MSBuild must be used instead of xbuild. Please enable MSBuild in preferences - Projects - Build and then re-open the solution.");
				return false;
			}

			return false;
		}

		static bool IsNetStandardProject (DotNetProject project)
		{
			return project.TargetFramework.IsNetStandard ();
		}

		/// <summary>
		/// Handle any failures to parse the target framework.
		/// </summary>
		static bool CanReferenceNetStandardProject (DotNetProject project, DotNetProject targetProject)
		{
			try {
				return DotNetCoreFrameworkCompatibility.CanReferenceNetStandardProject (project, targetProject);
			} catch (Exception ex) {
				LoggingService.LogError ("Error checking target framework compatibility.", ex);
			}

			return false;
		}
	}
}
