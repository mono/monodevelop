//
// PackageManagementCanReferenceProjectExtension.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;
using NuGet.Frameworks;

namespace MonoDevelop.PackageManagement
{
	class PackageManagementCanReferenceProjectExtension : DotNetProjectExtension
	{
		protected override bool OnGetCanReferenceProject (DotNetProject targetProject, out string reason)
		{
			if (base.OnGetCanReferenceProject (targetProject, out reason)) {
				return true;
			}

			if (CanReferenceProject (targetProject)) {
				return true;
			}

			if (targetProject.HasMultipleTargetFrameworks) {
				reason = GetCanReferenceErrorReason (targetProject);
			}

			return false;
		}

		bool CanReferenceProject (DotNetProject targetProject)
		{
			foreach (TargetFrameworkMoniker targetFramework in Project.TargetFrameworkMonikers) {
				var nugetFramework = GetNuGetFramework (targetFramework);
				if (CanReferenceProject (nugetFramework, targetProject)) {
					return true;
				}
			}
			return false;
		}

		static bool CanReferenceProject (NuGetFramework nugetFramework, DotNetProject targetProject)
		{
			foreach (TargetFrameworkMoniker targetFramework in targetProject.TargetFrameworkMonikers) {
				var targetNuGetFramework = GetNuGetFramework (targetFramework);
				if (DefaultCompatibilityProvider.Instance.IsCompatible (nugetFramework, targetNuGetFramework)) {
					return true;
				}
			}
			return false;
		}

		static NuGetFramework GetNuGetFramework (TargetFrameworkMoniker targetFramework)
		{
			return new NuGetFramework (
				targetFramework.Identifier,
				Version.Parse (targetFramework.Version),
				targetFramework.Profile);
		}

		static string GetCanReferenceErrorReason (DotNetProject project)
		{
			var reason = StringBuilderCache.Allocate ();

			reason.Append (GettextCatalog.GetString ("Incompatible target frameworks: "));

			var frameworks = project.TargetFrameworkMonikers;
			for (int i = 0; i < frameworks.Length; ++i) {
				if (i > 0)
					reason.Append (", ");
				reason.Append (frameworks [i]);
			}

			return StringBuilderCache.ReturnAndFree (reason);
		}
	}
}
