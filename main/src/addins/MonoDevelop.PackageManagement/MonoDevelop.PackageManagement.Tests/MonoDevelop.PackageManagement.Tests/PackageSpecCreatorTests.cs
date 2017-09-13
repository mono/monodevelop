//
// PackageSpecCreatorTests.cs
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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NuGet.Commands;
using NuGet.Configuration;
using NUnit.Framework;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectManagement;
using NuGet.ProjectModel;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageSpecCreatorTests
	{
		PackageSpec spec;
		FakeDotNetProject project;
		FakeSolution solution;
		PackageManagementEvents packageManagementEvents;
		PackageManagementLogger logger;
		FakeNuGetSettings settings;

		void CreateProject (string name, string fileName = @"d:\projects\MyProject\MyProject.csproj")
		{
			packageManagementEvents = new PackageManagementEvents ();
			logger = new PackageManagementLogger (packageManagementEvents);
			settings = new FakeNuGetSettings ();
			solution = new FakeSolution ();
			project = new FakeDotNetProject (fileName.ToNativePath ());
			project.ParentSolution = solution;
			project.Name = name;
		}

		void AddTargetFramework (string targetFramework)
		{
			project.AddTargetFramework (targetFramework);
		}

		void CreatePackageSpec ()
		{
			var context = new DependencyGraphCacheContext (logger, settings);
			spec = PackageSpecCreator.CreatePackageSpec (project, context);
		}

		TestableProjectPackageReference AddPackageReference (string id, string version)
		{
			var packageReference = new TestableProjectPackageReference (id, version);
			project.PackageReferences.Add (packageReference);
			return packageReference;
		}

		FakeDotNetProject AddProjectReference (string projectName, string fileName, string include, bool referenceOutputAssembly = true)
		{
			fileName = fileName.ToNativePath ();
			var projectReference = ProjectReference.CreateCustomReference (ReferenceType.Project, include);
			projectReference.ReferenceOutputAssembly = referenceOutputAssembly;
			project.References.Add (projectReference);

			var fakeOtherProject = new FakeDotNetProject (fileName);
			fakeOtherProject.Name = projectName;
			solution.Projects.Add (fakeOtherProject);

			return fakeOtherProject;
		}

		void AddPackageTargetFallback (string packageTargetFallback)
		{
			project.AddPackageTargetFallback (packageTargetFallback);
		}

		void AddAssetTargetFallback (string assetTargetFallback)
		{
			project.AddProperty ("AssetTargetFallback", assetTargetFallback);
		}

		void AddPackagesPath (string path)
		{
			settings.SetValue (SettingsUtility.ConfigSection, "globalPackagesFolder", path);
		}

		void AddFallbackFolder (params string[] folders)
		{
			var values = folders.Select (folder => new SettingValue (folder, folder, false)).ToList ();
			settings.SetValues (ConfigurationConstants.FallbackPackageFolders, values);
		}

		void AddRestoreAdditionalProjectFallbackFolders (string folders)
		{
			project.AddProperty ("RestoreAdditionalProjectFallbackFolders", folders);
		}

		void AddRestoreFallbackFolders (string folders)
		{
			project.AddProperty ("RestoreFallbackFolders", folders);
		}

		void AddRestoreAdditionalProjectSources (string sources)
		{
			project.AddProperty ("RestoreAdditionalProjectSources", sources);
		}

		void AddRestoreSources (string sources)
		{
			project.AddProperty ("RestoreSources", sources);
		}

		[Test]
		public void CreatePackageSpec_NewProject_BaseIntermediatePathUsedForProjectAssetsJsonFile ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			project.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			AddTargetFramework ("netcoreapp1.0");
			string expectedFilePath = @"d:\projects\MyProject\MyProject.csproj".ToNativePath ();

			CreatePackageSpec ();

			Assert.AreEqual (expectedFilePath, spec.FilePath);
			Assert.AreEqual ("MyProject", spec.Name);
			Assert.AreEqual ("1.0.0", spec.Version.ToString ());
			Assert.AreEqual (ProjectStyle.PackageReference, spec.RestoreMetadata.ProjectStyle);
			Assert.AreEqual ("MyProject", spec.RestoreMetadata.ProjectName);
			Assert.AreEqual (project.FileName.ToString (), spec.RestoreMetadata.ProjectPath);
			Assert.AreEqual (project.FileName.ToString (), spec.RestoreMetadata.ProjectUniqueName);
			Assert.AreEqual (project.BaseIntermediateOutputPath.ToString (), spec.RestoreMetadata.OutputPath);
			Assert.AreEqual ("netcoreapp1.0", spec.RestoreMetadata.OriginalTargetFrameworks.Single ());
		}

		[Test]
		public void CreatePackageSpec_OnePackageReference_PackageReferencedAddedToPackageSpec ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			AddPackageReference ("Newtonsoft.Json", "9.0.1");

			CreatePackageSpec ();

			var targetFramework = spec.TargetFrameworks.Single ();
			var dependency = targetFramework.Dependencies.Single ();
			Assert.AreEqual ("MyProject", spec.Name);
			Assert.AreEqual (ProjectStyle.PackageReference, spec.RestoreMetadata.ProjectStyle);
			Assert.AreEqual ("MyProject", spec.RestoreMetadata.ProjectName);
			Assert.AreEqual ("netcoreapp1.0", spec.RestoreMetadata.OriginalTargetFrameworks.Single ());
			Assert.AreEqual (".NETCoreApp,Version=v1.0", targetFramework.FrameworkName.ToString ());
			Assert.AreEqual ("Newtonsoft.Json", dependency.Name);
			Assert.AreEqual (LibraryDependencyType.Default, dependency.Type);
			Assert.AreEqual (LibraryIncludeFlags.All, dependency.IncludeType);
			Assert.AreEqual (LibraryIncludeFlagUtils.DefaultSuppressParent, dependency.SuppressParent);
			Assert.AreEqual ("[9.0.1, )", dependency.LibraryRange.VersionRange.ToString ());
			Assert.AreEqual (LibraryDependencyTarget.Package, dependency.LibraryRange.TypeConstraint);
			Assert.AreEqual ("Newtonsoft.Json", dependency.LibraryRange.Name);
		}

		[Test]
		public void CreatePackageSpec_NonDotNetCoreProjectWithOnePackageReference_TargetFrameworkTakenFromProjectNotTargetFrameworkProperty ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			project.TargetFrameworkMoniker = TargetFrameworkMoniker.Parse (".NETFramework,Version=v4.6.1");
			AddPackageReference ("Newtonsoft.Json", "9.0.1");

			CreatePackageSpec ();

			var targetFramework = spec.TargetFrameworks.Single ();
			var dependency = targetFramework.Dependencies.Single ();
			Assert.AreEqual ("MyProject", spec.Name);
			Assert.AreEqual (ProjectStyle.PackageReference, spec.RestoreMetadata.ProjectStyle);
			Assert.AreEqual ("MyProject", spec.RestoreMetadata.ProjectName);
			Assert.AreEqual (".NETFramework,Version=v4.6.1", targetFramework.FrameworkName.ToString ());
			Assert.AreEqual ("net461", spec.RestoreMetadata.OriginalTargetFrameworks.Single ());
			Assert.AreEqual ("Newtonsoft.Json", dependency.Name);
			Assert.AreEqual (LibraryDependencyType.Default, dependency.Type);
			Assert.AreEqual (LibraryIncludeFlags.All, dependency.IncludeType);
			Assert.AreEqual (LibraryIncludeFlagUtils.DefaultSuppressParent, dependency.SuppressParent);
			Assert.AreEqual ("[9.0.1, )", dependency.LibraryRange.VersionRange.ToString ());
			Assert.AreEqual (LibraryDependencyTarget.Package, dependency.LibraryRange.TypeConstraint);
			Assert.AreEqual ("Newtonsoft.Json", dependency.LibraryRange.Name);
		}

		[Test]
		public void CreatePackageSpec_OneProjectReference_ProjectReferencedAddedToPackageSpec ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string referencedProjectFileName = @"d:\projects\MyProject\Lib\Lib.csproj".ToNativePath ();
			string include = @"Lib\Lib.csproj".ToNativePath ();
			var referencedProject = AddProjectReference ("Lib", referencedProjectFileName, include);
			solution.OnResolveProject = pr => {
				if (pr.Include == include)
					return referencedProject;
				return null;
			};
			CreatePackageSpec ();

			var targetFramework = spec.RestoreMetadata.TargetFrameworks.Single ();
			var projectReference = targetFramework.ProjectReferences.Single ();
			Assert.AreEqual ("MyProject", spec.Name);
			Assert.AreEqual ("MyProject", spec.RestoreMetadata.ProjectName);
			Assert.AreEqual ("netcoreapp1.0", spec.RestoreMetadata.OriginalTargetFrameworks.Single ());
			Assert.AreEqual (".NETCoreApp,Version=v1.0", targetFramework.FrameworkName.ToString ());
			Assert.AreEqual (referencedProjectFileName, projectReference.ProjectPath);
			Assert.AreEqual (referencedProjectFileName, projectReference.ProjectUniqueName);
			Assert.AreEqual (LibraryIncludeFlags.All, projectReference.IncludeAssets);
			Assert.AreEqual (LibraryIncludeFlags.None, projectReference.ExcludeAssets);
			Assert.AreEqual (
				LibraryIncludeFlags.Analyzers | LibraryIncludeFlags.Build | LibraryIncludeFlags.ContentFiles,
				projectReference.PrivateAssets);
		}

		[Test]
		public void CreatePackageSpec_OneSharedProjectReference_NoProjectReferencedAddedToPackageSpec ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string referencedProjectFileName = @"d:\projects\MyProject\Lib\Lib.shproj".ToNativePath ();
			AddProjectReference ("Lib", referencedProjectFileName, @"Lib\Lib.shproj".ToNativePath ());

			CreatePackageSpec ();

			var targetFramework = spec.RestoreMetadata.TargetFrameworks.Single ();
			Assert.AreEqual ("MyProject", spec.Name);
			Assert.AreEqual ("MyProject", spec.RestoreMetadata.ProjectName);
			Assert.AreEqual ("netcoreapp1.0", spec.RestoreMetadata.OriginalTargetFrameworks.Single ());
			Assert.AreEqual (".NETCoreApp,Version=v1.0", targetFramework.FrameworkName.ToString ());
			Assert.AreEqual (0, targetFramework.ProjectReferences.Count);
		}

		[Test]
		public void CreatePackageSpec_OneProjectReferenceWhichCannotBeResolved_WarningLoggedAndNoProjectReferencedAddedToPackageSpec ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string referencedProjectFileName = @"d:\projects\MyProject\Lib\Lib.csproj".ToNativePath ();
			string include = @"Lib\Lib.csproj".ToNativePath ();
			AddProjectReference ("Lib", referencedProjectFileName, include);
			solution.OnResolveProject = pr => {
				return null;
			};
			PackageOperationMessage messageLogged = null;
			packageManagementEvents.PackageOperationMessageLogged += (sender, e) => {
				messageLogged = e.Message;
			};
			CreatePackageSpec ();

			var targetFramework = spec.RestoreMetadata.TargetFrameworks.Single ();
			string expectedMessage = string.Format ("WARNING: Unable to resolve project '{0}' referenced by 'MyProject'.", include);
			Assert.AreEqual ("MyProject", spec.Name);
			Assert.AreEqual ("MyProject", spec.RestoreMetadata.ProjectName);
			Assert.AreEqual ("netcoreapp1.0", spec.RestoreMetadata.OriginalTargetFrameworks.Single ());
			Assert.AreEqual (".NETCoreApp,Version=v1.0", targetFramework.FrameworkName.ToString ());
			Assert.AreEqual (expectedMessage, messageLogged.ToString ());
			Assert.AreEqual (MessageLevel.Warning, messageLogged.Level);
			Assert.AreEqual (0, targetFramework.ProjectReferences.Count);
		}

		[Test]
		public void CreatePackageSpec_PackageTargetFallback_ImportsAddedToTargetFramework ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			AddPackageTargetFallback (";dotnet5.6;portable-net45+win8;");
			CreatePackageSpec ();

			var targetFramework = spec.TargetFrameworks.Single ();
			var fallbackFramework = targetFramework.FrameworkName as FallbackFramework;
			Assert.AreEqual (2, targetFramework.Imports.Count);
			Assert.AreEqual ("dotnet5.6", targetFramework.Imports[0].GetShortFolderName ());
			Assert.AreEqual ("portable-net45+win8", targetFramework.Imports[1].GetShortFolderName ());
			Assert.IsNotNull (fallbackFramework);
			Assert.AreEqual (".NETCoreApp,Version=v1.0", targetFramework.FrameworkName.ToString ());
			Assert.AreEqual (2, fallbackFramework.Fallback.Count);
			Assert.AreEqual ("dotnet5.6", fallbackFramework.Fallback[0].GetShortFolderName ());
			Assert.AreEqual ("portable-net45+win8", fallbackFramework.Fallback[1].GetShortFolderName ());
		}

		[Test]
		public void CreatePackageSpec_RuntimeIdentifiers_AddedToRuntimeGraph ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			project.AddProperty ("RuntimeIdentifiers", ";win7-x64;win7-x86;");
			project.AddProperty ("RuntimeSupports", ";dotnet5.6;portable-net45+win8;");
			CreatePackageSpec ();

			var runtimeGraph = spec.RuntimeGraph;
			Assert.AreEqual (2, runtimeGraph.Runtimes.Count);
			Assert.AreEqual ("win7-x64", runtimeGraph.Runtimes["win7-x64"].RuntimeIdentifier);
			Assert.AreEqual ("win7-x86", runtimeGraph.Runtimes["win7-x86"].RuntimeIdentifier);
			Assert.AreEqual (2, runtimeGraph.Supports.Count);
			Assert.AreEqual ("dotnet5.6", runtimeGraph.Supports["dotnet5.6"].Name);
			Assert.AreEqual ("portable-net45+win8", runtimeGraph.Supports["portable-net45+win8"].Name);
		}

		[Test]
		public void CreatePackageSpec_OneProjectReferenceWithReferenceAssemblyIsFalse_ProjectReferencedNotAddedToPackageSpec ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string referencedProjectFileName = @"d:\projects\MyProject\Lib\Lib.csproj".ToNativePath ();
			string include = @"Lib\Lib.csproj".ToNativePath ();
			var referencedProject = AddProjectReference ("Lib", referencedProjectFileName, include, referenceOutputAssembly: false);
			solution.OnResolveProject = pr => {
				if (pr.Include == include)
					return referencedProject;
				return null;
			};
			CreatePackageSpec ();

			var targetFramework = spec.RestoreMetadata.TargetFrameworks.Single ();
			Assert.AreEqual ("MyProject", spec.Name);
			Assert.AreEqual ("MyProject", spec.RestoreMetadata.ProjectName);
			Assert.AreEqual ("netcoreapp1.0", spec.RestoreMetadata.OriginalTargetFrameworks.Single ());
			Assert.AreEqual (".NETCoreApp,Version=v1.0", targetFramework.FrameworkName.ToString ());
			Assert.AreEqual (0, targetFramework.ProjectReferences.Count);
		}

		[Test]
		public void CreatePackageSpec_PackagesPath_RestoreMetadataHasPackagesPathTakenFromSettings ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			project.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			AddTargetFramework ("netcoreapp1.0");
			string packagesPath = @"c:\users\test\packages".ToNativePath ();
			AddPackagesPath (packagesPath);

			CreatePackageSpec ();

			Assert.AreEqual (packagesPath, spec.RestoreMetadata.PackagesPath);
		}

		[Test]
		public void CreatePackageSpec_ConfigFilePaths_RestoreMetadataHasConfigFilesTakenFromSettings ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			project.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			AddTargetFramework ("netcoreapp1.0");
			string configFilePath = @"c:\users\test\.nuget\NuGet\NuGet.Config".ToNativePath ();
			settings.FileName = configFilePath;

			CreatePackageSpec ();

			Assert.AreEqual (1, spec.RestoreMetadata.ConfigFilePaths.Count);
			Assert.AreEqual (configFilePath, spec.RestoreMetadata.ConfigFilePaths[0]);
		}

		[Test]
		public void CreatePackageSpec_PackageSources_RestoreMetadataHasSourcesTakenFromSettings ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			project.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			AddTargetFramework ("netcoreapp1.0");
			var packageSource = new PackageSource ("https://nuget.org", "NuGet source");
			var sources = new List<SettingValue> ();
			sources.Add (new SettingValue (packageSource.Name, packageSource.Source, false));
			settings.SetValues (ConfigurationConstants.PackageSources, sources);

			CreatePackageSpec ();

			Assert.AreEqual (1, spec.RestoreMetadata.Sources.Count);
			Assert.AreEqual (packageSource.Name, spec.RestoreMetadata.Sources[0].Name);
			Assert.AreEqual (packageSource.Source, spec.RestoreMetadata.Sources[0].Source);
		}

		[Test]
		public void CreatePackageSpec_FallbackFolders_RestoreMetadataHasFallbackFoldersTakenFromSettings ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			project.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			AddTargetFramework ("netcoreapp1.0");
			string folderPath = @"c:\users\test\packages".ToNativePath ();
			AddFallbackFolder (folderPath);

			CreatePackageSpec ();

			Assert.AreEqual (1, spec.RestoreMetadata.FallbackFolders.Count);
			Assert.AreEqual (folderPath, spec.RestoreMetadata.FallbackFolders[0]);
		}

		[Test]
		public void CreatePackageSpec_ProjectWideWarningProperties_RestoreMetadataHasProjectWideWarningProperties ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			project.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			AddTargetFramework ("netcoreapp1.0");

			project.AddProperty ("TreatWarningsAsErrors", bool.TrueString);
			project.AddProperty ("WarningsAsErrors", "NU1801");
			project.AddProperty ("NoWarn", "NU1701");

			CreatePackageSpec ();

			Assert.IsTrue (spec.RestoreMetadata.ProjectWideWarningProperties.AllWarningsAsErrors);
			Assert.IsTrue (spec.RestoreMetadata.ProjectWideWarningProperties.WarningsAsErrors.Contains (NuGet.Common.NuGetLogCode.NU1801));
			Assert.IsTrue (spec.RestoreMetadata.ProjectWideWarningProperties.NoWarn.Contains (NuGet.Common.NuGetLogCode.NU1701));
		}

		[Test]
		public void CreatePackageSpec_TreatWarningsAsErrorsIsFalse_RestoreMetadataHasTreatWarningsAsErrorsSetToFalse ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			project.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			AddTargetFramework ("netcoreapp1.0");

			project.AddProperty ("TreatWarningsAsErrors", bool.FalseString);
			project.AddProperty ("WarningsAsErrors", "NU1801");
			project.AddProperty ("NoWarn", "NU1701");

			CreatePackageSpec ();

			Assert.IsFalse (spec.RestoreMetadata.ProjectWideWarningProperties.AllWarningsAsErrors);
			Assert.IsTrue (spec.RestoreMetadata.ProjectWideWarningProperties.WarningsAsErrors.Contains (NuGet.Common.NuGetLogCode.NU1801));
			Assert.IsTrue (spec.RestoreMetadata.ProjectWideWarningProperties.NoWarn.Contains (NuGet.Common.NuGetLogCode.NU1701));
		}

		[Test]
		public void CreatePackageSpec_AssetTargetFallback_ImportsAddedToTargetFramework ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			AddAssetTargetFallback (";dotnet5.6;portable-net45+win8;");
			CreatePackageSpec ();

			var targetFramework = spec.TargetFrameworks.Single ();
			var fallbackFramework = targetFramework.FrameworkName as AssetTargetFallbackFramework;
			Assert.AreEqual (2, targetFramework.Imports.Count);
			Assert.AreEqual ("dotnet5.6", targetFramework.Imports[0].GetShortFolderName ());
			Assert.AreEqual ("portable-net45+win8", targetFramework.Imports[1].GetShortFolderName ());
			Assert.IsNotNull (fallbackFramework);
			Assert.AreEqual (".NETCoreApp,Version=v1.0", targetFramework.FrameworkName.ToString ());
			Assert.AreEqual (2, fallbackFramework.Fallback.Count);
			Assert.AreEqual ("dotnet5.6", fallbackFramework.Fallback[0].GetShortFolderName ());
			Assert.AreEqual ("portable-net45+win8", fallbackFramework.Fallback[1].GetShortFolderName ());
			Assert.IsTrue (targetFramework.Warn);
			Assert.IsTrue (targetFramework.AssetTargetFallback);
		}

		[Test]
		public void CreatePackageSpec_AssetTargetFallbackAndPackageTargetFallbackBothDefined_ExceptionThrown ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			AddAssetTargetFallback (";dotnet5.6;portable-net45+win8;");
			AddPackageTargetFallback (";dotnet5.5;portable-net45+win8;");

			var ex = Assert.Throws<RestoreCommandException> (CreatePackageSpec);
			string expectedMessage = "PackageTargetFallback and AssetTargetFallback cannot be used together. Remove PackageTargetFallback(deprecated) references from the project environment.";
			Assert.AreEqual (expectedMessage, ex.Message);
		}

		[Test]
		public void CreatePackageSpec_OneImplicitlyDefinedPackageReference_PackageReferencedAddedMarkedAsAutoReferenced ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			var packageReference = AddPackageReference ("Test", "1.2.3");
			packageReference.IsImplicit = true;

			CreatePackageSpec ();

			var targetFramework = spec.TargetFrameworks.Single ();
			var dependency = targetFramework.Dependencies.Single ();
			Assert.AreEqual ("Test", dependency.Name);
			Assert.IsTrue (dependency.AutoReferenced);
			Assert.AreEqual (LibraryDependencyType.Default, dependency.Type);
			Assert.AreEqual (LibraryIncludeFlags.All, dependency.IncludeType);
			Assert.AreEqual (LibraryIncludeFlagUtils.DefaultSuppressParent, dependency.SuppressParent);
			Assert.AreEqual ("[1.2.3, )", dependency.LibraryRange.VersionRange.ToString ());
			Assert.AreEqual (LibraryDependencyTarget.Package, dependency.LibraryRange.TypeConstraint);
			Assert.AreEqual ("Test", dependency.LibraryRange.Name);
		}

		[Test]
		public void CreatePackageSpec_RestoreAdditionalProjectFallbackFolders_IncludedInFallbackFolders ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string folderPath1 = @"c:\users\test1\packages".ToNativePath ();
			AddFallbackFolder (folderPath1);
			string folderPath2 = @"c:\users\test2\packages".ToNativePath ();
			string folderPath3 = @"c:\users\test3\packages".ToNativePath ();
			string folders = folderPath2 + ";" + folderPath3;
			AddRestoreAdditionalProjectFallbackFolders (folders);

			CreatePackageSpec ();

			Assert.AreEqual (3, spec.RestoreMetadata.FallbackFolders.Count);
			Assert.AreEqual (folderPath1, spec.RestoreMetadata.FallbackFolders[0]);
			Assert.AreEqual (folderPath2, spec.RestoreMetadata.FallbackFolders[1]);
			Assert.AreEqual (folderPath3, spec.RestoreMetadata.FallbackFolders[2]);
		}

		[Test]
		public void CreatePackageSpec_RestoreAdditionalProjectFallbackFoldersWithBackSlashes_BackSlashesFixed ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string folderPath = @"c:\users\test1\packages".ToNativePath ();
			string folderPathWithBackSlashes = folderPath.Replace ('/', '\\');
			AddRestoreAdditionalProjectFallbackFolders (folderPathWithBackSlashes);

			CreatePackageSpec ();

			Assert.AreEqual (folderPath, spec.RestoreMetadata.FallbackFolders[0]);
		}

		[Test]
		public void CreatePackageSpec_RestoreAdditionalProjectFallbackFoldersUsingRelativePath_PathResolved ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string folderPath = @"..\packages".ToNativePath ();
			string expectedFallbackFolder = @"d:\projects\packages".ToNativePath ();
			AddRestoreAdditionalProjectFallbackFolders (folderPath);

			CreatePackageSpec ();

			Assert.AreEqual (expectedFallbackFolder, spec.RestoreMetadata.FallbackFolders[0]);
		}

		[Test]
		public void CreatePackageSpec_RestoreFallbackFolders_IncludedInFallbackFoldersInsteadOfSettingsFallbackFolders ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string folderPath1 = @"c:\users\test1\packages".ToNativePath ();
			AddFallbackFolder (folderPath1);
			string folderPath2 = @"c:\users\test2\packages".ToNativePath ();
			string folderPath3 = @"c:\users\test3\packages".ToNativePath ();
			string folders = folderPath2 + ";" + folderPath3;
			AddRestoreFallbackFolders (folders);

			CreatePackageSpec ();

			Assert.AreEqual (2, spec.RestoreMetadata.FallbackFolders.Count);
			Assert.AreEqual (folderPath2, spec.RestoreMetadata.FallbackFolders[0]);
			Assert.AreEqual (folderPath3, spec.RestoreMetadata.FallbackFolders[1]);
		}

		[Test]
		public void CreatePackageSpec_RestoreFallbackFoldersIsSetToClear_NoFallbackFolders ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string folderPath = @"c:\users\test1\packages".ToNativePath ();
			AddFallbackFolder (folderPath);
			AddRestoreFallbackFolders ("clear");

			CreatePackageSpec ();

			Assert.AreEqual (0, spec.RestoreMetadata.FallbackFolders.Count);
		}

		[Test]
		public void CreatePackageSpec_RestoreFallbackFoldersIsSetToClearInUpperCase_NoFallbackFolders ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string folderPath = @"c:\users\test1\packages".ToNativePath ();
			AddFallbackFolder (folderPath);
			AddRestoreFallbackFolders ("CLEAR");

			CreatePackageSpec ();

			Assert.AreEqual (0, spec.RestoreMetadata.FallbackFolders.Count);
		}

		[Test]
		public void CreatePackageSpec_RestorePackagesPath_PackagesPathTakenFromRestorePackagesPathNotFromSettings ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			project.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			AddTargetFramework ("netcoreapp1.0");
			string packagesPath = @"c:\users\test\packages1".ToNativePath ();
			string restorePackagesPath = @"c:\users\test\packages2".ToNativePath ();
			AddPackagesPath (packagesPath);
			project.AddProperty ("RestorePackagesPath", restorePackagesPath);

			CreatePackageSpec ();

			Assert.AreEqual (restorePackagesPath, spec.RestoreMetadata.PackagesPath);
		}

		[Test]
		public void CreatePackageSpec_RelativeRestorePackagesPath_PackagesPathResolvedFromProjectDirectory ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			project.BaseIntermediateOutputPath = @"d:\projects\MyProject\obj".ToNativePath ();
			AddTargetFramework ("netcoreapp1.0");
			string restorePackagesPath = @"d:\projects\packages".ToNativePath ();
			project.AddProperty ("RestorePackagesPath", @"..\packages");

			CreatePackageSpec ();

			Assert.AreEqual (restorePackagesPath, spec.RestoreMetadata.PackagesPath);
		}

		[Test]
		public void CreatePackageSpec_RestoreAdditionalProjectSources_IncludedSources ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			var packageSource = new PackageSource ("https://nuget.org", "NuGet source");
			var sources = new List<SettingValue> ();
			sources.Add (new SettingValue (packageSource.Name, packageSource.Source, false));
			settings.SetValues (ConfigurationConstants.PackageSources, sources);
			AddRestoreAdditionalProjectSources ("https://myget.org;https://microsoft.com");

			CreatePackageSpec ();

			Assert.AreEqual (3, spec.RestoreMetadata.Sources.Count);
			Assert.AreEqual (packageSource.Name, spec.RestoreMetadata.Sources[0].Name);
			Assert.AreEqual (packageSource.Source, spec.RestoreMetadata.Sources[0].Source);
			Assert.AreEqual ("https://myget.org", spec.RestoreMetadata.Sources[1].Name);
			Assert.AreEqual ("https://myget.org", spec.RestoreMetadata.Sources[1].Source);
			Assert.AreEqual ("https://microsoft.com", spec.RestoreMetadata.Sources[2].Name);
			Assert.AreEqual ("https://microsoft.com", spec.RestoreMetadata.Sources[2].Source);
		}

		[Test]
		public void CreatePackageSpec_RestoreAdditionalProjectSourcesUsingRelativePath_PathResolvedUsingProjectDirectory ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string expectedSource = @"d:\projects\packages".ToNativePath ();
			AddRestoreAdditionalProjectSources (@"..\packages");

			CreatePackageSpec ();

			Assert.AreEqual (1, spec.RestoreMetadata.Sources.Count);
			Assert.AreEqual (expectedSource, spec.RestoreMetadata.Sources[0].Name);
			Assert.AreEqual (expectedSource, spec.RestoreMetadata.Sources[0].Source);
		}

		[Test]
		public void CreatePackageSpec_RestoreAdditionalProjectSourcesFullDirectoryPath_PathIsUsedAsSource ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			string expectedSource = @"d:\projects\packages".ToNativePath ();
			AddRestoreAdditionalProjectSources (expectedSource);

			CreatePackageSpec ();

			Assert.AreEqual (1, spec.RestoreMetadata.Sources.Count);
			Assert.AreEqual (expectedSource, spec.RestoreMetadata.Sources[0].Name);
			Assert.AreEqual (expectedSource, spec.RestoreMetadata.Sources[0].Source);
		}

		[Test]
		public void CreatePackageSpec_RestoreSources_SourcesDefinedInSettingsAreNotUsed ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			var packageSource = new PackageSource ("https://nuget.org", "NuGet source");
			var sources = new List<SettingValue> ();
			sources.Add (new SettingValue (packageSource.Name, packageSource.Source, false));
			settings.SetValues (ConfigurationConstants.PackageSources, sources);
			AddRestoreSources ("https://myget.org");

			CreatePackageSpec ();

			Assert.AreEqual (1, spec.RestoreMetadata.Sources.Count);
			Assert.AreEqual ("https://myget.org", spec.RestoreMetadata.Sources[0].Name);
			Assert.AreEqual ("https://myget.org", spec.RestoreMetadata.Sources[0].Source);
		}

		[Test]
		public void CreatePackageSpec_RestoreSourcesSetToClear_NoPackageSources ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			var packageSource = new PackageSource ("https://nuget.org", "NuGet source");
			var sources = new List<SettingValue> ();
			sources.Add (new SettingValue (packageSource.Name, packageSource.Source, false));
			settings.SetValues (ConfigurationConstants.PackageSources, sources);
			AddRestoreSources ("clear");

			CreatePackageSpec ();

			Assert.AreEqual (0, spec.RestoreMetadata.Sources.Count);
		}

		[Test]
		public void CreatePackageSpec_RestoreSourcesSetToClearInUpperCase_NoPackageSources ()
		{
			CreateProject ("MyProject", @"d:\projects\MyProject\MyProject.csproj");
			AddTargetFramework ("netcoreapp1.0");
			var packageSource = new PackageSource ("https://nuget.org", "NuGet source");
			var sources = new List<SettingValue> ();
			sources.Add (new SettingValue (packageSource.Name, packageSource.Source, false));
			settings.SetValues (ConfigurationConstants.PackageSources, sources);
			AddRestoreSources ("CLEAR");

			CreatePackageSpec ();

			Assert.AreEqual (0, spec.RestoreMetadata.Sources.Count);
		}
	}
}
