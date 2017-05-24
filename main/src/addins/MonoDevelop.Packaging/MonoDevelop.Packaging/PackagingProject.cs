//
// PackagingProject.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Serialization;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;

namespace MonoDevelop.Packaging
{
	class PackagingProject : DotNetProject, INuGetAwareProject
	{
		PackageReferenceCollection packageReferences;
		ReferenceAssemblyFrameworkCollection referenceAssemblyFrameworks;

		public PackagingProject ()
		{
			UsePartialTypes = false;
			RequiresMicrosoftBuild = true;
		}

		[ItemProperty ("PackageId")]
		string id;

		[ItemProperty ("PackageVersion")]
		string version;

		[ItemProperty ("Authors")]
		string authors;

		[ItemProperty ("Owners")]
		string owners;

		[ItemProperty ("Copyright")]
		string copyright;

		[ItemProperty ("DevelopmentDependency")]
		bool developmentDependency;

		[ItemProperty ("PackageTags")]
		string tags;

		[ItemProperty ("Title")]
		string title;

		[ItemProperty ("NeutralLanguage")]
		string language;

		[ItemProperty ("PackageReleaseNotes")]
		string releaseNotes;

		[ItemProperty ("Summary")]
		string summary;

		[ItemProperty ("PackageProjectUrl")]
		string projectUrl;

		[ItemProperty ("PackageIconUrl")]
		string iconUrl;

		[ItemProperty ("PackageLicenseUrl")]
		string licenseUrl;

		[ItemProperty ("PackageRequireLicenseAcceptance")]
		bool requireLicenseAcceptance;

		protected override DotNetCompilerParameters OnCreateCompilationParameters (
			DotNetProjectConfiguration config,
			ConfigurationKind kind)
		{
			return new PackagingCompilerParameters ();
		}

		protected override ClrVersion [] OnGetSupportedClrVersions ()
		{
			return new ClrVersion[] {
				ClrVersion.Net_4_5
			};
		}

		protected override bool OnSupportsFramework (TargetFramework framework)
		{
			return true;
		}

		protected override bool OnGetCanExecute (Projects.ExecutionContext context, ConfigurationSelector configuration, SolutionItemRunConfiguration runConfiguration)
		{
			return false;
		}

		protected override void PopulateOutputFileList (
			List<FilePath> list,
			ConfigurationSelector configuration)
		{
			list.Add (OnGetOutputFileName (configuration));
		}

		protected override void OnPrepareForEvaluation (MSBuildProject project)
		{
			MSBuildPropertyGroup globalGroup = project.GetGlobalPropertyGroup ();
			var provider = new MSBuildGlobalPropertyProvider ();
			foreach (KeyValuePair<string, string> property in provider.GetGlobalProperties ()) {
				globalGroup.SetValue (property.Key, property.Value, property.Value);
			}
		}

		protected override void OnGetDefaultImports (List<string> imports)
		{
			imports.Add (@"$(MSBuildBinPath)\Microsoft.Common.targets");
		}

		protected override void OnWriteProject (ProgressMonitor monitor, MSBuildProject msproject)
		{
			base.OnWriteProject (monitor, msproject);

			bool newProject = FileName == null || msproject.IsNewProject;
			if (newProject) {
				AddPackagingPropsImport (msproject);
				AddPackagingTargetsImport (msproject);
			}
		}

		void AddPackagingPropsImport (MSBuildProject msproject)
		{
			MSBuildObject insertBefore = msproject.GetAllObjects ().FirstOrDefault ();
			msproject.AddNewImport (
				@"$(NuGetAuthoringPath)\NuGet.Packaging.Authoring.props",
				@"Exists('$(NuGetAuthoringPath)\NuGet.Packaging.Authoring.props')",
				insertBefore);
		}

		void AddPackagingTargetsImport (MSBuildProject msproject)
		{
			// Create dummy new msbuild object to ensure import is added as last child.
			var insertBefore = new MSBuildItem ();
			msproject.AddNewImport (
				@"$(NuGetAuthoringPath)\NuGet.Packaging.Authoring.targets",
				@"Exists('$(NuGetAuthoringPath)\NuGet.Packaging.Authoring.targets')",
				insertBefore);
		}

		protected override string OnGetDefaultTargetPlatform (ProjectCreateInformation projectCreateInfo)
		{
			return String.Empty;
		}

		protected override void OnInitializeFromTemplate (ProjectCreateInformation projectCreateInfo, XmlElement projectOptions)
		{
			base.OnInitializeFromTemplate (projectCreateInfo, projectOptions);

			id = projectCreateInfo.Parameters ["PackageId"];
			version = projectCreateInfo.Parameters ["PackageVersion"];
			authors = projectCreateInfo.Parameters ["PackageAuthors"];
			Description = projectCreateInfo.Parameters ["PackageDescription"];
		}

		public NuGetPackageMetadata GetPackageMetadata ()
		{
			return new NuGetPackageMetadata {
				Id = id,
				Authors = authors,
				Description = Description,
				Version = version,

				Copyright = copyright,
				DevelopmentDependency = developmentDependency,
				IconUrl = iconUrl,
				Language = language,
				LicenseUrl = licenseUrl,
				ProjectUrl = projectUrl,
				Owners = owners,
				ReleaseNotes = releaseNotes,
				RequireLicenseAcceptance = requireLicenseAcceptance,
				Summary = summary,
				Tags = tags,
				Title = title
			};
		}

		public void UpdatePackageMetadata (NuGetPackageMetadata metadata)
		{
			id = ToNullIfEmpty (metadata.Id);
			version = ToNullIfEmpty (metadata.Version);
			authors = ToNullIfEmpty (metadata.Authors);
			Description = ToNullIfEmpty (metadata.Description);
			copyright = ToNullIfEmpty (metadata.Copyright);
			developmentDependency = metadata.DevelopmentDependency;
			iconUrl = ToNullIfEmpty (metadata.IconUrl);
			language = ToNullIfEmpty (metadata.Language);
			licenseUrl = ToNullIfEmpty (metadata.LicenseUrl);
			projectUrl = ToNullIfEmpty (metadata.ProjectUrl);
			owners = ToNullIfEmpty (metadata.Owners);
			releaseNotes = ToNullIfEmpty (metadata.ReleaseNotes);
			requireLicenseAcceptance = metadata.RequireLicenseAcceptance;
			summary = ToNullIfEmpty (metadata.Summary);
			tags = ToNullIfEmpty (metadata.Tags);
			title = ToNullIfEmpty (metadata.Title);
		}

		static string ToNullIfEmpty (string text)
		{
			if (String.IsNullOrEmpty (text))
				return null;

			return text;
		}

		protected override void OnReferenceAddedToProject (ProjectReferenceEventArgs e)
		{
			base.OnReferenceAddedToProject (e);

			DotNetProject project = GetDotNetProject (e.ProjectReference);
			if (project != null) {
				EnsureBuildPackagingNuGetPackageIsInstalled (project);
			}
		}

		DotNetProject GetDotNetProject (ProjectReference projectReference)
		{
			if (Loading || ParentSolution == null || projectReference.ReferenceType != ReferenceType.Project)
				return null;

			var project = projectReference.ResolveProject (ParentSolution) as DotNetProject;
			if (!(project is PackagingProject))
				return project;

			return null;
		}

		protected override void OnInitialize ()
		{
			packageReferences = new PackageReferenceCollection ();
			Items.Bind (packageReferences);

			referenceAssemblyFrameworks = new ReferenceAssemblyFrameworkCollection ();
			Items.Bind (referenceAssemblyFrameworks);

			base.OnInitialize ();
		}

		public PackageReferenceCollection PackageReferences {
			get { return packageReferences; }
		}

		public PackageReference FindPackageReference (PackageIdentity packageIdentity)
		{
			return PackageReferences.FirstOrDefault (packageReference => IsMatch (packageReference, packageIdentity));
		}

		bool IsMatch (PackageReference packageReference, PackageIdentity packageIdentity)
		{
			return String.Equals (packageReference.Include, packageIdentity.Id, StringComparison.OrdinalIgnoreCase);
		}

		public IEnumerable<TargetFrameworkMoniker> GetReferenceAssemblyFrameworks ()
		{
			return referenceAssemblyFrameworks.Select (item => item.GetTargetFrameworkMoniker ());
		}

		public void UpdateReferenceAssemblyFrameworks (IEnumerable<TargetFrameworkMoniker> frameworks)
		{
			referenceAssemblyFrameworks.Clear ();

			referenceAssemblyFrameworks.AddRange (frameworks.Select (fx => new ReferenceAssemblyFramework (fx)));
		}

		PackageReference GetNuGetBuildPackagingPackageReference ()
		{
			return PackageReferences.FirstOrDefault (packageReference => IsNuGetBuildPackagingReference (packageReference));
		}

		bool IsNuGetBuildPackagingReference (PackageReference packageReference)
		{
			return StringComparer.OrdinalIgnoreCase.Equals (packageReference.Include, "NuGet.Build.Packaging");
		}

		public NuGet.ProjectManagement.NuGetProject CreateNuGetProject ()
		{
			return new PackagingNuGetProject (this);
		}

		public bool HasPackages ()
		{
			return PackageReferences.Any ();
		}

		public async Task<bool> HasMissingPackages (IMonoDevelopSolutionManager solutionManager)
		{
			PackageIdentity packageIdentity = await GetNuGetBuildPackagingPackage ();

			if (packageIdentity == null)
				return false;

			return GlobalPackagesExtractor.IsMissing (solutionManager, packageIdentity);
		}

		public async Task RestorePackagesAsync (
			IMonoDevelopSolutionManager solutionManager,
			INuGetProjectContext context,
			CancellationToken token)
		{
			PackageIdentity packageIdentity = await GetNuGetBuildPackagingPackage ();

			if (packageIdentity == null)
				return;

			await GlobalPackagesExtractor.Download (solutionManager, packageIdentity, context, token);

			await Runtime.RunInMainThread (() => {
				PackagingNuGetProject.GenerateNuGetBuildPackagingTargets (packageIdentity, this);
				ReloadProjectBuilder ();
			});
		}

		Task<PackageIdentity> GetNuGetBuildPackagingPackage ()
		{
			return Runtime.RunInMainThread (() => {
				var packageReference = GetNuGetBuildPackagingPackageReference ();
				if (packageReference != null) {
					return packageReference.ToNuGetPackageReference ().PackageIdentity;
				}
				return null;
			});
		}

		void EnsureBuildPackagingNuGetPackageIsInstalled (DotNetProject project)
		{
			if (!project.IsBuildPackagingNuGetPackageInstalled ()) {
				project.InstallBuildPackagingNuGetPackage ();
			}
		}
	}
}

