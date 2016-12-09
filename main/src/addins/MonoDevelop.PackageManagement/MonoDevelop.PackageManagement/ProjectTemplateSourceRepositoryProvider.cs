//
// ProjectTemplateSourceRepositoryProvider.cs
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
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class ProjectTemplateSourceRepositoryProvider
	{
		ISourceRepositoryProvider provider;
		List<SourceRepository> projectTemplateRepositories;
		SourceRepository nugetSourceRepository;

		public ProjectTemplateSourceRepositoryProvider ()
		{
			provider = SourceRepositoryProviderFactory.CreateSourceRepositoryProvider ();
			var packageSource = new PackageSource (NuGetConstants.V3FeedUrl);
			nugetSourceRepository = provider.CreateRepository (packageSource);
		}

		public IEnumerable<SourceRepository> GetRepositories (ProjectTemplatePackageReference packageReference)
		{
			if (packageReference.Directory.IsNotNull) {
				yield return CreateRepository (packageReference.Directory);
				yield break;
			}

			foreach (SourceRepository sourceRepository in GetProjectTemplateRepositories ()) {
				if (!packageReference.IsLocalPackage || sourceRepository.PackageSource.IsLocal) {
					yield return sourceRepository;
				}
			}

			if (!packageReference.IsLocalPackage) {
				yield return nugetSourceRepository;
			}
		}

		IEnumerable<SourceRepository> GetProjectTemplateRepositories ()
		{
			if (projectTemplateRepositories == null) {
				projectTemplateRepositories = GetProjectTemplatePackageSources ()
					.Select (provider.CreateRepository)
					.ToList ();
			}

			return projectTemplateRepositories;
		}

		IEnumerable<PackageSource> GetProjectTemplatePackageSources ()
		{
			var packageSources = new List<PackageSource> ();

			foreach (PackageRepositoryNode node in AddinManager.GetExtensionNodes ("/MonoDevelop/Ide/ProjectTemplatePackageRepositories")) {
				packageSources.Add (node.CreatePackageSource ());
			}

			return packageSources;
		}

		SourceRepository CreateRepository (FilePath directory)
		{
			var source = new PackageSource (directory);
			return provider.CreateRepository (source);
		}
	}
}

