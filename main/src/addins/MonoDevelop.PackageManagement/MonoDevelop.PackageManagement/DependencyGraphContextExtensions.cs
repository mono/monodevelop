//
// DependencyGraphContextExtensions.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using NuGet.ProjectManagement;
using NuGet.ProjectModel;

namespace MonoDevelop.PackageManagement
{
	static class DependencyGraphContextExtensions
	{
		public static void AddToCache (this DependencyGraphCacheContext context, DependencyGraphSpec dependencyGraphSpec)
		{
			if (context == null)
				return;

			foreach (PackageSpec packageSpec in dependencyGraphSpec.Projects) {
				if (CanCachePackageSpec (packageSpec)) {
					AddToCache (context, packageSpec);
				}
			}
		}

		static bool CanCachePackageSpec (PackageSpec packageSpec)
		{
			if (packageSpec.RestoreMetadata == null)
				return false;

			ProjectStyle style = packageSpec.RestoreMetadata.ProjectStyle;

			return style == ProjectStyle.PackageReference ||
				style == ProjectStyle.ProjectJson ||
				style == ProjectStyle.DotnetCliTool ||
				style == ProjectStyle.PackagesConfig;
		}

		static void AddToCache (DependencyGraphCacheContext context, PackageSpec projectPackageSpec)
		{
			if (IsMissingFromCache (context, projectPackageSpec)) {
				context.PackageSpecCache.Add (
					projectPackageSpec.RestoreMetadata.ProjectUniqueName,
					projectPackageSpec);
			}
		}

		static bool IsMissingFromCache (
			DependencyGraphCacheContext context,
			PackageSpec packageSpec)
		{
			PackageSpec ignore;
			return !context.PackageSpecCache.TryGetValue (
				packageSpec.RestoreMetadata.ProjectUniqueName,
				out ignore);
		}

		public static PackageSpec GetExistingProjectPackageSpec (this DependencyGraphCacheContext context, string projectPath)
		{
			if (context == null)
				return null;

			if (context.PackageSpecCache.TryGetValue (projectPath, out PackageSpec packageSpec)) {
				return packageSpec;
			}
			return null;
		}

		public static IReadOnlyList<PackageSpec> GetExistingProjectPackageSpecs (this DependencyGraphCacheContext context, string projectPath)
		{
			var mainPackageSpec = GetExistingProjectPackageSpec (context, projectPath);
			if (mainPackageSpec == null)
				return null;

			var specs = new List<PackageSpec> ();
			specs.Add (mainPackageSpec);

			// Look for DotNetCliTools.
			foreach (var spec in context.PackageSpecCache.Values) {
				if (spec.IsDotNetCliToolPackageSpecForProject (projectPath)) {
					specs.Add (spec);
				}
			}

			return specs;
		}
	}
}
