//
// FakePackage.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.IO;
using System.Runtime.Versioning;
using ICSharpCode.PackageManagement;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakePackage : IPackageFromRepository
	{
		public Stream Stream = null;
		public List<string> AuthorsList = new List<string> ();
		public List<string> OwnersList = new List<string> ();
		public List<IPackageFile> FilesList = new List<IPackageFile> ();

		public List<PackageDependency> DependenciesList = 
			new List<PackageDependency> ();

		public List<IPackageAssemblyReference> AssemblyReferenceList =
			new List<IPackageAssemblyReference> ();

		public FakePackage ()
			: this (String.Empty)
		{
		}

		public FakePackage (string id)
			: this (id, "1.0.0.0")
		{
		}

		public FakePackage (string id, string version)
		{
			this.Id = id;
			this.Description = String.Empty;
			this.Version = new SemanticVersion (version);
			this.Listed = true;
			this.IsLatestVersion = true;
			IsValid = true;
		}

		public static FakePackage CreatePackageWithVersion (string version)
		{
			return CreatePackageWithVersion ("Test", version);
		}

		public static FakePackage CreatePackageWithVersion (string id, string version)
		{
			return new FakePackage (id, version);
		}

		public string Id { get; set; }
		public SemanticVersion Version { get; set; }
		public string Title { get; set; }
		public Uri IconUrl { get; set; }
		public Uri LicenseUrl { get; set; }
		public Uri ProjectUrl { get; set; }
		public bool RequireLicenseAcceptance { get; set; }
		public string Description { get; set; }
		public string Summary { get; set; }
		public string Language { get; set; }
		public string Tags { get; set; }
		public Uri ReportAbuseUrl { get; set; }
		public int DownloadCount { get; set; }
		public int RatingsCount { get; set; }
		public double Rating { get; set; }

		public IEnumerable<IPackageAssemblyReference> AssemblyReferences {
			get { return AssemblyReferenceList; }
		}

		public IEnumerable<string> Authors {
			get { return AuthorsList; }
		}

		public IEnumerable<string> Owners {
			get { return OwnersList; }
		}

		public IEnumerable<IPackageFile> GetFiles ()
		{
			return FilesList;
		}

		public Stream GetStream ()
		{
			return Stream;
		}

		public override string ToString ()
		{
			return String.Format ("{0} {1}", Id, Version);
		}

		public override bool Equals (object obj)
		{
			IPackage rhs = obj as IPackage;
			if (rhs != null) {
				return (Id == rhs.Id) && (Version == rhs.Version);
			}
			return false;
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}

		public void AddAuthor (string author)
		{
			AuthorsList.Add (author);
		}

		public void AddDependency (string id, SemanticVersion minVersion, SemanticVersion maxVersion)
		{
			var versionSpec = new VersionSpec ();
			versionSpec.MinVersion = minVersion;
			versionSpec.MaxVersion = maxVersion;
			var dependency = new PackageDependency (id, versionSpec);
			DependenciesList.Add (dependency);
		}

		public void AddDependency (string id)
		{
			DependenciesList.Add (new PackageDependency (id));
		}

		public List<FrameworkAssemblyReference> FrameworkAssembliesList = 
			new List<FrameworkAssemblyReference> ();

		public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies {
			get { return FrameworkAssembliesList; }
		}

		public FakePackageRepository FakePackageRepository = new FakePackageRepository ();

		public IPackageRepository Repository {
			get { return FakePackageRepository; }
		}

		public bool HasDependencies { get; set; }

		public void AddFile (string fileName)
		{
			var file = new PhysicalPackageFile ();
			file.TargetPath = fileName.ToNativePath ();
			FilesList.Add (file);
		}

		public DateTime? LastUpdated { get; set; }

		public bool IsLatestVersion { get; set; }

		public Nullable<DateTimeOffset> Published { get; set; }

		public string ReleaseNotes { get; set; }

		public string Copyright { get; set; }

		public bool IsAbsoluteLatestVersion { get; set; }

		public bool Listed { get; set; }

		public IEnumerable<PackageDependencySet> DependencySets {
			get {
				return new PackageDependencySet[] {
					new PackageDependencySet (null, DependenciesList)
				};
			}
		}

		public List<FrameworkName> SupportedFrameworks = new List<FrameworkName> ();

		public FrameworkName AddSupportedFramework (string identifier)
		{
			var framework = new FrameworkName (identifier);
			SupportedFrameworks.Add (framework);
			return framework;
		}

		public IEnumerable<FrameworkName> GetSupportedFrameworks ()
		{
			return SupportedFrameworks;
		}

		List<PackageReferenceSet> FakePackageAssemblyReferences = 
			new List<PackageReferenceSet> ();

		public ICollection<PackageReferenceSet> PackageAssemblyReferences {
			get { return FakePackageAssemblyReferences; }
		}

		public void AddPackageReferences (params string[] names)
		{
			var frameworkName = new FrameworkName (".NET Framework, Version=4.0");
			var packageReferenceSet = new PackageReferenceSet (frameworkName, names);
			FakePackageAssemblyReferences.Add (packageReferenceSet);
		}

		public Version MinClientVersion { get; set; }

		public Uri GalleryUrl { get; set; }

		public bool DevelopmentDependency { get; set; }

		public bool IsValid { get; set; }

		public IFileSystem FileSystemPassedToExtractContents;
		public string ExtractPathPassedToExtractContents;

		public void ExtractContents (IFileSystem fileSystem, string extractPath)
		{
			FileSystemPassedToExtractContents = fileSystem;
			ExtractPathPassedToExtractContents = extractPath;
		}
	}
}

