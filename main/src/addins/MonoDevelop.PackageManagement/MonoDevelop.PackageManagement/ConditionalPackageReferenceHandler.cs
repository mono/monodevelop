//
// ConditionalPackageReferenceHandler.cs
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
using System.Linq;
using MonoDevelop.Projects.MSBuild;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	class ConditionalPackageReferenceHandler : IDisposable
	{
		PackageIdentity packageIdentity;
		INuGetProjectContext context;
		BuildIntegratedInstallationContext installationContext;

		public ConditionalPackageReferenceHandler ()
		{
			PackageManagementMSBuildExtension.ConditionalPackageReferenceHandler = this;
		}

		public void Dispose ()
		{
			PackageManagementMSBuildExtension.ConditionalPackageReferenceHandler = null;
		}

		public void AddConditionalPackageReference (
			PackageIdentity packageIdentity,
			INuGetProjectContext context,
			BuildIntegratedInstallationContext installationContext)
		{
			this.packageIdentity = packageIdentity;
			this.context = context;
			this.installationContext = installationContext;
		}

		public void UpdateProject (MSBuildProject project)
		{
			foreach (NuGetFramework framework in installationContext.SuccessfulFrameworks) {

				MSBuildItem packageReference = AddPackageReference (project, framework);

				if (installationContext.IncludeType != LibraryIncludeFlags.All &&
					installationContext.SuppressParent != LibraryIncludeFlagUtils.DefaultSuppressParent) {
					packageReference.Metadata.SetMetadataValue (ProjectItemProperties.IncludeAssets, installationContext.IncludeType);
					packageReference.Metadata.SetMetadataValue (ProjectItemProperties.PrivateAssets, installationContext.SuppressParent);
				}
			}
		}

		MSBuildItem AddPackageReference (MSBuildProject project, NuGetFramework framework)
		{
			string originalFramework;
			if (!installationContext.OriginalFrameworks.TryGetValue (framework, out originalFramework)) {
				originalFramework = framework.GetShortFolderName ();
			}

			MSBuildItemGroup itemGroup = GetOrAddItemGroup (project, originalFramework);
			MSBuildItem packageReference = GetOrAddPackageReference (itemGroup);

			packageReference.Metadata.SetValue ("Version", packageIdentity.Version.ToNormalizedString ());

			return packageReference;
		}

		static MSBuildItemGroup GetOrAddItemGroup (MSBuildProject project, string originalFramework)
		{
			string condition = string.Format ("'$(TargetFramework)' == '{0}'", originalFramework);

			MSBuildItemGroup itemGroup = project
				.ItemGroups
				.FirstOrDefault (item => StringComparer.OrdinalIgnoreCase.Equals (item.Condition, condition));

			if (itemGroup == null) {
				itemGroup = project.AddNewItemGroup ();
				itemGroup.Condition = condition;
			}

			return itemGroup;
		}

		MSBuildItem GetOrAddPackageReference (MSBuildItemGroup itemGroup)
		{
			MSBuildItem packageReference = itemGroup
				.Items
				.FirstOrDefault (item => IsPackageReferenceMatch (item));

			if (packageReference == null)
				packageReference = itemGroup.AddNewItem ("PackageReference", packageIdentity.Id);

			return packageReference;
		}

		bool IsPackageReferenceMatch (MSBuildItem item)
		{
			return item.Name == "PackageReference" &&
				StringComparer.OrdinalIgnoreCase.Equals (item.Include, packageIdentity.Id);
		}
	}
}
