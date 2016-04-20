// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Projects;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.ProjectManagement.Projects;

namespace MonoDevelop.PackageManagement
{
	internal class BuildIntegratedProjectSystem : BuildIntegratedNuGetProject
	{
		IPackageManagementEvents packageManagementEvents;
		Project project;

		public BuildIntegratedProjectSystem (
			string jsonConfigPath,
			Project project,
			IMSBuildNuGetProjectSystem msbuildProjectSystem,
			string uniqueName)
			: base (jsonConfigPath, msbuildProjectSystem)
		{
			this.project = project;
			packageManagementEvents = PackageManagementServices.PackageManagementEvents;
		}

		public override Task<bool> ExecuteInitScriptAsync (PackageIdentity identity, string packageInstallPath, INuGetProjectContext projectContext, bool throwOnFailure)
		{
			// Not supported. Report this as a warning.
			packageManagementEvents.OnPackageOperationMessageLogged (NuGet.MessageLevel.Warning, "PowerShell script init.ps1 is not supported.");

			return Task.FromResult (true);
		}

		public override Task PostProcessAsync (INuGetProjectContext nuGetProjectContext, System.Threading.CancellationToken token)
		{
			packageManagementEvents.OnFileChanged (JsonConfigPath);

			return base.PostProcessAsync (nuGetProjectContext, token);
		}

		public override Task<IReadOnlyList<BuildIntegratedProjectReference>> GetProjectReferenceClosureAsync (NuGet.Logging.ILogger logger)
		{
			// TODO: Needs to be implemented.
			return base.GetProjectReferenceClosureAsync (logger);
		}
	}
}
