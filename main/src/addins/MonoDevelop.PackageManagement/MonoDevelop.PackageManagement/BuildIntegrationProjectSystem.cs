// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Projects;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;
using NuGet.ProjectModel;

namespace MonoDevelop.PackageManagement
{
	internal class BuildIntegratedProjectSystem : BuildIntegratedNuGetProject
	{
		IPackageManagementEvents packageManagementEvents;

		public BuildIntegratedProjectSystem (
			string jsonConfigPath,
			string msbuildProjectFilePath,
			DotNetProject project,
			IMSBuildNuGetProjectSystem msbuildProjectSystem,
			string uniqueName)
			: base (jsonConfigPath, msbuildProjectFilePath, msbuildProjectSystem)
		{
			packageManagementEvents = PackageManagementServices.PackageManagementEvents;
		}

		public override Task<bool> ExecuteInitScriptAsync (PackageIdentity identity, string packageInstallPath, INuGetProjectContext projectContext, bool throwOnFailure)
		{
			// Not supported. This gets called for every NuGet package
			// even if they do not have an init.ps1 so do not report this.
			return Task.FromResult (false);
		}

		public override Task PostProcessAsync (INuGetProjectContext nuGetProjectContext, System.Threading.CancellationToken token)
		{
			packageManagementEvents.OnFileChanged (JsonConfigPath);

			return base.PostProcessAsync (nuGetProjectContext, token);
		}

		public override Task<IReadOnlyList<ExternalProjectReference>> GetProjectReferenceClosureAsync (ExternalProjectReferenceContext context)
		{
			// TODO: Needs to be implemented.
			return base.GetProjectReferenceClosureAsync (context);
		}
	}
}
