//
// SdkProjectExtension.cs
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.Projects
{
	[ExportProjectModelExtension]
	public class SdkProjectExtension : DotNetProjectExtension
	{
		MSBuildSdkProject msbuildSdkProject = new MSBuildSdkProject ();

		internal static bool IsSupportedProjectFileExtension (FilePath file)
		{
			return file.HasExtension (".csproj") || file.HasExtension (".fsproj") || file.HasExtension (".vbproj");
		}

		/// <summary>
		/// HACK: Hide certain files in Solution window. The solution's .userprefs
		/// file is the only file is included properly with the .NET Core MSBuild
		/// targets.
		/// </summary>
		internal static bool FileShouldBeHidden (FilePath file)
		{
			return file.HasExtension (".userprefs") ||
				file.FileName == ".DS_Store";
		}

		public IEnumerable<string> TargetFrameworks {
			get { return msbuildSdkProject.TargetFrameworks; }
		}

		internal protected override bool SupportsObject (WorkspaceObject item)
		{
			return base.SupportsObject (item) && IsSdkProject ((DotNetProject)item);
		}

		internal protected override bool OnGetSupportsFramework (TargetFramework framework)
		{
			// Allow all SDK style projects to be loaded even if the framework is unknown.
			// A PackageReference may define the target framework with an imported MSBuild file.
			return true;
		}

		/// <summary>
		/// Currently this project extension is enabled for all SDK style projects and
		/// not just for .NET Core and .NET Standard projects. SDK project support
		/// should be separated out from this extension so it can be enabled only for
		/// .NET Core and .NET Standard projects.
		/// </summary>
		bool IsSdkProject (DotNetProject project)
		{
			return project.MSBuildProject.GetReferencedSDKs ().Length > 0;
		}

		internal protected override void OnReadProjectHeader (ProgressMonitor monitor, MSBuildProject msproject)
		{
			// Do not read the project header when re-evaluating to prevent the
			// ToolsVersion that was initially read from the project being changed.
			if (!Project.IsReevaluating)
				msbuildSdkProject.ReadProjectHeader (msproject);
			base.OnReadProjectHeader (monitor, msproject);
		}

		internal protected override void OnReadProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			msbuildSdkProject.AddKnownItemAttributes (Project.MSBuildProject);

			base.OnReadProject (monitor, msproject);

			msbuildSdkProject.ReadProject (msproject, Project.TargetFramework.Id);

			if (!msbuildSdkProject.IsOutputTypeDefined)
				Project.CompileTarget = msbuildSdkProject.DefaultCompileTarget;

			Project.UseAdvancedGlobSupport = true;
			Project.UseDefaultMetadataForExcludedExpandedItems = true;
			Project.UseFileWatcher = true;
		}

		internal protected override void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnWriteProject (monitor, msproject);

			msbuildSdkProject.WriteProject (msproject, Project.TargetFramework.Id);
		}

		internal protected override void OnPrepareForEvaluation (MSBuildProject project)
		{
			base.OnPrepareForEvaluation (project);
			var referencedSdks = project.GetReferencedSDKs ();
			msbuildSdkProject.HasSdk = referencedSdks.Length > 0;
		}

		internal protected override async Task<ImmutableArray<ProjectFile>> OnGetSourceFiles (ProgressMonitor monitor, ConfigurationSelector configuration)
		{
			var sourceFiles = await base.OnGetSourceFiles (monitor, configuration);

			return AddMissingProjectFiles (sourceFiles, configuration);
		}

		ImmutableArray<ProjectFile> AddMissingProjectFiles (ImmutableArray<ProjectFile> files, ConfigurationSelector configuration)
		{
			ImmutableArray<ProjectFile>.Builder missingFiles = null;
			foreach (ProjectFile existingFile in Project.Files.Where (file => file.BuildAction == BuildAction.Compile)) {
				if (!files.Any (file => file.FilePath == existingFile.FilePath)) {
					if (missingFiles == null)
						missingFiles = ImmutableArray.CreateBuilder<ProjectFile> ();
					missingFiles.Add (existingFile);
				}
			}

			// Ensure generated assembly info file is available to type system. It is created in the obj
			// directory and is excluded from the project with a wildcard exclude but the type system needs it to
			// ensure the project's assembly information is correct to prevent diagnostic errors.
			var generatedAssemblyInfoFile = GetGeneratedAssemblyInfoFile (configuration);
			if (generatedAssemblyInfoFile != null) {
				if (missingFiles == null)
					missingFiles = ImmutableArray.CreateBuilder<ProjectFile> ();
				missingFiles.Add (generatedAssemblyInfoFile);
			}

			if (missingFiles == null)
				return files;

			missingFiles.Capacity = missingFiles.Count + files.Length;
			missingFiles.AddRange (files);
			return missingFiles.MoveToImmutable ();
		}

		ProjectFile GetGeneratedAssemblyInfoFile (ConfigurationSelector configuration)
		{
			var projectConfig = configuration.GetConfiguration (Project) as ProjectConfiguration;
			if (projectConfig == null)
				return null;

			bool generateAssemblyInfo = projectConfig.Properties.GetValue ("GenerateAssemblyInfo", true);
			FilePath assemblyInfoFile = projectConfig.Properties.GetPathValue ("GeneratedAssemblyInfoFile");

			if (generateAssemblyInfo && assemblyInfoFile.IsNotNull)
				return new ProjectFile (assemblyInfoFile, BuildAction.Compile);
			return null;
		}

		internal protected override void OnSetFormat (MSBuildFileFormat format)
		{
			// Do not call base class since the solution's FileFormat will be used which is
			// VS 2012 and this will set the ToolsVersion to "4.0" which we are preventing.
			// Setting the ToolsVersion to "4.0" can cause the MSBuild tasks such as
			// ResolveAssemblyReferences to fail for .NET Core projects when the project
			// xml is generated in memory for the project builder at the same time as the
			// project file is being saved.
		}

		bool IsFSharpSdkProject ()
		{
			var sdks = Project.MSBuildProject.GetReferencedSDKs ();
			for (var i = 0; i < sdks.Length; i++) {
				if (sdks [i].Contains ("FSharp"))
					return true;
			}
			return false;
		}

		internal protected override bool OnGetSupportsImportedItem (IMSBuildItemEvaluated buildItem)
		{
			if (!BuildAction.DotNetActions.Contains (buildItem.Name))
				return false;

			if (IsFSharpSdkProject ()) {
				// Ignore imported F# files. F# files are defined in the main project.
				// This prevents duplicate F# files when a new project is first created.
				if (buildItem.Include.EndsWith (".fs", StringComparison.OrdinalIgnoreCase))
					return false;
			}

			if (IsFromSharedProject (buildItem))
				return false;

			// HACK: Remove any imported items that are not in the EvaluatedItems
			// This may happen if a condition excludes the item. All items passed to the
			// OnGetSupportsImportedItem are from the EvaluatedItemsIgnoringCondition
			return Project.MSBuildProject.EvaluatedItems
				.Any (item => item.IsImported && item.Name == buildItem.Name && item.Include == buildItem.Include);
		}

		/// <summary>
		/// Checks that the project has the HasSharedItems property set to true and the SharedGUID
		/// property in its global property group. Otherwise it is not considered to be a shared project.
		/// </summary>
		bool IsFromSharedProject (IMSBuildItemEvaluated buildItem)
		{
			var globalGroup = buildItem?.SourceItem?.ParentProject?.GetGlobalPropertyGroup ();
			return globalGroup?.GetValue<bool> ("HasSharedItems") == true &&
				globalGroup?.HasProperty ("SharedGUID") == true;
		}

		/// <summary>
		/// HACK: Hide certain files that are currently being added to the Solution window.
		/// </summary>
		internal protected override void OnItemsAdded (IEnumerable<ProjectItem> objs)
		{
			if (Project.Loading) {
				UpdateHiddenFiles (objs.OfType<ProjectFile> ());
			}
			base.OnItemsAdded (objs);
		}

		void UpdateHiddenFiles (IEnumerable<ProjectFile> files)
		{
			foreach (var file in files) {
				if (FileShouldBeHidden (file.FilePath))
					file.Flags = ProjectItemFlags.Hidden;
			}
		}

		internal protected override async Task OnReevaluateProject (ProgressMonitor monitor)
		{
			await base.OnReevaluateProject (monitor);
			UpdateHiddenFiles (Project.Files);
		}

		/// <summary>
		/// Returns all transitive references.
		/// </summary>
		internal protected override async Task<List<AssemblyReference>> OnGetReferences (
			ConfigurationSelector configuration,
			System.Threading.CancellationToken token)
		{
			var references = new List<AssemblyReference> ();

			var traversedProjects = new HashSet<string> ();
			traversedProjects.Add (Project.ItemId);

			await GetTransitiveAssemblyReferences (traversedProjects, references, configuration, true, token);

			return references;
		}

		/// <summary>
		/// Recursively gets all transitive project references for .NET Core projects
		/// and if includeNonProjectReferences is true also returns non project
		/// assembly references.
		/// 
		/// Calling base.OnGetReferences returns the directly referenced projects and
		/// also all transitive references which are not project references.
		/// 
		/// includeNonProjectReferences should be set to false when getting the
		/// assembly references for referenced projects since the assembly references
		/// from OnGetReferences already contains any transitive references which are
		/// not projects.
		/// </summary>
		async Task GetTransitiveAssemblyReferences (
			HashSet<string> traversedProjects,
			List<AssemblyReference> references,
			ConfigurationSelector configuration,
			bool includeNonProjectReferences,
			System.Threading.CancellationToken token)
		{
			foreach (var reference in await base.OnGetReferences (configuration, token)) {
				if (!reference.IsProjectReference) {
					if (includeNonProjectReferences) {
						references.Add (reference);
					}
					continue;
				}

				// Project references with ReferenceOutputAssembly false should be
				// added but there is no need to check any further since there will not
				// any transitive project references.
				if (!reference.ReferenceOutputAssembly) {
					references.Add (reference);
					continue;
				}

				var project = reference.GetReferencedItem (Project.ParentSolution) as DotNetProject;
				if (project == null)
					continue;

				if (traversedProjects.Contains (project.ItemId))
					continue;

				references.Add (reference);
				traversedProjects.Add (project.ItemId);

				var extension = project.AsFlavor<SdkProjectExtension> ();
				if (extension != null)
					await extension.GetTransitiveAssemblyReferences (traversedProjects, references, configuration, false, token);
			}
		}

		/// <summary>
		/// ASP.NET Core projects have different build actions if the file is in the wwwroot folder.
		/// It also uses Content build actions for *.json, *.config and *.cshtml files. To support
		/// this the default file globs for the file are found and the MSBuild item name is returned.
		/// </summary>
		internal protected override string OnGetDefaultBuildAction (string fileName)
		{
			string include = MSBuildProjectService.ToMSBuildPath (Project.ItemDirectory, fileName);
			var globItems = Project.MSBuildProject.FindGlobItemsIncludingFile (include).ToList ();
			if (globItems.Count == 1)
				return globItems [0].Name;

			return base.OnGetDefaultBuildAction (fileName);
		}
	}
}
