//
// HasNuGetPackageOrReference.cs
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
using System.Linq;
using System.Xml;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;
using MonoDevelop.Projects.SharedAssetsProjects;

namespace MonoDevelop.PackageManagement
{
	/// <summary>
	/// Allows a file template to be enabled if a project has a particular assembly reference
	/// or has a NuGet package reference. Using a NuGet package id is required for projects
	/// that do not use a packages.config file, but instead use a project.json file or
	/// PackageReference MSBuild items, since these projects do not have explicit references
	/// to assemblies in the NuGet packages being used.
	/// </summary>
	class HasNuGetPackageOrReferenceFileTemplateCondition : HasReferenceFileTemplateCondition
	{
		string packageId;

		public override void Load (XmlElement element)
		{
			base.Load (element);

			packageId = element.GetAttribute ("PackageId");
			if (string.IsNullOrWhiteSpace (packageId))
				throw new InvalidOperationException ("Invalid value for PackageId condition in template.");
		}

		public override bool ShouldEnableFor (Project proj, string projectPath)
		{
			if (base.ShouldEnableFor (proj, projectPath))
				return true;

			// No need to explicitly check a shared project since base.ShouldEnableFor will call
			// this method again for each project reference.
			if (proj is DotNetProject dotNetProject) {
				return HasPackageInstalled (dotNetProject);
			}
			return false;
		}

		bool HasPackageInstalled (DotNetProject project)
		{
			return PackageManagementServices.ProjectOperations.GetInstalledPackages (project)
				.Any (packageReference => StringComparer.OrdinalIgnoreCase.Equals (packageId, packageReference.Id));
		}
	}
}
